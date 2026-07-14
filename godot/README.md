# The Idea Zoo: Glassmarket

A mobile-first third-person civic-creature game and real-world idea laboratory built in **Godot 4.7**.

This is not a GTA clone. Glassmarket is a living civic terrarium: circular specimen streets, brass institutions, paper-glass organisms and weather made from public language. The player has no gun and no wanted meter. The core verbs are **trace, interpret, test, molt and judge**.

## Current playable chapter: make or break a real idea

The current scene begins at the Whisper Gate. The player enters a startup, product, policy, invention, creative project or personal plan they may actually build. The Zoo extracts its promise, audience, payer, appetite, maintenance burden, evidence, risk and likely behavioural class, then hatches a creature from that structure.

The player takes the specimen through four evidence habitats:

1. **Desire Yard** — does the problem exist before the solution is pitched?
2. **Commitment Paddock** — will anyone risk time, money, access or reputation?
3. **Burrower Tunnel** — who performs the invisible recurring work?
4. **Refusal Gate** — can affected people leave, appeal, delete or stop it?

After testing, the player edits the real idea in the Molt House and issues a Build, Molt, Hibernate, Sanctuary or Break ruling. The result is saved locally with a concrete real-world action plan.

## Institutional story

Mara Rook, Toma Reed, Sefu Anik, Nara Voss, Elian Thread and Sen Osei occupy physical Zoo departments. A sealed Board record reveals that the institution may classify ideas before evidence is collected because certain categories are easier to fund, sell or suppress.

The original Queue-Eater and full ecology remain design foundations: Flecks, Hands, Mirrors, Teeth, Swarms, Weather and Burrowers represent different ways ideas behave in the world. The core story is discovered through systems, spaces and consequences rather than article narration.

## V9 stability protections

- Evidence and risk survive creature Molts.
- Camera and joystick touch IDs reset when overlays interrupt a gesture.
- Evidence habitats reject duplicate, unknown or malformed submissions.
- Cancelling the Molt House returns cleanly to testing.
- The Board Wing remains accessible after a cancelled Molt.
- A Molt must change the promise, audience or guardrails.
- Verdicts require a second confirmation tap within four seconds.
- Classification uses word boundaries, so `maintenance` is not interpreted as `AI` and `unpaid` is not treated as `paid` evidence.
- Recording “no evidence” no longer increases the evidence score.
- Corrupt local archives are backed up before recovery.
- The previous valid archive is preserved before replacement.
- Local storage failures are surfaced to the player.

## Controls

### Desktop

- `WASD` / arrow keys — move
- right mouse drag — orbit camera
- hold `Space` — Resonance Lens
- `E` — interact, test, molt or judge

### Mobile

- left thumb — virtual joystick
- drag the upper-right play area — orbit camera
- hold **LENS** — reveal hidden burdens
- **TOUCH** — contextual action

## Mobile performance choices

- Godot Compatibility renderer
- procedural meshes with no large textures
- one unshadowed directional light
- no dynamic GI, SSAO or volumetric effects
- reduced internal 3D scale on mobile
- limited physics bodies and creature segments
- compact authored Zoo instead of an empty oversized map
- safe-area-aware touch controls
- PWA Web export and native Android preset

## Automated validation

The Idea Lab contract runs at 896×414 and covers the complete intake-to-ruling flow, invalid submissions, duplicate inputs, interrupted touch gestures, cancelled and reopened Molts, evidence preservation, corrupt archive recovery, verdict confirmation, restart behavior and safe-area layout.

The independent mobile pipeline validates script parsing, startup, the iPhone-sized touch contract, Web export and preview publication.

Automated tests cannot certify Safari virtual-keyboard behavior, device thermal response, real finger comfort or IndexedDB persistence under iOS storage eviction. Those require a physical iPhone session.

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

This branch should replace the earlier browser prototypes only after its story, feel, camera, touch controls and art direction are approved. The old builds remain useful as systems sketches, not as the final production foundation.
