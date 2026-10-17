# Toy Economy + ImageAI fracture

Implemented in `/Users/khoa/Documents/GitHub/My project`, Unity 6000.3.10f1 / URP.
Open `Assets/ToyEconomy/Scenes/ToyEconomy.unity` and press Play.

## Play

Start with 10 coins and Cotton Bunny earning 1 coin/sec. Buy an egg, tap its shell to detach clusters near the touch point, reveal the toy, then press **NEXT EGG** to return to the shop. A toy is selected once when buying; final cracking adds it to production exactly once. Toys continue earning during cracking, reward display and collection browsing.

| Egg | Price | Source fracture pieces | Hits | Reward / income |
|---|---:|---:|---:|---|
| Basic | 10 | 52 | 15 | Cotton Bunny (1/sec), Cotton Dino (2/sec) |
| Chocolate | 50 | 101 | 33 | Toast Cat (5/sec) |
| Crystal | 200 | 200 | 66 | Raincoat Cat (15/sec) |

Open **TOYS** to browse the four collectible toys. Locked toys appear as silhouettes; unlocked toys show owned count and 1–3 stars. Each duplicate raises collection progress up to 3 stars and adds another productive copy. Reward selection uses a shuffle bag per egg pool, matching the source's variety behavior.

## Reuse from imageai

Source project: `/Users/khoa/Documents/GitHub/imageai` (read only during this task).

- Real prefractured meshes and layouts: `Assets/_Egg/EggFragtureMesh/EggFrac50`, `EggFrac100`, `EggFrac200` and corresponding mesh folders.
- Original reward images: `Assets/_Egg/Imgs/rewards/NewCollection` (Cotton 05, Cotton 02, Animal 03, Animal 04).
- Original URP shader: `Assets/_Egg/Shaders/RewardQuadReveal_URP.shader`.
- Adapted flow: `EggPcsMrg` (adjacent chunks, unfreezing, reveal, Next), `UnfreezeFragment` (gravity and 2-second lifetime), `RewardMgr` (preselected reward, shuffle bag), `AchievementManager` (ID-based collection and capped stars), `EggPiecesByLevelSO` (50/100/200 tiers and 15/33/66 hits).

Imported prefabs contain only meshes, renderers and transforms. `ImageAIIntegration` bakes normalized meshes with centered fragment pivots; runtime uses shared asset meshes. Detached fragments receive Rigidbody impulses and torque, use gravity, and disappear after 2 seconds. They do not collide with one another. This avoids dependencies on the source project's ad SDKs, feedback plugins, download service or native fracture DLL.

## Structure

`EggDefinition` and `ToyDefinition` remain ScriptableObjects. Runtime components: `EconomyManager`, `ToyGarden`, `EggShop`, `EggCrackController`, `GameLoopController`, `HUD`, `ToyCollection` and `EggTapTarget`.

`ImportedImageAI` contains source assets and provenance; `FractureBaked` contains normalized reusable prefabs and meshes. Definitions and scene references live in `Data` and `Scenes`.

Editor menu: **Tools > Toy Economy > Integrate ImageAI Fracture and Toy Flow** updates the MVP data/scene using these assets. Stop Play Mode and save any unrelated scene first.

## Validation

The Editor-only Play Mode harness tests actual Canvas raycasts and pointer dispatch, real coin accrual, all three purchase/fracture/reveal/Next loops, exact piece counts after each hit, one-time rewards, duplicate stars, collection navigation and debris cleanup. It is excluded from player builds. Completed results and screenshots are delivered separately alongside this README.

## Remaining scope

Progress is session-only: no save/load or offline income. Source toy artwork is 2D, shown on a URP quad inside the egg and enlarged on unlock. Collection stars are capped at 3; income is based on the number of productive toy copies, not star count. There is no projectile/hammer mode, rewarded ads, downloaded collection catalog, haptics, sound, or source achievement fly animation. Device performance, Android/iOS builds, notched-screen hardware and real touchscreen input still require device testing.

Source naming detail: EggFrac50 actually contains 52 MeshFilters, EggFrac100 contains 101, and EggFrac200 contains 200. Every source chunk is kept so the assembled shell stays closed. Definitions and runtime counters use these actual counts.

## Completed validation (2026-10-17)

Unity 6000.3.10f1 batch Editor, Play Mode, isolated copy of the same scene/scripts/assets: **417 checks passed, zero captured runtime errors**. Verified 52/101/200 source chunks, all 15/33/66 hit sequences, UI raycasts, one reward per egg, Next gating, collection Back, six additional duplicate eggs, three-star caps, live coin production and 2-second debris cleanup. Ended with 10 productive toys after 9 eggs and 32 coins/sec. Portrait renders are 480 × 900. Results and four screenshots are in `ValidationImageAI` in the workspace and `Benchmarks/ToyEconomy/ImageAI` in the Unity project.
