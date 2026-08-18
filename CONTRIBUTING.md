# Contributing

## Requirements

- **.NET 10 SDK** — `global.json` pins `10.0.0` with `allowPrerelease` and `rollForward: latestMajor`
- **Windows 10/11** — the rendering layer targets DirectX 12; the Core layer builds anywhere
- C# IDE: Rider, Visual Studio 2022, or VS Code

```bash
dotnet restore HEngine.sln
dotnet build HEngine.sln
dotnet test HEngine.sln
```

## Workflow

1. New branch from `master` (e.g. `SR/short-description`, `feature/name`, `fix/name`).
2. Make changes + commit.
3. Pull request to `master`.
4. After merging, the branch is deleted automatically.

We don't push directly to `master` — it's protected by a repository ruleset (no force push, no deletion, linear history required, changes land through a pull request).

**Merges are squash-only.** Rebase and merge commits are disabled, and linear history is enforced.

## Commits

- First line: short summary of the whole change (no leading `#`), imperative mood.
- Rest (if needed): bullet list with details.
- One commit = one coherent change.
- Keep messages short — no walls of text.
- **No self-attribution** — no `Co-Authored-By: Claude` trailers, no "Generated with Claude Code" lines, nothing identifying AI involvement.

## Pull requests

- Title: short summary of the change, same style as a commit's first line.
- Keep PRs small and focused: roughly **≤400 changed lines** and **≤15 files**. Split larger changes.
- Description structure:

```
## Summary
1-3 bullet points describing what changed and why.

## Test plan
Bulleted checklist of how the change was verified (build, tests, what was observed on screen).
```

- The same no-self-attribution rule as commits applies to the PR body.

### Responding to review comments

- When a fix addresses a reviewer's inline comment, reply directly in that comment's thread (not a new top-level PR comment) and mark the thread resolved once the fix is pushed.
- Keep the reply to one brief sentence: what changed and, if useful, the commit it landed in. No walls of text.
- Do this for every review thread the fix addresses — don't leave threads open once the code has moved on.

## C# code

- Match the style of the surrounding code. There is **no `.editorconfig` in this repo yet** — until there is, the existing sources are the reference.
- No comments in committed code unless the code genuinely cannot express the intent on its own.
- New code must not introduce build warnings. Fix warnings before opening a PR — the solution currently builds with a small number of pre-existing warnings, and that number should only go down.
- Guard expensive logging: `if (_logger.IsEnabled(LogLevel.X))`.
- Async tests using a `CancellationTokenSource` pass `TestContext.Current.CancellationToken`.

## Documentation

`docs/` is listed in `.gitignore` — files there are working documents and are not committed by default. When a document needs to be shared or reviewed, it is added deliberately with `git add -f`.

Two documents there are required reading before making architectural changes:

- `docs/ENGINE_STATE_ANALYSIS.md` — what the engine actually does at runtime
- `docs/TARGET_ARCHITECTURE.md` — the target module split and public API

## CI

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on `windows-latest` for pushes and pull requests targeting `main`, `master` and `develop`:

1. Restore + Release build with `ContinuousIntegrationBuild=true`
2. `HEngine.Core.Tests`
3. `HEngine.Rendering.Tests`

CI must be green before merging.

## Before a PR

- `dotnet build HEngine.sln` — no new warnings
- `dotnet test HEngine.sln` — all tests green
- If the change touches the render path, **look at the result** — the test suite covers subsystems that are not reachable from the game loop, so green tests alone do not prove a rendering change works.
