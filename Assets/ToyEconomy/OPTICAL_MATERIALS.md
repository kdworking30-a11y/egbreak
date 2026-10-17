# Optical materials — 17/10/2026

Updated the existing ToyEconomy URP scene in `/Users/khoa/Documents/GitHub/My project`.

The preceding material pass only used a direct-light highlight and opaque frost/dissolve. This pass replaces the optical path for Ice and Crystal and supplies an environment for reflective materials.

## Rendering changes

- HDR studio cubemap with softbox light panels, mipmapped for surface roughness. Shared by glass and solid surfaces; also set as the scene's custom reflection environment. This is a baked studio environment, not a realtime capture of moving toys.
- Separate transparent `ToyEconomy/OpticalGlass` shader. Fresnel reflection samples the cubemap with the reflected view vector. Screen-space transmission samples URP's opaque color texture with a refracted direction, IOR, optical thickness and wavelength-dependent offsets.
- IOR: Ice 1.31, Crystal 1.52. Volume tint uses an absorption/thickness approximation; no full ray-traced volume transport.
- Ice: rounded normals, rough frost blur that decreases with the existing per-fragment thaw value, subtle dispersion.
- Crystal: sharp faceted reflection normals, continuous underlying lens normal to keep the toy recognizable, stronger refraction and dispersion.
- Chocolate: roughness-controlled reflected studio light with a less reflective cocoa cut face. Basic stays matte.
- Per-camera URP opaque/depth texture overrides enabled. No global pipeline asset needs to be changed.
- Reward quad renders in the opaque-copy phase so it can appear inside the refracted image. HUD remains outside the refracted image.
- Opaque studio backdrop provides contrasting geometry/color detail to judge distortion and a contact-shadow cue.
- Closed glass shells suppress internal Voronoi cut planes until cracking begins; otherwise the original solid fracture cells would obscure the optical surface.

## Verification

**21 render checks passed**, zero captured runtime errors. Captured 480 × 900 images with reflection enabled/disabled and refraction enabled/disabled. Mean absolute RGB differences over the egg region were nonzero and above the test threshold for both effects:

| Effect | Crystal | Ice |
|---|---:|---:|
| Reflection on/off | 0.04429 | 0.01870 |
| Refraction on/off | 0.02767 | 0.01344 |

**95 gameplay regression checks passed** with the optical setup active, zero captured runtime errors. Tap, hold/release, rub/melt, control switching, reward-once, income, collection/Next and cleanup remain functional. Test harnesses use extra test coins; normal Play Mode still starts with the existing wallet.

Results and A/B screenshots: `Validation` here; in the Unity project, `Benchmarks/ToyEconomy/Optics`.

## Editor setup

Stop Play Mode and use **Tools > Toy Economy > Upgrade Reflection and Refraction**. It configures the currently saved ToyEconomy scene and existing material profiles in the Editor. Starting from a fresh initial project, run the existing fracture/material gameplay setup first.

Screen-space refraction can only use content present in URP's color copy. It does not trace offscreen objects or nested refractive volumes. Reflections use the baked studio environment. Transparent fragment overdraw, texture-copy cost and device appearance still need mobile profiling. Input/game economy were not redesigned in this pass.
