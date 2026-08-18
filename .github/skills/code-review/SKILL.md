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

- `HEngine.Core` must remain free of rendering API references.
- Contracts belong in Core; rendering implementations belong in Rendering.
- Reuse existing configuration and service registration patterns instead of introducing ad hoc paths.

## Findings bar

Report only issues that are likely to cause incorrect behavior, broken integration, misleading tests, or maintainability risk tightly coupled to the change.

Do not spend review budget on formatting, naming nits, or subjective style unless they mask a real bug.
