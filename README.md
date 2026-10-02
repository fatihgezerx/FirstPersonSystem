# FirstPersonSystem

Data-driven first person controller for Unity: look, move, jump, crouch, headbob and surface footsteps.

## Overview

FirstPersonSystem is a WASD + mouse-look rig built on a `CharacterController`. Every number it uses
lives in two ScriptableObjects - `FPSData` (look, move, jump, crouch, footsteps) and an optional
`HeadbobData` (camera bob) - so a heavier, floatier or slower rig is a different asset, never a code
change. Both have custom Inspectors; the headbob one includes a live preview panel.

The controller owns no `Update`: it runs its own per-frame loop on UniTask, started in `OnEnable` and
cancelled in `OnDisable`. Headbob and footsteps are both optional - the rig works with neither
assigned.

## Features

- Mouse look with a pitch clamp, click-to-recapture cursor handling (Esc releases it, a click on the
  Game view locks it again) and a smooth FOV narrowing while sprinting
- Walk / run / crouch speeds; **crouch is hold-to-crouch** (`Left Ctrl`). When released under a low
  ceiling the rig stays crouched until there is room to stand, checked against a configurable layer mask
- Jump with height and gravity, plus an **Ascent Ease** curve that reshapes how the rise *looks*
  without changing how high or how long the jump is
- Speed is locked at the moment the feet leave the ground (jumping or walking off a ledge), so
  holding or releasing Run in mid-air changes nothing - and crouch-jumping works
- Optional camera headbob: continuous Idle (breathing), Walk, Run and Crouch sway, plus one-shot
  **Jump Start** and **Land** bumps shaped by curves. While a land bump plays, the next jump is held
  back for `Land Duration`, so a landing can never be cut off by an instant re-jump
- Footsteps paced per move state (walk / run / crouch interval) and played immediately on landing,
  through [SurfaceEngine](https://github.com/fatihgezerx/SurfaceEngine)'s `SurfaceHandler` when it is
  installed - entirely independent of headbob
- Custom Inspectors: `FPSData` with sliders and a pitch min/max slider; `HeadbobData` with an
  Idle / Walk / Run / Crouch / Jump Start / Land tab strip and a live bottom preview
- A dependency guard that installs what is missing and keeps the project compiling without it

## Setup

### Requirements

- Unity 6000.3 LTS or newer
- [UniTask](https://github.com/Cysharp/UniTask) (required)
- Unity [Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest) package, with
  **Active Input Handling** set to *Input System Package* or *Both* (required)
- [SurfaceEngine](https://github.com/fatihgezerx/SurfaceEngine) (optional, for surface-aware footsteps)

### Installation

Clone or download this repository, then copy its contents into `Assets/Scripts/FirstPersonSystem/`.

On the first import FirstPersonSystem's setup dialog offers to install whatever is missing: UniTask and
the Input System through the Package Manager, SurfaceEngine by downloading its repository into
`Assets/Scripts/SurfaceEngine/`. Until the required dependencies are present, the runtime and editor
assemblies are simply left out of compilation, so adding FirstPersonSystem to a project never
produces compile errors.

## Quick Start

**1. Build the player.** Create a GameObject with a `CharacterController` and add
`FirstPersonController` to it. Parent your **Main Camera** under it at eye height - the camera must be
a child, because yaw rotates the body and pitch rotates only the camera pivot.

**2. Create the data.** `Create > First Person System > FPS Data` and, optionally,
`Create > First Person System > Headbob Data`. Assign them to the controller:

| Field | Required | Notes |
|-------|----------|-------|
| Data | yes | `FPSData` asset |
| Headbob Data | no | Leave empty to disable headbob entirely |
| Camera Pivot | no | Child transform that pitch is applied to. Falls back to `Camera.main` |
| Player Camera | no | Camera whose FOV changes while sprinting. Falls back to `Camera.main` |

**3. Press Play.**

| Input | Action |
|-------|--------|
| `W` `A` `S` `D` | Move |
| Mouse | Look |
| `Left Shift` | Run (hold) |
| `Left Ctrl` | Crouch (hold) |
| `Space` | Jump |
| `Esc` | Release / recapture the cursor |

### Surface footsteps (optional)

Add a `SurfaceHandler` from SurfaceEngine to the same GameObject and initialize `SurfaceEngine` with a
`SurfaceData` asset (see the SurfaceEngine README). The controller finds the handler on its own and
calls `Footstep()` at the interval of the current move state and on every landing.

## FPS Data

| Section | Settings |
|---------|----------|
| Look | Mouse Sensitivity, Pitch Clamp, Run FOV, FOV Transition Speed |
| Move | Walk Speed, Run Speed |
| Jump | Jump Height, Gravity, Ascent Ease |
| Crouch | Crouch Speed, Stand Height, Crouch Height, Transition Speed, Stand Up Obstruction Mask |
| Footsteps | Walk / Run / Crouch Interval (seconds) |

The "normal" FOV is whatever is already set on the Player Camera; Run FOV is only the target while
sprinting (grounded, moving, Shift held, not crouching).

## Headbob Data

| Section | Settings |
|---------|----------|
| Idle (Breathing) | Frequency, Amplitude |
| Walk / Run / Crouch | Frequency, Amplitude |
| Jump Start | Curve, Amplitude, Duration (one-shot, vertical only) |
| Land | Curve, Amplitude, Duration (one-shot, vertical only) |
| Blend | Smoothing - how quickly the continuous sway blends between states |

## License

[MIT License](LICENSE)
