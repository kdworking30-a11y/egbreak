# Idle progression and tools — 18/10/2026

## Play loop

Productive toys earn coins continuously. Buy an affordable unlocked egg, open it with its material's interaction, receive one productive toy and return to the shop. Show the next egg unlock and current income at all times. Each toy pool prioritizes a currently unowned toy when one remains in its shuffle bag. Duplicates are additional productive friends; collection stars stay capped at three and do not add a second multiplier.

No real-money shop, paid randomness, streak penalty, countdown pressure or loss for an incorrect gesture is included. These are first-pass tuning values, not a playtest-validated age-specific balance.

## Egg milestones

| Egg | Completed eggs required | Price | Input | Reward base income |
|---|---:|---:|---|---:|
| Basic | 0 | 10 | Tap | 1–2/sec |
| Chocolate | 3 | 50 | Hold / release | 5/sec |
| Ice | 6 | 100 | Rub / melt | 15/sec |
| Crystal | 10 | 200 | Tap | 15/sec |

Every three completed eggs adds 10 percentage points to production by all owned toys. Maximum +100% at 30 completed eggs; this is applied once to the sum of toy production. Afterward the goal text invites further collection, without promising unavailable boosts. Idle income continues while interacting with eggs, browsing tools, seeing rewards and viewing collection.

## Permanent tools

Tools are automatically active after purchase; no equip or consumable management.

| Tool | Gate | Effect per level | Max level | Upgrade prices (levels 1–5) |
|---|---:|---|---:|---|
| Soft Hammer | 0 eggs | +25 percentage points tap power | 5 | 20, 45, 90, 160, 260 |
| Cocoa Gloves | 3 eggs | +20 percentage points charge speed | 5 | 30, 68, 135, 240, 390 |
| Warm Cloth | 6 eggs | +25 percentage points melt speed | 5 | 40, 90, 180, 320, 520 |

Hammer affects Basic and Crystal. Gloves shorten charging without removing hold/release. Cloth keeps local rubbing and the per-frame heat budget; stationary fingers still do not melt the shell. SIMPLE stays six taps with the same reward. Level-one Hammer reduces Basic from six taps to five. Max Hammer power is x2.25, Gloves x2 and Cloth x2.25.

The tool shop displays wallet, passive income, current level/effect, next effect, price or lock requirement. Upgrades are only available in Shop state. Busy/reward states, insufficient funds, locked tools and level five all reject purchases without spending.

Tools in this MVP are upgrades in the shop and their interaction effects; there are no animated handheld tool models or final tool icons yet.

## Local progress

`IdleSave` writes `egbreak-progress-v1.json` under Unity's persistent data directory, keeping a `.bak` through replacement of the prior file. Autosave every 15 seconds and on egg/tool purchase, egg completion, application pause and quit. Validates version, IDs, counts, wallet and tool ranges before mutating state. Corrupt current saves can recover from the backup.

Saves owned toy counts, coins, completed eggs, tool levels and UTC timestamp. Offline credit is saved production times elapsed seconds, capped at four hours; a backward clock credits zero. Resume after suspension also credits elapsed time. Unfinished eggs restore to Shop and refund their exact paid price, without granting a toy. In-progress cracking and shuffle-bag order are not resumed.

Batch verification disables ordinary save loading/writing. File IO tests use an explicit `/tmp/egbreak-idle-save-verification` override so they cannot modify player progress.

## Verification and remaining work

- Idle test entry: `ToyEconomy.Editor.IdleProgressionBatch.Run`.
- Material regression: `ToyEconomy.Editor.OpticalBatch.RunRegression` (verification-only gate bypass; no tools purchased).
- Render regression: `ToyEconomy.Editor.OpticalBatch.Run` (verification-only gate bypass).
- Reports and screenshots: `Benchmarks/ToyEconomy/IdleProgression/`.

No device profiling, kid playtesting, cloud sync, localization, new toy art or production audio in this change. The garden remains a production summary and collection cards. Four reward designs are available. The same-material Crystal request was left at clarification; this expansion preserves the prior optical materials and depth fixes.
