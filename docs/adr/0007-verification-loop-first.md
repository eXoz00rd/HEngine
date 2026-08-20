# 0007 — The verification loop is built before the work that needs it

**Status:** Accepted (2026-08-20)
**Relates to:** `TARGET_ARCHITECTURE.md` §4.3, §7.4, §8.5 · issue #51

## Context

`TARGET_ARCHITECTURE.md` §7.4 describes a closed verification loop: change → build → module tests → headless render to texture → compare against a reference image. Its stated purpose is that a contributor — human or agent — can confirm their own rendering change instead of needing someone to look at a window.

The pieces that make it work are `TextureTarget` (§4.3), `HEngine.Testing` (§3.2), and `frame.capture`/`frame.compare` (§8.5). In the migration plan these sit in the presentation phase and the MCP phase — that is, **after most of the rendering work that would benefit from them.**

The consequence is observable rather than theoretical. Every rendering change in this repo so far has been verified by running the demo host and reading console output: frame counter, mesh count, FPS. That confirms the engine did not crash and is drawing roughly the expected number of things. It cannot detect a wrong colour, an inverted normal, a shadow in the wrong place, or a post-process stage silently doing nothing — which is precisely the class of defect that rendering work produces. `CONVENTIONS.md` already acknowledges this by requiring a `needs visual check` label, and the migration plan carries that label on more than a dozen tasks.

So the ordering has a cost that compounds: the largest remaining rendering task (splitting the D3D12 backend from the abstraction layer) is also the one where an undetected visual regression is most likely, and under the planned order it lands with no automated way to detect one.

There is a second argument, from §8.2. The MCP surface is described there as the cheapest available test of whether the public API is real — a capability that cannot be exposed as a tool probably does not properly exist. Building the capture/compare slice early exercises `TextureTarget` and the presentation-target split against a real consumer, rather than assuming they are adequate and finding out later.

## Decision

`TextureTarget`, `HeadlessTarget`, `HEngine.Testing` and the `frame.capture`/`frame.compare` slice of the MCP surface are built **immediately after the module split completes**, ahead of the frame-contract reshape, the world-lifetime work and the component registry.

The rest of the MCP surface stays in its planned position — those tools depend on APIs that genuinely do not exist yet.

## Consequences

- The presentation-target split lands earlier than planned. It does not depend on the frame contract or the registry, so this is a reordering, not a dependency violation.
- Reference images become load-bearing early, which raises the stakes on how the first ones are produced. **Every first reference image must be reviewed by a human before it is accepted** — an unreviewed baseline cements a wrong render as ground truth for every later comparison, including automated ones. This is the one step in the loop that cannot itself be automated.
- Rendering tasks after this point can carry a real Definition of Done instead of "console output looked plausible."
- The backend split gets an automated regression check for the change most likely to need one.
- Some of `HEngine.Testing` will be built before every consumer exists, so parts of its shape are provisional and may move when the remaining MCP tools arrive.
