using System.Numerics;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Batches;
using HEngine.Rendering.Data;
using HEngine.Rendering.Devices;
using HEngine.Rendering.DirectX12;
using HEngine.Rendering.Managers;
using HEngine.Rendering.Renderers;
using Microsoft.Extensions.Logging;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.Systems;

public class SilkDirectX12Renderer : IRenderer
{
    private readonly IGraphicsDevice _device;
    private readonly ILogger<SilkDirectX12Renderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IShaderManager _shaderManager;
    private readonly IRenderBatch<SpriteData> _spriteBatch;
    private readonly ISpriteRenderer _spriteRenderer;

    private DirectX12CommandList _commandList;
    private bool _disposed;
    private bool _frameInProgress;
    private bool _initialized;

    private DirectX12BufferManager _meshBufferManager = null!;
    private DirectX12PipelineStateManager _meshPipelineManager = null!;

    public SilkDirectX12Renderer(IGraphicsDevice device, IRenderBatch<SpriteData> spriteBatch,
        ISpriteRenderer spriteRenderer, IShaderManager shaderManager, ILogger<SilkDirectX12Renderer> logger,
        ILoggerFactory loggerFactory)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _spriteRenderer = spriteRenderer ?? throw new ArgumentNullException(nameof(spriteRenderer));
        _shaderManager = shaderManager ?? throw new ArgumentNullException(nameof(shaderManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _commandList = null!;
    }

    public Action<float>? OnFrameUpdate { get; set; }
    public bool IsInitialized => _initialized && _device.IsInitialized;
    public bool ShouldClose => _device.ShouldClose;

    public void Initialize(int width, int height, string title)
    {
        try
        {
            _logger.LogInformation("Initializing SilkDirectX12Renderer...");

            _device.Initialize(width, height, title);

            if (!_device.IsInitialized)
            {
                var errorMessage = "GraphicsDevice failed to initialize";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _shaderManager.Initialize();

            var commandQueue = _device.GetCommandQueue();

            var commandListLogger = _loggerFactory.CreateLogger<DirectX12CommandList>();
            _commandList = new DirectX12CommandList(commandQueue, commandListLogger);

            _spriteRenderer.Initialize(_device);
            _spriteBatch.Initialize(_spriteRenderer);

            var dx12Device = (DirectX12Device)_device;
            var d3dDevice = dx12Device.GetDevice();
            _meshBufferManager = new DirectX12BufferManager();
            _meshBufferManager.Initialize(d3dDevice, dx12Device.GetWindowSize());
            var dx12ShaderManager = (DirectX12ShaderManager)_shaderManager;
            _meshPipelineManager = new DirectX12PipelineStateManager();
            _meshPipelineManager.Initialize(d3dDevice, dx12ShaderManager);

            _initialized = true;
            _logger.LogInformation("SilkDirectX12Renderer initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SilkDirectX12Renderer");
            _initialized = false;
            throw;
        }
    }

    public void Run()
    {
    }

    public void Present()
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }

        try
        {
            _device.Present();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Present");
            throw;
        }
    }

    public void PollEvents()
    {
    }

    public void BeginFrame()
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }

        try
        {
            if (_frameInProgress)
            {
                _logger.LogWarning("Previous frame still in progress - skipping BeginFrame");
                return;
            }

            _device.BeginFrame();

            if (_device.ShouldClose)
            {
                return;
            }

            var commandQueue = _device.GetCommandQueue();
            if (!commandQueue.IsFrameInProgress)
            {
                _logger.LogWarning("GraphicsDevice failed to start frame - skipping");
                return;
            }

            _commandList.Reset();
            _spriteBatch.Clear();

            if (_spriteRenderer is DirectX12SpriteRenderer dx12SpriteRenderer)
            {
                dx12SpriteRenderer.InvalidateStateCache();
            }

            _frameInProgress = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin frame");
            _frameInProgress = false;
            throw;
        }
    }

    public void EndFrame()
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }

        try
        {
            if (_device.ShouldClose)
            {
                return;
            }

            if (!_frameInProgress)
            {
                _logger.LogWarning("No frame in progress - skipping EndFrame");
                return;
            }

            _commandList.Close();
            _device.EndFrame();
            _frameInProgress = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end frame");
            _frameInProgress = false;
            throw;
        }
    }

    public void Clear(Vector4 clearColor)
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }

        _device.Clear(clearColor);
    }

    public void SetViewMatrix(Matrix4x4 viewMatrix)
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }

        _commandList.SetViewMatrix(viewMatrix);
    }

    public void SetProjectionMatrix(Matrix4x4 projectionMatrix)
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }
        
        _commandList.SetProjectionMatrix(projectionMatrix);
    }

    public void DrawSprite(Vector2 position, Vector2 size, Vector4 color)
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }
        
        _spriteBatch.Add(new SpriteData
        {
            Position = position,
            Size = size,
            Color = color
        });
    }

    public void FlushBatch()
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }

        if (!_frameInProgress)
        {
            return;
        }

        _spriteRenderer.UpdateCameraMatrices(_commandList.CurrentViewMatrix, _commandList.CurrentProjectionMatrix);
        
        _spriteBatch.Render(_commandList);
    }

    public void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices)
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }

        if (!_frameInProgress)
        {
            return;
        }

        if (vertices.Length == 0 || indices.Length == 0)
        {
            return;
        }

        try
        {
            var dx12Device = (DirectX12Device)_device;
            var commandList = dx12Device.GetDirectX12CommandQueue().CommandList;

            _meshBufferManager.UpdateCameraConstants(_commandList.CurrentViewMatrix,
                _commandList.CurrentProjectionMatrix);

            var triangleVertexCount = indices.Length;
            var temp = new SpriteVertex[triangleVertexCount];

            var o = 0;
            for (var i = 0; i < indices.Length; i++)
            {
                var idx = (int)indices[i];
                var baseFloat = idx * 12;
                if (baseFloat + 11 >= vertices.Length)
                {
                    break;
                }

                var pos = new Vector3(
                    vertices[baseFloat + 0],
                    vertices[baseFloat + 1],
                    vertices[baseFloat + 2]);

                var col = new Vector4(
                    vertices[baseFloat + 8],
                    vertices[baseFloat + 9],
                    vertices[baseFloat + 10],
                    vertices[baseFloat + 11]);

                var worldPos = Vector3.Transform(pos, transform);
                temp[o++] = new SpriteVertex(worldPos, col);
            }

            if (o == 0)
            {
                return;
            }

            if (o != temp.Length)
            {
                Array.Resize(ref temp, o);
            }

            var frameIndex = dx12Device.GetCurrentFrameIndex();
            _meshBufferManager.SetFrameIndex(frameIndex);

            var vertexSpan = new ReadOnlySpan<SpriteVertex>(temp);
            _meshBufferManager.UpdateVertexBuffer(vertexSpan);

            commandList.SetGraphicsRootSignature(_meshPipelineManager.RootSignature);
            commandList.SetPipelineState(_meshPipelineManager.PipelineState);
            commandList.SetGraphicsRootConstantBufferView(0, _meshBufferManager.ConstantBuffer.GetGPUVirtualAddress());
            var vbv = _meshBufferManager.GetCurrentVertexBufferView();
            commandList.IASetVertexBuffers(0, 1, in vbv);
            commandList.DrawInstanced((uint)temp.Length, 1, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to draw mesh");
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing SilkDirectX12Renderer");
        _initialized = false;

        _meshBufferManager.Dispose();
        _meshPipelineManager.Dispose();
        _spriteBatch.Dispose();
        _spriteRenderer.Dispose();
        _commandList.Dispose();
        _shaderManager.Dispose();
        _device.Dispose();
        _disposed = true;
    }
}