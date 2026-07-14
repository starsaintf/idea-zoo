# Idea Zoo complete art package

This directory is the authoritative character and creature design handoff.

- `DESIGN_BIBLE.md` — visual direction, cast, creature families, rigs, budgets and acceptance rules.
- `production_manifest.json` — machine-readable asset list and family mapping.
- `ANIMATION_MATRIX.csv` — shared, signature and family animation requirements.
- `MATERIAL_PALETTE.json` — canonical civic-surrealism materials and meaning.
- `CLOUD_PIPELINE.md` — Blender-to-Unity cloud generation and integration flow.
- `REVIEW_CHECKLIST.md` — visual, rig, runtime, performance and delivery approval gate.
- `generated_manifest.json` — produced by the cloud build with actual files and geometry counts.
- `Source/` — editable Blender and portable GLB deliverables generated in CI.
- `Previews/` — individual review renders and the complete contact sheet generated in CI.

Validation is executed entirely in the cloud: Blender generation first, then licensed Unity import, prefab baking, EditMode, PlayMode and WebGL review.

The committed package contains 17 proportion-safe rigged assets generated from an empty output directory. This synchronization commit runs the licensed Unity gate against those exact FBX files.
