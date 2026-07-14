# The Idea Zoo — Unity Production Foundation

This directory is the controlled Unity rebuild of The Idea Zoo. The Godot branch remains a gameplay prototype. The Unity project separates the real-idea reasoning from the camera, interface and world so presentation bugs cannot corrupt the idea record.

## The playable promise

Bring a startup, product, policy, invention, creative project or personal plan into the Whisper Gate.

1. The idea hatches into one of seven creature classes.
2. Its appetite and hidden burden become visible.
3. The Keeper walks it through Desire, Commitment, Burden and Refusal tests.
4. A sealed Board record exposes institutional pressure.
5. The Molt House changes the real promise, audience and guardrails.
6. The Decision Garden ends in Build, Molt, Hibernate, Sanctuary or Break.
7. A private specimen record stores the ruling and three real-world actions.

## Architecture

`Assets/IdeaZoo/Core/IdeaZooDomain.cs` contains the engine-independent model. It can compile and run without Unity.

`Assets/IdeaZoo/Runtime` owns world construction, the Keeper, mobile input, creatures, interface, story flow and the private archive.

The production scene is intentionally minimal. `IdeaZooAutoLoad` constructs the vertical slice after the scene loads, avoiding fragile scene references during the architecture pass.

## Run

1. Install Unity 6.5 (`6000.5.0f1`).
2. Open the `unity` directory as a Unity project.
3. Open `Assets/IdeaZoo/Scenes/WhisperGate.unity`.
4. Enter Play Mode.

## Current boundary

This is a functional production foundation and detailed greybox-plus world, not final art. The next visual pass should replace generated primitives with a modular environment kit, rigged staff, authored creature parts, animation, Timeline sequences and Cinemachine cameras.

A native iPhone build still requires macOS, Xcode, Apple signing and physical-device profiling. CI currently validates the engine-independent contracts and Unity project structure without requiring a Unity licence.
