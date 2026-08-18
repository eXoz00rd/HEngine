# HEngine code review instructions

## Purpose

These instructions tailor Copilot code review for HEngine pull requests.
Use them for all repository reviews alongside `AGENTS.md` and any matching skills.

## Review priorities

- Prioritize correctness, missing wiring, silent feature downgrades, and regressions over style
- Treat green tests as insufficient proof for render-path changes
- Flag any change that leaves a subsystem unreachable from the game loop
- Flag DI changes that hide missing dependencies behind fallback constructors, `Null*` implementations, or silent defaults
- Flag changes that add new build warnings or normalize existing warnings as acceptable

## HEngine-specific architecture checks

- `docs/ENGINE_STATE_ANALYSIS.md` is the factual reference for current runtime behavior when repository docs disagree
- `HEngine.Core` must stay platform-agnostic and must not depend on rendering APIs
- Contracts belong in Core, implementations belong in Rendering
- New subsystems are only complete when they are registered in DI, wired into the runtime path, and their effect is observable
- `GameLoop`, `SystemManager`, `RenderPipeline`, and `GameEngine.Initialize()` are the primary reachability checkpoints for runtime features

## Rendering review checks

- Treat render-path changes as incomplete unless the runtime path is still connected end to end
- Check that 3D rendering changes preserve or improve actual GPU reachability, not only isolated classes or tests
- Flag any change that reintroduces silent disabling of shadows, post-processing, lighting, materials, or depth handling
- For mesh rendering changes, look for depth buffer binding, appropriate pipeline state, and runtime registration instead of trusting shader or manager presence alone
- If a change affects rendering output, expect a verification path that includes visual confirmation, not only automated tests

## ECS and systems review checks

- Verify systems that are expected to run are registered with the `SystemManager` that `GameLoop` actually drives
- Treat `foreach` tuple iteration over ECS queries as copy-based; flag writes that appear to mutate copied components instead of `ref` access
- Preserve the SRT transform convention: Scale -> Rotation -> Translation
- Prefer fixes to ECS defects over new workarounds that depend on broken behavior

## Dependency injection and configuration checks

- Missing required dependencies must fail at startup; do not accept fallback constructors that silently disable behavior
- Configuration should come from `EngineConfiguration`, not new hardcoded values
- Review service registration changes for completeness across settings, systems, and concrete implementations

## Tests and validation expectations

- Expect targeted tests for changed behavior when tests exist in the affected area
- For rendering changes, prefer findings that distinguish between tested subsystems and runtime-reachable behavior
- Flag missing validation for observable behavior when code claims a feature is now wired or enabled

## Noise reduction

- Ignore purely stylistic issues unless they hide a correctness, maintenance, or reviewability problem
- Do not request comments in committed code unless the logic is otherwise hard to understand and the repository already allows that exception
