# DadsFPP verification

Build target: Valheim 1.0.16, BepInExPack 5.4.2351. Original game assemblies are used; no publicized references or game DLL modifications are required.

Before publishing, check in-game:

1. Walk, sprint, jump, crouch, swim and aim in first person. Verify head/helmet/hair do not cover the view and the body and weapons remain present.
2. Press F6 repeatedly. Third-person camera and clipping plane should return to normal; head visibility should return.
3. Change the FOV, near clipping and offsets through the F1 manager. Record another toggle shortcut, including mouse side buttons.
4. Open inventory and check the character preview, EPI compartments, crafting and Reclaim. Close inventory to return to first person.
5. Enter chat, console and configuration fields. Bound keys should not toggle perspective while entering text.
6. Equip several helmets, change hair/beard, and toggle Hide Head. Verify other players still see the normal character.
7. Die and respawn, teleport, sit on a chair and use ship controls. Vanilla camera should handle these states, with first person restored when eligible.
8. Enable and disable free-fly using devcommands. Disable DadsFPP in configuration and verify optics and visibility restore.
9. Swing a one-handed weapon, use a two-handed weapon, draw and release a bow, block with a shield, and use the hammer, hoe and pickaxe. Check animated hands and equipment at level aim and while looking up and down.
10. Toggle Show Arms and Weapons and adjust Arm View Offset. Check third-person arms and the inventory preview retain their original poses after each change.
11. Equip a tool, enter first person with F6 and leave it active for at least ten seconds. The held tool must remain visible after the game's camera-distance update. Repeat with another tool, then return to third person and check normal visibility behavior.

Initial compilation succeeds. Automated API and reflection checks accompany the build. Gameplay, multiplayer and rendering behavior cannot be certified without running these scenarios inside Valheim.

Tool grip framing is checked across 48 combinations of hand position, field of view and aspect ratio, including the Dad profile's 90.02386-degree FOV. Each resting grip must project into the lower camera view and remain ahead of the clipping plane.

Automated result: zero build warnings/errors, 22 passing original-game metadata checks, and no missing game references or invalid Harmony patch targets. These include the installed game's two-meter camera hide request and its LOD effect. Run `dotnet run --project tests/ApiChecks/ApiChecks.csproj -- <plugin-dll> <game-managed-directory> <bepinex-core-directory>` to repeat the metadata checks. The checks use Mono.Cecil rather than executing Unity assemblies on the desktop .NET runtime.
