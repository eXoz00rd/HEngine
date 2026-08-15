# Agent instructions — HEngine

**This file is the single source of truth for AI tools working on this repository.** It applies to Claude Code, Codex, Junie, Kilocode and any other assistant. Earlier instruction files (`.agents/`, `.junie/`, `.aiassistant/`) have been removed — do not recreate them.

| Document | Scope |
|---|---|
| [`CONVENTIONS.md`](CONVENTIONS.md) | Writing tasks/issues |
| [`CONTRIBUTING.md`](CONTRIBUTING.md) | Branch/PR flow, commits, CI |
| `docs/ENGINE_STATE_ANALYSIS.md` | What the engine actually does at runtime |
| `docs/TARGET_ARCHITECTURE.md` | Target module split and public API |
| [`README.md`](README.md) | Project pitch — aspirational, not factual |

---

## 1. Verify before you trust

`README.md` and `docs/PROJECT_OVERVIEW.md` describe an architecture that does not match what executes at runtime. Several subsystems are fully implemented and unit-tested but unreachable from the game loop.

Two rules follow:

- **Read `docs/ENGINE_STATE_ANALYSIS.md` before acting on any claim about how the engine works.** It is the factual reference; everything else about current behaviour is suspect.
- **Green tests do not imply a working feature here.** When changing the render path, look at actual output.

`GameLoop`, `GameEngine.CreateExampleEntities()` and the demo scene are scaffolding for previewing results, not designed subsystems. Don't treat their shape as intentional.

## 2. Build and test

```bash
dotnet build HEngine.sln -c Debug
dotnet test HEngine.sln
```

Two test projects: `Tests/HEngine.Core.Tests` (platform-agnostic) and `Tests/HEngine.Rendering.Tests`. Narrow the loop when iterating:

```bash
dotnet test Tests/HEngine.Core.Tests/HEngine.Core.Tests.csproj --filter FullyQualifiedName~HEngine.Core.Tests.Managers.WorldManagerTests
```

Measure performance-sensitive changes in `Benchmarks/HEngine.Core.Benchmarks` rather than asserting timings in unit tests.

.NET 10 throughout. Core builds anywhere; the rendering layer needs Windows and a DirectX 12 GPU. There is no `Samples/` directory and no Native AOT configuration.

## 3. Layout

```
Src/Core/HEngine.Core/           platform-agnostic: ECS, transforms, queries, math, contracts
Src/Rendering/HEngine.Rendering/ DirectX 12 via Silk.NET
HEngine/                         composition root + demo scene
Tests/ · Benchmarks/
```

Contracts live in Core, implementations in Rendering; Core must never reference a rendering API. Place new code where `docs/TARGET_ARCHITECTURE.md` says it belongs, not where the current structure suggests.

## 4. Code rules

**Do not write comments.** Do not add explanatory comments, and do not comment out code — delete it.

- No new build warnings; the existing count should only go down.
- Guard expensive logging: `if (_logger.IsEnabled(LogLevel.X))`.
- Async tests using a `CancellationTokenSource` pass `TestContext.Current.CancellationToken`.
- Constructor injection with interfaces; `readonly` fields for injected dependencies.
- Never use primary constructors for services with injected dependencies.
- No static mutable state in Core.
- Keep components small and value-oriented — they live in dense arrays.
- Match surrounding style; there is no `.editorconfig` yet.

**Never add self-attribution** to commits or PR bodies — no `Co-Authored-By` trailers, no "Generated with Claude Code", nothing identifying AI involvement.

## 5. ECS usage

Transform convention is **SRT** — Scale → Rotation → Translation.

Use `ref` returns for zero-copy mutation. Note that `foreach (var (e, t, m) in query)` yields **copies** — writes to `t` are silently lost; use `item.Component1` for `ref` access.

```csharp
ref var transform = ref worldManager.GetComponent<Transform>(entity);
transform.Position = new Vector3(1, 2, 3);
```

Systems implement `ISystem` with `Initialize(WorldManager)`, `Update(float)` and `Dispose()`.

Several ECS primitives behave differently than their names suggest — entity generations, query invalidation and storage compaction all have documented defects. Check `docs/ENGINE_STATE_ANALYSIS.md` before relying on them, and fix the defect rather than working around it.

## 6. Wiring new subsystems

A subsystem is done when it is **reachable from the game loop and its effect is observable** — not when its class exists and has tests.

- Register services explicitly in DI. **Never add fallback constructors that substitute defaults for missing dependencies** — a missing dependency must fail at startup, not silently disable a feature.
- Register systems with the `SystemManager` that `GameLoop` actually drives.
- Read feature configuration from `EngineConfiguration` rather than hardcoding it.

## 7. Scope discipline

- One task at a time; finish it before starting another.
- Write Polish documents in Polish, not translated from English (§8).
- Keep pull requests small: roughly ≤400 changed lines and ≤15 files.
- Do not commit until the work has been reviewed and you get an explicit go-ahead.
- Ask when a requirement or expected behaviour is unclear rather than assuming.
- Prefer the Rider MCP tools over raw console commands; fall back to the console when they fail.

## 8. Writing documents

Planning documents in `docs/` are written in Polish. Code, comments, commits, pull requests and issues stay in English — see `CONVENTIONS.md`.

A Polish document must read as if it were written in Polish, not translated from an English draft. Word-for-word renderings of English jargon are the main failure mode here and they are not acceptable:

- **Never calque a term.** A "pump" is not a `pompa`, a "GPU fence" is not an `ogrodzenie GPU`, a "swap chain" is not a `łańcuch wymiany`, "mergeable" is not `mergowalny`. When a literal rendering would puzzle a Polish reader, name the thing by what it does — `pętla zewnętrzna`, `synchronizacja z GPU` — or keep the established English term and gloss it once.
- **Expand every abbreviation on first use**, in parentheses, with what it means — not just what the letters stand for. This includes ones that feel obvious in context: MCP, DI, CI, TFM, CPM, AOT, ADR, ECS, PSO.
- **Keep an English term untranslated when it is the name actually used in the field** (backend, headless, swap chain, singleton, culling). Translating those invents private vocabulary that no one else uses. Gloss each one once in parentheses.
- **Any document introducing more than a handful of terms carries a glossary near the top**, before the body.
- Coin no new Polish jargon. If a term needs a paragraph of explanation, the term is wrong.

Test before committing a document: could someone who knows C# and game development, but has never read the English sources behind these ideas, read each paragraph once and understand it? If a sentence only parses after mentally translating it back into English, rewrite it.
