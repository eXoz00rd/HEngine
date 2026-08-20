# 0004 — GameLoop is relocated before it is reshaped

**Status:** Accepted (2026-08-20)
**Relates to:** `TARGET_ARCHITECTURE.md` §4.2, §6.1, §12 · issue #51

## Context

`GameLoop` (currently in `HEngine.Core`) injects `IRenderManager` and `IRenderPipeline` and drives rendering from inside its own `while` loop. This is the pattern §4.2 exists to separate: the **outer loop** (when a frame starts, window messages, thread ownership) belongs to the host; the **frame pass** (stage order, fixed step, draw-data preparation, GPU sync) belongs to the engine. Today they are fused in one class.

It also blocks the module split. The render contracts cannot leave `Core` while a type in `Core` consumes them.

Two framings were considered.

**"Redesign the frame contract first."** Introduce `IEngineHost.Tick(FrameTiming)` before continuing the split. Correct, but large, and it makes the entire remaining module split wait on an architectural redesign.

**"It is misplaced, not just misshapen."** §3.2 defines `Runtime` as owning the host contract, the tick, DI composition and the optional ready-made loop — and §3.1 has `Runtime → Rendering`. A `GameLoop` in `Runtime` referencing `IRenderManager` is a legal downward reference. Nothing about its current shape violates the dependency graph once it sits in the right module; what violates §4.2 is the shape, and that is a separable problem.

### The objection, and why it is not dismissed

An editor must drive the tick from its own UI loop. A class that owns a `while` loop cannot be driven that way — so moving it without reshaping does not make the engine editor-ready, and §12 lists the frame-responsibility split among the properties that are *not* reversible.

That objection is correct and this decision does not claim otherwise. What it claims is narrower: **relocation is a strict prerequisite of the reshape, not an alternative to it.** `GameLoop`'s destination is `Runtime` under either framing, because that is where §3.2 puts it. Doing the move first costs nothing that the reshape would not also cost, and unblocks unrelated work in the meantime.

The reason deferral is *safe* is that the affected surface is currently two classes — `GameLoop` and `GameEngine`. The cost of an irreversible contract grows with the number of places that assume it, and right now that number is small. Deferring by one phase is cheap; deferring until systems have been written against the current shape is not. That is a real deadline, not a formality.

## Decision

`GameLoop` moves to `HEngine.Runtime` with its current shape unchanged, as part of the module split.

The `Tick(FrameTiming)` reshape follows **immediately** after the module split completes — not in parallel with, and not after, the world-lifetime or component-registry work. In the target shape `GameLoop` becomes two things: `IEngineHost.Tick(FrameTiming)` as the real contract, and `StandaloneLoopRunner` as an optional convenience built entirely on public API, which an editor simply never uses.

## Consequences

- The module split stops being blocked by an architectural redesign. This was the point.
- **`GameLoop` sitting in `Runtime` must not be read as finished.** It will look correct — right module, legal references, green build — while still fusing two responsibilities §4.2 separates. This record is the marker; the reshape is tracked on the board.
- The engine is not editor-drivable until the reshape lands. Nothing about the interim state makes that harder, and the editor is explicitly not being built yet (§6.1) — only its dependency direction and the properties expensive to defer are being protected.
- If the reshape slips far enough that new systems are written against the current shape, this decision should be revisited and the reshape pulled forward, because its cost will have started growing.
