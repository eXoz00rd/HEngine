---
name: hengine-task-conventions
description: Conventions for writing tasks/issues in the HEngine project (repo eXoz00rd/HEngine). Use this skill whenever you create, write, edit, or propose a task/issue/bug/feature/topic to do for this project — e.g. via `gh issue create`, or when the user asks to "add this to the board", "turn this into a task", "file a bug", "open an issue" — even if they don't explicitly say "convention" or "format". Applies only to the eXoz00rd/HEngine repo, not other projects.
---

# Task-writing conventions — HEngine

The full convention content (title format, description structure, labels, exceptions) lives in [`CONVENTIONS.md`](../../../CONVENTIONS.md) at the repo root — **read that file before writing/creating a task**. We keep it there (not here) because it's the one place readable by other AI tools and by humans too, not just Claude Code — don't duplicate that content in this file.

This `SKILL.md` is only responsible for making sure Claude Code knows *when* to reach for `CONVENTIONS.md`, and how to operationally create a task in this specific repo.

## Repo info

Repo: `eXoz00rd/HEngine`. Default branch: `master`.

Issues live on the **HEngine** GitHub Projects (v2) board, owner `eXoz00rd`, project number `2`. New issues aren't auto-added to it — add them explicitly, and see the `backlog-task` skill for the board's `Status` field IDs and how to move an item through Backlog → In progress → In review → Done.

```bash
gh project item-add 2 --owner eXoz00rd --url https://github.com/eXoz00rd/HEngine/issues/<number>
```

## Creating a task

**Default: a draft issue on the project board, not a repo Issue.** Per `CONVENTIONS.md`'s "Backlog items vs. issues" section — new backlog items (follow-up work discovered while doing something else, ideas, "we should do this eventually") go on the board as draft issues, not into the GitHub Issues tracker:

```bash
gh project item-create 2 --owner eXoz00rd --title "[System] ..." --body "..."
```

This creates a board-only card (no issue number, doesn't show up in `gh issue list` or the Issues tab). New draft items land wherever the board's default column is — explicitly set them to **Backlog** status if they don't (see the `backlog-task` skill for the `Status` field/option IDs).

Only use `gh issue create` (a real repo Issue) when the task is being picked up for work right now, or when the user explicitly asks for an issue specifically:

```bash
gh issue create --repo eXoz00rd/HEngine --title "[System] ..." --body "..."
```

If the task is already done at the time of filing (retroactive work logging), close it and reference the PR in the closing comment instead of in the title/body — the title and DoD should describe the problem/outcome, not the history of who did it and when:

```bash
gh issue close <number> --comment "Done in #<PR number>"
```

## Labels

The repo currently has only GitHub's default label set. The convention in `CONVENTIONS.md` also assumes `polish`, `tech-debt`, `architecture`, `needs visual check` and size labels `S`/`M`/`L`. Create a missing label before using it rather than silently skipping it:

```bash
gh label create tech-debt --repo eXoz00rd/HEngine --description "..." --color ededed
```

## Project-specific caution

When filing a task about a subsystem, check `docs/ENGINE_STATE_ANALYSIS.md` first. Several subsystems (PBR, shadow mapping, post-processing) are implemented and unit-tested but not reachable from the game loop — a task claiming a feature "doesn't work" may actually be a wiring task, and the Definition of Done should say so.

## Updating the conventions

If the user corrects a convention during a conversation, update `CONVENTIONS.md` (not this file) — that's the only source of truth.
