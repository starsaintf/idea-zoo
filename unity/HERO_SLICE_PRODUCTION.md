# The Idea Zoo — Cinematic Hero Slice V1

This production pass upgrades the playable Unity project from a functional stylized baseline into a richer story-driven world layer without breaking the existing universal systems.

## Implemented in the runtime

The pass installs automatically after the main scene loads. It preserves the existing Idea Zoo world, cloud-generated characters, creature rigs, gameplay, and procedural fallbacks, then adds a premium presentation layer over them.

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
- trusted or transformed specimens produce hopeful beats;
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

This replaces the old “mostly recolor the creature” presentation with a visible consequence system.

## Cinematic sequencing

The pass uses the existing `PresentationCameraRig` and Timeline-compatible presentation layer for:

- first hatch;
- evidence discovery;
- burden escalation;
- Molt entry;
- decision reveal;
- final ruling.

A cooldown prevents repeated camera interruptions during normal movement.

## Mobile production boundary

This is designed as **AA/AAA cinematic stylisation for mobile**, not a promise that a phone will render film-quality concept art literally in real time.

The pass enforces:

- 30 FPS mobile target and 60 FPS desktop/WebGL target;
- dynamic practical-light distance culling;
- adaptive shadow-distance reduction when sustained FPS drops;
- fixed particle budgets;
- a visible-triangle audit;
- retained low-poly models as LODs, crowd assets, distant silhouettes, and fallbacks.

## Concept art versus runtime art

The approved cinematic concept art establishes composition, mood, architecture, costume direction, lighting, and emotional intent. The runtime pass implements that language with production-safe geometry, lighting, materials, particles, camera behavior, and story reactions.

The current cloud-generated hero geometry is still a stylized production baseline. Bespoke sculpting, retopology, UV work, painted materials, facial blend shapes, cloth, and authored animation remain the next visual-asset pass. Those assets can now plug into stable hero IDs, bones, sockets, district anchors, transformation states, and performance contracts.

## Cloud validation

The production pass adds:

- a static source contract;
- editor import coverage;
- a runtime prefab baker;
- four review-scene bakers;
- exact transformation-stage checks;
- the existing licensed Unity EditMode, PlayMode, presentation-asset, and WebGL build gates.

Run the Unity menu command:

`Idea Zoo → Hero Slice → Bake Complete Production Pass`

This creates the runtime prefab, the four review scenes, and the production manifest.
