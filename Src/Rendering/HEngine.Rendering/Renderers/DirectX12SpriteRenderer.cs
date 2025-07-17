using System.Numerics;
using HEngine.Rendering.Contracts;
using HEngine.Rendering.Data;
using HEngine.Rendering.Devices;
using HEngine.Rendering.Managers;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.Renderers;

public class DirectX12SpriteRenderer : ISpriteRenderer
{
    private readonly List<SpriteVertex> _currentBatch = new();
    private DirectX12BufferManager _bufferManager;
    private IRenderDevice _device;
    private bool _disposed;
    private DirectX12PipelineStateManager _pipelineManager;
    private DirectX12ShaderManager _shaderManager;

    public bool IsInitialized { get; private set; }

    public void Initialize(IRenderDevice device)
    {
        try
        {
            _device = device;
            var d3dDevice = ((DirectX12Device)device).GetDevice();

            _shaderManager = new DirectX12ShaderManager();
            _shaderManager.Initialize();

            _pipelineManager = new DirectX12PipelineStateManager();
            _pipelineManager.Initialize(d3dDevice, _shaderManager);

            _bufferManager = new DirectX12BufferManager();
            _bufferManager.Initialize(d3dDevice, new Vector2(800, 600));

            IsInitialized = true;
            Console.WriteLine("DirectX12SpriteRenderer initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize DirectX12SpriteRenderer: {ex.Message}");
            throw;
        }
    }

    public void DrawSprite(Vector2 position, Vector2 size, Vector4 color)
    {
        if (!IsInitialized || _disposed)
            return;

        Console.WriteLine($"DirectX12SpriteRenderer: Adding sprite at ({position.X}, {position.Y})");

        var vertices = CreateQuadVertices(position, size, color);
        _currentBatch.AddRange(vertices);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _bufferManager?.Dispose();
        _pipelineManager?.Dispose();
        _shaderManager?.Dispose();
        _disposed = true;
    }

    public void FlushBatch()
    {
        if (!IsInitialized || _disposed || _currentBatch.Count == 0)
            return;

        try
        {
            Console.WriteLine($"DirectX12SpriteRenderer: Flushing {_currentBatch.Count} vertices");

            var commandList = ((DirectX12Device)_device).GetCommandQueue().CommandList;

            // Sprawdź czy command list jest prawidłowy
            unsafe
            {
                if (commandList.Handle == (void*)IntPtr.Zero)
                {
                    Console.WriteLine("ERROR: Command list is null!");
                    return;
                }
            }

            _bufferManager.UpdateVertexBuffer(_currentBatch.ToArray());

            // Sprawdź czy pipeline state jest prawidłowy
            unsafe
            {
                if (_pipelineManager.PipelineState.Handle == (void*)IntPtr.Zero)
                {
                    Console.WriteLine("ERROR: Pipeline state is null!");
                    return;
                }
            }

            commandList.SetGraphicsRootSignature(_pipelineManager.RootSignature);
            commandList.SetPipelineState(_pipelineManager.PipelineState);
            commandList.SetGraphicsRootConstantBufferView(0, _bufferManager.ConstantBuffer.GetGPUVirtualAddress());

            commandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);

            var vertexBufferView = _bufferManager.VertexBufferView;
            commandList.IASetVertexBuffers(0, 1, in vertexBufferView);

            commandList.DrawInstanced((uint)_currentBatch.Count, 1, 0, 0);

            Console.WriteLine($"DirectX12SpriteRenderer: Successfully flushed {_currentBatch.Count} vertices");
            _currentBatch.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in FlushBatch: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _currentBatch.Clear();
        }
    }


    private SpriteVertex[] CreateQuadVertices(Vector2 position, Vector2 size, Vector4 color)
    {
        var left = position.X;
        var right = position.X + size.X;
        var top = position.Y;
        var bottom = position.Y + size.Y;

        return
        [
            new SpriteVertex(new Vector3(left, top, 0), color),
            new SpriteVertex(new Vector3(right, top, 0), color),
            new SpriteVertex(new Vector3(left, bottom, 0), color),

            new SpriteVertex(new Vector3(right, top, 0), color),
            new SpriteVertex(new Vector3(right, bottom, 0), color),
            new SpriteVertex(new Vector3(left, bottom, 0), color)
        ];
    }
}