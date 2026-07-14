# The Idea Zoo — Cinematic Hero Slice V1

This production pass upgrades the playable Unity project from a functional stylized baseline into a richer story-driven world layer without breaking the existing universal systems.

## Implemented in the runtime

The pass installs automatically after the main scene loads. It preserves the existing Idea Zoo world, cloud-generated characters, creature rigs, gameplay, mobile quality profiles, and procedural fallbacks, then adds a premium presentation layer over them.

The four hero districts are:

- **Zoo Entrance** — the institutional threshold, admissions plaza, habitat signs, observation boards, wet reflective paving, brass-and-glass architecture, practical lamps, and public-life cues.
- **Lantern Fields** — a creature-care habitat with luminous pods, evidence gauges, care tools, observation tables, trust-before-touch language, and warm living light.
- **Silent Stacks** — a dense archive district with specimen drawers, record shelves, suspended field records, archive dust, Keeper staging, and fragile-idea documentation.
- **Evidence Forge / Molt Chamber** — a judgment and transformation space with a central dais, evidence protocols, observation rings, transformation lighting, molt-cycle language, and controlled spectacle.

## Story-driven character performance

The Keeper and Mara Rook now react to case progression rather than remaining decorative:

- gaze is kept on the living idea;
- evidence discoveries trigger inspect/explain gestures;
- risk changes Mara’s protective behavior;
- Molt and decision stages change emotional posture;
- Build, Molt, and Sanctuary rulings produce hopeful beats;
- Hibernate and Break rulings produce subdued, concerned, or grieving beats;
- burdened specimens produce concern and refusal beats.

The existing `CharacterPerformanceRig` remains the shared contract, so final hero geometry and bespoke animation can replace the current meshes without rewriting story logic.

## Creature transformation language

The hero creature has six visible narrative stages:

1. **Unproven**
2. **Observed**
3. **Tested**
4. **Trusted**
5. **Burdened**
6. **Transformed**

The stage is derived from real case state: evidence, completed tests, safety, assumptions, guardrails, ruling, and current campaign stage. Each stage changes scale, emission, evidence halos, trust fins, burden shards, light intensity, motion, and cinematic response.

The final Transformed stage is ruling-sensitive. Build, Molt, and Sanctuary retain a luminous evolved state; Hibernate becomes muted and dormant; Break becomes dimmer, smaller, and visibly fractured.

This replaces the old “mostly recolor the creature” presentation with a visible consequence system.

## Cinematic sequencing

The pass shares the existing `PresentationCameraRig` and Timeline-compatible presentation layer. The original presentation director remains the single owner of hatch, evidence, Molt, decision, and final-ruling shots. The hero layer adds only unique visual beats, such as burden escalation, so two directors cannot restart the same shot or fight for the camera.

A cooldown prevents repeated camera interruptions during normal movement.

## Mobile production boundary

This is designed as **AA/AAA cinematic stylisation for mobile**, not a promise that a phone will render film-quality concept art literally in real time.

The pass respects the existing **Eco 30, Balanced 45, and Quality 60** profiles rather than replacing them with one global frame-rate target. It adds:

- dynamic distance culling for hero practical lights;
- hero-only pressure reduction and recovery without overriding global quality settings;
- fixed particle budgets;
- hero-light, hero-particle, and all-submesh triangle audits;
- shared surface materials to preserve batching and reduce memory pressure;
- retained low-poly models as LODs, crowd assets, distant silhouettes, and fallbacks.

## Concept art versus runtime art

The approved cinematic concept art establishes composition, mood, architecture, costume direction, lighting, and emotional intent. The runtime pass implements that language with production-safe geometry, lighting, materials, particles, camera behavior, and story reactions.

The current cloud-generated hero geometry is still a stylized production baseline. Bespoke sculpting, retopology, UV work, painted materials, facial blend shapes, cloth, and authored animation remain the next visual-asset pass. Those assets can now plug into stable hero IDs, bones, sockets, district anchors, transformation states, and performance contracts.

## Cloud validation

The production pass adds:

- a static source and inconsistency contract;
- editor import and material-sharing coverage;
- runtime PlayMode checks for automatic installation, all four districts, transformation systems, and single camera ownership;
- a runtime prefab baker;
- four review-scene bakers;
- exact transformation-stage checks;
- explicit verification and artifact upload for the hero prefab, manifest, and all four hero review scenes;
- the licensed Unity EditMode, PlayMode, presentation-asset, and WebGL build gates.

Run the Unity menu command:

`Idea Zoo → Hero Slice → Bake Complete Production Pass`

This creates the runtime prefab, the four review scenes, and the production manifest.
