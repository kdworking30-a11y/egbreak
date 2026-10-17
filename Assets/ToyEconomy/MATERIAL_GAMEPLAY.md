# Material gameplay — 17 October 2026

Implemented in the existing Unity 6000.3.10f1 URP project and `ToyEconomy` scene.

## Play

- **Basic, 10 coins:** tap six times. Thin-looking matte, speckled shell; separate pale cut faces.
- **Chocolate, 50 coins:** hold on the shell until pressure builds, then release. A full hold takes 0.65 seconds. Four full breaks open an untouched egg. Short releases retain small progress. Dark tiled glossy surface, lighter cocoa cut faces, heavier debris and lower sound.
- **Crystal, 200 coins:** existing egg retained with twelve taps and a brighter glossy surface. Rhythm/resonance is not implemented yet.
- **Ice, 100 coins:** keep the pointer down and rub across the shell. Local heat clears the frost and dissolves fragments. Stationary holding and ordinary taps do not melt it. At 76% average thaw the remaining shell finishes automatically, avoiding tiny unreachable patches. Reuses the current Crystal toy pool.

**SIMPLE** toggles a six-tap alternative for all materials without changing the reward. Switching modes preserves progress. **SOUND** mutes the generated prototype feedback. The collection overlay and lost focus cancel an unfinished hold safely.

Toy income, spending, preselected reward, one-time unlock, duplicate production/stars, collection and Next remain shared across interactions. Start with the normal 10 coins and starter toy; there are no free test coins in ordinary Play Mode.

## Implementation

`EggMaterialProfile` ScriptableObjects configure interaction kind, outside/inside material, hold duration, minimum partial progress, break count, melt radius/rate, finish threshold, impulse and instruction text. Assets live in `Assets/ToyEconomy/Materials`.

`EggCrackController` supports tap, press/release and per-fragment thermal progress. Ice melting has one time budget per frame so input-event spam does not accelerate it. `EggTapTarget` routes single-pointer gestures and cancellation. `MeltDebris` shrinks detached ice. The URP shader uses procedural surface detail, distinct cut faces, pressure cracks and a local frost/dissolve effect. Ice is an opaque approximation with a dissolve mask, not physical refraction or fluid simulation.

Editor menu: **Tools > Toy Economy > Upgrade Material Gameplay** wires profiles into the currently loaded saved scene. Stop Play Mode first. The upgrade saves its own scene/data changes in the Editor; it does not replace a scene file externally.

## Verification

95 Play Mode checks passed with zero captured runtime errors in an isolated copy using the same runtime code, shader and setup. Checks dispatch Canvas pointer down/up/click and drag events. They cover every egg, holds, early release, a second pointer, collection cancellation, mode switching, stationary-ice rejection, frame-budget clamping, rub completion, exactly one productive reward, Next, six-tap Ice fallback, sound muting and debris cleanup.

The harness adds 1000 test coins to exercise all egg types quickly; this is recorded in its JSON report. Separate real-frame income checks still run. Portrait screenshots are 480 × 900. Results are in `Validation` here and `Benchmarks/ToyEconomy/Materials` in the Unity project.

Not included: wood, jelly, ceramic, metal interactions, Crystal timing, haptics, persistent save/offline progress, recorded final-quality sound, real ice refraction, and mobile device profiling. Those remain follow-up work from the design plan.
