# Changelog

## 1.1.1

- Frame the actual equipped tool's hand grip in the first-person view instead of relying solely on fixed arm offsets.
- Preserve animated swings, blocking and bow draws after positioning the resting grip.
- Restore the world-camera pose after nested auxiliary cameras render.

## 1.1.0

- Align animated arms, weapons and shields with the first-person camera so combat and tool actions remain visible while looking up or down.
- Added Show Arms and Weapons and Arm View Offset settings, enabled by default.
- Apply the arm pose only while the first-person camera renders and restore the original animation pose afterward.
- Keep the local player's animation and skin updates active while the first-person camera is enabled; restore previous culling settings on exit.

## 1.0.0

- Added first-person perspective with an F6 default toggle and configurable shortcut.
- Added FOV, eye offsets and near clipping settings.
- FOV slider defaults to Valheim's 65 degrees and ranges from 60 to 110 degrees.
- Added world-camera-only head and equipped helmet/hair/beard hiding with restoration.
- Retained native movement, aiming and camera input; suspend first person for inventory, death, teleportation, cutscenes, attached seats and free-fly.
