# 0008 — The solution migrates to .slnx

**Status:** Accepted (2026-08-20)
**Relates to:** `TARGET_ARCHITECTURE.md` §5.3, §11 decision #7 · issue #51

## Context

`TARGET_ARCHITECTURE.md` §11 recommends `.slnx` but makes it conditional: "after confirming tool support in our versions." That condition was never checked, so the decision stayed open.

The case for migrating is concrete and has been felt repeatedly during the module split. The classic `.sln` format identifies projects by GUID and stores configuration mappings per project per platform. Adding one project appends a GUID declaration plus a block of configuration lines, and nests it via a separate `NestedProjects` section keyed by more GUIDs. In practice, during this work, `dotnet sln add` twice produced a solution-folder structure that had to be corrected by hand-editing GUID mappings. §5.6 sets the standard that adding a module should cost one solution entry — the current format does not meet it, and the friction falls hardest on exactly the routine, mechanical operation the architecture wants to be cheap.

`.slnx` is plain XML: one line per project, no GUIDs, human-readable and mergeable.

**Tool support, verified 2026-08-20:** `global.json` pins 10.0.100 with `rollForward: latestMajor`, so the SDK in use is 10.0.201. It ships `dotnet sln migrate`, which generates `.slnx` from the existing `.sln`. The condition in decision #7 is satisfied.

## Decision

Migrate `HEngine.sln` to `HEngine.slnx` using `dotnet sln migrate`.

Bundled with it, since it touches the same file and §5.3 calls for it: reduce the configuration matrix from six variants (`Debug`/`Release` × `Any CPU`/`x64`/`x86`) to `Debug`/`Release` on `Any CPU`, keeping `x64` only where the D3D12 backend requires it. The `x86` variants have never been built and exist only because the template created them.

## Consequences

- Adding a project becomes a one-line change, which is what §5.6 asks for.
- Merge conflicts in the solution file become resolvable by reading, rather than by regenerating.
- `.github/workflows/ci.yml` hardcodes `HEngine.sln` and must be updated in the same change or CI breaks immediately.
- Contributors need SDK 9.0.200+ or a current IDE. The repo already pins .NET 10 via `global.json`, so this is not a new constraint.
- The portable solution filter (`HEngine.Portable.slnf`, needed for the no-GPU CI job) should be created after this migration rather than before, to avoid authoring it twice.
