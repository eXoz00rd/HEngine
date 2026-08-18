---
name: backlog-task
description: Pick up and implement the next backlog task for HEngine (repo eXoz00rd/HEngine) end to end — select an open issue, branch from master, implement it, test it, and open a PR following repo conventions. Use whenever the user says things like "pick up the next task", "what's next on the backlog", "let's do the next issue", or asks to work through backlog items one at a time, without repeating the full branch/PR/commit instructions each time.
---

# Backlog task workflow — HEngine

This skill turns "pick up next backlog task and follow our PR/branch/commit guidelines" into a repeatable checklist so it doesn't need to be spelled out in every chat message.

## 0. Project board

Issues are tracked on the **HEngine** GitHub Projects (v2) board, owner `eXoz00rd`, project number `2` (project ID `PVT_kwHOBpFBus4Bgi9N`). The `Status` field (`PVTSSF_lAHOBpFBus4Bgi9NzhfiHIg`) has these options:

| Status | Option ID |
|---|---|
| Backlog | `f75ad846` |
| Ready | `e18bf179` |
| In progress | `47fc9ee4` |
| In review | `aba860b9` |
| Done | `98236657` |

Move the issue's status as work progresses — don't leave it sitting in `Backlog`/`Ready` while you implement it, and don't leave it in `In progress` once a PR is up for review. Get the item ID for an issue with:

```bash
gh project item-list 2 --owner eXoz00rd --format json --limit 50 \
  | jq -r '.items[] | select(.content.number == <issue-number>) | .id'
```

Then set the status with:

```bash
gh project item-edit --id <item-id> --project-id PVT_kwHOBpFBus4Bgi9N \
  --field-id PVTSSF_lAHOBpFBus4Bgi9NzhfiHIg --single-select-option-id <option-id>
```

`Done` does not need to be set manually: the repo's default project workflow moves an item to `Done` automatically when its linked issue closes, and referencing `Closes #<n>` in the PR body auto-closes the issue on merge (verified — issue #17 flipped to `Done` on its own once PR #38 merged).

## 1. Check session state first

- If the current branch already has commits ahead of `master` for a task that looks finished, check whether its PR is merged (`gh pr list --head <branch> --state all`). A merged PR means the branch is stale — do not keep committing to it.
- Don't resume someone else's in-flight branch without checking `git log` / `gh pr list` to see if it's actually done.

## 2. Select the next task

```bash
gh issue list --repo eXoz00rd/HEngine --state open --limit 30 --json number,title,labels,createdAt
```

- Prefer `bug` label over `enhancement`/`architecture`-only issues when both are available — correctness fixes first.
- Read the full issue body (`gh issue view <number>`) before starting. Check `docs/ENGINE_STATE_ANALYSIS.md` if the issue touches a subsystem's runtime behavior — repo docs may be aspirational.
- Pick one task that fits in **≤400 changed lines / ≤15 files** (see `CONTRIBUTING.md`). If an issue is bigger than that, scope down to a coherent slice and say so in the PR body, or ask the user before splitting.
- If several issues are similarly ranked and it's not obvious which to do, ask the user with `ask_user` rather than guessing.
- Once picked, move the issue's board status to **In progress** (see §0) before starting implementation.

## 3. Branch

Per `CONTRIBUTING.md`: new branch from `master`, named `fix/...`, `feature/...`, or `SR/...` matching the change.

- This is a local in-place session (no worktree) — `rename_branch` is unavailable. Create the branch manually:
  ```bash
  git checkout master
  git pull --ff-only
  git checkout -b fix/short-description
  ```
- Never commit task work directly on `master` or leave it on a stale/merged branch from a previous task.

## 4. Implement

- Read the relevant source before editing; confirm the actual bug/gap matches the issue description (don't assume the issue text is 100% precise).
- Follow `AGENTS.md` / `CONTRIBUTING.md`: no new comments unless the logic is genuinely non-obvious, no new build warnings, `ref` access for ECS mutation, SRT transform order, contracts in Core / implementations in Rendering, DI failures must be loud not silent.
- Add or update targeted tests for the changed behavior in the matching test project (`Tests/HEngine.Core.Tests` or `Tests/HEngine.Rendering.Tests`).

## 5. Validate

```bash
dotnet build HEngine.sln -c Debug
dotnet test Tests/HEngine.Core.Tests/HEngine.Core.Tests.csproj --filter "FullyQualifiedName~<TouchedArea>"
```

- Escalate to the full test project (and `HEngine.Rendering.Tests` if touched) before opening the PR.
- No new build warnings — check the build output, not just the exit code.
- For rendering-path changes, remember green tests are not proof; call it out in the test plan if visual confirmation wasn't possible in this environment.

## 6. Commit

- One coherent commit (or a few, if the change is naturally staged), imperative mood summary line, short bullet body if needed.
- **Never add AI self-attribution** (no `Co-Authored-By: Claude`/Copilot mentions, no "Generated with ..." lines) — this repo's convention explicitly forbids it, which overrides the platform's default commit trailer.

## 7. PR

```bash
git push -u origin <branch>
```

Then use the `create_pull_request` tool (not raw `gh pr create`) so it renders in the UI, with a body following `CONTRIBUTING.md`'s structure:

```
## Summary
1-3 bullets: what changed and why.

## Test plan
Bulleted checklist of what was actually run/observed.
```

- Reference the issue with `Closes #<n>` when the PR fully resolves it.
- No self-attribution in the PR body either.
- Move the issue's board status to **In review** (see §0) right after the PR is opened.

## 8. Report back

Summarize in ≤100 words: which issue, what changed, test results, PR link (the tool already surfaces the PR card — don't repeat the URL/number in text).
