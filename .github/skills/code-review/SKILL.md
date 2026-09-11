---
name: code-review
description: Tailored code review workflow for HEngine. Use for pull requests and diffs in this repository to prioritize runtime reachability, DI correctness, ECS mutation safety, and rendering-path regressions over style-only feedback.
---

# HEngine code review skill

Use this skill for code review tasks in HEngine, especially pull requests that touch runtime wiring, rendering, ECS, dependency injection, or architecture boundaries.

## Primary objective

Find high-signal review findings that would ship incorrect runtime behavior even when tests are green.

## Review workflow

1. Start from the actual changed files and trace whether the behavior is reachable from the runtime path.
2. Use `docs/ENGINE_STATE_ANALYSIS.md` as the factual source when repository documents describe behavior that may not execute at runtime.
3. Focus on correctness, integration, and regressions before style or refactoring suggestions.
4. Prefer a small number of concrete, actionable findings over broad commentary.

## HEngine-specific checks

### Runtime reachability

- Verify the changed subsystem is reachable from `GameLoop`, `GameEngine.Initialize()`, `SystemManager`, and `RenderPipeline` where relevant.
- Treat a feature as incomplete if the code exists and tests pass but the runtime path never invokes it.
- Flag any change that adds a subsystem without registering it in DI or without wiring it into the executing system graph.

### Silent downgrade detection

- Flag fallback constructors that mask missing dependencies by creating disabled settings, no-op services, empty stacks, or `Null*` behavior.
- Flag changes that silently turn missing composition into disabled rendering, lighting, shadows, post-processing, or asset behavior.
- Prefer startup failure over a successful launch with degraded behavior.

### Rendering-specific review

- Treat render-path changes as risky unless depth, pipeline state, resource binding, and runtime registration are consistent end to end.
- Do not assume that a shader, renderer, or manager being present means it is used.
- If a PR claims to enable a rendering feature, check whether the frame path can actually reach it.
- Keep in mind that visual verification matters for rendering changes because tests may cover unreachable subsystems.

### ECS-specific review

- Watch for accidental mutation of ECS query copies produced by tuple deconstruction in `foreach`.
- Prefer `ref` component access for mutation.
- Preserve the SRT transform convention.
- Flag new logic that depends on known ECS defects instead of fixing them directly.

### Architecture boundaries

- Backend-agnostic modules must remain free of graphics API references; only `HEngine.Rendering.D3D12` and `HEngine.Platform.Windows` may name Silk.NET.
- Contracts belong in Core; rendering implementations belong in Rendering.
- Reuse existing configuration and service registration patterns instead of introducing ad hoc paths.

### Code quality: performance, maintainability, design patterns

Correctness and reachability come first, but every review also evaluates quality on three axes — this is a standing bar, not an optional pass:

- **Performance.** Flag avoidable allocations, boxing, or redundant computation/I/O on hot paths (per-frame code in ECS systems, the render loop, input handling). Flag sequential work that has no ordering dependency and could run independently. A contracts-only or interface-shape change still matters here: the shape constrains every future implementer, so check whether it forces an inefficient pattern later even if nothing calls it yet.
- **Maintainability.** Flag duplication, unclear naming, deep nesting, and code placed at the wrong architectural layer (a special case bolted onto shared infrastructure instead of generalizing the underlying mechanism). Prefer the simplest form that does the job — no speculative abstraction for hypothetical future needs, but also no copy-paste that a small shared helper would remove.
- **Design patterns.** Check that new abstractions follow established, idiomatic C#/.NET patterns and match how the rest of this codebase already solves the same kind of problem (Grep for a sibling module before inventing a new shape). Flag both under-engineering (a god-object doing several jobs) and over-engineering (a pattern applied where a plain method would do).

When these findings conflict with tight scope discipline (§7 of `AGENTS.md`), prefer flagging the issue over silently expanding the diff to fix it — note it, don't necessarily fix it inline unless it's small and squarely in the reviewed files.

## Findings bar

Report only issues that are likely to cause incorrect behavior, broken integration, misleading tests, or a real cost on the performance/maintainability/design-pattern axes above.

Do not spend review budget on formatting, naming nits, or subjective style unless they mask a real bug or a concrete quality cost.
