# HEngine

[![CI](https://github.com/YourUsername/HEngine/actions/workflows/ci.yml/badge.svg)](https://github.com/YourUsername/HEngine/actions/workflows/ci.yml)

High-performance game engine built with C# and .NET 10, featuring an Entity Component System (ECS) architecture and DirectX 12 rendering.

## Features

### Phase 1 (Completed)
- **Entity-Component-System architecture**
  - Generation-based entity IDs preventing use-after-free
  - Sparse-set component storage for O(1) operations
  - Cached query system with automatic invalidation
  - Priority-based system execution
- **DirectX 12 rendering pipeline**
  - Triple-buffered command submission
  - Persistent buffer mapping (zero-copy uploads)
  - Pipeline state caching
  - Depth testing and back-face culling
- **3D mesh rendering with lighting**
  - Directional and point light support
  - Basic diffuse lighting in shaders
  - Material system foundation
  - Mesh primitives (cube, sphere, plane)
- **Camera system**
  - Perspective and orthographic projection
  - Free camera controller (WASD + mouse look)
  - Integrated with rendering pipeline
- **Scene hierarchy and transform system**
  - Parent-child relationships
  - Cached world matrix calculation
  - Dirty flag optimization
  - Cycle prevention
- **Async asset loading system**
  - Thread-safe caching
  - Reference counting for automatic unloading
  - Simple binary mesh format
  - Component-based asset tracking
- **Frustum culling optimization**
  - View-frustum plane extraction
  - Bounding box intersection tests
  - Automatic culling of off-screen entities
- **Comprehensive test coverage**
  - 324 core unit tests
  - 40 rendering unit tests
  - Headless rendering tests (no GPU required)
  - Performance benchmarks with BenchmarkDotNet

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
- **Sparse-set component storage**: O(1) component operations with cache-friendly iteration
- **Query caching**: Automatic invalidation reduces redundant work

## Roadmap

### Phase 2 (Upcoming): Advanced Rendering & Physics
- PBR (Physically Based Rendering) pipeline
- Shadow mapping (directional and point lights)
- Post-processing effects
- Skeletal animation system
- Physics integration (collision detection, rigidbody dynamics)
- Particle system

### Phase 3 (Future): Networking & Multiplayer
- Client-server architecture
- Lockstep determinism
- Client-side prediction
- Server reconciliation
- Network component replication

### Phase 4 (Future): Tooling & Scripting
- Entity inspector and editor
- C# hot-reload via Roslyn
- Visual scripting system
- Profiling and debugging tools

For detailed task breakdown, see [.agents/roadmap-phase-1-plan.md](.agents/roadmap-phase-1-plan.md)

## Documentation

- **[Architecture Guide](docs/ARCHITECTURE.md)**: Detailed architecture overview, design patterns, and extension points
- **[Contributing Guide](docs/CONTRIBUTING.md)**: Code style, testing requirements, and contribution process
- **[CLAUDE.md](.agents/CLAUDE.md)**: Quick reference for Claude Code development

## Contributing

We welcome contributions! Please read our [Contributing Guide](docs/CONTRIBUTING.md) for details on:
- Code style and conventions
- Testing requirements
- Pull request process
- Development setup

## License

MIT License - See LICENSE file for details
