# Skills, upgrades and bosses (Unity port)

## Skills
- Data: `Assets/Resources/Data/skills.json` (names, descriptions, cooldowns, unlock levels, icons, 2 upgrades per skill).
- Code: `Assets/Scripts/Skills/`
  - `SkillCaster.cs` keys 1-9, 0, -, = (12 slots), cooldowns (lab cooldown x1.6), mana (3 for slot 1, +2.1 per slot), level unlocks, upgrade follow-ups, skill bar HUD with icons and upgrade pips.
  - `SkillRecipes*.cs` one recipe per skill and two follow-up recipes per skill, per class. Tune the numbers inside each recipe.
  - `Vfx.cs` sprite effects from `Assets/Resources/Art/VFX` (additive shader `Resources/Shaders/VfxAdditive.shader`).
  - `SkillCore.cs` data classes, damage helpers (`Combat`), flying skill bodies (`SkillProjectile`).
- Upgrade 1 and 2 switch on at the `unlockLevel` stored in skills.json (skill unlock + 8 and + 16) and fire `followUpDelay` (+0.5 s for the second) after the cast.
- Class art: `PlayerController.ApplyClassArt` uses `Art/Characters/warrior_male` and `mage_female`. The rogue still uses the placeholder box until its art exists.

## Bosses
- `Assets/Scripts/Enemies/Bosses.cs`
  - `BossBrain`: Bone Warden (walk, charge+dash, leap+shockwaves, summons skeletons under half HP) and Lord Vesper (walk, teleport, 3-way fireballs, slash-dash, summons bats under half HP). Values are the web numbers converted from pixels.
  - `ArenaController`: locks the room when the hero crosses `arena.trigger`, spawns the boss, unlocks and rewards on death (Shadow Step relic after the Warden, ending screen after Vesper).
  - `EnemyProjectile`: shockwaves and dark fireballs.
- Boss HP bar and the ending screen are drawn by `GameManager`.

## Things to check first in the Editor
1. Console: this code was type-checked against stubs only, never inside Unity. Fix whatever Unity reports.
2. Sprite orientation: `BaseDeg` and flip values assume the painted sprites point up or right. If a sword or dagger flies sideways, change `BaseDeg` in that recipe.
3. If effects are invisible in URP 2D, set `Vfx.Additive` to `Sprites/Default` to confirm the sprites load, then check the shader pass.
4. Boss art (`bone_dragon`, `vampire_noble`) may face left; flip with `Enemy.Face` handling in `Enemy.Update` if needed.
