# 0002 — Namespaces stay unchanged while modules are extracted

**Status:** Accepted (2026-08-20)
**Relates to:** `TARGET_ARCHITECTURE.md` §5.5 · issue #51

## Context

`TARGET_ARCHITECTURE.md` §5.5 states the target plainly: one project = one module = one assembly = one root namespace. Today that rule is broken on purpose. `WorldManager` lives in `HEngine.ECS.dll` but declares `namespace HEngine.Core.Managers`; `Transform` lives in `HEngine.Scene.dll` and declares `HEngine.Core.Components.Transform`.

This looks like an oversight and will keep looking like one. It is not.

Renaming a namespace during extraction is mechanically trivial but touches every consumer's `using` directives — dozens of files across Rendering, the host and the benchmarks, none of which the extraction itself needs to touch. Bundling that into the move would have turned each extraction PR from "these files relocated, nothing else changed" into a diff where the actual structural change is buried in import churn. The extraction PRs so far have been reviewable precisely because the diff is 100% renames with zero content edits.

There is also a correctness argument. A pure file move either compiles or does not; a move plus a rename can compile while having silently changed which type a consumer resolves to, in the specific case where two namespaces contain a same-named type. That situation existed in this repo — `DirectionalLight` and `PointLight` were duplicated across `HEngine.Core.Components.Rendering` and `HEngine.Rendering.Components`, and the host disambiguated with `using` aliases. Doing both operations at once is how that class of bug gets shipped.

## Decision

Extraction moves files between assemblies and leaves namespace declarations alone. Only genuinely new code gets the correct namespace — for example `HEngine.ECS.Extensions.ServiceCollectionExtensions`, which did not exist before.

Namespace unification is tracked as separate work, one task per module, to be done once the module boundaries have stopped moving.

## Consequences

- The repo temporarily violates §5.5. This is expected and is not evidence of a mistake.
- `HEngine.Core.Contracts` currently spans two assemblies — three of its files are in `HEngine.ECS`, two remain in `HEngine.Core`. Same reason.
- Anyone reading a file and finding its namespace disagreeing with its assembly should check this record before "correcting" it. Doing that piecemeal, one file at a time, produces the worst outcome: partial unification, where the namespace tells you nothing because it is sometimes right and sometimes not.
- The unification tasks are large-diff and low-risk, and should be done per module rather than all at once, so each stays reviewable.
