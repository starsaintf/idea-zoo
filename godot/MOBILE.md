# Mobile production notes

## Frame budget

Target: 60 FPS on recent mid-range devices and a 30 FPS fallback on older devices.

The first optimization pass should profile:

- draw calls from procedural city pieces;
- overdraw from transparent idea effects;
- UI fill rate on high-density displays;
- collision count around district buildings;
- CPU time from the Queue-Eater segment animation.

## Scale strategy

The project defaults to the Compatibility renderer and lowers 3D resolution on mobile. Art direction is designed to survive resolution scaling: strong silhouettes, flat materials, brass edge accents and large type instead of texture detail.

## Touch strategy

The game uses one analog stick, one held Lens control, one contextual action and one seal selector. It avoids a console controller copied onto the screen. Camera drag occupies the right half outside the buttons.

## Content strategy

Future districts should be compact hubs connected by surreal civic transit, not one giant streaming city. This keeps memory, battery use and traversal density under control while making every location visually authored.

## Android release checklist

- set a production keystore outside the repository;
- export ARM64 first;
- test thermal throttling for 20 minutes;
- test 16:9, 19.5:9 and tablet aspect ratios;
- verify safe areas and gesture navigation overlap;
- profile low-memory background/resume behaviour;
- add graphics presets for 30/60 FPS and 0.65/0.8/1.0 render scale.

## Session loop

The target mobile session is 8–12 minutes: trace, contain, escort, spend four bell-hours, classify, judge, then immediately replay for a better archive score or a different institutional route. Evidence chains expire quickly enough to encourage movement, while all major decisions remain untimed so the player can think.

The top HUD compresses on touch devices. Reports appear as temporary side cards instead of repeatedly stopping movement, while only major phase changes use full-screen records.
