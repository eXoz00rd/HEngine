HEngine Development Guidelines

Audience: Advanced HEngine contributors (C#/.NET 9)

This document captures project-specific knowledge: how to build, configure, test, and extend HEngine. It intentionally omits generic .NET guidance.

1. Build and Configuration
- Toolchain
  - .NET SDK: net9.0 is the active TFM throughout the solution. Use the latest 9.x SDK.
  - OS: Core projects are cross-platform; the Rendering projects target Windows/DirectX 12 specifics and may require Windows to build or run samples. Unit tests are Core-only and run on any OS with .NET 9.
- Solution layout
  - HEngine.sln – multi-project solution.
  - Src/Core/HEngine.Core – core engine (components, managers, time, queries, rendering contracts).
  - Src/Rendering/HEngine.Rendering – rendering implementation(s) (DirectX12, batching, systems, renderers/managers).
  - HEngine – composition/bootstrap utilities (e.g., Builders/EngineBuilder.cs).
  - Tests/HEngine.Core.Tests – xUnit test project for core logic.
  - Samples – runnable examples (may depend on platform-specific graphics backends).
- Building
  - Build whole solution (Debug):
    - dotnet build HEngine.sln -c Debug
  - Build Release:
    - dotnet build HEngine.sln -c Release /p:ContinuousIntegrationBuild=true
  - Project-scoped build (e.g., Rendering only):
    - dotnet build Src\Rendering\HEngine.Rendering\HEngine.Rendering.csproj -c Debug
- Native AOT (if required by a sample)
  - Some samples may opt-in to NativeAOT. Check the sample’s .csproj for <PublishAot>true</PublishAot> or related properties. When present, publish with:
    - dotnet publish Samples\<SampleProject>\<SampleProject>.csproj -c Release -r win-x64 -p:PublishAot=true
  - Note: Rendering backends using DirectX12 require Windows and the appropriate GPU/driver.
- Configuration hooks
  - Engine composition is centralized around EngineBuilder (HEngine/Builders/EngineBuilder.cs). When adding new systems, register them via the builder to ensure they are resolved consistently in the game loop and subsystems.
  - Rendering is split between contracts in HEngine.Core.Rendering.Contracts and concrete implementations in Src/Rendering. Keep interfaces stable; add adapters/managers in Rendering layer.

2. Testing
- Framework
  - Tests use xUnit. The test project is Tests/HEngine.Core.Tests/HEngine.Core.Tests.csproj targeting net9.0.
- Running tests
  - Entire solution:
    - dotnet test HEngine.sln -c Debug --no-build
  - Only Core tests:
    - dotnet test Tests\HEngine.Core.Tests\HEngine.Core.Tests.csproj -c Debug --no-build
  - Filtered runs:
    - dotnet test Tests\HEngine.Core.Tests\HEngine.Core.Tests.csproj -c Debug --filter FullyQualifiedName~HEngine.Core.Tests.Components.Core.ActiveTests
    - dotnet test Tests\HEngine.Core.Tests\HEngine.Core.Tests.csproj -c Debug --filter DisplayName~"Guidelines example test"
- Adding a new test
  - Place new files under Tests/HEngine.Core.Tests in an appropriate folder (mirroring the Core namespace).
  - Use minimal dependencies; tests should only reference HEngine.Core to maintain portability and speed.
  - Example tested (verified locally during guideline authoring):
    - File path suggestion: Tests/HEngine.Core.Tests/ExampleGuidelinesTest.cs
    - Contents:
      
      using Xunit;
      
      namespace HEngine.Core.Tests
      {
          public class ExampleGuidelinesTest
          {
              [Fact(DisplayName = "Guidelines example test should pass")]
              public void Example_Should_Pass()
              {
                  Assert.True(true);
              }
          }
      }
      
  - After adding, run:
    - dotnet test Tests\HEngine.Core.Tests\HEngine.Core.Tests.csproj -c Debug
  - Note: This example file was created, executed, and then removed to keep the repository clean as per process requirements.
- Tips
  - Prefer small, deterministic unit tests against HEngine.Core abstractions (Managers, Components, Time, Queries, etc.).
  - For performance-sensitive code (Storages/Queries), benchmark changes in Benchmarks/HEngine.Core.Benchmarks rather than unit tests.
  - Use FullyQualifiedName filters for fast feedback loops when iterating on specific test classes.

3. Additional Development Information
- Code organization conventions
  - Contracts (interfaces) live in HEngine.Core (and HEngine.Core.Rendering.Contracts). Implementations live in HEngine.Rendering or higher-level projects. This separation keeps Core testable and platform-agnostic.
  - Rendering layer breakdown:
    - Systems (Src/Rendering/HEngine.Rendering/Systems/…) orchestrate rendering passes. RenderingSystem coordinates Sprite/Mesh systems and consumes IRenderContext.
    - Managers (e.g., RenderManager) manage GPU resources and frame lifetime.
    - Devices/DirectX12 provide backend-specific integration (see DirectX12Core.cs, adapters, and renderers).
  - EngineBuilder: central extension point for wiring systems/managers. Prefer constructor injection with interfaces to keep components mockable.
- RenderingSystem specifics (as of recent changes)
  - HEngine.Rendering.Systems.Implementations.RenderingSystem implements IRenderingSystem and coordinates both sprite and mesh rendering. It relies on IRenderContext for view/projection matrices and delegates actual drawing to subsystems.
  - When extending rendering:
    - Add new subsystem interface to Core contracts if it must be referenced cross-layer.
    - Implement it in Rendering, register via EngineBuilder, and integrate in RenderingSystem.Render with appropriate ordering.
- Error handling and disposal
  - RenderingSystem tracks _disposed and _isInitialized; respect these flags in new systems. Throw ObjectDisposedException in Initialize() when appropriate to prevent invalid usage.
  - Catch-and-log inside top-level render loops, rethrow when invariants are broken to surface issues to the caller/game loop.
- Style and patterns
  - Favor readonly fields for injected dependencies.
  - Keep components and structs in HEngine.Core lightweight/value-oriented when possible to aid storage performance.
  - Avoid direct static state in Core; use Managers and WorldManager for lifecycle.
- Debugging tips
  - For rendering, validate that SetViewMatrix/SetProjectionMatrix is invoked before draw calls; mismatch between context and renderer is a common source of bugs.
  - When adding new render passes, log frame markers in the manager to correlate CPU-side setup with GPU execution.
  - For tests, use xUnit output helpers minimally; keep tests pure to allow parallelism.

4. Quick Recipes
- Build fast without tests:
  - dotnet build HEngine.sln -c Debug
- Run all tests quickly (no restore/build):
  - dotnet test HEngine.sln -c Debug --no-restore --no-build
- Run a single test class:
  - dotnet test Tests\HEngine.Core.Tests\HEngine.Core.Tests.csproj -c Debug --filter FullyQualifiedName~HEngine.Core.Tests.Managers.WorldManagerTests
- Add a new rendering system skeleton
  - Define interface in Src\Core\HEngine.Core\Rendering\Contracts.
  - Implement in Src\Rendering\HEngine.Rendering\Systems\Implementations.
  - Register in EngineBuilder and integrate in RenderingSystem.Render ordering.

Notes
- The example unit test described above was created and executed successfully during preparation of this document; it has been removed to keep the repository unchanged except for this guidelines file.
