# HEngine Improvement Plan

Date: 2025-08-26
Sources used: overview.md, docs/tasks.md

---

## 1. Executive Summary
HEngine aims to be a high‑performance, .NET 9–based 3D game engine with modular architecture, modern rendering, and robust multiplayer. The immediate needs are to unify rendering entry points, stabilize Core contracts, enforce correct lifecycles/threading, and raise test coverage without tying tests to GPU backends. This plan extracts key goals and constraints and proposes focused, staged improvements with rationale and expected impact.

---

## 2. Key Goals (extracted)
- High performance on .NET 9 with ECS, multithreading, and low GC pressure (overview.md: ECS, threading, memory, performance).
- Modern rendering pipeline with DirectX 12 initially; render graph readiness and future RT/path tracing (overview.md: Rendering Architecture).
- Clear Core vs Rendering separation; Core remains platform‑agnostic (guidelines, overview.md).
- Reliable multiplayer foundation (overview.md), but near‑term emphasis is core and rendering correctness.
- Strong test coverage for Core and orchestration without requiring a GPU (docs/tasks.md items 16–18, 21, 47).
- Robust configuration and composition via EngineBuilder (guidelines; tasks 11, 12, 32, 48).
- Developer experience: logging, diagnostics, CI, docs (tasks 6–7, 23, 33–45, 50).

## 3. Key Constraints (extracted)
- Target framework is net9.0 across projects; tests must run cross‑platform without GPU.
- Rendering specifics (DX12) are Windows‑only; Core must not depend on platform APIs.
- Maintain stable Core contracts; prefer adapters in Rendering to avoid API churn (tasks 3, 44, 46).
- Lifecycle correctness: disposed/initialized states respected across systems (tasks 5, 14, 18).
- Threading constraints: rendering calls on the correct thread; avoid blocking render thread (overview.md, tasks 29).
- Avoid hidden global/static state in rendering; flow state only via IRenderContext (tasks 10, 13).

---

## 4. Architecture and Contracts

### 4.1 Stabilize Core Rendering Contracts
- Actions
  - Review IRenderingSystem, IRenderPipeline, IRenderContext, IRenderer for minimal surface.
  - Mark experimental methods in XML docs, add TODOs for future breaking changes.
- Rationale
  - Stable contracts enable Rendering layer iteration without breaking Core; reduces churn (tasks 3).
- Expected Impact
  - Fewer cross‑project changes, easier testing, clearer extension points.

### 4.2 Introduce Camera Abstraction in Core
- Actions
  - Add ICamera (View, Projection) in Core; make RenderPipeline pull matrices from active ICamera.
- Rationale
  - Centralizes matrix responsibility and removes ad‑hoc setup (tasks 11–12).
- Expected Impact
  - Consistent camera behavior and simpler pipeline integration.

### 4.3 DI and Composition Boundaries
- Actions
  - Ensure EngineBuilder registers systems/managers with correct lifetimes; avoid multiple singletons for IRenderContext/device.
  - Document module boundaries: Core (no graphics deps), Rendering (DX12/Silk), Engine (composition) (tasks 33).
- Rationale
  - Prevent duplicate instances and lifecycle bugs; developer clarity.
- Expected Impact
  - Predictable lifetimes and easier debugging (tasks 2, 8, 50).

---

## 5. Rendering Pipeline and Orchestration

### 5.1 Single Authoritative Render Entry Point
- Actions
  - Make RenderingSystem.Render(IRenderContext) the only public render method.
  - Deprecate/remove parameterless Render() or scope it internal to the pipeline.
  - Refactor call sites so RenderPipeline drives Begin→Render→End once per frame (tasks 1, 4, 49).
- Rationale
  - Eliminates duplicate state setup and hidden state reliance; simplifies reasoning.
- Expected Impact
  - Correct frame lifecycle and reduced state bugs.

### 5.2 Render Context Ownership and Flow
- Actions
  - Define ownership with RenderManager; expose TryGetRenderContext(out ctx) or non‑null when IsInitialized==true (tasks 9).
  - Pass matrices/state only via IRenderContext; remove static/global context uses (tasks 10, 13).
- Rationale
  - Avoids null checks proliferation and enforces explicit state flow.
- Expected Impact
  - Safer rendering paths, fewer race conditions.

### 5.3 Introduce IRenderPass and Split Passes
- Actions
  - Add IRenderPass (Core contracts or Rendering contracts if cross‑layer needed).
  - Split sprite/mesh into passes invoked by RenderPipeline in order (tasks 31).
- Rationale
  - Better structure, clearer ordering, and easier extension without modifying RenderingSystem.
- Expected Impact
  - Cleaner pipeline composition via EngineBuilder (tasks 32).

---

## 6. Lifecycle, Error Handling, and Logging

### 6.1 Disposed/Initialized Guards
- Actions
  - Enforce _disposed and _isInitialized guards in RenderingSystem, RenderManager, and related public APIs; throw ObjectDisposedException where appropriate (tasks 5, 14, 18).
- Rationale
  - Prevent undefined behavior; improves resiliency during shutdown and errors.
- Expected Impact
  - Deterministic teardown; tests can validate guarantees.

### 6.2 Logging Strategy
- Actions
  - Replace Console.WriteLine in hot paths with ILogger; standardize lifecycle logs (BeginFrame, EndFrame, Clear, Present) with event IDs (tasks 6–7).
  - Add frame markers and scoped timers for Debug builds; consider EventSource/ETW on Windows (tasks 23).
- Rationale
  - Structured logs make diagnosing timing/state issues tractable without polluting perf paths.
- Expected Impact
  - Better observability, minimal perf impact in Release.

### 6.3 Exception Propagation
- Actions
  - Catch+log at top level loops; rethrow on invariant violations. Avoid swallowing exceptions in hot paths unless explicitly safe (tasks 45).
- Rationale
  - Keeps failures visible without corrupting engine state.
- Expected Impact
  - Faster failure diagnosis and safer recovery in dev.

---

## 7. Testing Strategy (GPU‑independent)

### 7.1 Core Unit Tests
- Actions
  - Expand tests for SystemManager order, WorldManager component behavior, EngineConfiguration defaults (tasks 16, 19).
- Rationale
  - Validates foundational behavior and wiring.
- Expected Impact
  - Increased confidence and faster iteration.

### 7.2 Rendering Orchestration Tests (Headless)
- Actions
  - Provide a minimal fake IRenderer and/or a Null graphics device for tests (tasks 17, 26, 47).
  - Verify sequence: BeginRender → RenderingSystem.Render(context) → EndRender via test doubles (tasks 17).
  - Add disposed guard tests across RenderingSystem, RenderManager, GameEngine (tasks 18).
- Rationale
  - Ensures correct orchestration without requiring a GPU.
- Expected Impact
  - Reliable CI and cross‑platform test runs.

### 7.3 CI Integration
- Actions
  - Add CI workflow to build on Windows and run Core tests cross‑platform; publish docs artifacts optionally (tasks 36, 39).
- Rationale
  - Prevent regressions and keep docs discoverable.
- Expected Impact
  - Automated quality gate.

---

## 8. Performance and Memory

### 8.1 Allocation Audits
- Actions
  - Audit per‑frame allocations in RenderPipeline/RenderingSystem; pool contexts, lists, matrices; use ArrayPool/Span (tasks 22).
- Rationale
  - Reduce GC pressure; stabilize frame times.
- Expected Impact
  - Lower GC frequency and stutter.

### 8.2 Sprite Batch Flush Policy
- Actions
  - Implement capped batch size and explicit FlushBatch in DirectX12 sprite renderer; add debug perf metrics (tasks 21).
- Rationale
  - Predictable batching and GPU utilization.
- Expected Impact
  - Throughput improvements and easier tuning.

### 8.3 Benchmarks
- Actions
  - Introduce benchmarks for hot paths (sprite batching, component queries) in Benchmarks project; track baselines (tasks 20).
- Rationale
  - Data‑driven performance work and regression detection.
- Expected Impact
  - Sustained performance over time.

---

## 9. DirectX 12 Device and Platform Abstraction

### 9.1 Device Lifecycle and Safety
- Actions
  - Wrap HRESULTs with descriptive exceptions; ensure deterministic creation/disposal of command queue, swap chain, descriptors (tasks 24–25).
  - Use SafeHandles/ComPtr guards where applicable.
- Rationale
  - Prevent leaks and obscure device failures.
- Expected Impact
  - Stable runtime and easier debugging of graphics issues.

### 9.2 Abstraction Layer
- Actions
  - Abstract platform specifics behind IGraphicsDevice; ensure DirectX12Device implements contract; provide Null device for headless tests (tasks 26, 47).
- Rationale
  - Keeps Core stable and tests portable.
- Expected Impact
  - Cleaner separation, easier future backends.

### 9.3 Configuration and Constants
- Actions
  - Replace magic numbers for projection/depth ranges with named constants/config; document coordinate conventions (tasks 27).
- Rationale
  - Correctness across scenes and clarity for contributors.
- Expected Impact
  - Fewer rendering bugs and easier onboarding.

---

## 10. Threading Model and Constraints

### 10.1 Thread Affinity Documentation and Enforcement
- Actions
  - Document threading constraints in contracts; ensure rendering occurs on the correct thread; add asserts/checks in debug (tasks 29).
- Rationale
  - Avoid subtle race conditions and GPU driver issues.
- Expected Impact
  - More deterministic behavior under load.

### 10.2 Cooperative Shutdown
- Actions
  - Add CancellationToken to IGameLoop and RenderPipeline; ensure RenderManager.Dispose is idempotent and that Present is not called after disposal (tasks 14–15).
- Rationale
  - Clean shutdown under errors or user exit.
- Expected Impact
  - No lingering resources; fewer intermittent test failures.

---

## 11. Documentation, Samples, and Developer Experience

### 11.1 Project Docs and Samples
- Actions
  - Add readme in each project folder documenting module boundaries (tasks 33).
  - Create Samples readme with OS/driver requirements and NativeAOT instructions (tasks 34, 43).
  - Troubleshooting guide for common rendering issues; link from guidelines (tasks 40, 41).
- Rationale
  - Reduces support load and accelerates contributor productivity.
- Expected Impact
  - Fewer misconfigurations; faster iteration.

### 11.2 Coding Standards and Analyzers
- Actions
  - Enable analyzers; treat warnings as errors in Debug; turn on nullable reference types; update XML docs (tasks 37–39, 42).
- Rationale
  - Consistent, safer codebase.
- Expected Impact
  - Early detection of issues in PRs.

### 11.3 Contribution Process
- Actions
  - Add CONTRIBUTING.md and versioning/changelog guidance (tasks 35, 46).
- Rationale
  - Predictable release and review process.
- Expected Impact
  - Higher quality contributions and traceability.

---

## 12. Prioritized Roadmap (near‑term 4–6 weeks)
1) Rendering orchestration hardening (tasks 1, 4, 9, 10, 13, 49) – High impact, moderate effort.
2) Lifecycle/error handling/logging (tasks 5–7, 14, 23, 45) – High impact, low–moderate effort.
3) DI and context lifetimes (tasks 2, 8, 50) – Medium impact, low effort.
4) Core tests and headless rendering tests (tasks 16–19, 47) – High impact, moderate effort.
5) DX12 device lifecycle and abstraction (tasks 24–26, 27) – High impact, moderate–high effort.
6) Performance audits and batching policy (tasks 20–22) – Medium impact, moderate effort.
7) Threading constraints docs and shutdown (tasks 29, 15) – Medium impact, low effort.
8) Docs/CI/standards (tasks 33–43, 46) – Medium impact, low–moderate effort.

---

## 13. Risks and Mitigations
- Contract churn risk: Mitigate by marking experimental APIs and adding adapter layers (tasks 3, 44).
- Hidden globals causing state bugs: Mitigate by enforcing IRenderContext flow and unit tests (tasks 10, 13, 17).
- Device lifecycle leaks: Mitigate with SafeHandles/ComPtr and deterministic Dispose tests (tasks 24–25).
- Threading violations: Mitigate with asserts, docs, and CI tests (tasks 29, 36).
- GC spikes: Mitigate with allocation audits and pooling (tasks 22, 20).

---

## 14. Acceptance Criteria (for this plan)
- docs/plan.md present with clear themed sections, rationale per change, and prioritized roadmap.
- Plan aligns with overview.md goals and docs/tasks.md items; constraints on Core vs Rendering separation and test portability are respected.
- Explicit references to tasks for traceability and execution.

---

## 15. References
- overview.md (Architecture Design Document & Development Roadmap)
- docs/tasks.md (HEngine Improvement Tasks Checklist)
- .junie/guidelines.md (development guidelines referenced by contributors)
