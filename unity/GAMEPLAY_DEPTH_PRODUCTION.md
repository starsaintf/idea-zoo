# The Idea Zoo — Gameplay Depth and Performance Pass

This pass turns the existing evidence forms into playable choices while preserving the current Zoo, creature, campaign, camera, character and ruling systems.

## The complete case loop

A real idea now moves through:

`Capture → Diagnose → Care → Test → Confront → Decide`

The Whisper Gate still creates the creature and the existing four habitats still control progression. Entering a habitat now opens a fast three-decision encounter instead of asking the player to grade their own evidence.

The four encounters are:

- **Customer Interview** — choose questions, handle contradictions and decide what counts as real demand.
- **Prototype Trial** — choose the smallest promise to test, the first tester and the commitment signal.
- **Feasibility Audit** — expose recurring labour, dependency cost and ownership after launch.
- **Children’s Jury** — explain the idea plainly, design refusal and name the cruelest plausible use.

Every response changes evidence quality, desirability, feasibility, viability or safety. It also reveals a player tendency: Experimenter, Protector, Builder, Skeptic or Simplifier.

## Limited resources

Each case begins with:

- 18 Time
- 8 Trust
- 7 Momentum
- 0 earned Evidence

Strong tests usually cost more time or momentum. Fast moves can damage trust or conceal risk. The resources remain visible during exploration and every encounter.

No loading scene is introduced. Each habitat is limited to three decisions and each decision advances immediately.

Scarcity cannot soft-lock a case. When no remaining choice is affordable, the **Reserve Protocol** releases only the minimum capacity needed for the least expensive response. It does not refill the player or make more costly choices available.

## Disruptive events

After the second and third completed tests, deterministic case events can change the plan:

- a competitor launches first;
- a dependency doubles in price;
- an unexpected audience wants the idea;
- a tester finds a privacy failure;
- public attention arrives before the idea is ready.

The event is chosen from the case record ID, so save/reload cannot reroll a more convenient crisis.

## Permanent consequences and Zoo memory

The game now stores:

- encounter choices and evidence strength;
- disruptive events and responses;
- remaining resources;
- dominant decision tendency;
- final ruling;
- scars created by lost trust, exhausted time, unresolved safety or destruction.

The Silent Stacks receives pooled memory cards for the latest twelve ideas. Save history is capped at twenty cases to prevent unbounded file growth. The existing campaign consequence system remains the authority for chapters, factions, relationships and city effects.

## Runtime compatibility

The pass does not replace `IdeaZooGame`, create a second case director or duplicate the world. A cached compatibility adapter observes the current HUD evidence request and invokes the existing private evidence method after the playable encounter resolves.

Reflection metadata is resolved once during binding. No reflection lookup, hierarchy scan, UI construction or collection conversion occurs inside the normal gameplay update loop.

## Performance safeguards

The implementation keeps speed and visual quality through:

- one gameplay-depth owner;
- one existing Zoo world and camera;
- four pooled choice buttons;
- twelve pooled archive cards;
- twenty-case save cap;
- state-change-only persistence;
- no new simulation loop for creatures or districts;
- no per-frame hierarchy scans or UI rebuilding;
- the shared safe-area fitter for notches, rounded corners and compact phone screens;
- active Eco 30, Balanced 45 or Quality 60 target detection rather than overriding the selected tier;
- one-second frame sampling;
- quality reduction only after three sustained bad samples;
- quality restoration only after eight sustained healthy samples;
- adaptive reduction limited to nonessential particle capacity and desktop/WebGL LOD;
- explicit deference to the existing mobile-quality controller for render scale, tier, shadows and mobile LOD;
- explicit deference to the hero-slice governor for practical-light pressure.

Gameplay logic, evidence, input and ruling systems are never disabled to recover performance.

## Validation

The pass adds:

- deterministic encounter tests;
- resource-boundary, affordability and reserve-protocol tests;
- disruption repeatability tests;
- persistence-cap tests;
- runtime ownership and world-reuse PlayMode coverage;
- safe-area, allocation, governor-ownership and performance contracts;
- the existing licensed Unity EditMode, PlayMode and WebGL build pipeline.

Physical iPhone testing remains necessary for sustained heat, battery use, touch comfort and device-specific frame pacing.
