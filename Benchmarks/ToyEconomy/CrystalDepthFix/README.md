# Optical shell thickness and crystal occlusion

- Optical shell writes depth while composing refraction from the opaque color copy. Nearest surfaces win even when fragment sorting order changes.
- Reward writes depth only at visible artwork pixels, preventing rear shell/cuts from drawing over the toy without an invisible rectangle masking the shell.
- Volume density and higher optical thickness make intact Ice/Crystal conceal the toy. Ice density clears with per-fragment heat.
- OpticalCutFaceVisibility pairs coplanar Voronoi cut triangles and hides them while both adjacent cells remain attached. Detachment restores that cell's cut mesh and refreshes exposed faces on remaining cells. Mesh clones have runtime lifetime owners.
- Completing any egg disables debris renderers so the reward screen is clear; physics and scheduled cleanup remain active.

This is increased optical density/path thickness, not a remesh of the original fracture cells. Coplanar matching uses quantized planes and should be revalidated if replacing source fracture meshes. New runtime mesh copies and cut updates need device profiling.
