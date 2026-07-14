# The Idea Zoo — Unity Production Vertical Slice

This directory contains the controlled Unity rebuild of The Idea Zoo. The Godot branch remains a gameplay prototype. The Unity project separates real-idea reasoning from camera, interface, presentation and world systems so visual or input bugs cannot corrupt a specimen record.

## The playable promise

Bring a startup, product, policy, invention, creative project or personal plan into the Whisper Gate.

1. The idea hatches into one of seven creature classes.
2. Its appetite, hidden burden and assumptions become visible.
3. The Keeper walks it through Desire, Commitment, Burden and Refusal tests.
4. A sealed Board record exposes institutional pressure.
5. The Molt House changes the real promise, audience and guardrails.
6. The Decision Garden ends in Build, Molt, Hibernate, Sanctuary or Break.
7. A private specimen record stores the ruling and three real-world actions.

## Production architecture

`Assets/IdeaZoo/Core/IdeaZooDomain.cs` contains the engine-independent model. It compiles and runs without Unity.

`Assets/IdeaZoo/Runtime` owns case orchestration, mobile input, world construction, baseline creatures, interface and private archive recovery.

`Assets/IdeaZoo/Presentation` adds the civic-surrealism production pass:

- shared paper, brass, clay, glass, ink and patina materials;
- modular façades, rails, pipes, workbenches, vitrines and public-language banners;
- department-specific dressing across the complete Whisper Gate district;
- six animated specialist rigs with distinct tools and silhouettes;
- authored presentation parts for seven creature classes and eight appetites;
- visible hidden burden and guardrail transformations;
- PlayableDirector hatching, inspection, Molt, Decision and ruling shots;
- optional Cinemachine runtime bridge with direct-camera fallback;
- procedural civic ambience and story cues;
- distance culling, device tiers and adaptive render scale for mobile.

`Assets/IdeaZoo/Editor/IdeaZooPresentationBaker.cs` provides menu commands to bake the procedural production kit into a district prefab and a review scene.

## Run

1. Install Unity 6.5 (`6000.5.0f1`).
2. Open the `unity` directory as a Unity project.
3. Open `Assets/IdeaZoo/Scenes/WhisperGate.unity`.
4. Enter Play Mode.

To create reviewable assets inside Unity, use:

- `Idea Zoo > Presentation > Bake District Prefab`
- `Idea Zoo > Presentation > Bake Review Scene`
- `Idea Zoo > Presentation > Validate Baked Assets`

## Validation boundary

CI compiles and executes the engine-independent case contracts and audits the Unity source, presentation modules, mobile safeguards and editor baking workflow without requiring a Unity licence.

A native Unity player has not been built in this environment. A true Unity Editor import, iPhone build, shader validation and physical-device performance pass still require Unity 6000.5.0f1. iOS additionally requires macOS, Xcode, Apple signing and an actual device.
