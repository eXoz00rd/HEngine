# Task-writing conventions — HEngine

Applies to tasks/issues on this repo. This document is the source of truth — AI tools (Claude, Codex, others) and humans should refer to it instead of copying these rules elsewhere.

This is a living, working document — meant to be extended as the convention matures.

## Language

Tasks/issues are written in **English** — title, description, and comments. Reason: the repo, its code and its public-facing docs are in English, and English keeps the issue tracker readable for any tool or collaborator regardless of their native language.

Note: some planning documents in `docs/` are written in Polish. That's fine for internal working documents — the convention above applies to the issue tracker, commits and pull requests.

## Backlog items vs. issues

Not everything belongs in the GitHub Issues tracker. A **backlog item** (an idea, a piece of follow-up work discovered while doing something else, a "we should do this eventually") is filed as a **draft issue on the HEngine Projects (v2) board** (`gh project item-create`) — a board-only card, not a repo Issue. This keeps the Issues tab reserved for work that's actually being tracked/actioned, instead of accumulating speculative or not-yet-scoped items.

A backlog item gets **promoted to a real GitHub Issue** (`gh issue create`, then added to the board) only when someone is about to pick it up — i.e. right before starting implementation, per the `backlog-task` skill's step 2. At that point the draft item's project card is normally converted/replaced by the real issue's card (the project UI supports converting a draft item to an issue directly).

Rule of thumb: if you're not starting work on it right now, it's a draft backlog item, not an issue.

## Task title

Format: `[System] Description of the problem/topic`

By default, **no verb in the title** — describe the problem or topic, not a ready-made action to perform. Reason: a title with a verb assumes the solution is already decided, which often isn't the case — whoever picks up the task should be able to judge for themselves how to solve it, instead of receiving a ready-made instruction that might not be the best approach.

**Bugs** — describe the symptom, not the fix:
- ✅ `[Rendering] Meshes render without depth testing`
- ❌ `[Rendering] Add a depth buffer`
- ✅ `[ECS] Destroyed entity stays valid until its id is recycled`
- ❌ `[ECS] Fix DestroyEntity`

**Feature / design** — describe the topic, not the finished solution:
- ✅ `[Architecture] Module boundaries required by a future editor`
- ❌ `[Architecture] Split Core into 9 assemblies`

### Exception: simple, unambiguous technical tasks

When the solution is obvious and unambiguous (no design decision or "how to do it" discussion needed), a verb form is fine — the problem and the action then coincide:
- `[Build] Align the StbImageSharp version across projects`
- `[Tech] Update Silk.NET to 2.23`
- `[CI] Fail the build on new compiler warnings`

**Rule of thumb:** if a title with a verb doesn't assume any still-undecided solution (config, version, simple fix), the verb form is fine. If it requires research, a design decision, or a "how to do it" discussion — go back to the verb-less form (problem/topic description). When in doubt: the verb-less form is the safer choice.

The bracketed prefix `[System]` = module/area. Use the target architecture module names where they apply: `Foundation`, `ECS`, `Scene`, `Rendering`, `Rendering.D3D12`, `Assets`, `Serialization`, `Platform`, `Platform.Windows`, `Runtime`, `Testing`, `Tooling.Mcp`. Plus the cross-cutting ones: `Architecture`, `Build`, `CI`, `Docs`, `Tech`.

## Task type

Distinguished via labels, not in the title: `bug`, `enhancement`, `polish`, `tech-debt`, `architecture`, `documentation`.

## Estimation

Label `S/M/L` (or story points) — task scale varies heavily here, so even a rough label helps with planning.

## Verification-sensitive work

A separate label for tasks whose result cannot be confirmed by a unit test alone — e.g. `needs visual check` for rendering changes that require looking at actual output.

This distinction matters more in this repo than in most: the engine currently has 602 green tests covering subsystems that are not reachable from the game loop. A green test suite is not evidence that a rendering change works. Tasks touching the render path should state in the Definition of Done how the result was actually observed.

## Task description (body)

A task should be **self-contained** — don't rely solely on a link to a document. Links rot, documents get moved, and the tracker (especially during quick triage) needs to be understandable without clicking through everywhere. Always include a short summary in the task itself, with the longer context in a linked document (if one exists).

Description structure:

```
## Context
1-3 sentences: what, why, what problem this solves.

## Details
Concrete info needed to do the task (not the whole document).

## References
Link to the analysis / architecture doc / external material — for anyone who wants more context.

## Definition of Done
- [ ] ...
```

Example of a filled-in Definition of Done:

```
- [ ] Geometry behind other geometry is occluded correctly from every camera angle
- [ ] Depth buffer is bound in OMSetRenderTargets and DepthEnable is on in the mesh PSO
- [ ] Demo scene verified visually
- [ ] No new build warnings
```

The DoD must be concrete and verifiable — something that can be checked off as done/not done without interpretation, not a vague statement like "works correctly".

**For any task that adds or wires up a subsystem, the DoD must include reachability from the game loop** — not merely "class exists and has tests". This rule exists because three completed roadmap phases produced tested code that never executed.

The design/architecture document remains the source of truth for larger decisions, but the task itself must provide enough context to do the work without opening anything else.

## Milestones

Grouped by capability or version (e.g. `v0.2 - Lit 3D pipeline`), not by time-based sprints — scope shifts often, so sprint dates go stale quickly, while a capability name stays current.
