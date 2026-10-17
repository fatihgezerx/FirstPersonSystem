# Changelog

## Editor (studio look)

- New studio-look Inspectors on Veneer (sky blue brand, `Editor/Brand`): `FPSData` (Move / Look / Jump / Crouch tabs, pitch clamp as one two-handle slider, validation hints), `HeadbobData` (live preview of the camera dot, Moving / Idle and air tabs, curves) and `FirstPersonController` (Data and Camera cards, state pill, hints; it had no custom Inspector before).
- The old IMGUI Inspectors are now small default-Inspector fallbacks, active only while Veneer is missing. The headbob preview moved into the new Inspector.
- Number fields keep the old sliders' lower limits (no negative speeds or amplitudes, jump height >= 0.1, durations >= 0.01); the slider upper ends are now soft, so a larger value can be typed.
- Setup guard: Veneer added as an optional own-system dependency (`HAS_VENEER`).
- New asmdef `FirstPersonSystem.Editor.Studio` (gated by `HAS_VENEER`); `FirstPersonSystem.Editor` is unchanged.
