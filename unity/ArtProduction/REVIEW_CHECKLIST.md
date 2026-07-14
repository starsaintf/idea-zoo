# Character and creature review checklist

## Visual identity

- [ ] Reads as civic surrealism rather than fantasy, military or generic sci-fi.
- [ ] Silhouette remains identifiable at phone scale.
- [ ] Tools and accessories communicate the character’s belief before dialogue.
- [ ] Materials follow the paper/brass/clay/glass/ink/rust/moss/teal system.
- [ ] No weapon-like prop has been introduced.

## Rig and hierarchy

- [ ] All required human bones or creature sockets use exact names.
- [ ] Head and hand attachment points have correct orientation.
- [ ] Root transform imports at zero rotation and one scale.
- [ ] No unexpected camera or light is included in the FBX.
- [ ] Mirrored limbs behave consistently.

## Runtime

- [ ] Procedural fallback hides only when the imported prefab loads.
- [ ] Keeper body, skin, hair, coat and lens choices apply correctly.
- [ ] Each specialist maps to the correct prefab and prop.
- [ ] Each Jury member maps to the correct prefab.
- [ ] Creature family changes when the idea genome changes.
- [ ] Burden and guardrail parts reflect the current idea state.
- [ ] Imported head tracks the Keeper without flipping.

## Performance

- [ ] Character stays under 35k triangles at LOD0.
- [ ] Creature stays under 30k triangles at LOD0.
- [ ] Renderer count stays below 64.
- [ ] Material count stays below 32 in the generated baseline and is reduced further in final hand-authored assets.
- [ ] Transparency is limited to intentional glass elements.
- [ ] ECO profile remains above the 30 FPS target in the review scene.

## Delivery

- [ ] `.blend`, `.fbx`, `.glb` and `.png` exist.
- [ ] Manifest entry is present.
- [ ] Unity prefab was baked by the editor test.
- [ ] EditMode and PlayMode tests pass.
- [ ] WebGL artifact contains the imported art.
- [ ] Physical iPhone review is recorded before release approval.
