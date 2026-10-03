# DadsFPP

First-person perspective for Valheim 1.0.16 and BepInExPack 5.4.2351.

Press **F6** to switch between first and third person. First person starts enabled. Change the toggle, FOV, eye offset and clipping plane in `BepInEx/config/com.dadisbored.dadsfpp.cfg`. DadsBepInExModManager 1.1.1 supports recording the toggle key; it is optional.

The Field of View slider starts at Valheim's default **65 degrees** and supports **60–110 degrees**. Changes apply immediately in first person. An existing configured value is retained.

Uses Valheim's existing aiming, movement, combat and camera rotation. Your head, helmet, beard and hair are hidden only while the world camera renders. Your body and equipped items remain present. This uses existing third-person animations rather than new first-person weapon animations.

The normal camera is used for inventory previews, death, teleportation, cutscenes, attached seats and free-fly devcommands. Switching back restores camera optics and local visibility. Toggle input is ignored while interacting with menus or text fields.

Install the package DLL into `BepInEx/plugins/DadsFPP`, or import the ZIP into Thunderstore Mod Manager. Install on clients; servers do not need the plugin.

## Build

Run `powershell -File build.ps1 -Package`. Override `ValheimManagedPath` and `BepInExCorePath` as environment variables for a different installation. The ZIP and unpacked folder are created under `dist/`; previous artifacts move to `Archive/package-builds/`. Neither folder belongs in Git.

Status: initial 1.0.0 build. Compilation and game API checks are performed locally; in-game camera, equipment and multi-camera verification is required before publishing.
