# 0005 — Vertex3D and loaded mesh data belong to Assets

**Status:** Accepted (2026-08-20)
**Relates to:** `TARGET_ARCHITECTURE.md` §3.1, §3.2 · issue #51

## Context

`LoadedMesh` returns `Vertex3D[]`, and `AssetManager.LoadMeshAsync` returns `Task<LoadedMesh>` in its signature. `Vertex3D` currently sits in `HEngine.Core/Rendering/Data/`.

Reading "vertex data is a rendering concern" literally suggests moving `Vertex3D` to `HEngine.Rendering`. That produces a cycle: `Assets` would need `Rendering` for the vertex type, while `Rendering` needs `Assets` to load meshes.

The cycle is not real, though — it comes from misreading the graph. §3.1 has `REN → AST`: **Assets sits below Rendering.** Rendering may freely use Assets types; Assets may not reference Rendering. So the question is not "how do we break the cycle" but "which side of an already-directed edge does this type belong on."

Framed that way it resolves cleanly. `Vertex3D` is what an importer *produces*. §3.2 defines Assets as the GUID-based asset database, importers and reference counting — the output of an importer is squarely inside that. That it is later uploaded to a GPU buffer does not make it a rendering type, any more than a texture's pixel data is.

## Decision

`AssetManager`, `LoadedMesh` and `Vertex3D` all move into `HEngine.Assets`. Mesh renderers consume `Vertex3D` through the `Rendering → Assets` reference that the graph already requires.

`LightData` and `MaterialData` do **not** move with them. They are GPU constant-buffer layouts, not import output, and stay on the Rendering side.

`MeshPrimitives` (procedural cube/sphere generation) moves with Assets: it produces the same shape as an importer, from a different source.

## Consequences

- No new project beyond `HEngine.Assets`, which was already planned.
- The split inside the current `Rendering/Data/` folder is by origin, not by file location: import output goes to Assets, GPU-facing layouts stay in Rendering. Anyone moving the folder wholesale in either direction will reintroduce the problem.
- `Vertex3D` in Assets is a constraint on Assets: it must stay a plain data type with no rendering-backend types in it, or Assets acquires a dependency it is not allowed to have. It is currently pure `System.Numerics` and must remain so.
- This also unblocks referencing materials by stable asset id instead of `Mesh.MaterialPath` (a `string`, which makes `Mesh` non-blittable), since the asset identity and the mesh data will then live in the same module.
