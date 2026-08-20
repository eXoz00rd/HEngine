# 0006 — Component type identifiers are stable strings, not GUIDs

**Status:** Accepted (2026-08-20)
**Relates to:** `TARGET_ARCHITECTURE.md` §4.5, §6.2, §11 decision #2 · issue #51

## Context

The component type registry needs a stable identifier that survives type renames, because saved scenes reference components by that identifier and `Type.FullName` changes under refactoring. `TARGET_ARCHITECTURE.md` §11 lists two candidates — a GUID in an attribute, or a stable string — and is the **only one of its fifteen decisions with no recommendation**.

Both options satisfy the core requirement equally: both are stable under rename, and both demand the same discipline (once shipped, the identifier may never change, regardless of what happens to the type name). The difference is elsewhere.

**Readability.** Decision #3 commits the scene format to being text, mergeable in Git, and readable by an agent. A scene file keyed by `{a1b2c3d4-e5f6-...}` satisfies none of those three in practice: the diff is unreviewable, a merge conflict in it is unresolvable by inspection, and an agent reading the file learns nothing from the identifier. Short semantic strings preserve all three. In a text format a GUID is also simply *larger* — 36 characters against 17 for `hengine.transform`.

**Collision safety.** This is the GUID's genuine advantage: uniqueness with no coordination. Strings require a naming convention and can collide.

The asymmetry is that collision safety is recoverable and readability is not. A duplicate identifier is detectable at registration and can be turned into a loud startup failure — exactly the Z5 pattern the architecture already relies on everywhere else. Choosing GUIDs, by contrast, permanently costs readability in every scene file ever written, with no later remedy.

### Performance

The question that decided the shape of this record: does a string identifier cost anything at runtime?

**No — provided the stable identifier is the persistence identity and never the runtime identity.** These are two different things and conflating them is where the cost would come from:

| Identity | Used for | Representation |
|---|---|---|
| Runtime | component storage lookup, query iteration — the hot path, tens of thousands of times per second | dense integer index, resolved once at registration |
| Persistence | scene files, network packets, editor inspector, MCP introspection — cold paths | stable string |

Concretely: string hashing and comparison happen once per component type at startup, when the registry maps `stable id → dense index`. After that the identifier is not touched by anything on a frame path. Component access continues to resolve through the index, exactly as it does today. Memory cost is one interned string per component type — on the order of a kilobyte for the whole engine.

On the serialization path, where the identifier genuinely is used, a short string is cheaper to write than a GUID (no formatting) and comparable to parse, while producing a smaller file.

This is a constraint, not merely an observation: **if the stable identifier ever appears in a per-frame lookup, that is a defect regardless of which representation was chosen.** A GUID on a hot path would be just as wrong.

## Decision

Component type identifiers are stable strings with an enforced namespace prefix — `hengine.transform`, `hengine.rendering.mesh` — declared in an attribute on the component type.

Supporting requirements:

- The registry maps each identifier to a dense runtime index at registration; the identifier itself never participates in frame-path lookups.
- A duplicate identifier is a startup failure with a message naming both colliding types (Z5).
- An analyzer flags a changed identifier on an existing type, since that silently invalidates saved scenes.

## Consequences

- Scene files stay reviewable and mergeable, which is what decision #3 was for.
- Every new component needs a deliberately chosen identifier. The namespace convention makes this mechanical rather than a design question each time.
- The registry gains a responsibility beyond field metadata: owning the identifier-to-index mapping and enforcing uniqueness.
- Field identifiers (§4.5) inherit this decision. They are scoped within their component, so they need no prefix and can be short.
- **This decision is irreversible in practice.** Every scene saved after the registry ships uses these identifiers, and there is no migration from string identifiers to GUIDs that does not invalidate all of them — the same asymmetry §6.2 notes about field identifiers, which cannot be added retroactively at all.
