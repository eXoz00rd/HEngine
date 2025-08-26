# HEngine Improvement Tasks Checklist

Below is an ordered, actionable checklist covering architectural, code-level, testing, performance, documentation, and tooling improvements. Use this as a working plan; each item is intentionally small and verifiable.

1. [x] Consolidate rendering entry points: decide single authoritative path (RenderPipeline.RenderFrame vs RenderingSystem.Render) and refactor call sites to prevent duplicate state setup. 
2. [x] Define clear ownership of IRenderContext creation and lifetime (RenderManager vs DI singleton) and remove the second registration to avoid duplicate instances.
3. [x] Stabilize Core contracts: review IRenderingSystem, IRenderPipeline, IRenderContext, IRenderer for minimal cross-layer surface;
4. [x] Make RenderingSystem.Render(IRenderContext) the only rendering method; deprecate parameterless Render() or make it internal to the pipeline to avoid hidden state reliance.
5. [x] Ensure RenderingSystem respects _disposed and _isInitialized in all public APIs; add ObjectDisposedException guards consistently (Initialize(WorldManager), Render, SetRenderContext).
6. [x] Remove runtime Console.WriteLine in rendering hot paths (RenderingSystem, RenderManager) and replace with ILogger usage; keep warnings/errors only.
7. [x] Normalize logging levels and messages for render lifecycle (BeginFrame/EndFrame/Clear/Present) with structured logs and event IDs for easier tracing.
8. [ ] Align DI registrations: register concrete context (SilkRenderContext) as factory-scoped per device/window instead of singleton; avoid registering IRenderContext as a singleton in ServiceCollectionExtensions.
9. [ ] Add IRenderManager.GetRenderContext nullability contract: annotate as non-null when IsInitialized == true, or split into TryGetRenderContext(out ctx) to avoid double null checks.
10. [ ] Move matrix setup responsibilities fully into RenderPipeline (or a dedicated pass) and ensure all subsystems receive matrices via the provided IRenderContext only.
11. [ ] Validate RenderManager.Initialize projection matrix for all aspect ratios; add configurable projection mode (Orthographic vs Perspective) in EngineConfiguration.
12. [ ] Introduce a simple camera abstraction in Core (ICamera, with View/Projection) and adapt RenderPipeline to pull matrices from the active camera.
13. [ ] Ensure SpriteRenderingSystem and MeshRenderingSystem share common renderer state via context only; remove any hidden static/global state.
14. [ ] Add graceful shutdown flow: RenderManager.Dispose should be idempotent and safe to call from GameEngine.Stop; verify final Present is not called after disposal.
15. [ ] Add CancellationToken support to IGameLoop and RenderPipeline to allow cooperative shutdown.
16. [ ] Expand unit tests for Core: SystemManager add/remove/update order; WorldManager component add/remove/update behavior; basic rendering-contract behavior via fakes/mocks (no GPU).
17. [ ] Add tests for rendering orchestration without GPU: verify RenderPipeline calls BeginRender → RenderingSystem.Render(context) → EndRender using a test double IRenderer.
18. [ ] Add guard tests for disposed state across RenderingSystem, RenderManager, and GameEngine (calling methods after Dispose throws or no-ops as designed).
19. [ ] Add configuration tests: validate EngineConfiguration defaults and that EngineBuilder wires the same instance across consumers.
20. [ ] Introduce Benchmark(s) for hot paths (Sprite batching, component queries) in Benchmarks project; wire simple baseline to track regressions.
21. [ ] Implement sprite batch flush policy: cap batch size and flush in DirectX12SpriteRenderer.FlushBatch; add performance metric logs in Debug builds.
22. [ ] Audit allocations per frame in RenderPipeline/RenderingSystem; avoid per-frame new allocations for contexts, lists, and matrices.
23. [ ] Add frame markers and scoped timers around render passes (systems) to log durations in Debug; consider EventSource for ETW when on Windows.
24. [ ] DirectX12Core: add error checking helpers and wrap HRESULT with descriptive exceptions; ensure device feature level selection is configurable.
25. [ ] DirectX12 device lifecycle: ensure command queue/swap chain/descriptors are created and disposed deterministically; add Dispose pattern with SafeHandles/ComPtr guards.
26. [ ] Abstract platform specifics behind IGraphicsDevice and ensure DirectX12Device fully implements required contract; add a null device for headless tests.
27. [ ] Replace magic numbers in projection and depth range with named constants/configuration; document coordinate system conventions.
28. [ ] Strengthen RenderManager.ShouldClose semantics; forward from IRenderer only after polling events; add tests.
29. [ ] Validate threading model: ensure all rendering calls occur on the correct thread; document constraints in contracts.
30. [ ] Add basic error overlay (debug mode) to show critical render errors on screen without crashing immediately; still rethrow for dev builds.
31. [ ] Introduce IRenderPass interface to structure pipeline stages; split sprite/mesh into passes invoked by RenderPipeline.
32. [ ] Ensure EngineBuilder exposes extension points to register additional render passes and systems without modifying core code.
33. [ ] Document DI module boundaries: Core (no graphics deps), Rendering (DX12/Silk), Engine (composition). Add readme in each project folder.
34. [ ] Create a Samples readme to explain OS/driver requirements and how to run samples with/without NativeAOT.
35. [ ] Add CONTRIBUTING.md with code style, test requirements, and PR checklist including running dotnet test and benchmarks locally.
36. [ ] Add CI workflow (GitHub Actions/Azure Pipelines) to build on Windows and run Core tests on all platforms; artifact build for Samples optional.
37. [ ] Enable analyzers and treat warnings as errors in Debug builds; add .editorconfig with consistent C# style (readonly fields, naming, nullability).
38. [ ] Turn on Nullable reference types across projects; fix annotations on all public APIs in Core and Rendering.
39. [ ] Add XML documentation for public Core contracts and systems; generate docs in CI as an artifact.
40. [ ] Create a troubleshooting guide for common rendering issues (matrix order, not calling SetView/SetProjection, device/context mismatch) and link it in the guidelines.
41. [ ] Add FullyQualifiedName test filter examples to the docs/tasks to speed dev iteration (already in guidelines; consolidate into tests readme).
42. [ ] Review and clean any non-English comments/strings; standardize to English for code and logs; move localization to a resources file if needed.
43. [ ] Ensure Samples opt-in to NativeAOT only when required; document publish commands and runtime flags.
44. [ ] Add interfaces for new rendering subsystems cautiously; prefer adapters in Rendering project to keep Core stable.
45. [ ] Validate exception propagation strategy: catch+log at top level, rethrow for invariants; avoid swallowing exceptions in hot paths unless safe.
46. [ ] Add versioning and changelog process; semantic version guidance for Core contracts.
47. [ ] Provide minimal fake renderer implementation for tests (headless, no GPU) implementing IRenderer; place in Tests or a TestUtilities project.
48. [ ] Add integration path in GameEngine to use RenderPipeline instead of manual system update when pipeline is enabled via configuration.
49. [ ] Remove duplicate or dead code in RenderingSystem (parameterless Render and SetRenderContext) after pipeline integration is finalized.
50. [ ] Verify that ServiceCollectionExtensions does not register overlapping concrete types more than once and avoids multiple singletons of renderers/devices.
