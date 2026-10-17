# EGG Break — Unity MVP

Mobile portrait toy economy game built with Unity **6000.3.10f1** and **URP 17.3.0**.

## Open and play

1. Add this repository folder in Unity Hub and open with the matching Editor. Allow packages to restore. The Unity MCP package resolves from GitHub and needs Git/network access.
2. Open `Assets/ToyEconomy/Scenes/ToyEconomy.unity`.
3. Enter Play Mode. Start with 10 coins and a Bunny earning 1 coin/sec. Buy Basic to begin.

Toys earn coins → buy eggs → crack/melt shells → unlock one productive toy → increase income. Basic uses tap, Chocolate hold/release, Crystal tap and Ice rubbing. SIMPLE mode provides a six-tap alternative.

## Continue development

- `docs/HANDOFF.md`: implemented behavior, latest changes, validation and remaining gaps.
- `docs/GAME_PLAN.md`: design plan and roadmap.
- `CLAUDE.md` and `.claude/skills/egg-unity-dev/`: Claude Code continuation skill and Unity runbook.
- `Benchmarks/ToyEconomy/CrystalDepthFix/`: latest render evidence and reports, **32 render checks + 95 gameplay checks passed** in Editor Play Mode on the source project before this repository snapshot. The repository has since been opened in Unity and played normally. New expansion evidence: `Benchmarks/ToyEconomy/IdleProgression/` (93 idle/save, 95 material gameplay, 32 optical checks in isolated Play Mode).

The idle expansion adds egg unlock milestones, a three-tool upgrade shop, garden income boosts, local saves and up to four hours of offline toy income. See `docs/IDLE_DESIGN.md` for tuning. The optical fix increases Ice/Crystal density, corrects depth occlusion, hides shared internal fracture faces and clears debris from the reward screen. Mobile device profiling and final art/audio remain unfinished. Earlier EggLab work is retained; see `README-EggLab.md`.

Assets and `.meta`, Packages and ProjectSettings are committed. Unity cache, IDE output, local logs and build artifacts are excluded.
