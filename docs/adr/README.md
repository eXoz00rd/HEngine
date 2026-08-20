# Architecture Decision Records

Short records of non-obvious decisions: what was decided, and **why**.

The "why" is the point. `TARGET_ARCHITECTURE.md` §7.8 states the reason directly: someone who does not know the rationale behind a deliberate decision will read it as a defect and "fix" it. That applies to humans under time pressure and to agents always. These records exist so that a choice which looks redundant — a type living somewhere unexpected, a missing reference, an identifier that is not the obvious one — can be recognised as intentional before it is undone.

Unlike the working documents in `docs/`, which are local and gitignored, ADRs are committed. They are reference material read alongside the code, so they follow the same language rule as `AGENTS.md` and `CONTRIBUTING.md` and are written in English.

## Format

Each record: `NNNN-short-slug.md`, with Status / Context / Decision / Consequences. Numbers are never reused. A superseded record is not deleted — its status is updated and it points at the record that replaced it, because the fact that something *was* decided differently is itself context.

## Index

| # | Title | Status |
|---|---|---|
| [0001](0001-ten-runtime-modules.md) | Ten runtime modules, extracted in dependency order | Accepted |
| [0002](0002-namespaces-unchanged-during-extraction.md) | Namespaces stay unchanged while modules are extracted | Accepted |
| [0003](0003-foundation-deferred.md) | HEngine.Foundation is deferred; Color lives in Scene | Accepted |
| [0004](0004-gameloop-placement-before-shape.md) | GameLoop is relocated before it is reshaped | Accepted |
| [0005](0005-vertex-data-belongs-to-assets.md) | Vertex3D and loaded mesh data belong to Assets | Accepted |
| [0006](0006-component-ids-are-stable-strings.md) | Component type identifiers are stable strings, not GUIDs | Accepted |
| [0007](0007-verification-loop-first.md) | The verification loop is built before the work that needs it | Accepted |
| [0008](0008-solution-migrates-to-slnx.md) | The solution migrates to .slnx | Accepted |
