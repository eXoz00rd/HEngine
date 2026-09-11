using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Data;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Diagnostics;
using HEngine.Rendering.Managers;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.Renderers;

public class DirectX12SpriteRenderer : ISpriteRenderer
{
    private readonly ILogger<DirectX12SpriteRenderer> _logger;
    private readonly ShaderFileLoader _shaderFileLoader;
    private readonly ShaderDiskCache _shaderDiskCache;
    private readonly ShaderFileWatcher _shaderFileWatcher;
    private readonly RenderingMetrics _metrics = new();

    private const int MAX_SPRITES = 10000;
    private const int VERTICES_PER_SPRITE = 6;
    private const int MAX_VERTICES = MAX_SPRITES * VERTICES_PER_SPRITE;
    private readonly SpriteVertex[] _batchBuffer = new SpriteVertex[MAX_VERTICES];
    private int _currentVertexCount;

    private DirectX12BufferManager _bufferManager = null!;
    private IGraphicsDevice _device = null!;
    private bool _disposed;
    private DirectX12PipelineStateManager _pipelineManager = null!;
    private DirectX12ShaderManager _shaderManager = null!;

    private ComPtr<ID3D12PipelineState> _lastBoundPipeline;
    private ComPtr<ID3D12RootSignature> _lastBoundRootSig;
    private ulong _lastBoundConstantBufferAddress;
    private bool _stateValid;
    private bool _useHdrRenderTarget;

    public DirectX12SpriteRenderer(
        ILogger<DirectX12SpriteRenderer> logger,
        ShaderFileLoader shaderFileLoader,
        ShaderDiskCache shaderDiskCache,
        ShaderFileWatcher shaderFileWatcher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _shaderFileLoader = shaderFileLoader ?? throw new ArgumentNullException(nameof(shaderFileLoader));
        _shaderDiskCache = shaderDiskCache ?? throw new ArgumentNullException(nameof(shaderDiskCache));
        _shaderFileWatcher = shaderFileWatcher ?? throw new ArgumentNullException(nameof(shaderFileWatcher));
        _currentVertexCount = 0;
    }

    public bool IsInitialized { get; private set; }
    public RenderingMetrics Metrics => _metrics;

    public void Initialize(IGraphicsDevice device)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DirectX12SpriteRenderer));
        if (IsInitialized)
            throw new InvalidOperationException("DirectX12SpriteRenderer is already initialized.");

        try
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));

            var dx12Device = (DirectX12Device)device;
            var d3dDevice = dx12Device.GetDevice();

            _shaderManager = new DirectX12ShaderManager(_shaderFileLoader, _shaderDiskCache, _shaderFileWatcher);
            _shaderManager.ShaderReloaded += OnShaderReloaded;
            _shaderManager.Initialize();

            _pipelineManager = new DirectX12PipelineStateManager();
            _pipelineManager.Initialize(d3dDevice, _shaderManager);

            _bufferManager = new DirectX12BufferManager();
            _bufferManager.Initialize(d3dDevice, dx12Device.GetWindowSize());
            _bufferManager.Metrics = _metrics;

            IsInitialized = true;
            _logger.LogInformation("DirectX12SpriteRenderer initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize DirectX12SpriteRenderer");
            throw;
        }
    }

    public void DrawSprite(Vector2 position, Vector2 size, Vector4 color)
    {
        if (!IsInitialized || _disposed)
            return;

        if (_currentVertexCount + VERTICES_PER_SPRITE > MAX_VERTICES)
        {
            FlushBatch();
        }

        var left = position.X;
        var right = position.X + size.X;
        var top = position.Y;
        var bottom = position.Y + size.Y;

        _batchBuffer[_currentVertexCount++] = new SpriteVertex(new Vector3(left, top, 0), color);
        _batchBuffer[_currentVertexCount++] = new SpriteVertex(new Vector3(right, top, 0), color);
        _batchBuffer[_currentVertexCount++] = new SpriteVertex(new Vector3(left, bottom, 0), color);
        _batchBuffer[_currentVertexCount++] = new SpriteVertex(new Vector3(right, top, 0), color);
        _batchBuffer[_currentVertexCount++] = new SpriteVertex(new Vector3(right, bottom, 0), color);
        _batchBuffer[_currentVertexCount++] = new SpriteVertex(new Vector3(left, bottom, 0), color);

        _metrics.IncrementSprites(1);
        _metrics.IncrementVertices(VERTICES_PER_SPRITE);
    }

    public void UpdateCameraMatrices(Matrix4x4 view, Matrix4x4 projection)
    {
        if (!IsInitialized || _disposed)
            return;
        _bufferManager.UpdateCameraConstants(view, projection);
    }

    /// <summary>
    /// Selects the PSO variant used by subsequent <see cref="FlushBatch"/> calls: the swap-chain-format
    /// PSO by default, or <see cref="DirectX12PipelineStateManager.HdrPipelineState"/> when the sprite
    /// pass is redirected to <see cref="RenderTargetManager"/>'s HDR target for post-processing (tracks #45).
    /// </summary>
    public void SetRenderTargetFormat(bool useHdrRenderTarget)
    {
        _useHdrRenderTarget = useHdrRenderTarget;
    }

    public void InvalidateStateCache()
    {
        unsafe
        {
            _lastBoundPipeline = new ComPtr<ID3D12PipelineState>((ID3D12PipelineState*)null);
            _lastBoundRootSig = new ComPtr<ID3D12RootSignature>((ID3D12RootSignature*)null);
        }
        _lastBoundConstantBufferAddress = 0;
        _stateValid = false;
    }

    public void FlushBatch()
    {
        if (!IsInitialized || _disposed || _currentVertexCount == 0)
            return;

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Flushing {Count} vertices", _currentVertexCount);

            var dx12Device = (DirectX12Device)_device;
            var commandList = dx12Device.GetDirectX12CommandQueue().CommandList;

            unsafe
            {
                if (commandList.Handle == (void*)IntPtr.Zero)
                {
                    _logger.LogError("Command list is null!");
                    return;
                }
            }

            var frameIndex = dx12Device.GetCurrentFrameIndex();
            _bufferManager.SetFrameIndex(frameIndex);

            var vertexSpan = new ReadOnlySpan<SpriteVertex>(_batchBuffer, 0, _currentVertexCount);
            _bufferManager.UpdateVertexBuffer(vertexSpan);

#if DEBUG
            unsafe
            {
                if (_pipelineManager.PipelineState.Handle == (void*)IntPtr.Zero)
                {
                    _logger.LogError("Pipeline state is null!");
                    return;
                }
            }
#endif

            unsafe
            {
                var currentRootSig = _pipelineManager.RootSignature;
                if (!_stateValid || _lastBoundRootSig.Handle != currentRootSig.Handle)
                {
                    commandList.SetGraphicsRootSignature(currentRootSig);
                    _lastBoundRootSig = currentRootSig;
                }

                var currentPipeline = _useHdrRenderTarget ? _pipelineManager.HdrPipelineState : _pipelineManager.PipelineState;
                if (!_stateValid || _lastBoundPipeline.Handle != currentPipeline.Handle)
                {
                    commandList.SetPipelineState(currentPipeline);
                    _lastBoundPipeline = currentPipeline;
                }

                var currentConstantBufferAddress = _bufferManager.ConstantBuffer.GetGPUVirtualAddress();
                if (!_stateValid || _lastBoundConstantBufferAddress != currentConstantBufferAddress)
                {
                    commandList.SetGraphicsRootConstantBufferView(0, currentConstantBufferAddress);
                    _lastBoundConstantBufferAddress = currentConstantBufferAddress;
                }

                _stateValid = true;
            }

            commandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);

            var vertexBufferView = _bufferManager.GetCurrentVertexBufferView();
            commandList.IASetVertexBuffers(0, 1, in vertexBufferView);
            commandList.DrawInstanced((uint)_currentVertexCount, 1, 0, 0);

            _metrics.IncrementBatchFlushes();
            _metrics.IncrementDrawCalls();

            _currentVertexCount = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FlushBatch");
            _currentVertexCount = 0;
            throw;
        }
    }

    private void OnShaderReloaded()
    {
        try
        {
            _logger.LogInformation("Rebuilding sprite rendering pipeline due to shader reload");
            _pipelineManager.Rebuild(_shaderManager);
            InvalidateStateCache();
            _logger.LogInformation("Sprite rendering pipeline rebuilt successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild sprite rendering pipeline after shader reload");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing DirectX12SpriteRenderer");

        if (_shaderManager != null)
        {
            _shaderManager.ShaderReloaded -= OnShaderReloaded;
        }

        _bufferManager?.Dispose();
        _pipelineManager?.Dispose();
        _shaderManager?.Dispose();
        _disposed = true;
    }
}