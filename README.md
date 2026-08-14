# HEngine — .NET AAA Game Engine

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![DirectX 12](https://img.shields.io/badge/DirectX-12-green)](https://docs.microsoft.com/en-us/windows/win32/direct3d12/)
[![Silk.NET](https://img.shields.io/badge/Silk.NET-2.22-purple)](https://github.com/dotnet/Silk.NET)

**HEngine** is a high-performance AAA game engine built entirely in C# on .NET 10, leveraging **Silk.NET** for low-level DirectX 12 bindings. It follows a data-oriented **Entity Component System (ECS)** architecture designed for maximum throughput and minimal GC pressure, targeting Windows desktop with DirectX 12 rendering.

---

## 🎯 Project Goal

Build a fully-featured, production-grade **.NET AAA game engine** comparable to engines like Unity or Godot, but built natively in C# with:

- **Silk.NET** as the hardware abstraction layer (DirectX 12, input, windowing)
- **Data-oriented ECS** architecture for cache-friendly, scalable game logic
- **Zero-allocation rendering** pipeline to eliminate GC pauses during gameplay
- **Native AOT** compilation support for maximum runtime performance
- **Modular, layered design** — platform-agnostic core, swappable rendering backends

---

## 🏗️ Architecture Overview

HEngine is structured in **three distinct layers** with strict dependency rules:

```
┌──────────────────────────────────────────────────────┐
│                  Application Layer                    │
│            (HEngine — Composition Root)               │
│   EngineBuilder · GameEngine · Program.cs             │
├──────────────────────────────────────────────────────┤
│               Rendering Layer                         │
│         (HEngine.Rendering — Platform-Specific)       │
│  DirectX12 · Silk.NET · Shaders · Materials · Input   │
├──────────────────────────────────────────────────────┤
│                  Core Layer                            │
│        (HEngine.Core — Platform-Agnostic)             │
│  ECS · Transforms · Scene · Queries · Assets · Math   │
└──────────────────────────────────────────────────────┘
```

### Core Layer (`Src/Core/HEngine.Core/`)
Platform-agnostic engine foundation. **Zero dependencies on any rendering API.** Pure C# with `System.Numerics`. Can run on any .NET platform, fully testable without a GPU.

- Entity Component System (EntityManager, ComponentManager, SystemManager, WorldManager)
- Sparse-set component storage for O(1) operations
- Cached query system with automatic invalidation
- Transform hierarchy with dirty flag propagation
- Scene graph management
- Frustum culling mathematics (AABB, Frustum planes)
- Async asset management with reference counting
- Game loop and timing
- Engine configuration
- Rendering contracts/interfaces (IRenderer, IRenderPipeline, IGraphicsDevice, etc.)
- Network serialization primitives

### Rendering Layer (`Src/Rendering/HEngine.Rendering/`)
DirectX 12 implementation via **Silk.NET 2.22**. Windows-only. Abstracts behind interfaces so Core never touches platform types.

- DirectX 12 device, swap chain, and command queue management
- Triple-buffered command submission with GPU fence synchronization
- Persistent buffer mapping (zero-copy uploads)
- Pipeline state caching to eliminate redundant state changes
- HLSL shader compilation with variant system and disk caching
- Sprite batching (2D) and 3D mesh rendering with lighting
- Material system (templates, instances, property blocks)
- Lighting system (directional, point, spot lights)
- Input handling (keyboard, mouse) via Silk.NET
- Rendering diagnostics and metrics

### Application Layer (`HEngine/`)
Composition root. Wires everything together using Microsoft.Extensions.DependencyInjection.

- `EngineBuilder` — fluent API for engine configuration
- `GameEngine` — main entry point, initialization, and lifecycle
- Native AOT-ready (`PublishAot=true`)

---

## ✅ Implemented Features (Phase 1 — Complete)

### Entity Component System
| Feature | Status | Details |
|---|---|---|
| Generation-based entity IDs | ✅ | `Entity(uint Id, uint Generation)` — prevents use-after-free |
| Entity creation/destruction/recycling | ✅ | ID recycling via free-list with generation bump |
| Entity capacity reservation | ✅ | `ReserveCapacity()` for pre-allocation |
| Sparse-set component storage | ✅ | O(1) add/remove/lookup, cache-friendly dense iteration |
| Thread-safe component operations | ✅ | `ReaderWriterLockSlim` on storage, `Lock` on managers |
| Generic component queries (1–3 types) | ✅ | `Query<T1>`, `Query<T1,T2>`, `Query<T1,T2,T3>` |
| Query caching with auto-invalidation | ✅ | Dirty flag cleared on component add/remove |
| Custom `QueryEnumerator` (foreach) | ✅ | Zero-alloc iteration over query results |
| Priority-based system execution | ✅ | `SystemManager` with sorted, enable/disable, active cache |
| WorldManager unified API | ✅ | Single entry point: create, destroy, add/set/get components, systems |
| Bulk entity destruction | ✅ | `DestroyEntities(ReadOnlySpan<Entity>)` |

### Components Implemented
| Category | Components |
|---|---|
| **Core** | `Active`, `Children` (inline 4 + overflow list), `DirtyFlag`, `Name`, `Parent`, `Timer` |
| **Transform** | `Transform` (3D pos/rot/scale + world matrix caching), `Transform2D`, `WorldTransform` |
| **Rendering** | `Camera` (perspective/ortho, clear flags, culling mask, depth), `Renderable` (layer, LOD, shadow, render mode), `BoundingBox`, `Color`, `Culled`, `DirectionalLight`, `PointLight`, `SpotLight` |
| **Physics** | `Rigidbody` (mass, drag, kinematic, gravity), `Velocity` (linear + angular), `Acceleration`, `BoxCollider`, `SphereCollider`, `CollisionInfo` |
| **Animation** | *(placeholder — folder created)* |
| **Audio** | *(placeholder — folder created)* |
| **Networking** | *(placeholder — folder created)* |

### Systems Implemented
| System | Layer | Description |
|---|---|---|
| `TransformHierarchySystem` | Core | Parent-child resolution, dirty flag propagation, world matrix recalculation |
| `FreeCameraSystem` | Core | WASD + mouse look camera controller, configurable speed |
| `FrustumCullingSystem` | Core | View-frustum plane extraction, AABB intersection, `Culled` component tagging |
| `LightingSystem` | Rendering | Gathers directional/point lights (max 4), respects culling |
| `MeshRenderingSystem` | Rendering | Queries `Transform + Mesh`, handles primitives (cube/plane), draws with world matrices |
| `MeshAssetLoadingSystem` | Rendering | Async loading of mesh assets with state tracking (NotLoaded → Loading → Loaded/Failed) |
| `SpriteRenderingSystem` | Rendering | 2D sprite batching and rendering |
| `RenderingSystem` | Rendering | Orchestrator — delegates to sprite + mesh sub-systems |

### DirectX 12 Rendering Pipeline
| Feature | Status | Details |
|---|---|---|
| Silk.NET DirectX 12 device init | ✅ | `DirectX12Core`, `DirectX12Device` |
| Triple-buffered swap chain | ✅ | `DirectX12SwapChain` with 3 back buffers |
| Command queue with fencing | ✅ | `DirectX12CommandQueue` — per-frame allocators, GPU fence sync |
| Command list management | ✅ | `DirectX12CommandList` — reset/close lifecycle |
| Persistent vertex buffer mapping | ✅ | Ring buffer per frame, zero Map/Unmap overhead |
| Upload ring buffer (16 MB) | ✅ | `UploadRingBuffer` for dynamic data |
| Constant buffer with dirty tracking | ✅ | Only updates GPU when camera/view changes |
| Pipeline state caching | ✅ | `DirectX12PipelineStateManager` — skip redundant state binds |
| Sprite renderer (2D batching) | ✅ | `DirectX12SpriteRenderer` with `SpriteBatchManager` |
| 3D mesh renderer | ✅ | `DirectX12MeshRenderer` with lighting constant buffer |
| HLSL shaders (Mesh + Sprite) | ✅ | Vertex + pixel shaders with normal/specular map variants |
| Shader variant compilation | ✅ | `ShaderVariantCompiler` with D3DCompiler, `ShaderFeatureFlags` (12 flags) |
| Shader disk caching | ✅ | `ShaderDiskCache` — binary caching with hash-based invalidation |
| Shader hot-reload (file watcher) | ✅ | `ShaderFileWatcher` for live shader editing |
| Depth testing and back-face culling | ✅ | Configured in pipeline state |
| Render pipeline orchestration | ✅ | `RenderPipeline` — BeginFrame → Camera → Systems → EndFrame |
| Rendering metrics/diagnostics | ✅ | Draw calls, batch flushes, frame times, vertex counts |

### Material System
| Feature | Status | Details |
|---|---|---|
| Material with property block | ✅ | Diffuse, specular, metallic, roughness, textures |
| Material instances (overrides) | ✅ | Per-object overrides without duplicating base material |
| Material templates | ✅ | Serializable templates for material presets |
| Material template serializer | ✅ | JSON-based serialization |
| Material manager (registry) | ✅ | Named material lookup with thread safety |
| Material instance manager | ✅ | Pool/tracking of material instances |
| Material presets (enum) | ✅ | Pre-defined material configurations |
| Material property buffer (GPU) | ✅ | Constant buffer upload for material properties |

### Camera System
| Feature | Status | Details |
|---|---|---|
| Perspective projection | ✅ | Configurable FOV, near/far planes, aspect ratio |
| Orthographic projection | ✅ | Configurable size |
| View matrix (LookAt) | ✅ | Position, target, up vector |
| Free camera controller | ✅ | WASD movement + mouse look with configurable speed |
| Camera input via Silk.NET | ✅ | `SilkCameraInputProvider` — keyboard + mouse delta |
| Clear flags & background color | ✅ | SolidColor, Skybox, DepthOnly, Nothing |
| Culling mask | ✅ | Per-camera layer culling |
| Camera depth ordering | ✅ | Multi-camera support foundation |

### Scene & Transform
| Feature | Status | Details |
|---|---|---|
| Parent-child hierarchy | ✅ | `SetParent()` with cycle detection (max depth 1024) |
| World matrix caching | ✅ | Cached until dirty flag set, recursive parent chain |
| Dirty flag propagation | ✅ | Automatic cascade to all children |
| Children component (inline 4) | ✅ | Stack-allocated for first 4 children, heap overflow list |
| SceneGraph API | ✅ | `CreateEntity()`, `SetParent()`, `GetChildren()`, `RemoveEntity()` (recursive) |
| Named entities | ✅ | `Name` component |

### Asset Management
| Feature | Status | Details |
|---|---|---|
| Async mesh loading | ✅ | `AssetManager.LoadMeshAsync()` |
| Thread-safe caching | ✅ | `ConcurrentDictionary` with deduplication |
| Reference counting | ✅ | Auto-unload when ref count reaches zero |
| Simple binary mesh format | ✅ | `SimpleMeshFormat` — custom binary format |
| Component-based asset tracking | ✅ | `MeshAsset` component with load state |
| Asset load state machine | ✅ | `NotLoaded → Loading → Loaded / Failed` |

### Mesh Primitives
| Primitive | Status |
|---|---|
| Cube (24 verts, proper normals) | ✅ |
| Plane | ✅ |
| Sphere | ✅ |

### Networking (Foundation)
| Feature | Status | Details |
|---|---|---|
| `NetworkWriter` / `NetworkReader` | ✅ | Binary serialization (int, float, string) |
| `INetworkSerializable` interface | ✅ | Contract for network-serializable types |

### Engine Infrastructure
| Feature | Status | Details |
|---|---|---|
| DI-based composition | ✅ | `Microsoft.Extensions.DependencyInjection` |
| Fluent builder pattern | ✅ | `EngineBuilder.AddCore().AddRendering().AddLogging().Build()` |
| Structured logging | ✅ | `Microsoft.Extensions.Logging` + console provider, log events |
| Engine configuration | ✅ | `EngineConfiguration` — window, rendering, performance settings |
| Game loop (fixed timestep) | ✅ | `GameLoop` with FPS logging, configurable target FPS |
| Input state tracking | ✅ | `InputState` — keyboard + mouse via Silk.NET |
| Native AOT support | ✅ | `PublishAot=true` in application project |

---

## 🧪 Testing

Comprehensive test coverage across 44 test files:

| Suite | Tests | Coverage |
|---|---|---|
| **HEngine.Core.Tests** | ~324 | ECS operations, transforms, hierarchy, frustum, camera, scene graph, assets, mesh primitives |
| **HEngine.Rendering.Tests** | ~40 | Render pipeline, lighting, mesh rendering, mesh asset loading, smoke tests (headless) |
| **HEngine.Core.Benchmarks** | — | BenchmarkDotNet: component storage, queries, transform hierarchy (100/1K/10K entities) |

```bash
# Run all tests
dotnet test HEngine.sln

# Run specific suite
dotnet test Tests/HEngine.Core.Tests
dotnet test Tests/HEngine.Rendering.Tests

# Run filtered tests
dotnet test --filter "FullyQualifiedName~Transform"

# Run benchmarks
dotnet run --project Benchmarks/HEngine.Core.Benchmarks -c Release
```

---

## 📦 Project Structure

```
HEngine/
├── HEngine.sln                          # Solution (6 projects)
├── global.json                          # .NET SDK pinning
│
├── HEngine/                             # 🟢 Application Layer (Composition Root)
│   ├── Program.cs                       #    Entry point (4 lines)
│   ├── GameEngine.cs                    #    Engine lifecycle, camera setup, scene init
│   └── Builders/EngineBuilder.cs        #    Fluent DI builder
│
├── Src/
│   ├── Core/HEngine.Core/              # 🔵 Core Layer (Platform-Agnostic)
│   │   ├── Primitives/                  #    Entity, Aabb, EntityManagerStats
│   │   ├── Contracts/                   #    IComponent, ISystem, IGameLoop, IComponentStorage
│   │   ├── Managers/                    #    EntityManager, ComponentManager, SystemManager, WorldManager
│   │   ├── Storages/                    #    ComponentStorage<T> (sparse-set)
│   │   ├── Queries/                     #    QueryBuilder, Query<T1..T3>, QueryEnumerator
│   │   ├── Components/
│   │   │   ├── Core/                    #    Active, Children, DirtyFlag, Name, Parent, Timer
│   │   │   ├── Transform/              #    Transform, Transform2D, WorldTransform
│   │   │   ├── Rendering/              #    Camera, Renderable, Lights, BoundingBox, Color, Culled
│   │   │   ├── Physics/                #    Rigidbody, Velocity, Acceleration, Colliders, CollisionInfo
│   │   │   ├── Animation/              #    (placeholder)
│   │   │   ├── Audio/                   #    (placeholder)
│   │   │   └── Networking/              #    (placeholder)
│   │   ├── Systems/                     #    TransformHierarchy, FreeCameraSystem, FrustumCulling
│   │   ├── Scene/                       #    SceneGraph
│   │   ├── Mathematics/                 #    Frustum (6-plane extraction + AABB test)
│   │   ├── Rendering/                   #    Contracts (15 interfaces), Data (Vertex3D, MeshPrimitives), FreeCamera
│   │   ├── Assets/                      #    AssetManager, LoadedMesh
│   │   ├── Network/                     #    NetworkWriter, NetworkReader
│   │   ├── Time/                        #    GameLoop, GameTime
│   │   ├── Configuration/               #    EngineConfiguration (window, rendering, performance)
│   │   └── Extensions/                  #    DI ServiceCollectionExtensions
│   │
│   └── Rendering/HEngine.Rendering/    # 🔴 Rendering Layer (DirectX 12 + Silk.NET)
│       ├── Devices/                     #    DirectX12Device (IGraphicsDevice)
│       ├── DirectX12/                   #    Core, CommandQueue, CommandList, SwapChain, SpriteRenderer, UploadRingBuffer
│       ├── Managers/                    #    BufferManager, PipelineState, ShaderManager, MaterialManager, RenderManager
│       │                                #    ShaderDiskCache, ShaderFileWatcher, ShaderVariantCompiler
│       ├── Renderers/                   #    DirectX12MeshRenderer, DirectX12SpriteRenderer
│       ├── Systems/                     #    SilkDirectX12Renderer, LightingSystem, MeshRendering, MeshAssetLoading
│       │   ├── Contracts/               #    IMeshRenderingSystem, ISpriteRenderingSystem
│       │   └── Implementations/         #    RenderingSystem, SpriteRenderingSystem
│       ├── Shaders/                     #    Mesh.hlsl, Sprite.hlsl
│       ├── Data/                        #    Material, MaterialInstance, MaterialTemplate, MaterialPropertyBlock
│       │                                #    LightData, ShaderVariant, SpriteVertexStruct
│       ├── Components/                  #    Mesh, MeshAsset, Sprite, DirectionalLight, PointLight, AssetLoadState
│       ├── Batches/                     #    SpriteBatchManager
│       ├── Input/                       #    InputState, SilkCameraInputProvider
│       ├── Diagnostics/                 #    RenderingMetrics
│       ├── Enums/                       #    FrameState, MaterialPreset, MaterialPropertyType, ShaderFeatureFlags
│       ├── Factories/                   #    RenderContextFactory
│       ├── Adapters/                    #    CommandQueue/ShaderManager adapters
│       ├── Serialization/               #    MaterialTemplateSerializer
│       ├── Logging/                     #    RenderLogEvents
│       └── Extensions/                  #    DI ServiceCollectionExtensions
│
├── Tests/
│   ├── HEngine.Core.Tests/             #    ~324 unit tests (37 test files)
│   └── HEngine.Rendering.Tests/        #    ~40 unit tests (7 test files, headless)
│
├── Benchmarks/
│   └── HEngine.Core.Benchmarks/        #    BenchmarkDotNet performance tests
│
└── docs/
    ├── ARCHITECTURE.md                  #    Detailed architecture guide
    └── CONTRIBUTING.md                  #    Contribution guidelines
```

---

## 🔑 Core Design Principles

1. **Data-Oriented Design** — Components stored in contiguous arrays; sparse-set enables cache-friendly iteration
2. **Separation of Concerns** — Core has zero rendering dependencies; rendering abstracts behind 15 interfaces
3. **Zero-Allocation Rendering** — Pre-allocated ring buffers, persistent mapping, `ReadOnlySpan<T>`, no `List<T>` in hot path
4. **Platform-Agnostic Core** — All game logic testable without GPU; rendering layer is swappable
5. **Performance First** — `[AggressiveInlining]`, triple buffering, constant buffer dirty tracking, pipeline state caching
6. **Native AOT Ready** — Application layer configured for ahead-of-time compilation

---

## ⚡ Performance Characteristics

- **Triple buffering** — CPU works 2–3 frames ahead, eliminates CPU/GPU bubbles
- **Persistent buffer mapping** — Vertex buffers mapped once, never unmapped (~30ms/sec savings at 60 FPS)
- **Ring buffers** — Separate vertex buffer per frame prevents overwriting in-flight data
- **Smart constant buffer** — Only updates GPU memory when camera matrix changes
- **Sparse-set ECS** — O(1) component add/remove/lookup, O(n) dense iteration
- **Query caching** — Results invalidated only on structural changes
- **Upload ring buffer** — 16 MB dynamic upload heap with per-frame partitioning

---

## 📋 Prerequisites

- **.NET 10 SDK** (RC2 or later)
- **Windows 10+** (DirectX 12 required for rendering)
- **DirectX 12 compatible GPU**
- **Visual Studio 2022** / **Rider 2024+** / **VS Code with C# DevKit**

## 🚀 Building & Running

```bash
# Restore and build
dotnet restore HEngine.sln
dotnet build HEngine.sln -c Release

# Run the engine
dotnet run --project HEngine -c Release

# Publish Native AOT
dotnet publish HEngine -c Release -r win-x64
```

---

## 🗺️ Development Roadmap

### ✅ Phase 1 — Foundation (COMPLETE)
ECS architecture, DirectX 12 rendering, transforms, scene graph, camera, lighting, frustum culling, material system, asset management, shader variants with disk caching, comprehensive testing.

### 🔲 Phase 2 — Advanced Rendering & Physics
- PBR (Physically Based Rendering) pipeline
- Shadow mapping (directional + point lights)
- Post-processing effects (bloom, tone mapping, FXAA)
- Skeletal animation system
- Physics integration (collision detection, rigid body dynamics, broadphase)
- Particle system
- Texture loading and management (DDS, PNG, etc.)
- LOD system implementation

### 🔲 Phase 3 — Networking & Multiplayer
- Client-server architecture
- Lockstep determinism
- Client-side prediction
- Server reconciliation
- Network component replication
- Lobby and matchmaking

### 🔲 Phase 4 — Tooling & Scripting
- Entity inspector / editor GUI
- C# hot-reload via Roslyn
- Visual scripting nodes
- Profiling and debugging overlays
- Scene serialization / deserialization
- Asset import pipeline (FBX, glTF, OBJ)

### 🔲 Phase 5 — Production
- Audio system (spatial audio, mixing)
- AI/Navigation (navmesh, pathfinding)
- Terrain system
- UI framework (in-engine UI rendering)
- Vulkan backend (cross-platform)
- Console / mobile platform targets

---

## 📖 Documentation

- **[Contributing Guide](CONTRIBUTING.md)** — Branch flow, commits, code style, CI
- **[Task Conventions](CONVENTIONS.md)** — Writing issues and definitions of done
- **[Agent Instructions](AGENTS.md)** — Single source of truth for AI tools
- **[Engine State Analysis](docs/ENGINE_STATE_ANALYSIS.md)** — What the engine actually does at runtime
- **[Target Architecture](docs/TARGET_ARCHITECTURE.md)** — Target module split and public API

> **Note:** this README describes intended capabilities. For what currently executes at runtime, read the Engine State Analysis first — several documented subsystems are implemented but not yet reachable from the game loop.

---

## 📜 License

MIT License — See LICENSE file for details.
