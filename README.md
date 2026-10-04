# DadsFPP

First-person perspective for Valheim 1.0.16 and BepInExPack 5.4.2351.

Press **F6** to switch between first and third person. First person starts enabled. Change the toggle, FOV, eye offset and clipping plane in `BepInEx/config/com.dadisbored.dadsfpp.cfg`. DadsBepInExModManager 1.1.1 supports recording the toggle key; it is optional.

The Field of View slider starts at Valheim's default **65 degrees** and supports **60–110 degrees**. Changes apply immediately in first person. An existing configured value is retained.

Uses Valheim's existing aiming, movement, combat and camera rotation. Your animated arms, weapons and shields follow the first-person view, including when looking up or down. Attacks, bow draws, blocking and tool use retain their game animations. Your head, helmet, beard and hair are hidden only while the world camera renders.

Under Visibility, **Show Arms and Weapons** enables this positioning. **Arm View Offset** adjusts the rendered position in meters: X right, Y up, Z forward. The original arm pose is restored after rendering so gameplay, third person and inventory previews use the normal character pose.

The equipped tool's resting hand grip is positioned within the lower part of the first-person view. During attacks, blocking and bow draws, its animation moves from that position. The tool model stays attached to the character's hand.

While first person is active, the local player is exempt from Valheim's camera-distance hide request so held tools remain visible after the camera moves to the eyes.

Damage taken and healing appear in a square at the middle right of the screen. Red damage and green healing numbers show the actual health change, with up to three recent entries visible for four seconds.

The first-person camera is raised 20 centimeters above its previous eye position. Vertical Offset adjusts this raised position, including for existing profiles with a zero offset.

The normal camera is used for inventory previews, death, teleportation, cutscenes, attached seats and free-fly devcommands. Switching back restores camera optics and local visibility. Toggle input is ignored while interacting with menus or text fields.

Install the package DLL into `BepInEx/plugins/DadsFPP`, or import the ZIP into Thunderstore Mod Manager. Install on clients; servers do not need the plugin.

## Build

Run `powershell -File build.ps1 -Package`. Override `ValheimManagedPath` and `BepInExCorePath` as environment variables for a different installation. The ZIP and unpacked folder are created under `dist/`; previous artifacts move to `Archive/package-builds/`. Neither folder belongs in Git.

Status: 1.1.4 build. The camera follows the animated head and refreshes its position before rendering. Compilation and game API checks are performed locally; in-game camera, equipment and multi-camera verification is required before publishing.
