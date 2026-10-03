# Changelog

## [1.3.0] - 2026-10-03

### Changed
- Gameplay input is read from the project-wide input actions (`Move`, `Look`, `Jump`, `Crouch`, `Sprint`)
  instead of polling `Keyboard` / `Mouse` / `Gamepad` directly, so bindings can be changed or rebound in your
  input actions asset. An action the asset lacks is replaced by a built-in one with the previous default
  bindings (the Console lists which). Mouse and stick look are told apart by the device that moved the action.
- Esc and click-to-capture of the cursor still read the keyboard and mouse directly.

## [1.2.0] - 2026-10-03

### Added
- UniMVC integration (optional dependency, `HAS_UNIMVC`): while a panel or popup with **Blocks Gameplay**
  ticked is open (`UIBlocking.IsBlocking`), the rig stops reading movement, look, jump, crouch and run input,
  releases the cursor and ignores Esc / click-to-capture; when the last blocking panel closes the cursor is
  captured again. A rig in the air still falls and lands.
- `DependencyGuard` offers UniMVC as an optional dependency.

## [1.1.2] - 2026-10-03

### Fixed
- Deleting First Person System no longer clears the define symbols it shares with other systems
  (`HAS_UNITASK`, `HAS_INPUT_SYSTEM`): the guard now sets them to whatever is still installed, so other
  systems' assemblies keep compiling.
- The setup dialog now names the optional dependency "Surface Engine" instead of "Surface System".

## [1.1.1] - 2026-10-03

### Fixed
- Footsteps no longer throw `MissingComponentException` in the Editor when SurfaceEngine is installed but
  the player has no `SurfaceHandler` (the handler is now fetched with `TryGetComponent` and null-checked).
- An unassigned **Data** field now logs a clear error and disables the controller instead of throwing a
  `NullReferenceException`.
- A positive or zero **Gravity** (which made the jump velocity `NaN`) is now corrected to a negative value
  by `FPSData.OnValidate`; **Jump Height** can no longer be negative.
- A single long frame (hitch, breakpoint) no longer spikes gravity: frame time is clamped to 0.1 s.

## [1.1.0] - 2026-10-03

### Added
- Gamepad support: left stick moves, right stick looks, South jumps, East crouches (hold), L3 runs.
  Keyboard / mouse and gamepad are read together every frame.
- `FPSData` > Look > **Gamepad Sensitivity** (degrees per second at full stick deflection).

### Fixed
- The controller loop no longer ends permanently when no keyboard or mouse is present (gamepad-only
  setups, or a device unplugged and plugged back in).
- Forced crouch under a low ceiling no longer disables and re-enables the `CharacterController` every
  frame, which reset `isGrounded` and stopped footsteps / headbob while walking under the ceiling. The
  stand-up check now uses `Physics.OverlapCapsuleNonAlloc` and skips the rig's own collider.

## [1.0.0] - 2026-10-03

### Added
- `FirstPersonController`: WASD + mouse-look rig on a `CharacterController`, driven by its own UniTask
  loop (no `Update`). Look with pitch clamp, run / crouch / jump, click-to-recapture cursor handling,
  and a sprint FOV effect.
- Hold-to-crouch with a stand-up obstruction check: releasing Crouch under a low ceiling keeps the rig
  crouched until there is room, checked against a configurable layer mask.
- Airborne speed lock (speed is captured when the feet leave the ground) and crouch-jumping.
- `FPSData` ScriptableObject (**Create > First Person System > FPS Data**) holding every tunable look,
  move, jump, crouch and footstep value, including a jump **Ascent Ease** curve.
- Optional `HeadbobData` ScriptableObject (**Create > First Person System > Headbob Data**): continuous
  Idle / Walk / Run / Crouch sway plus one-shot, curve-shaped Jump Start and Land bumps. The next jump
  is held back for the Land Duration so a landing is never cut off.
- Footsteps paced per move state and played on landing, through SurfaceEngine's `SurfaceHandler`
  when installed.
- Custom Inspectors: `FPSDataEditor` and `HeadbobDataEditor` (tab strip, sliders and a live preview).
- `DependencyGuard`: installs UniTask, the Input System and SurfaceEngine when missing and keeps the
  `HAS_*` define symbols in sync, so the project compiles without them.
