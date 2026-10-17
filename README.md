# First Person System

A first person rig for keyboard, mouse and gamepad. `FirstPersonController` moves a `CharacterController` (walk, run, jump, crouch with a stand-up check), looks with mouse or stick, eases the field of view while sprinting, plays footsteps through Surface Engine and bobs the camera through an optional `HeadbobData`. All tuning lives in two ScriptableObjects. It runs its own loop through UniTask (no `Update`).

## Setup

1. Put the rig on a GameObject with a `CharacterController` and parent the camera under it at eye height (the Camera Pivot must be a child, or the camera will not follow the body).
2. Create **Create > First Person System > FPS Data** and assign it to the controller's *FPS Data* field (required).
3. Optionally create **Create > First Person System > Headbob Data** for camera headbob; leave the field empty to turn headbob off.
4. Input comes from the project-wide input actions by name (`Move`, `Look`, `Jump`, `Crouch`, `Sprint`); a missing action is replaced by a built-in default binding.

Dependencies are offered by the Setup guard: UniTask and Input System (required), Surface Engine (footsteps), UniMVC (stop and free the cursor while a blocking panel is open) and Veneer (the Inspector look, optional).

## In the editor

With **Veneer** in the project, the three Inspectors share the studio look (sky blue accent, header card, cards, tabs):

- **FPS Data**: a header card with a walk / run speed pill and four tabs. *Move* holds the walk and run speeds and the footstep intervals, *Look* the mouse and gamepad sensitivity, the pitch clamp (one two-handle slider) and the run FOV, *Jump* the height, the gravity (with a take-off speed and rise time summary) and the ascent curve, *Crouch* the crouch speed, the stand and crouch heights, the transition speed and the stand-up obstruction layers. Hints warn when run is slower than walk, when the crouch is not lower than standing, and when a footstep interval is 0.
- **Headbob Data**: a live preview card under the tabs (pick Idle, Walk, Run, Crouch, Jump or Land and watch the camera dot move while you edit), then two tabs. *Moving* holds the Walk, Run and Crouch sway (frequency and amplitude) and the shared smoothing, *Idle and air* the breathing sway and the one-shot Jump start and Land bumps (curve, amplitude, duration). Frequency is in radians per second (6.28 is one bob per second).
- **First Person Controller**: a header card whose pill shows what the rig is doing in Play Mode, a *Data* card (FPS Data, optional Headbob Data) and a *Camera* card (Camera Pivot, Player Camera) with hints for a missing Data asset and a pivot that is not a child of the rig.

- **Customise it.** All three Inspectors are drawn from default layouts that First Person System ships (tabs, cards, fields, notes). With Veneer installed, open `Tools > Veneer > Inspector Designer`, drop the `FPSData`, `HeadbobData` or `FirstPersonController` script into it and press **Customize** to make your own copy; **Reset to system default** returns to the shipped design. The pills, the computed hints, the slider and pitch-clamp widgets and the headbob preview are *system items*: you can move or hide them, but what they do stays in the system's code.

The studio look needs Veneer (`Assets/Scripts/Veneer`, installed by the Setup guard when you accept it). Without it the system still compiles and Unity's default Inspectors are used for FPS Data and Headbob Data.

## Folders

- `Runtime/` the controller and the two data assets (`FirstPersonSystem.Runtime`).
- `Editor/` the default-Inspector fallbacks (`FirstPersonSystem.Editor`; the classes exist only while the kit is missing).
- `Editor/Studio/` the studio-look Inspectors and the headbob preview (`FirstPersonSystem.Editor.Studio`, needs `HAS_VENEER`).
- `Editor/Brand/` logo, banner and the brand style sheet.
- `Editor/Setup/` the dependency guard.
