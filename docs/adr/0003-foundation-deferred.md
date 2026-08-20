# 0003 — HEngine.Foundation is deferred; Color lives in Scene

**Status:** Accepted (2026-08-20)
**Relates to:** `TARGET_ARCHITECTURE.md` §2 (Z3), §3.2 · issue #51, PR #67

## Context

`HEngine.Foundation` sits at the bottom of the §3.1 graph and the migration plan scheduled it first, as the natural starting point for extraction. Two separate attempts to create it were abandoned, for two different reasons worth recording — both of which pointed at the same conclusion.

**First attempt.** Scoping the module found nothing to put in it. The obvious candidates, `Frustum.cs` and `ShadowUtils.cs`, turned out on inspection to be Scene- and Rendering-specific rather than general-purpose maths. Creating the project anyway would have produced an empty assembly, which Z3 exists to prevent.

**Second attempt.** Moving `Camera` into `Scene` surfaced a genuine cycle: `Camera.BackgroundColor` is typed `Color`, and `Color` is also used by `DirectionalLight`, `PointLight`, `SpotLight` and `Vertex3D` — all of which were staying in `Core`. `Color` is a dependency-free `Vector4` wrapper, so `Foundation` looked like the obvious home.

It was not. `Color` implements `IComponent`, which lives in `HEngine.ECS`. A `Foundation` containing `Color` would have to reference `ECS` — inverting the intended `ECS → Foundation` direction and breaking the one property that makes Foundation worth having.

## Decision

`HEngine.Foundation` is not created until code exists that genuinely belongs in it: dependency-free, shared by more than one module, and not implementing an ECS contract.

`Color` moves into `HEngine.Scene` instead. This resolves the cycle because `HEngine.Core` already references `Scene`, so the light components keep compiling with no new reference and no new project. It is the same resolution used for `Name.cs` during the Scene extraction: when a type is needed by a module already downstream, move the type rather than invent a module.

## Consequences

- The extraction order deviates from the migration plan's bottom-up sequence. That plan assumed Foundation had content; it does not.
- `Color` sitting in `Scene` is arguably the wrong long-term home — it is not a scene concept. It is the right home *given the current module set*, and should be revisited when Foundation is created for real.
- The trigger for creating Foundation is content, not schedule. Likely candidates as work proceeds: metadata attributes for the component registry (`[Tooltip]`, `[Range]`, `[ComponentId]`), which §6.1 explicitly places in Foundation, and diagnostics counters (§8.5).

  **Update (2026-08-20, after review):** the attribute trigger is no longer hypothetical. The component type registry cannot exist without `[ComponentId]`, and §6.1 puts that attribute in Foundation — so Foundation must be created *before* the registry, not "eventually". Review of the roadmap caught that no task existed for this at all, leaving the plan building eight modules against a target of ten. Foundation now has its own task and an ordering constraint against the registry work. When it is created, `Color`'s placement in `Scene` should be revisited — the constraint that ruled Foundation out for `Color` (`IComponent` conformance) still applies, so it may well stay put, but the question deserves asking once rather than being inherited by default.
- Generalisable rule, learned twice here: a cycle discovered during extraction is more often a misplaced *type* than a missing *module*. Check whether moving one type resolves it before creating an assembly to hold it.
