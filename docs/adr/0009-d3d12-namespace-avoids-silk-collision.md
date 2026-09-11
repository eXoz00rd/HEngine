# 0009 — New code in the Direct3D 12 backend uses the `Direct3D12` namespace segment

**Status:** Accepted (2026-09-11)
**Relates to:** `TARGET_ARCHITECTURE.md` §3.2, §5.5 · [0002](0002-namespaces-unchanged-during-extraction.md) · issue #124

## Context

`HEngine.Rendering.D3D12` was extracted from `HEngine.Rendering`. Per [0002](0002-namespaces-unchanged-during-extraction.md) the moved files kept their existing `HEngine.Rendering.*` namespaces, so the backend assembly declares `HEngine.Rendering.Managers`, `HEngine.Rendering.DirectX12`, `HEngine.Rendering.Devices` and so on.

The genuinely new code in that assembly — the DI entry point and the texture format mapping — needed a namespace of its own, because a second `ServiceCollectionExtensions` in `HEngine.Rendering.Extensions` would be ambiguous for any consumer importing both assemblies. The obvious choice, `HEngine.Rendering.D3D12`, does not compile.

Silk.NET exposes a type named `D3D12` (`Silk.NET.Direct3D12.D3D12`), used throughout the backend as the API entry point. C# resolves a name by walking enclosing namespaces before considering `using` directives. Inside `namespace HEngine.Rendering.Managers`, the walk reaches `HEngine.Rendering`, finds a member named `D3D12` there, and binds it — so every file using Silk's `D3D12` type fails with *'D3D12' is a namespace but is used like a type*. Fourteen files broke this way.

The collision is not an artefact of keeping the old namespaces. Naming the moved files `HEngine.Rendering.D3D12.Managers` produces the same failure, because the walk still passes through `HEngine.Rendering` and still finds the `D3D12` member. Any namespace named `D3D12` under `HEngine.Rendering` shadows the type for the whole assembly.

## Decision

New code in the backend assembly is declared in `HEngine.Rendering.Direct3D12` (and `HEngine.Rendering.Direct3D12.Extensions`). The assembly keeps the name `HEngine.Rendering.D3D12` from §3.2.

The alternative — keeping the `D3D12` segment and fully qualifying `Silk.NET.Direct3D12.D3D12` at every use — was rejected. It spreads a workaround across fourteen files to protect a name, and any future file that writes the short name reintroduces the break.

## Consequences

- The assembly name and its new code's root namespace differ by one word, which §5.5 otherwise forbids. This is the documented exception, not drift.
- Namespace unification for this module (0002's deferred follow-up) must use `Direct3D12`, not `D3D12`, or it will reintroduce the collision it is meant to clean up.
- Generalisable: a namespace segment that matches a type name from a dependency shadows that type across the whole enclosing namespace. Worth checking before naming a module after the API it wraps — `Vulkan` would collide with Silk.NET the same way.
