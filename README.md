# Vesperhold (Unity)

Unity 6 port of the browser game Vesperhold. The web build in `Docs/vesperhold-web-reference.html` is the design reference.

## Open it
1. Install Unity 6 LTS (6000.0.x) with the **WebGL Build Support** module.
2. Unity Hub > Add > select this folder. Unity regenerates `ProjectSettings` and imports packages.
3. Create or open any empty scene and press Play. `GameManager` builds the whole game at runtime, no scene setup needed.
4. In Project Settings > Player > Active Input Handling choose **Input System Package (New)** (the default in Unity 6).

Controls: A/D move, Space jump, J strike (3-hit combo), L dash, Q potion, E use, Esc pause.

## What is ported
- All 12 rooms (10 castle rooms, Ashen Hunting Grounds, Ember Depths) built from `Assets/Resources/Data/rooms.json`, exported straight from the web build.
- Doors and portals, braziers (save), chests, candles, spawners, elite scaling in Ember Depths.
- Hero with variable jump, double jump (Raven Feather), dash, combo, potions, XP and levels.
- Skeleton, bat, ghost and armor knight with web-build stats. Sword and class tables loaded from JSON.
- Save and load through PlayerPrefs.

## Not ported yet
- Bone Warden and Lord Vesper boss fights, arena locks, and the ending.
- Class picker, skills 1-4, stat allocation screen, inventory and sword equipping, shops (Mirelda, Borin) and NPC dialogue.
- Real art, lighting (URP 2D lights), audio, minimap and map screen, touch controls.
- Compile check: the scripts were written without a Unity Editor and have not been compiled yet. Expect a few fixes on first open.

## Layout
- `Assets/Scripts/Core` data, state, game manager
- `Assets/Scripts/Player`, `Enemies`, `World`
- `Assets/Resources/Data` JSON tables from the web build
