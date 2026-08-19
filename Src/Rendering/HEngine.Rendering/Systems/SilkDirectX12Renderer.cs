using System.Buffers;
using System.Numerics;
using HEngine.Core.Configuration;
using HEngine.Core.Rendering.Contracts;
using HEngine.Core.Rendering.Data;
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
    private readonly ShadowMapManager _shadowMapManager;
    private readonly ShadowSettings _shadowSettings;
    private readonly TextureManager _textureManager;
    private readonly DescriptorHeapManager _descriptorHeapManager;

    private DirectX12CommandList _commandList;
    private bool _disposed;
    private bool _frameInProgress;
    private bool _initialized;

    private DirectX12MeshRenderer _meshRenderer = null!;
    private MeshDrawContext _meshDrawContext = null!;
    private LightData[] _lights = Array.Empty<LightData>();

    private const int MeshDrawStackAllocThreshold = 256;

    public SilkDirectX12Renderer(IGraphicsDevice device, IRenderBatch<SpriteData> spriteBatch,
        ISpriteRenderer spriteRenderer, IShaderManager shaderManager, ShadowMapManager shadowMapManager,
        ShadowSettings shadowSettings, TextureManager textureManager, DescriptorHeapManager descriptorHeapManager,
        ILogger<SilkDirectX12Renderer> logger, ILoggerFactory loggerFactory)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _spriteRenderer = spriteRenderer ?? throw new ArgumentNullException(nameof(spriteRenderer));
        _shaderManager = shaderManager ?? throw new ArgumentNullException(nameof(shaderManager));
        _shadowMapManager = shadowMapManager ?? throw new ArgumentNullException(nameof(shadowMapManager));
        _shadowSettings = shadowSettings ?? throw new ArgumentNullException(nameof(shadowSettings));
        _textureManager = textureManager ?? throw new ArgumentNullException(nameof(textureManager));
        _descriptorHeapManager = descriptorHeapManager ?? throw new ArgumentNullException(nameof(descriptorHeapManager));
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

                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(errorMessage);
                }

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
            _descriptorHeapManager.Initialize(d3dDevice);
            _textureManager.SetDevice(d3dDevice);
            _meshRenderer = new DirectX12MeshRenderer();
            _meshRenderer.Initialize(d3dDevice, _shadowMapManager, _shadowSettings.Enabled, _textureManager);
            _meshRenderer.SetCommandQueue(dx12Device.GetDirectX12CommandQueue());
            _meshDrawContext = new MeshDrawContext(this);

            _initialized = true;
            _logger.LogInformation("SilkDirectX12Renderer initialized successfully");
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to initialize SilkDirectX12Renderer");
            }

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
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error in Present");
            }

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

            _meshRenderer.BeginFrame();

            _frameInProgress = true;
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to begin frame");
            }

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
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to end frame");
            }

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

    public void SetLights(ReadOnlySpan<LightData> lights)
    {
        if (_disposed || !IsInitialized)
        {
            return;
        }

        if (lights.Length == 0)
        {
            _lights = Array.Empty<LightData>();
            return;
        }

        if (_lights.Length != lights.Length)
        {
            _lights = new LightData[lights.Length];
        }

        lights.CopyTo(_lights);
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

    public void DrawMesh(Matrix4x4 transform, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices, MaterialData? material = null)
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

        const int floatsPerVertex = 12;
        if (vertices.Length % floatsPerVertex != 0)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "DrawMesh received {FloatCount} floats which is not a multiple of the {Stride}-float vertex stride; skipping draw",
                    vertices.Length, floatsPerVertex);
            }

            return;
        }

        var vertexCount = vertices.Length / floatsPerVertex;
        if (vertexCount == 0)
        {
            return;
        }

        foreach (var index in indices)
        {
            if (index >= (uint)vertexCount)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "DrawMesh received index {Index} out of range for {VertexCount} vertices; skipping draw",
                        index, vertexCount);
                }

                return;
            }
        }

        Vertex3D[]? rented = null;

        try
        {
            Span<Vertex3D> meshVertices = vertexCount <= MeshDrawStackAllocThreshold
                ? stackalloc Vertex3D[vertexCount]
                : (rented = ArrayPool<Vertex3D>.Shared.Rent(vertexCount)).AsSpan(0, vertexCount);

            for (var i = 0; i < vertexCount; i++)
            {
                var baseFloat = i * floatsPerVertex;

                var position = new Vector3(
                    vertices[baseFloat + 0],
                    vertices[baseFloat + 1],
                    vertices[baseFloat + 2]);

                var normal = new Vector3(
                    vertices[baseFloat + 3],
                    vertices[baseFloat + 4],
                    vertices[baseFloat + 5]);

                var texCoord = new Vector2(
                    vertices[baseFloat + 6],
                    vertices[baseFloat + 7]);

                var color = new Vector4(
                    vertices[baseFloat + 8],
                    vertices[baseFloat + 9],
                    vertices[baseFloat + 10],
                    vertices[baseFloat + 11]);

                meshVertices[i] = new Vertex3D(position, normal, texCoord, color);
            }

            _meshDrawContext.ViewMatrix = _commandList.CurrentViewMatrix;
            _meshDrawContext.ProjectionMatrix = _commandList.CurrentProjectionMatrix;

            Material? meshMaterial = null;
            var diffuseTextureHandle = -1;
            if (material.HasValue)
            {
                var m = material.Value;
                meshMaterial = new Material
                {
                    DiffuseColor = m.DiffuseColor,
                    Metallic = m.Metallic,
                    Roughness = m.Roughness
                };
                meshMaterial.SetProperty("_AO", m.AO);
                meshMaterial.SetProperty("_EmissiveColor", m.EmissiveColor);
                meshMaterial.SetProperty("_EmissiveIntensity", m.EmissiveIntensity);
                diffuseTextureHandle = m.DiffuseTextureHandle;
            }

            _meshRenderer.DrawMesh(transform, meshVertices, indices, _meshDrawContext, meshMaterial, _lights, diffuseTextureHandle);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to draw mesh");
            }

            throw;
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<Vertex3D>.Shared.Return(rented);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Disposing SilkDirectX12Renderer");
        }

        _initialized = false;

        _meshRenderer?.Dispose();
        _spriteBatch.Dispose();
        _spriteRenderer.Dispose();
        _commandList?.Dispose();
        _shaderManager.Dispose();
        _device.Dispose();
        _disposed = true;
    }

    private sealed class MeshDrawContext : IRenderContext
    {
        public MeshDrawContext(IRenderer renderer)
        {
            Renderer = renderer;
        }

        public IRenderer Renderer { get; }
        public Matrix4x4 ViewMatrix { get; set; }
        public Matrix4x4 ProjectionMatrix { get; set; }
        public Vector4 ClearColor { get; set; }
    }
}