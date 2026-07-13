# The Idea Zoo: Glassmarket

A mobile-first third-person civic-creature game built in **Godot 4.7**.

This is not a GTA clone. Glassmarket is a living civic terrarium: circular specimen streets, brass institutions, paper-glass organisms and weather made from public language. The player has no gun and no wanted meter. The core verbs are **trace, interpret, place rules, tether and judge**.

## Vertical slice: The Queue-Eater

Glassmarket celebrates a system that appears to remove waiting. The system manifests as a ribbon-like creature whose hidden appetite is the unrecorded time of porters, temporary workers and maintenance staff.

The player:

1. uses the Resonance Lens to tune three physical traces;
2. chooses and places civic seals around the creature;
3. learns that rules change its body and behaviour;
4. tethers it back to the Idea Zoo as a public procession;
5. walks through one of five spatial verdict gates;
6. sees the city respond to the decision.

The correct containment is not a quiz answer. The clues reveal three necessary rules: an exit, a named keeper and a boundary. An `AMPLIFY` seal rewards speed while making the creature less governable.


## Institutional expansion

After field containment, the case opens into the full Idea Zoo rather than ending at a verdict menu. The player physically enters the departments, has four bell-hours to consult eight specialists, studies the seven living classes, and decides whether to spend scarce time uncovering a sealed board conflict.

The playable ecology now includes Flecks, Hands, Mirrors, Teeth, Swarms, Weather and Burrowers. Each behaves differently in the city and responds to stability and story leakage. Staff reports push the official classification in competing directions; the board can still override the keeper when its funding conflict remains hidden.

The replay loop is built around fast evidence chains, taxonomy discoveries, limited consultations, classification risk, persistent best score and keeper ranks. Rewards come from noticing incentives and second-order effects—not from skipping the intellectual work.

## Identity pillars

- **Civic surrealism:** institutions, incentives and maintenance become physical geography.
- **Non-combat mastery:** observation and rule design replace weapons.
- **Consequences remain visible:** creatures return through the city rather than disappearing into inventory.
- **Small and boring things matter:** optional organisms can affect the ending without becoming collectibles for their own sake.
- **No article transcription:** the original Idea Zoo logic becomes systems, spaces and player decisions rather than narrated passages.

## Controls

### Desktop

- `WASD` / arrow keys — move
- right mouse drag — orbit camera
- hold `Space` — Resonance Lens
- `E` — interact / place / tether / judge
- `Q` — change civic seal

### Mobile

- left thumb — virtual joystick
- drag the right side — orbit camera
- hold **LENS** — tune a trace
- **TOUCH** — contextual action
- **SEAL** — change selected seal

## Mobile performance choices

- Godot Compatibility renderer
- procedural meshes with no large textures
- one unshadowed directional light
- no dynamic GI, SSAO or volumetric effects
- 0.72–0.8 internal 3D scale on mobile
- limited physics bodies and creature segments
- compact district instead of an empty oversized map
- large safe-area-aware touch controls
- optional PWA web export and native Android preset

## Run in Godot

1. Install Godot 4.7 stable.
2. Open this folder as a project.
3. Run `scenes/main.tscn`.

Command line validation:

```bash
godot --headless --path . --editor --quit
```

Web export:

```bash
godot --headless --path . --export-release Web build/web/index.html
```

Android export requires the Android SDK, JDK and Godot export templates described in the official Godot Android export documentation.

## Repository direction

This Godot branch should replace the browser prototypes only after its feel, camera, touch controls and art direction are approved. The old builds remain useful as systems sketches, not as the final production foundation.
