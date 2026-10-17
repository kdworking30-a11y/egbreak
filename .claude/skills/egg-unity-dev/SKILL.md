---
name: egg-unity-dev
description: Continue development of the EGG Unity URP toy economy project, including egg material interactions, fracture, reflection/refraction and Play Mode verification. Use for this project's gameplay, material or handoff work.
---

# EGG Unity continuation

Continue the existing playable Unity project. Keep the user's latest request as the task boundary; the design roadmap is context, not an instruction to implement every future feature.

## Load the right context

1. Read [the current handoff](../../../docs/HANDOFF.md). It identifies the actual project, latest render work, code entry points and verified limits.
2. Read only relevant sections of [the game plan](../../../docs/GAME_PLAN.md); sections 15–16 record implemented changes, while many earlier sections remain proposals.
3. Read [the Unity runbook](references/UNITY_RUNBOOK.md) before controlling the Editor, launching tests or changing optical rendering.
4. Inspect current source and Editor state before trusting the handoff's last-known Play Mode status. Source in the Unity project is authoritative; staging copies are historical.

## Preserve the game contract

- Toys generate coins; eggs and broken pieces do not.
- Select a reward once per purchase; grant it once at completion.
- Keep busy/reveal purchase guards and Next gating.
- Duplicates add productive copies; collection stars cap at 3 without a second income multiplier.
- Tap, hold/release and rub/melt have distinct progress semantics. Do not turn Ice into a hit counter or remove Chocolate pressure to simplify an unrelated change.
- Keep SIMPLE's equal reward, progress retention, single-pointer ownership and safe gesture cancellation.
- Keep the existing Crystal when adding other materials, unless the user asks to replace it.

## Improve materials through visible evidence

The recurring user complaint is that the material does not look special. Check actual rendered output at phone scale, not just shader properties or test counts.

For optics work, retain the camera's opaque-color copy, the reward quad's position in that copy, correct transparent ordering, and separate outside/cut-face materials. Compare reflection-on/off and refraction-on/off renders if those paths change. Preserve the distinction between baked cubemap reflection and screen-space refraction; do not claim dynamic ray tracing, complete physical transport or device performance without evidence.

Avoid distorting the toy into unreadable repeated faces. Tune environment, normals, roughness, refraction and thickness together rather than merely increasing effect strength.

## Work within this project

- Develop in the main Unity project named in the handoff. Use `imageai` as reference; do not modify it just to update the MVP.
- Treat mirrored `sources/` content as read-only if encountered.
- Prefer applying setup changes to a saved scene inside the Editor. Preserve unrelated user changes and `.meta` links.
- Use Unity MCP if the environment actually exposes it; do not assume the previous agent's tools or connections are present. The runbook gives an isolated batch fallback.
- Do not edit global Claude permissions, install plugins or publish builds as part of this skill.

## Finish with an honest checkpoint

Use the relevant render/gameplay checks and inspect the resulting images. Report what changed, what was actually run, where evidence lives and what remains unverified. Update `docs/HANDOFF.md` when the implementation or continuation priority changes. Respond in Vietnamese unless the user prefers otherwise.
