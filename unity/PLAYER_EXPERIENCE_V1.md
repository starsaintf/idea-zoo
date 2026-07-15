# The Idea Zoo — Player Experience V1

Player Experience V1 sharpens the existing production game without creating a second case loop, world, camera, creature or campaign.

## Guided first case

First-time players can begin **The Neighbourhood Umbrella Library**, an authored seven-minute case that enters through the real private `IdeaZooGame.BeginCase` intake boundary.

The case teaches the live sequence:

`Capture → Diagnose → Care → Test → Confront → Decide`

It uses the same creature, four habitats, limited resources, deterministic disruptions, Molt and final rulings as user-supplied ideas. Players may skip the guided case and bring their own idea immediately, and may replay it later from Accessibility & Comfort.

## Hidden case archetypes

Each case receives one of eight hidden patterns:

- good idea, wrong first audience;
- useful but commercially weak;
- profitable but harmful without guardrails;
- valid need, impossible for now;
- ordinary idea where execution is the advantage;
- impressive mechanism without an urgent problem;
- useful core that should become a feature;
- hidden audience stronger than the intended one.

The pattern is derived deterministically from the real profile and record ID. It changes encounter clues and metric consequences, but remains hidden until the player issues a final ruling.

## Tactile evidence preparation

Every round in all four evidence habitats now begins with a bounded touch-first preparation task:

- Customer Interview: sort behavioural evidence from compliments and feature requests.
- Prototype Trial: spend a tiny scope budget and select honest test conditions.
- Feasibility Audit: map recurring labour, dependencies and accountable ownership.
- Children’s Jury: assemble plain-language explanations, real exits and concrete misuse cases.

Each task uses at most six prebuilt token buttons and requires no drag physics, scene load or runtime object creation. Strong preparation can add one earned evidence point and a small domain-specific metric improvement. The result is persisted in the Player Experience case record.

## Visible long-term consequences

The Decision Garden receives eight pooled consequence monuments. Completed cases become visible as:

- working beacons for Build;
- broken memorials for Break;
- protected glass forms for Sanctuary;
- shed skins for Molt;
- sealed markers for Hibernate.

The monuments are refreshed only when state changes. They are attached to the existing world and never run a simulation loop.

## Contextual character response

The existing character performance rigs react to test progress, Molt and rulings. Short Keeper, Mara and Nara lines respond to the hidden archetype and the player’s persistent tendency without adding a second dialogue system.

## Keeper ranks

Completed rulings advance five ranks:

`Apprentice → Keeper → Curator → Warden → Founder`

Ranks do not inflate costs. They represent accumulated judgment and are shown in the post-case archetype reveal.

## Accessibility and comfort

The safe-area accessibility panel includes:

- three bounded text sizes;
- reduced motion;
- high contrast;
- larger touch targets;
- decision focus mode;
- haptic feedback control;
- guided-case replay.

Settings persist on-device. Decision focus mode suppresses nonessential particles while an evidence decision is open. It does not disable creature feedback, input, evidence, story or ruling logic.

## Performance boundaries

Player Experience V1 keeps the existing Eco 30, Balanced 45 and Quality 60 ownership unchanged.

The pass adds:

- one persistent Player Experience owner;
- one accessibility controller;
- six pooled tactile token buttons;
- eight pooled consequence monuments;
- a twenty-case Player Experience history cap;
- state-change-only saves and world refreshes;
- no per-frame hierarchy scan, UI build, reflection lookup or collection conversion;
- no new loading scene, camera or creature simulation.

## Validation

The branch adds EditMode coverage for the tutorial intake, all eight archetypes, all twelve tactile rounds, accessibility bounds, history caps and rank progression. The canonical full-runtime PlayMode test verifies one Player Experience owner, one reused Zoo world, pooled monuments, tactile UI, safe-area protection and accessibility boot.

Physical iPhone testing is still required for fifteen-minute heat and battery behaviour, touch comfort, software keyboard, suspend/resume and device-specific frame pacing. Those tests cannot be completed by repository code alone.
