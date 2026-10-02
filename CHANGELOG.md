# Changelog

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
