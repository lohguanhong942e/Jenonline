# Improvements added after the skill and boss port

## Items and combat
- `Items/ItemDb.cs`: 30 swords, 30 wands, 30 bows (your class uses its own kind) and 32 armors, matching the Canva icons in `Art/Items`.
  Attack, defense, HP, rarity and price follow smooth curves; change the formulas at the top of the file.
- Wands shoot homing arcane bolts and bows shoot arrows with the J key; swords keep the 3-hit combo. Crits (5% + 1.2% per Luck) deal 1.8x and show gold numbers.
- Drops: every enemy can drop a weapon (5%) or armor (3%) near your level; elites drop 2.5x as often; bosses always drop both. Gold, hearts and potions are pickups that bounce and fly to you.
- Mages scale attack with Int, rogues with Dex, warriors with Str.

## Screens (UI/MenuUI.cs)
Esc opens the menu. I = Items, K = Skills, M = Map. Items: equip weapons and armor. Stats: spend level-up points. Skills: what you own, MP costs and which upgrades are unlocked. Map: rooms you have seen. Options: volume, mute, touch controls, save, quit to title.
Shops: Mirelda (potions and light gear) and Borin (heavy steel) open from their NPCs with E.

## Look and feel
- `World/TileArt.cs`: generated brick, platform and spike tiles per theme, plus a generated backdrop per theme (painted PNGs in `Art/Backdrops` still take priority).
- `World/Lighting.cs`: 2D lights (room ambient, torches, hero lantern) and a bloom/vignette volume. Skipped automatically if the project is not on URP.
- `Editor/ProjectSetup.cs`: creates a URP 2D renderer asset the first time the project opens (menu: Vesperhold > Set up URP 2D).
- Player motion: the character art is one pose, so idle breathing, run bob and lean, jump stretch, attack twist and recoil are done in `PlayerController.Animate`.
- Hit sparks, death bursts, level-up ring, brazier pillar, chest sparkle.

## Sound (Audio/Sfx.cs)
No audio files are needed: effects and two music loops (exploring, boss) are synthesised at runtime. Replace any with `Sfx.Override(name, clip)`.

## Phones (UI/TouchControls.cs)
On-screen buttons appear when a touchscreen is detected (or force them on in Options). Tap the skill bar icons to cast.

## Release (Editor/WebGLBuild.cs)
Menu Vesperhold > Build WebGL, or from a terminal:
`Unity -batchmode -quit -projectPath . -executeMethod Vesperhold.EditorTools.WebGLBuild.Build`
It creates the empty start scene, sets Brotli with the decompression fallback, high code stripping and gamma color space, and writes `Builds/WebGL`.

## Balance knobs
- Mana regen per second: `Tuning.MpRegenBase/PerInt/PerLevel` (`Core/GameData.cs`). Skill cost: `SkillCatalog.ManaCost`. Cooldowns are the lab values x1.6 (`SkillCaster.Cast`).
- XP to next level: `GameData.XpToLevel` (20 + 9 x level, gentler than the web build because skills unlock up to level 42).
- Skill damage is the `mult` number in each recipe; enemy stats are in `Resources/Data/enemies.json`.
- None of this has been play-tested: expect to retune after the first run.

## Still not done
Real character animation frames, rogue art, painted backdrops and tiles for all rooms, music and sound files made by a person.
