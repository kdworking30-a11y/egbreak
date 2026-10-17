# Egg & goo — mobile URP prototype

Open `Assets/Scenes/EggLabPrototype.unity` and press Play. The scene can also be opened from **Tools > Egg Lab > Create or Open Prototype**.

- Tap/click the egg five times. Cracks grow along the curved fragment boundaries; the fifth hit releases 24 physical shell pieces.
- Hold the left mouse button (or a touch) on the slime to dent it, then move while holding to stretch it. Release to watch a damped spring return it to rest. Picking tests the actual deformed mesh on pointer-down, so shell debris cannot steal the grab. In the Editor, interact inside the Game view while Play is running.
- **New Egg** reuses the existing fragment objects and mesh buffers.
- **Benchmark** runs four 5-second scenarios: intact shell, cracked shell, repeated fractures, slime drag. The first two frames after starting or resuming an Editor pause are ignored to avoid stale Editor delta times. Each phase skips 2 seconds for warm-up and records the remaining 3 seconds. Reports go to `Application.persistentDataPath/EggLabBenchmarks`; the full path appears in the console. No data is uploaded.

## Scope

Unity 6000.3.10f1, URP 17.3.0, Input System. Runtime uses the existing **Mobile_RPAsset** through a scene-owned reference (the Editor quality label can remain PC), a 60 FPS cap, one directional light, opaque jelly, and a cheap contact disk. Original pipeline, quality, frame cap, vSync and background setting restore when the prototype is destroyed. Portrait is configured for mobile and the prototype is first in Build Settings.

Shells now use spherical Voronoi cells mapped onto the egg, with curved outer/inner surfaces and real cut rims. The cut layout and convex colliders are generated at egg creation or when changing piece count, not at the instant of impact. Reset reuses the existing cells. The 425-vertex slime uses three bounded spring substeps, local grab deformation and a floor clamp; it approximates soft-body behavior without fluid physics, mass conservation or self-collision. Jelly shading fakes translucency and highlights with an opaque single pass. Fragment physics ends visually after seven seconds; reset reuses all 24 objects. The slime has a coarse trigger sphere; shell debris does not collide with it. Its surface uses a visual floor constraint, and deformation does not rebuild a solid physics collider. Pointer picking uses the actual deformed triangles once per grab, independently of the physics sphere. Exact soft-body collision remains outside this prototype.

## Benchmark interpretation

CSV measures **frame pacing** from unscaledDeltaTime. Summary includes device, GPU, resolution, render pipeline, frame cap, samples, mean, nearest-rank p95, max, FPS and percentage above 16.667 ms. These are not separate CPU/GPU timings. Editor results include Editor/MCP overhead and are not proof of phone performance. Fracture spikes during per-phase warm-up are excluded; the measured repeated-fracture scenario includes subsequent releases. No heap allocation-free claim is made: Unity normal updates, physics and IMGUI require device profiling.

For mobile:

1. Install the matching Unity Android/iOS build-support modules. Build this scene to physical Android and iPhone devices. Start with a mid-range Android, an older supported iPhone, then a higher-end device.
2. Run a Development Build with Unity Profiler attached to inspect CPU, GPU (where supported), GC Alloc, Physics and Rendering. Capture around the fifth tap and a long slime drag. Use a normal non-development build for frame-pacing acceptance.
3. Run Benchmark at the actual device resolution three times after a one-minute warm-up. Then repeat after 10 minutes of sustained use to reveal thermal throttling. Record device/OS, build type, refresh rate, resolution and quality alongside exported files. Compare identical build settings.
4. Target 60 FPS / 16.67 ms; treat p95 > 20 ms or frequent missed frames as investigation thresholds, not automated pass/fail. For a 30 FPS tier use a 33.33 ms budget; the current exported percentage column remains explicitly a 60 FPS threshold.
5. If fracture is CPU-heavy: reduce pieces (6×3), disable shell-shell collisions with a dedicated physics layer, or shorten fragment lifetime. If deformation is CPU-heavy: reduce sphere mesh resolution or normal update frequency. If GPU-heavy: lower render scale and inspect overdraw. Never infer these bottlenecks solely from the on-screen FPS label.

No Android/iOS build or on-device performance claim is made until a physical device run is recorded.

## Verified locally

- Input System virtual-mouse regression passed: held press, held move, release, reset. Result: `Captures/EggLab/input-smoke-test.json`. This validates game input events, not an Android/iOS touch-device run.
- Editor benchmark on Apple M1 Pro with Mobile_RPAsset at 538×1075, 60 FPS cap: intact 60.0 FPS / p95 18.351 ms; cracked 59.8 / 18.513 ms; repeated fracture 60.0 / 18.655 ms; slime drag 60.0 / 18.547 ms. Raw and summary files are in `Benchmarks/`. No physical phone benchmark has been performed.
- Editor input uses the Game camera pixel viewport to avoid confusing Scene-view Screen dimensions with Game-view mouse coordinates.

## Easter + two-finger update

- The egg starts painted in PETAL pastels. Tap **EASTER / PETAL** above it to cycle PETAL → LAGOON → PEACH → PLAIN. Splotches, polka dots, chevrons and stripes are procedural in the shell shader; no texture download is required. Every broken shell piece keeps its paint.
- On a touch device, press two different points on the slime. Move apart to stretch, together to pinch, or rotate the line between your fingers to twist the surface. Each grab retains its touch ID; lifting one finger leaves the other grab active. Canceled touches and loss of focus release their grabs.
- Desktop preview: hold **Shift + left mouse**, beginning slightly to one side of the slime. The second grab mirrors the mouse around the slime center. Move while holding to preview paired pulls; release Shift to continue with one grab. The plus markers show both anchors. This is a symmetric preview, not two independent desktop cursors.
- Automated Input System virtual-device checks passed for mouse hold/drag/release, two touch IDs with one-finger release/cancel, palette cycling, and Shift + mouse. JSON reports live in `Captures/EggLab/`. Physical Android/iOS multitouch still needs device testing.
- Benchmark's last phase now exercises two anchor fields and is labeled `two_finger_slime`; earlier `slime_drag` reports describe the preceding single-anchor version.

Latest Easter/two-anchor Editor run: `Benchmarks/20261014-145602-summary.txt` (336×671). Two-anchor phase: 60.0 FPS, p95 18.207 ms. Repeated fracture: 54.7 FPS, p95 23.839 ms, max 198.756 ms; this observed spike needs device Profiler investigation and is not hidden by the mean. This run uses a different resolution from the earlier report, so the runs are not a controlled before/after comparison.

## Shell piece size

Use **PIECES / 48** above the intact egg to cycle **24 → 48 → 96** fragments. More pieces means smaller shell fragments. The default is 48; 96 is the finest option. Changing the setting rebuilds just the shell and starts a fresh egg, keeping the Easter palette. You can also choose **Fragment Size** on the Egg Lab component in the Inspector before Play and save the scene for a persistent default. Runtime button changes last for the current Play session. Benchmark records the actual fragment count; comparing counts should use identical device and resolution settings.

## Crack-to-fracture alignment

Cracks are masked by outer-shell UV2 boundaries only. Decorative interior crack branches were removed because they did not correspond to actual cuts. Inner and rim vertices have an explicit zero crack mask; rims use separate vertices. Shared outer normals come from the same egg surface function, preventing lighting seams on an intact egg. Geometry checks passed for 24/48/96 pieces with zero unpaired boundary positions and identical normals at shared edges (`Captures/EggLab/fracture-seam-test.json`). Easter stripes and chevrons are paint and do not indicate fracture lines.

## Voronoi fragment update

The rectangular cut lattice has been replaced by a spherical Voronoi partition. Jittered Fibonacci sites create irregular 4–8-sided pieces at the tested presets. Gnomonic half-plane clipping constructs each cell; canonical triple-site intersections and shared edge sampling keep neighbors joined. Shader UV2 stores distance to the same cell boundary, so painted cracks agree with the fracture. Radial rings curve the surface, and separate cut-face vertices provide shell thickness. Easter paint remains continuous across fragments.

Choose 24/48/96 with PIECES. In the Inspector before Play, **Fracture Seed** changes the repeatable pattern, and **Fracture Irregularity** changes site jitter. This is a precomputed surface fracture approximation; it does not solve material stresses or propagate cracks physically. Total shell rigidbody mass stays constant across presets.

Validation: topology passed 3 counts × 3 seeds (all shared edges have two owners, Euler characteristic 2, total spherical area 4π). Mesh validation passed for 24/48/96 (no zero-area triangles, shared-edge mismatch below 0.00001 units, shared normals agree, and all fragments activate physics). JSON evidence: `Captures/EggLab/voronoi-topology-test.json` and `voronoi-mesh-test.json`. Earlier grid-fracture benchmark and seam reports are historical results; use the new Voronoi reports for this version.

Voronoi version Editor benchmark (48 pieces): `Benchmarks/20261014-162018-summary.txt` and matching raw CSV. A 96-fragment live physics check passed with all body positions finite and no shell falling through the ground at the sampled time (`Captures/EggLab/voronoi-physics-test.json`). This is a short smoke test, not exhaustive physics or mobile performance validation.

## Slime filling and spill

The same 425-vertex slime mesh starts active inside the egg, scaled from its inner egg profile rather than spawned already flat on the dish. It follows the intact egg wobble. On the fifth tap, the root follows a gravity-shaped drop, the lower material spreads first, and the upper material follows into a softly lobed puddle. The eye positions follow surface vertices. An explicit floor constraint prevents the visual mesh from penetrating the dish.

Inspector settings: **Slime Fill** (0.55–0.82, default 0.76) and **Spill Duration** (0.7–2.5 seconds, default 1.4). Set before Play. Reset restores the contained shape. Grabbing unlocks after the spill completes; the prompt shows “Let it flow...” during the transition. Mouse, Shift preview and two-finger gestures then work on the spread mesh.

This is a procedural viscous-flow approximation using shape targets and spring deformation, not particle-based liquid simulation, a fluid pressure solver, or exact volume conservation. Shell/slime collision is not simulated; debris is prevented from hitting the coarse trigger volume.

Containment/spill smoke test passed: all 425 vertices fit inside the shell with >0.20 unit clearance at the default fill, zero instant spawn jump, continuous downward movement, floor y ≥ 0.016, and a wider/flatter settled shape. Evidence: `Captures/EggLab/slime-spill-test.json`.

## Liquid squish + interactive audio

The surface now uses welded neighbors, coupled displacement springs, viscous velocity smoothing, a delayed pull/release field, soft volume-pressure correction and decaying ripples at grab/release. The new wet shader adds a broad sheen, a tight highlight, Fresnel reflection and lighter thin edges. These remain mobile-oriented surface approximations, not a particle liquid or an incompressible fluid solver. Floor contact and packed-inside-egg startup are retained.

On the **Egg Lab** component, tune **Spring Stiffness**, **Viscosity**, **Surface Tension**, and **Stretch Limit**. Lower stiffness is softer; higher viscosity is slower/heavier; tension couples nearby surface motion. Force integration is bounded to three substeps with a velocity cap; duplicate seam and pole vertices are welded to avoid visible tears.

Seventeen original, locally synthesized stereo PCM clips across eight effect families live in `Assets/EggLab/Audio`: tap, crack, shell break, squish, release, pour, liquid loop and bubble. They contain no third-party recordings. The **Egg Lab Audio** component holds replaceable clip references and **Master Volume**. **SOUND ON/OFF** in the HUD mutes game effects without changing the global listener. Unity Editor preview audio has been unmuted so the effects are audible in Play mode.

Wet-loop volume and pitch respond to grab motion and strain; bubble events have a cooldown. Reset stops all sound, mute stops current voices, and the ASMR pool is capped at eight one-shot voices (two reserved for break/pour) plus one loop. Per-source gains leave mixing headroom. Clips are preloaded/decompressed PCM for short-effect latency. The generator is retained at `Tools/GenerateEggLabSounds.py`; running it regenerates the eight WAV files in this project’s audio folder.

Checks: waveform amplitudes are nonzero and individually below clipping (`Captures/EggLab/audio-waveform-test.json`), actual pointer-event regressions cover hold/pull/release and two touches, and audio tests cover playback, wet-loop activation, mute and reset (`Captures/EggLab/liquid-audio-test.json`). Synthetic input tests do not replace physical phone testing or listening on target speakers.

Liquid/audio version Editor benchmark: `Benchmarks/20261015-153857-summary.txt`, 24 fragments, 574×1149, Mobile_RPAsset. Each phase averaged 60.0 FPS; the two-finger phase p95 was 18.512 ms. This is a frame-pacing observation on this Mac, not an on-device CPU/GPU profile or a guarantee of mobile performance.

## ASMR build-ready profile

The latest audio set is **ASMR-1**: 17 close-up stereo clips across tap, crack, shell break, wet squish, release, pour, continuous liquid friction and bubble families. Colored noise and small damped liquid grains replace the older long synthetic tones. Squish/release/tap/bubble clips have multiple takes; immediate repeats are avoided. Shell transients are softened, loop pitch modulation is narrow, and fades reduce abrupt changes in the continuous texture. The stereo field subtly follows the mean grab position; this is ordinary stereo, not a measured binaural recording.

**ASMR ON/OFF** and the slider at top right control this game's audio. Volume and mute are stored with versioned PlayerPrefs keys and restored on later player launches. **Master Volume** and **Stereo Width** are also available on Egg Lab Audio in the Inspector. Existing foley voices are not abruptly stolen when the pool is full; break and pour have reserved voices. Reset/mute stop current sounds.

The clips are serialized directly into the enabled prototype scene, preloaded as stereo PCM with explicit Android/iPhone overrides. The set uses about **1.82 MiB of decoded PCM**. An Editor-only pre-build check validates scene dependencies, nonempty stereo clips, preloading and mobile import settings. Run **Tools > Egg Lab > Validate ASMR Build Audio** manually if desired. The player needs neither MCP nor a network connection for these clips. The validation does not replace a physical-device listening test. No APK or Xcode project was exported for this request.

Evidence: `Captures/EggLab/asmr-waveform-test.json` checks all 17 clips for stereo content, nonzero RMS, bounded peaks, clean one-shot endpoints and a non-discontinuous loop boundary. `Captures/EggLab/asmr-runtime-test.json` checks variation selection, wet-loop playback, subtle pan, voice saturation, mute and preference reload. `Captures/EggLab/ASMR-preview.wav` is a 14.2-second offline listening montage, peak-normalized for preview; its loudness is not a recording of the in-game master setting. The original procedural generator and source manifest remain in `Tools/GenerateEggLabSounds.py` and `Assets/EggLab/Audio/ASMR-audio-manifest.json`.

### Egg tap regression fix

Egg taps now raycast directly against cached shell colliders. The contained slime trigger can overlap the shell and must not consume taps. The cache is rebuilt when the fragment preset changes. Input System mouse press/release regression verified five taps break the egg for all three presets (24/48/96); report: `Captures/EggLab/egg-pointer-hit-test.json`.
