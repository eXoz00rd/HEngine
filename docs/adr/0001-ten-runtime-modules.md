# 0001 — Ten runtime modules, extracted in dependency order

**Status:** Accepted (2026-08-20)
**Relates to:** `TARGET_ARCHITECTURE.md` §3.2, §11 decision #1 · issue #51

## Context

`TARGET_ARCHITECTURE.md` §11 offers two shapes for the runtime: ten modules per §3.2, or a merged set of seven (`Scene` folded into `ECS`, `Serialization` into `Assets`, `Platform` left undivided). The document recommends ten but records it as a decision to confirm rather than a settled matter.

The merge argument is real — fewer assemblies means fewer project files, fewer references, less ceremony. The counter-argument is that each of the three proposed merges removes a boundary that exists for a stated reason under Z3 ("a separate assembly needs justification"), and two of them remove boundaries the architecture depends on elsewhere.

## Decision

Ten runtime modules, as listed in §3.2.

The three merges were rejected for specific reasons rather than on principle:

- **`Scene` into `ECS`** would mean a consumer wanting only the entity/component machinery drags in transforms, hierarchy and culling. The whole point of extracting ECS was that it be consumable alone.
- **`Serialization` into `Assets`** conflates a mechanism with a domain. Serialization must not know about specific components — the module defining a component registers its serializer (Z4). Folding it into Assets invites exactly the coupling that rule forbids.
- **`Platform` left undivided** is the one that breaks something concrete: splitting contracts from the Windows backend is the precondition for Z8. Without it the graphics backend depends on a windowing library, and headless stops being a capability.

## Consequences

- Roughly ten project files rather than seven, plus one test project each (§5.5).
- Adding a module must stay cheap for this to be sustainable — §5.6 sets the bar: touch only new files, one solution entry, one registration call. If adding a module ever requires editing an existing one, Z4 has been broken and this decision becomes a liability.
- Extraction proceeds bottom-up along the §3.1 graph, never the reverse. `HEngine.ECS` (#62) and `HEngine.Scene` (#64, #67) are done; the rest are tracked on the project board.
- `Foundation` is a deliberate exception to the bottom-up order — see [0003](0003-foundation-deferred.md).
