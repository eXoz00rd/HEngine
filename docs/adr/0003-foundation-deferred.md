# 0003 — HEngine.Foundation is deferred; Color lives in Scene

**Status:** Accepted (2026-08-20) · **partly overtaken (2026-08-20)** — the deferral has expired on its own terms: the trigger named below fired, and `HEngine.Foundation` is now scheduled as roadmap task R-A.0, ahead of the component registry. The `Color` half of this record still holds and is now **settled** rather than open — see *Resolution*. Read this for **why Foundation was skipped during the module split**, not as a claim that it stays skipped.
**Relates to:** `TARGET_ARCHITECTURE.md` §2 (Z3), §3.2, §6.1 · issue #51, PR #67 · superseded in part by roadmap R-A.0

## Context

`HEngine.Foundation` sits at the bottom of the §3.1 graph and the migration plan scheduled it first, as the natural starting point for extraction. Two separate attempts to create it were abandoned, for two different reasons worth recording — both of which pointed at the same conclusion.

**First attempt.** Scoping the module found nothing to put in it. The obvious candidates, `Frustum.cs` and `ShadowUtils.cs`, turned out on inspection to be Scene- and Rendering-specific rather than general-purpose maths. Creating the project anyway would have produced an empty assembly, which Z3 exists to prevent.

**Second attempt.** Moving `Camera` into `Scene` surfaced a genuine cycle: `Camera.BackgroundColor` is typed `Color`, `Color` was in `Core`, and `Core` already referenced `Scene`. `Color` is a dependency-free `Vector4` wrapper, so `Foundation` looked like the obvious home.

> **Factual correction (2026-08-20, second review).** The first version of this paragraph also claimed `Color` was "used by `DirectionalLight`, `PointLight`, `SpotLight` and `Vertex3D`". That is false, and the error is worth naming because of how it was made: the search behind it was `\bColor\b`, which matches a *field named* `Color` as readily as the *type* `Color`. Those four types declare `Vector3 Color` / `Vector4 Color` — raw vectors, not this type. The cycle above was real without them; the sharing was not. Verified against the code, not re-derived from the same grep.

It was not — or so it appeared at the time. `Color` implements `IComponent`, which lives in `HEngine.ECS`. A `Foundation` containing `Color` would have to reference `ECS`, inverting the intended `ECS → Foundation` direction and breaking the one property that makes Foundation worth having.

That reasoning is preserved as written because it is what drove the decision, but it did not survive the second review either: `IComponent` on `Color` turns out to be an unused marker, so this constraint was never the binding one. See *Resolution*.

## Decision

`HEngine.Foundation` is not created until code exists that genuinely belongs in it: dependency-free, shared by more than one module, and not implementing an ECS contract.

`Color` moves into `HEngine.Scene` instead. This resolves the cycle because `Camera` — the type that needs it — is itself in `Scene`, and `HEngine.Core` already references `Scene`, so nothing downstream needs a new reference or a new project. It is the same resolution used for `Name.cs` during the Scene extraction: when a type is needed by a module already downstream, move the type rather than invent a module.

## Consequences

- The extraction order deviates from the migration plan's bottom-up sequence. That plan assumed Foundation had content; it does not.
- `Color` sitting in `Scene` is arguably the wrong long-term home — it is not a scene concept. It was the right home *given the current module set*. This is no longer an open question: see *Resolution*.
- The trigger for creating Foundation is content, not schedule. Likely candidates as work proceeds: metadata attributes for the component registry (`[Tooltip]`, `[Range]`, `[ComponentId]`), which §6.1 explicitly places in Foundation, and diagnostics counters (§8.5).

  **Update (2026-08-20, after review):** the attribute trigger is no longer hypothetical. The component type registry cannot exist without `[ComponentId]`, and §6.1 puts that attribute in Foundation — so Foundation must be created *before* the registry, not "eventually". Review of the roadmap caught that no task existed for this at all, leaving the plan building eight modules against a target of ten. Foundation now has its own task and an ordering constraint against the registry work.

  This update originally ended by saying `Color`'s placement should be "revisited when Foundation exists", on the grounds that `IComponent` conformance still ruled it out. Both halves were wrong — the conformance is vestigial, and "revisit later" schedules nothing. Superseded by *Resolution*.
- Generalisable rule, learned twice here: a cycle discovered during extraction is more often a misplaced *type* than a missing *module*. Check whether moving one type resolves it before creating an assembly to hold it.

## Resolution — where `Color` goes (2026-08-20, second review)

The Consequences above left `Color`'s home open, to be "revisited when Foundation is created for real". Reviewing the actual usage closed it instead, because the premise recorded in Context does not survive contact with the code:

- **`Color` has exactly one production consumer:** `Camera.BackgroundColor`, in `HEngine.Scene` — the same module the type lives in. Every other reference is a test.
- **The light components cited above do not use it.** `DirectionalLight`, `PointLight` and `SpotLight` each declare a raw `Vector3 Color` field, and `Vertex3D` declares `Vector4 Color`. Four duplicated colour fields, none of them typed `Color`. The "shared across modules" framing in Context described an intent, not the code.
- **`IComponent` on `Color` is vestigial.** There is no `AddComponent<Color>` and no query over it anywhere in the repository. The constraint that ruled Foundation out for this type is an unused marker interface, not a real dependency.

So the blocker recorded here was never the binding one. The binding one is this record's own test for Foundation membership — *shared by more than one module* — and `Color` fails it today with a single consumer.

**Decision.** `Color`'s home is decided by R-A.1 (lights move to `Rendering`) and R-A.2 (`Vertex3D` moves to `Assets`), not by Foundation's existence. Both tasks carry it in their Definition of Done:

- If they **unify** the duplicated colour fields onto `Color`, then `Color` gains consumers in `Scene`, `Rendering` and `Assets`, `IComponent` is dropped from it, and it moves to `Foundation` under R-A.0.
- If they **do not**, `Color` stays in `Scene` permanently and this question is closed rather than carried forward.

### Caveat on unification — it is not free, and not obviously right

An earlier draft of this Resolution said the tasks *should* unify those fields, "since that duplication is exactly what a shared value type exists to prevent". That reads well and is wrong in at least two of the four cases. Checked against the code and the shaders:

- **`LightData.Color` is `Vector3` and GPU-layout-bound.** `PBR.hlsl:28` declares `float3 Color` in the light struct. Replacing it with a `Vector4`-backed `Color` changes 12 bytes to 16 and silently corrupts the constant buffer — the kind of break that compiles, passes every existing test, and shows up as wrong lighting.
- **`Vertex3D.GetStride()` hardcodes `sizeof(float) * (3 + 3 + 2 + 4)`.** The colour slot is four floats by contract with the input layout and with `float4 Color : COLOR` in the shader. A `Vector4`-backed `Color` happens to match that, so this one is safe — but it is safe by coincidence of layout, not by design, and anything that changes `Color`'s backing field breaks it.
- **They are not the same quantity.** A light's colour is HDR radiance and legitimately exceeds 1.0; `Color`, with `White`/`Black`/`Red` constants and a `Luminance` property, is shaped for LDR display colour. Merging them puts two different physical quantities behind one type.

None of this forbids unification — the ECS-facing light components could adopt `Color` while `LightData` keeps `Vector3` at the GPU boundary, where a conversion already happens (`LightingSystem.cs:82,101,122`). It does mean unification is a design decision with a real failure mode, not a tidy-up, and the tasks that carry it should treat it as such.

Either way the question is answered by the work that determines the answer, instead of waiting on a trigger that may never fire by itself. A colour-as-component, if anything ever genuinely needs one, is a distinct named component in the owning module (`Tint` in `Rendering`) — not a marker bolted onto the value type.

**Generalisable, and the second time this record has produced the same lesson:** a type's module is decided by who consumes it, and the answer changes as consumers move. "Revisit later" on a placement question tends to mean "inherit by default", because nothing schedules the revisit. Attach it to the task that moves the consumers.
