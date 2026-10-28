# Art status

## Done (in repo)
- Items: Assets/Resources/Art/Items (30 swords, 30 wands, 30 bows, 32 armor), Canva.
- Monsters: Assets/Resources/Art/Monsters (skeleton, vampire_bat, ghost, haunted_armor, gargoyle, ghoul, wraith, giant_spider, vampire_noble, bone_dragon), Canva.
- Backdrop: Assets/Resources/Art/Backdrops/gate.jpg, Canva.

## Generated in SpriteCook, not downloaded yet
The container's network blocks api.spritecook.ai, so these live only in the SpriteCook account.
- Male Warrior "Ashen Warden" base (platformer, 188x188): asset_id 0c4dfee6-e326-444f-97b5-c2a52361cb32
- Female Mage (cute, chibi-leaning, 80x80): asset_id 7273cdcd-57ed-45c2-bb10-6c8942818cdd
Download both from the SpriteCook dashboard and save as Assets/Resources/Art/Characters/warrior_male.png and mage_female.png.
Credits left: 16 of 40.

## Not started
Rogue (male and female), Warrior female, Mage male, character animations, tilesets, more backdrops (hall, library, chapel, catacomb, under, armory, tower, throne).

## Skill art (Canva, added)
- Skill icons: Assets/Resources/Art/Skills/{warrior,mage,rogue}/skill_01..12.png (256 px, round).
- Upgrade icons: Assets/Resources/Art/Upgrades/{warrior,mage,rogue}/up_01..24.png (190 px, round). Upgrade n of skill i is up_(2*i+n) with i from 0.
- Names, descriptions, unlock levels and icon paths: Assets/Resources/Data/skills.json.
- Effect designs live in the browser Skill Lab and are the reference for the Unity VFX port.

## Added in the polish pass
- Characters: warrior_male (cute), mage_female, rogue_male in Assets/Resources/Art/Characters.
- Backdrops (1280x720 JPG, Canva): gate, hall, library, chapel, catacomb, under, armory, tower, throne in Assets/Resources/Art/Backdrops. All nine themes now have painted backdrops.
- Tiles are generated in code (World/TileArt.cs); painted tiles could replace them later.
- Still missing: female warrior, male mage, female rogue, real animation frames.

## 30 more Canva images (UI and props)
- Art/UI: logo, title_art, panel_frame, slot_frame, button_plate, bossbar_frame, banner_victory, banner_gameover, icon_heart, icon_mana, icon_gold, icon_potion, crest_warrior, crest_mage, crest_rogue.
- Art/Props: brazier, chest, candle, portal, relic_feather, relic_shadowstep, key_sovereign, gate_sealed.
- Art/Portraits: npc_sexton, npc_mirelda, npc_borin, portrait_bone_warden, portrait_lord_vesper.
- Art/Backdrops: field.jpg (Ashen Hunting Grounds) and depths.jpg (Ember Depths); a file named after the room id wins over the theme backdrop.
- Where they are used: title screen (title_art + logo + class crests), HUD (icons, crest, boss bar frame), skill slots (slot_frame), menus and dialogue (panel_frame, button_plate), death and victory banners, NPC dialogue and shop portraits, boss intro cards, brazier/chest/candle/portal/relic/sealed gate sprites.
- sword/key_sovereign has no use yet (the key is a chest reward); it is ready for an inventory entry.
