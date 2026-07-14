# The Idea Zoo — Character and Creature Production Bible

Version 1.0 — cloud-production package

## 1. Creative north star

The Idea Zoo is a living civic institution, not a fantasy theme park and not a realistic modern city. Its people and creatures are assembled from the visual logic of public service: folded paper, stamped classifications, brass instruments, clay bodies, glass evidence, visible repairs, maintenance thread, civic uniforms and phrases that have become physical objects.

Every asset must satisfy three tests:

1. **Readable at phone scale.** The identity must survive at a small on-screen height.
2. **Meaning before decoration.** A tool, seam, burden or silhouette must communicate role or consequence.
3. **Institutional, not heroic.** Nobody carries a weapon. Authority appears through clothing, tools, barriers, records and access.

The package uses stylized low-poly construction and rigid bone-parented parts. This is intentional. It keeps the silhouettes graphic, makes the cloud build deterministic, and allows the same skeletons to support many variants without mobile-heavy deformation.

## 2. Scale and proportion system

- Adult Keeper reference height: 2.35 Unity metres.
- Adult specialist range: 2.30–2.43 metres.
- Jury child range: 1.72–1.82 metres.
- Standard creature shoulder/core height: 1.5–1.9 metres.
- Large Burden Beast footprint: roughly 1.6 × 2.0 metres.
- Choir orbit diameter: roughly 1.5 metres.

Human proportions are deliberately elongated through coat length and head scale. Creatures are not cute pets by default; they should feel observable, consequential and capable of changing the institution around them.

## 3. Shared material language

### Paper
Warm, fibrous and slightly worn. Used for authority that can still be amended: coats, records, wings, plates, flags and appeals.

### Brass
Used for durable mechanisms, commitments and instruments. Brass should read as worked and maintained, not luxurious.

### Clay
Used for living mass. Clay bodies should show softness and fingerprints without relying on high-frequency texture detail.

### Glass
Used for evidence, reflection, inner ideas and uncertain boundaries. Transparency must be controlled and never layered excessively on mobile.

### Ink
Used for institutional shadow, private records, forecasts and unresolved decisions.

### Rust
Used for premature authority, coercion, danger and systems that have outlived their justification.

### Moss
Used for care, sanctuary, patience and useful neglected work.

### Teal light
Used for resonance, observation and living evidence. It is a signal, not general neon decoration.

## 4. Human rig standard

All adult characters share the same named transform hierarchy:

- Hips
- Spine
- Chest
- Neck
- Head
- LeftShoulder
- LeftUpperArm
- LeftLowerArm
- LeftHand
- RightShoulder
- RightUpperArm
- RightLowerArm
- RightHand
- LeftUpperLeg
- LeftLowerLeg
- LeftFoot
- RightUpperLeg
- RightLowerLeg
- RightFoot

Required attachment transforms:

- HeadSocket
- LeftHandSocket
- RightHandSocket

The child rig uses the same names at reduced proportions. This lets shared procedural and authored animation logic operate across the entire cast.

## 5. Keeper of Unfinished Things

### Role
The Keeper is the player’s field identity: observer, investigator and final decision-maker. The Keeper should look equipped to handle uncertainty, not equipped for combat.

### Silhouette
- Long civic field coat.
- Broad, simple shoulder line.
- Resonance Lens hanging or mounted near the chest.
- Five visible ruling plates.
- Containment thread running from coat to lens/tool area.
- Stable boots and gloved hands.

### Modular body frames

1. **Frame 0 — narrow:** light and attentive, reduced coat width.
2. **Frame 1 — standard:** balanced silhouette and canonical reference.
3. **Frame 2 — broad:** stronger shoulder and coat volume without becoming militaristic.

### Customization support
- Six skin-tone values.
- Five hair silhouettes.
- Four coat colour treatments.
- Three Resonance Lens designs.

The modular pieces use predictable names so runtime code can activate one selection while preserving a single rig.

### Expression language
The Keeper’s face is restrained. Curiosity is shown through head angle and the single bright intent mark. Concern, resolve and hope should come primarily from posture and gaze.

## 6. Specialist cast

### Mara Rook — Hatchkeeper

**Belief:** unfinished things deserve protection before judgment.

**Silhouette:** protective paper cape, compact field coat, fork-shaped hatch instrument.

**Movement:** plants herself between the creature and danger; gestures open outward; head tracks fragile specimens closely.

**Key prop:** Hatch Fork. Brass stem with a teal cross-piece that reads clearly from a distance.

### Toma Reed — Release Shepherd

**Belief:** useful ideas should eventually survive outside the Zoo.

**Silhouette:** long recall sash, upright staff, slightly taller and more open posture.

**Movement:** forward-facing, inviting, hopeful. Staff plants before major statements.

**Key prop:** Release Staff with a teal recall light.

### Sefu Anik — Appetite Reader

**Belief:** an idea cannot be judged until its hidden appetite is named.

**Silhouette:** narrow coat, visible specimen vials across the torso, hand lens.

**Movement:** leans toward evidence; small, precise hand movements; circles rather than confronts.

**Key prop:** Appetite Lens and four contrasting vials.

### Elian Thread — Molt Surgeon

**Belief:** transformation is often more honest than destruction.

**Silhouette:** wider surgical frame, restrained dark coat, large thread spool.

**Movement:** measured and procedural. Hands move along seams, frames and invisible cut lines.

**Key prop:** Molt Spool with external surgical rails.

### Sen Osei — Counterfactual Veterinarian

**Belief:** the most dangerous consequence may be the one that has not happened yet.

**Silhouette:** forecast coat, dark rear plane, three floating or handheld future frames.

**Movement:** asymmetrical; looks between present subject and projected outcomes.

**Key prop:** Counterfactual Frames.

### Nara Voss — Mercy Butcher

**Belief:** ending an idea can be an act of care when continuation multiplies harm.

**Silhouette:** pale White Room plate, rust mercy bell, controlled and compact stance.

**Movement:** almost still. The bell moves only when a decision is serious.

**Key prop:** Mercy Bell.

## 7. Children’s Jury

The Jury must not look like miniature adults. Their coats are shorter, their question plates are larger relative to the torso, and their gestures should be direct rather than formal.

### Lio
Rust question plate. Most likely to challenge who bears the hidden cost.

### Amara
Teal question plate. Most likely to ask what the idea feels like to use.

### Kweku
Paper question plate. Most likely to ask whether the explanation is actually understandable.

All three share one skeleton and animation set, with differences carried through body width, skin tone, coat treatment and plate colour.

## 8. Creature system

The seven Idea Zoo behavioural classes are expressed through five reusable rig families. Class, appetite, evidence, burden and guardrails alter the family rather than spawning unrelated monsters.

### Avian family

**Supports:** Fleck, Hand and Swarm variants.

**Core read:** a civic bird made from paper wings, clay mass and memory tail.

**Rig:** Root, Body, Head, LeftWing, RightWing, LeftLeg, RightLeg, Tail.

**Motion:** glide, hop, alert wing pulses.

**Class treatment:**
- Fleck: lighter paper, gentle teal intent marks.
- Hand: brass working plates and stronger legs.
- Swarm: repeated eye/choir markers and faster wing agitation.

### Burden Beast family

**Supports:** Hand, Teeth and Burrower variants.

**Core read:** a working animal carrying the labour the idea hides.

**Rig:** Root, Body, Head, four leg bones and Tail.

**Motion:** trot, load shift, defensive stance.

**Class treatment:**
- Hand: visible harness and task surfaces.
- Teeth: rust authority, harder head silhouette.
- Burrower: archive plates and maintenance attachments.

### Lantern family

**Supports:** Fleck, Mirror and Weather variants.

**Core read:** a glass civic vessel containing an unstable inner idea.

**Rig:** Root, Body, Head, two appendages and Tail.

**Motion:** hop, hover and pulse.

**Class treatment:**
- Fleck: soft glow and small rings.
- Mirror: stronger glass response and doubled reflection marks.
- Weather: pressure rings and broader oscillation.

### Serpentine family

**Supports:** Mirror, Teeth and Burrower variants.

**Core read:** a chain of decisions that has learned how to move through systems.

**Rig:** Root, seven spine segments and Head.

**Motion:** coil, pause, inspect and withdraw.

**Class treatment:**
- Mirror: glass head planes.
- Teeth: rust mouth and harder front mass.
- Burrower: record plates and low, persistent movement.

### Choir family

**Supports:** Swarm and Weather variants.

**Core read:** one idea expressed as several orbiting public voices.

**Rig:** Root, Body, five Voice bones and Head.

**Motion:** orbit, convergence, dispersal and pressure pulse.

**Class treatment:**
- Swarm: independent voices with rapid disagreement.
- Weather: slower, larger collective movement.

## 9. Semantic creature attachments

Every creature prefab must expose:

- HeadSocket
- AppetiteSocket
- BurdenSocket
- GuardrailRoot
- TailSocket
- EffectRoot

### Appetite encoding
- Attention: teal eye/halo emphasis.
- Data: glass or squared information marks.
- Money: brass tokens and weighted plates.
- Trust: paired links and visible reciprocity.
- Obedience: rust collar or directive plate.
- Labour: harness, limbs and maintenance tags.
- Care: moss surfaces and softened guard structures.
- Time: repeated rings, clock-spine rhythm or delayed pulses.

### Hidden burden
Burden crates increase according to unresolved assumptions. They should feel carried, tethered or embedded, never like decorative luggage.

### Evidence
Evidence makes form more definite: stronger shadows, steadier scale, clearer edge language and more coherent movement. Evidence should not simply make a creature larger.

### Guardrails
Guardrail rings become visible after Molt decisions. They are not cages; they are readable limits around capability.

## 10. Animation package

### Shared human states
- Idle neutral
- Idle curious
- Idle protective
- Idle concerned
- Idle hopeful
- Walk
- Turn left
- Turn right
- Invite
- Explain
- Refuse
- Inspect
- Celebrate
- Mourn

### Specialist accents
- Mara shields specimen with cape/fork.
- Toma plants staff and signals release path.
- Sefu raises lens and compares vials.
- Elian pulls thread through surgical frame.
- Sen separates and recombines future frames.
- Nara raises and rings mercy bell once.

### Creature emotional states
- Dormant
- Curious
- Trusting
- Hungry
- Afraid
- Agitated
- Resolute

### Motion families
- Hop
- Trot
- Glide
- Coil
- Orbit

## 11. Mobile budgets

### Main humans
- Target LOD0: 20k–35k triangles.
- Target LOD1: 10k–18k triangles.
- Target LOD2: 3k–7k triangles.
- Maximum two materials per full character in final hand-authored replacement assets.
- Maximum four bone influences per vertex if deforming skin is later introduced.

### Jury
- Target LOD0: 10k–18k triangles.
- Shared child rig and shared material atlas.

### Creatures
- Target family body: 12k–30k triangles.
- Each optional attachment below 5k triangles.
- Maximum two primary materials plus one controlled emissive/glass treatment.

### Current cloud-generated package
The procedural cloud package is intentionally below these limits and uses separate rigid pieces. It is a production-ready stylized baseline and also serves as a validated blockout for later hand-authored replacements.

## 12. Naming contract

### Characters
- Keeper_0
- Keeper_1
- Keeper_2
- Mara_Rook
- Toma_Reed
- Sefu_Anik
- Elian_Thread
- Sen_Osei
- Nara_Voss
- Lio_Jury
- Amara_Jury
- Kweku_Jury

### Creatures
- Avian
- BurdenBeast
- Lantern
- Serpentine
- Choir

Do not rename bones, sockets or top-level asset IDs without updating the runtime bridge and validation tests.

## 13. Deliverables per asset

- Editable `.blend` source.
- Unity-compatible `.fbx` runtime export.
- Portable `.glb` review/export copy.
- PNG review render.
- Manifest entry with triangle and object counts.
- Unity prefab generated from the FBX.
- Successful editor validation of required bones and sockets.

## 14. Acceptance criteria

An asset is accepted only when:

1. Its silhouette is distinguishable from the other cast members at phone scale.
2. Required bones and sockets exist with exact names.
3. It imports through the cloud Unity workflow without warnings that block build.
4. It is replaced correctly at runtime while the procedural fallback remains available.
5. Materials remain legible under ECO mobile settings.
6. The character or creature can complete its basic gaze and motion behaviour.
7. The model stays inside the stated renderer, material and triangle budgets.
8. A complete WebGL case can be played with the imported asset enabled.

## 15. Replacement policy

The cloud-generated assets are the baseline production cast. A later artist may replace geometry, textures and deformation while preserving:

- asset ID;
- rig hierarchy;
- socket names;
- broad silhouette intent;
- mobile budget;
- semantic props;
- runtime contract.

This prevents visual refinement from becoming another gameplay rewrite.
