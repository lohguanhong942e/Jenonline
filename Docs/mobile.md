# Making Vesperhold a phone game

## What is already in the code
- **Scaled UI** (`UI/UiScale.cs`): every screen is drawn in a virtual 600-unit-tall space on phones and scaled up, so text and buttons are finger-sized. Safe-area insets keep the HUD away from notches and rounded corners.
- **Touch controls** (`UI/TouchControls.cs`): move, jump, attack, dash, potion and use buttons appear on any touchscreen. Skills are cast by tapping the skill bar icons. A **Menu** button replaces the Esc key (items, stats, skills, map, options).
- **Menus work with a finger**: lists scroll by dragging.
- **Landscape only**: the game asks for landscape and shows "Rotate your phone" in portrait.
- **App lifecycle**: when you switch apps the game saves and opens the pause menu.
- **Battery and speed**: Options has a 30 fps battery saver; particles are halved and the vignette is dropped on phones; the music loops are built a slice per frame so nothing freezes; the screen never sleeps during play.
- **Vibration** on hits and boss roars (Options can turn it off).
- **Texture settings** (`Editor/ArtImport.cs`): ASTC compression and size caps for Android and iOS, pixel-art characters left crisp.

## Three ways to ship it
1. **Mobile web (works today).** `Vesperhold > Build WebGL`, upload `Builds/WebGL` to your host, open the link on the phone, then "Add to Home Screen". Unity 6 WebGL runs on current iPhone and Android browsers. Limits: no store listing, and iPhones are tight on memory, so test early.
2. **Android app.** Install the Android Build Support module in Unity Hub. `Vesperhold > Build Android (.apk to sideload)` for testing on your own phone, or `.aab` for Google Play. Change `BundleId` in `Editor/MobileBuild.cs`, create a keystore (Project Settings > Player > Publishing Settings), and a Play Console account is a one-off 25 USD fee.
3. **iPhone app.** `Vesperhold > Prepare iOS` writes an Xcode project. Building and uploading needs a Mac with Xcode and an Apple developer account (99 USD a year).

## Not done and not testable from here
- None of this has been run on a real phone. Expect layout tweaks (button sizes and positions are the first things to adjust in `TouchControls.Layout`).
- Gamepad support, a settings screen for left-handed layouts, and a tutorial for touch controls.
- Portrait layout is not designed; the game is a side-scrolling platformer, so landscape is the right choice.
