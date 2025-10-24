using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Data;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Managers;
using Microsoft.Extensions.Logging;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.Renderers;

public class DirectX12SpriteRenderer : ISpriteRenderer
{
    private readonly List<SpriteVertex> _currentBatch;
    private readonly ILogger<DirectX12SpriteRenderer> _logger;
    private readonly SpriteVertex[] _quadVertices = new SpriteVertex[6];

    private DirectX12BufferManager _bufferManager = null!;
    private IGraphicsDevice _device = null!;
    private bool _disposed;
    private DirectX12PipelineStateManager _pipelineManager = null!;
    private DirectX12ShaderManager _shaderManager = null!;

    public DirectX12SpriteRenderer(ILogger<DirectX12SpriteRenderer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentBatch = new List<SpriteVertex>(4096);
    }

    public bool IsInitialized { get; private set; }

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

            _shaderManager = new DirectX12ShaderManager();
            _shaderManager.Initialize();

            _pipelineManager = new DirectX12PipelineStateManager();
            _pipelineManager.Initialize(d3dDevice, _shaderManager);

            _bufferManager = new DirectX12BufferManager();
            _bufferManager.Initialize(d3dDevice, dx12Device.GetWindowSize());

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

        CreateQuadVerticesInPlace(position, size, color);
        _currentBatch.AddRange(_quadVertices);
    }

    public void UpdateCameraMatrices(Matrix4x4 view, Matrix4x4 projection)
    {
        if (!IsInitialized || _disposed)
            return;
        _bufferManager.UpdateCameraConstants(view, projection);
    }

    public void FlushBatch()
    {
        if (!IsInitialized || _disposed || _currentBatch.Count == 0)
            return;

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Flushing {Count} vertices", _currentBatch.Count);

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

            var vertexArray = _currentBatch.ToArray();
            _bufferManager.UpdateVertexBuffer(vertexArray);

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

            commandList.SetGraphicsRootSignature(_pipelineManager.RootSignature);
            commandList.SetPipelineState(_pipelineManager.PipelineState);
            commandList.SetGraphicsRootConstantBufferView(0, _bufferManager.ConstantBuffer.GetGPUVirtualAddress());
            commandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);

            var vertexBufferView = _bufferManager.VertexBufferView;
            commandList.IASetVertexBuffers(0, 1, in vertexBufferView);
            commandList.DrawInstanced((uint)_currentBatch.Count, 1, 0, 0);

            _currentBatch.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FlushBatch");
            _currentBatch.Clear();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _logger.LogInformation("Disposing DirectX12SpriteRenderer");

        _bufferManager?.Dispose();
        _pipelineManager?.Dispose();
        _shaderManager?.Dispose();
        _disposed = true;
    }

    private void CreateQuadVerticesInPlace(Vector2 position, Vector2 size, Vector4 color)
    {
        var left = position.X;
        var right = position.X + size.X;
        var top = position.Y;
        var bottom = position.Y + size.Y;

        _quadVertices[0] = new SpriteVertex(new Vector3(left, top, 0), color);
        _quadVertices[1] = new SpriteVertex(new Vector3(right, top, 0), color);
        _quadVertices[2] = new SpriteVertex(new Vector3(left, bottom, 0), color);
        _quadVertices[3] = new SpriteVertex(new Vector3(right, top, 0), color);
        _quadVertices[4] = new SpriteVertex(new Vector3(right, bottom, 0), color);
        _quadVertices[5] = new SpriteVertex(new Vector3(left, bottom, 0), color);
    }
}