# HEngine

[![CI](https://github.com/YourUsername/HEngine/actions/workflows/ci.yml/badge.svg)](https://github.com/YourUsername/HEngine/actions/workflows/ci.yml)

High-performance game engine built with C# and .NET 10, featuring an Entity Component System (ECS) architecture and DirectX 12 rendering.

## Features

### Phase 1 (Current)
- Entity-Component-System architecture
- DirectX 12 rendering pipeline
- 3D mesh rendering with lighting
- Camera system with free camera controller
- Scene hierarchy and transform system
- Async asset loading system
- Frustum culling optimization
- Comprehensive unit test coverage (364+ tests)

### Rendering
- DirectX 12 backend with triple buffering
- Sprite rendering with batching
- 3D mesh rendering with basic lighting
- Directional and point light support
- Material system foundation
- Persistent buffer mapping for zero-copy uploads
- Pipeline state caching

### Asset Management
- Async mesh loading from binary format
- Reference counting for automatic unloading
- Thread-safe caching
- Component-based asset tracking

## Prerequisites

- .NET 10 SDK (RC or later)
- Windows 10+ (DirectX 12 required for rendering)
- DirectX 12 compatible GPU

## Building

```bash
dotnet restore HEngine.sln
dotnet build HEngine.sln -c Release
```

## Running Tests

```bash
dotnet test HEngine.sln

dotnet test Tests/HEngine.Core.Tests

dotnet test Tests/HEngine.Rendering.Tests

dotnet test --filter "FullyQualifiedName~Transform"
```

## Project Structure

```
HEngine/
├── Src/
│   ├── Core/HEngine.Core/           # Platform-agnostic ECS and core systems
│   └── Rendering/HEngine.Rendering/ # DirectX 12 rendering implementation
├── Tests/
│   ├── HEngine.Core.Tests/          # Core unit tests (324 tests)
│   └── HEngine.Rendering.Tests/     # Rendering unit tests (40 tests)
├── Benchmarks/
│   └── HEngine.Core.Benchmarks/     # Performance benchmarks
└── HEngine/                         # Engine composition and builder

```

## Architecture

### Entity Component System
- **EntityManager**: Handles entity lifecycle with generation-based IDs
- **ComponentManager**: Manages component storage with sparse-set pattern
- **SystemManager**: Executes systems with priority ordering
- **WorldManager**: Unified API wrapping all ECS managers

### Rendering Pipeline
- **DirectX12Core**: Device initialization and adapter management
- **DirectX12CommandQueue**: Triple-buffered command submission with fencing
- **DirectX12BufferManager**: Persistent buffer mapping and ring buffers
- **DirectX12SpriteRenderer**: Batched 2D sprite rendering
- **DirectX12MeshRenderer**: 3D mesh rendering with lighting

### Key Design Patterns
- Sparse-set component storage for cache-friendly iteration
- Generation-based entity IDs to prevent use-after-free
- Query caching with automatic invalidation
- Zero-allocation rendering with pre-allocated buffers
- Async asset loading with reference counting

## Performance Characteristics

- **Zero-allocation rendering**: No GC pressure during gameplay
- **Triple buffering**: CPU can work 2-3 frames ahead of GPU
- **Persistent buffer mapping**: Eliminated Map/Unmap overhead
- **Smart constant buffer updates**: Only updates when camera moves
- **Batch rendering**: Minimizes draw calls

## License

[Your License Here]

## Contributing

[Your contribution guidelines here]
