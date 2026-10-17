# EGG Unity runbook

Use this reference for Editor automation, validation or optics changes. Resolve paths from `docs/HANDOFF.md`; verify them locally rather than assuming the previous machine state.

## Editor and scene setup

Unity executable on the original machine:

```text
/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity
```

Main project: `/Users/khoa/Documents/GitHub/egbreak`.
Scene: `Assets/ToyEconomy/Scenes/ToyEconomy.unity`.

If Unity MCP is available, inspect its exposed editor/project resources first, wait for compilation/domain reload, then use its editor/scene/console tools. The package `com.coplaydev.unity-mcp` exists in the main project, but availability of a live MCP connection is not guaranteed.

The current scene is already configured. Rebuilding everything is not necessary for ordinary shader/input changes.

Useful menus under **Tools > Toy Economy**:

- **Create or Open MVP**: initial/open behavior; does not recreate all later features by itself.
- **Integrate ImageAI Fracture and Toy Flow**: earlier source mesh/data setup; can reset legacy hit settings.
- **Upgrade Material Gameplay**: configures the four eggs and input profiles. It assigns the baseline material shader, so run the optical upgrade afterward if using it on an optical scene.
- **Upgrade Reflection and Refraction**: configures latest optical shaders, cubemap, per-camera texture overrides and backdrop.

Setup menus check that Play Mode is stopped and the current scene is clean before applying their changes. They save the scene/data they modify. Prefer these tools over overwriting an open `.unity` file from an external copy. Keep unrelated unsaved user edits; do not blindly Reload/Discard or treat historical permission as permission to lose new work.

The Mac can lock while files/tests remain accessible. UI automation then needs the user to unlock it; do not try to bypass the lock. Independent read/edit/batch work can continue when authorized.

## Batch validation without taking over the main Editor

Never launch a second Unity instance against a project currently open in another Editor. Use an isolated copy, retaining `Assets` and `.meta`, `ProjectSettings`, and package configuration. Skip `Library`, `Temp` and `Logs`; use cache/references only if needed and understood.

A previous copy exists at `/tmp/material-gameplay-validation`. It is disposable and may be stale; compare it to main source before reuse. Do not copy it wholesale back over the main project. The copy used a reduced package manifest to avoid unrelated SDK dependencies; preserve needed built-in modules such as audio, image conversion, UI, physics and screen capture.

### Optical render checks

The entry point expects material gameplay to be configured. Archive/remove the prior `/tmp/optical-material-verification.json` before launch: the batch monitor otherwise sees a stale report and can exit early.

Example, with an isolated copy:

```bash
'/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -projectPath '/tmp/material-gameplay-validation' \
  -executeMethod ToyEconomy.Editor.OpticalBatch.Run \
  -logFile '/tmp/optical-material-current.log'
```

Do not add `-quit`: the Editor launcher enters Play Mode and exits when its report is written. Do not add `-nographics` for image verification.

Output: `/tmp/optical-material-verification.json`, `/tmp/optical-{basic,chocolate,crystal,ice}.png`, plus effect-off images.

The last version has 21 checks. Inspect `passed`, `errors`, timestamps and the actual images. It compares mean absolute RGB differences in the egg area when reflection or refraction is disabled; passing a threshold proves a render path changes pixels, not that the material is aesthetically finished.

### Gameplay regression with optics active

```bash
'/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity' \
  -batchmode \
  -projectPath '/tmp/material-gameplay-validation' \
  -executeMethod ToyEconomy.Editor.OpticalBatch.RunRegression \
  -logFile '/tmp/optical-gameplay-current.log'
```

This calls the optical upgrade, clears the old material report and runs the 95-check material gameplay suite. Output: `/tmp/material-gameplay-verification.json` and material screenshots. The harness adds test coins; normal startup does not.

`MaterialGameplayBatch.Run` is the earlier baseline route and calls the baseline material setup. Use `OpticalBatch.RunRegression` to validate the current optical scene.

### Process and report pitfalls

- A report from a previous attempt is not a fresh result. Check its modification time and errors.
- Some local batch Editors stayed alive after writing a final report/Exit request. After preserving the report, identify and close only that exact isolated test process if necessary. Do not kill all Unity processes or delete a live project lock file.
- `ToyEconomySetup` polls the shared file `/tmp/toy-economy-command.txt`. Multiple Editors can consume each other's commands. Prefer MCP or the specific main Editor's menu. Do not rely on this shared command file until test Editors are closed and the target instance is unambiguous.
- Editor cloud/telemetry connection messages occurred in this environment. Distinguish them from compile, shader and gameplay exceptions. Do not suppress genuine runtime errors to make the harness pass.

## Optical invariants and knobs

- Camera `UniversalAdditionalCameraData.requiresColorTexture` must remain enabled. Camera-specific overrides avoid changing unrelated pipeline assets; the Mobile URP asset's global opaque-texture default was off.
- The reward material uses queue 2450 so its image is in the opaque color copy. Moving it back to the default transparent queue makes the glass sample a background without the toy.
- Glass is rendered after the opaque copy with `ZWrite On`; it samples the copy and composes transmission/reflection. HUD remains later/outside this path.
- `_EnvironmentCube` is actual HDR cubemap data with roughness mips. `_ReflectionStrength` affects reflected environment; `_RefractionStrength` affects offset scene sampling.
- `_IOR`, `_Thickness`, `_Absorption`, `_Roughness`, `_Dispersion`, `_Facet` are material controls. `_EggCenter` is set on runtime material clones; `_Heat` comes from per-fragment melt state.
- Internal fracture cut planes shared by attached cells are suppressed by OpticalCutFaceVisibility. Do not enable every interior plane globally after the first hit. Glass and visible reward pixels write depth to prevent rear cells drawing over nearer surfaces. Reward material sets _ZWrite=1; the shader clips zero-alpha pixels to avoid a rectangular depth mask. Optical _Thickness and _Density now control visual mass; this does not remesh the cells.
- Refraction uses the smoother original normal while reflections can be faceted. Applying strong facet normals to the lens made the toy appear as repeated, unreadable eyes/faces.
- StudioEnvironment is generated by the editor setup if absent. Editing the generation recipe alone does not regenerate an existing asset. Preserve or intentionally rebuild the asset and update references through the Editor.

## Reliable gesture tests

`MaterialGameplayVerification` sends Canvas pointer down/up/click and drag events. A click-only callback is not equivalent to a real press/release gesture. Wait for layout/activation after opening an overlay or switching reward UI before raycasting its next button.

Important behavior already covered: pressure clamping, partial early-release progress, secondary pointer isolation, cancel-on-collection, SIMPLE mode switching, no melt from a stationary finger, per-frame melt budget, one reward, Next and debris cleanup.

Create `MaterialPropertyBlock`, textures and other Unity native objects in `Awake`, `Start` or a valid method; not in a MonoBehaviour field initializer. Standard managed lists/dictionaries are fine there.

## Finish a change

Keep source and `.meta` together. Save validation artifacts to a new or clearly updated folder under `Benchmarks/ToyEconomy`; don't present historical 417-check tap tests as validation of new hold/rub code. Reopen normal Play Mode without a verification hook/test coin injection for user review. Update the handoff with real results and remaining gaps.

Skill format/invocation reference, checked 17/10/2026: [Claude Code skills](https://code.claude.com/docs/en/skills).

## Idle progression verification

Run `ToyEconomy.Editor.IdleProgressionBatch.Run` in the isolated batch project. Report: `/tmp/idle-progression-verification.json`; three `/tmp/idle-*.png` captures. Latest run: 93 checks. The harness disables player saves by default and exercises disk IO via an explicit `/tmp/egbreak-idle-save-verification` override.

Material and optical suites now set `Progression.VerificationBypass=true` in Editor only to exercise their original four eggs without grinding milestones. The dedicated idle suite tests real gates. Do not enable this bypass in a player's normal session. Tool levels remain zero in those two regression suites.

Normal Play Mode now persists real player progress and credits offline income (cap 4h). Avoid mixing injected test coins with ordinary saves. Never delete player progress just to restore a clean screenshot. Scene rebuild is unnecessary; idle UI and managers initialize at runtime.
