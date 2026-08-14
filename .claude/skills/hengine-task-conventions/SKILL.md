---
name: hengine-task-conventions
description: Conventions for writing tasks/issues in the HEngine project (repo eXoz00rd/HEngine). Use this skill whenever you create, write, edit, or propose a task/issue/bug/feature/topic to do for this project — e.g. via `gh issue create`, or when the user asks to "add this to the board", "turn this into a task", "file a bug", "open an issue" — even if they don't explicitly say "convention" or "format". Applies only to the eXoz00rd/HEngine repo, not other projects.
---

# Task-writing conventions — HEngine

The full convention content (title format, description structure, labels, exceptions) lives in [`CONVENTIONS.md`](../../../CONVENTIONS.md) at the repo root — **read that file before writing/creating a task**. We keep it there (not here) because it's the one place readable by other AI tools and by humans too, not just Claude Code — don't duplicate that content in this file.

This `SKILL.md` is only responsible for making sure Claude Code knows *when* to reach for `CONVENTIONS.md`, and how to operationally create a task in this specific repo.

## Repo info

Repo: `eXoz00rd/HEngine`. Default branch: `master`.

**No GitHub Projects board exists for this repo yet** — tasks live as plain issues. If a board is created later, add its number and owner here and extend the command below with `gh project item-add`.

## Creating a task via gh CLI

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
