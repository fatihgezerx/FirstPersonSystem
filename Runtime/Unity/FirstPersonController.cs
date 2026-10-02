using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonSystem
{
    /// <summary>
    /// WASD + mouse-look first person rig. Runs its own per-frame loop via UniTask (no Update), moves a
    /// CharacterController, and optionally plays footsteps through SurfaceSystem and bobs the camera through
    /// <see cref="HeadbobData"/> - both fully optional, the rig works correctly with neither assigned.
    /// </summary>
    /// <remarks>
    /// Camera Pivot must be a child of this object (parent Main Camera under the Player at eye height) - yaw is
    /// applied to this transform, pitch to Camera Pivot's local rotation, so the camera only follows the body if
    /// it's actually parented under it.
    /// </remarks>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Required. Look/move/jump/crouch tuning values.")]
        [SerializeField] private FPSData data;

        [Tooltip("Optional. Leave empty to disable camera headbob.")]
        [SerializeField] private HeadbobData headbobData;

        [Header("Wiring")]
        [Tooltip("Child transform pitch is applied to. Falls back to Camera.main if left empty.")]
        [SerializeField] private Transform cameraPivot;

        [Tooltip("Camera whose Field Of View is adjusted while running (see FPSData's Look/Run FOV). Falls back to Camera.main if left empty.")]
        [SerializeField] private Camera playerCamera;

        private CharacterController _controller;
        private CancellationTokenSource _loopCts;
        private float _pitch;
        private float _verticalVelocity;
        private float _initialJumpVelocity;
        private float _airborneSpeed;
        private float _footstepTimer;
        private float _headbobTimer;
        private Vector3 _headbobOffset;
        private float _baseFov;
        private AnimationCurve _bumpCurve;
        private float _bumpAmplitude;
        private float _bumpDuration;
        private float _bumpTime = float.PositiveInfinity;
        private float _targetHeight;
        private bool _cursorLocked;
        private bool _wasGrounded = true;
        private bool _isCrouching;
        private Vector3 _standCameraLocalPosition;
        // Blocks jump input for the same span as the land bump (HeadbobData.LandDuration) so a landing can't be
        // cut off by an immediate next jump. 0 when no HeadbobData is assigned - jump is never gated.
        private float _landLockTimer;

        private void OnEnable()
        {
            _controller = GetComponent<CharacterController>();
            CacheSurfaceHandler();

            _targetHeight = data.Crouch.StandHeight;
            _controller.height = data.Crouch.StandHeight;

            if (cameraPivot == null)
            {
                cameraPivot = Camera.main != null ? Camera.main.transform : null;
            }

            if (cameraPivot != null)
            {
                _standCameraLocalPosition = cameraPivot.localPosition;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerCamera != null)
            {
                _baseFov = playerCamera.fieldOfView;
            }

            SetCursorLocked(true);

            _loopCts = new CancellationTokenSource();
            LoopAsync(_loopCts.Token).Forget();
        }

        private void OnDisable()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;

            SetCursorLocked(false);
        }

        private async UniTaskVoid LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var keyboard = Keyboard.current;
                var mouse = Mouse.current;
                if (keyboard == null || mouse == null)
                {
                    return;
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    SetCursorLocked(!_cursorLocked);
                }
                else if (!_cursorLocked && mouse.leftButton.wasPressedThisFrame)
                {
                    // Standard "click to capture" behavior: once unlocked (Escape, or focus was lost elsewhere),
                    // the very first click back on the Game view re-locks and re-enables look immediately -
                    // matching every other FPS, instead of requiring an Escape press first.
                    SetCursorLocked(true);
                }

                // The Editor (and most builds) silently force the cursor unlocked whenever the Game view/
                // application loses focus - leaving Cursor.lockState stuck on None even though _cursorLocked is
                // still (correctly) true, with no reliable single event to react to. Re-asserting it every frame
                // instead of only on the toggles above means it's always correct again within one frame of
                // whatever caused it to drift.
                if (_cursorLocked && Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                var deltaTime = Time.deltaTime;

                if (_cursorLocked)
                {
                    Look(mouse);
                }

                // Crouch key held: always crouch. Released: stand back up, unless something above would block it -
                // in which case stay crouched (forced, not a toggle) until the obstruction clears on its own.
                var crouchHeld = keyboard.leftCtrlKey.isPressed;
                _isCrouching = crouchHeld || (_isCrouching && IsStandUpBlocked());

                var isRunning = keyboard.leftShiftKey.isPressed;
                var speed = _isCrouching ? data.Crouch.CrouchSpeed : isRunning ? data.Move.RunSpeed : data.Move.WalkSpeed;

                var moveInput = ReadMoveInput(keyboard);
                var isMoving = Move(moveInput, speed, keyboard.spaceKey.wasPressedThisFrame, deltaTime, out var justLanded, out var justJumped, out var grounded);

                UpdateCrouch(_isCrouching, deltaTime);
                UpdateHeadbob(isMoving, isRunning, _isCrouching, grounded, justJumped, justLanded, deltaTime);
                UpdateFootsteps(isMoving, isRunning, _isCrouching, justLanded, deltaTime);
                // Narrows FOV only while actually sprinting (grounded, moving, Shift held, not crouching) - just
                // holding Shift while standing still, crouched or airborne must not trigger it.
                UpdateFov(isMoving && isRunning && !_isCrouching, deltaTime);

                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow())
                {
                    return;
                }
            }
        }

        private void Look(Mouse mouse)
        {
            var delta = mouse.delta.ReadValue() * data.Look.MouseSensitivity;
            transform.Rotate(Vector3.up, delta.x);

            if (cameraPivot == null)
            {
                return;
            }

            _pitch = Mathf.Clamp(_pitch - delta.y, data.Look.MinPitch, data.Look.MaxPitch);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        // Narrows toward FPSData's Look/Run FOV while actually sprinting, smoothed by Fov Transition Speed; eases
        // back to whatever FOV was already set on the camera (cached once in OnEnable) the rest of the time.
        private void UpdateFov(bool isSprinting, float deltaTime)
        {
            if (playerCamera == null)
            {
                return;
            }

            var targetFov = isSprinting ? data.Look.RunFov : _baseFov;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, data.Look.FovTransitionSpeed * deltaTime);
        }

        private static Vector2 ReadMoveInput(Keyboard keyboard)
        {
            var x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            var y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y).normalized;
        }

        // Returns whether the rig counts as "walking" this frame (grounded + horizontal input), for the footstep
        // timer. justLanded is true on the exact frame the controller transitions from airborne to grounded.
        private bool Move(Vector2 input, float speed, bool jumpPressed, float deltaTime, out bool justLanded, out bool justJumped, out bool grounded)
        {
            grounded = _controller.isGrounded;
            justLanded = grounded && !_wasGrounded;
            justJumped = false;

            // Speed is locked to whatever it was the instant the feet left the ground (jumping or walking off a
            // ledge) - holding/releasing Run or Crouch mid-air never changes it, only landing does.
            if (!grounded && _wasGrounded)
            {
                _airborneSpeed = speed;
            }

            _wasGrounded = grounded;

            if (grounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
                _initialJumpVelocity = 0f;
            }

            if (justLanded)
            {
                _landLockTimer = headbobData != null ? headbobData.LandDuration : 0f;
            }
            else if (_landLockTimer > 0f)
            {
                _landLockTimer -= deltaTime;
            }

            if (grounded && jumpPressed && _landLockTimer <= 0f)
            {
                _initialJumpVelocity = Mathf.Sqrt(data.Jump.JumpHeight * -2f * data.Jump.Gravity);
                _verticalVelocity = _initialJumpVelocity;
                justJumped = true;
            }

            _verticalVelocity += data.Jump.Gravity * deltaTime;

            // AscentEase only reshapes the rising half's applied velocity (how the rise *looks*) - the fall below
            // always uses the plain gravity-integrated velocity, and apex height/timing are untouched either way.
            var appliedVerticalVelocity = _verticalVelocity;
            if (_initialJumpVelocity > 0f && _verticalVelocity > 0f)
            {
                var t = 1f - Mathf.Clamp01(_verticalVelocity / _initialJumpVelocity);
                appliedVerticalVelocity *= data.Jump.AscentEase.Evaluate(t);
            }

            var effectiveSpeed = grounded ? speed : _airborneSpeed;
            var horizontal = (transform.right * input.x + transform.forward * input.y) * effectiveSpeed;
            var motion = horizontal + Vector3.up * appliedVerticalVelocity;
            _controller.Move(motion * deltaTime);

            return grounded && input.sqrMagnitude > 0.01f;
        }

        // Checked only when Crouch is released: a capsule from the current (possibly still-shrinking) capsule top
        // up to where Stand Height would put the new top. True while anything occupies that space. The
        // CharacterController is briefly disabled for this one query only - otherwise it detects its own capsule
        // (it's a Collider too) and would report "blocked" against itself every time, regardless of layer mask.
        private bool IsStandUpBlocked()
        {
            var standHeight = data.Crouch.StandHeight;
            if (_controller.height >= standHeight)
            {
                return false;
            }

            var radius = _controller.radius;
            var from = transform.position + Vector3.up * _controller.height;
            var to = transform.position + Vector3.up * (standHeight - radius);

            _controller.enabled = false;
            var blocked = Physics.CheckCapsule(from, to, radius, data.Crouch.StandUpObstructionMask, QueryTriggerInteraction.Ignore);
            _controller.enabled = true;

            return blocked;
        }

        // Camera drops by exactly however much the capsule's height just shrank, so the head follows the crouch
        // instead of staying fixed at standing eye height.
        private void UpdateCrouch(bool isCrouching, float deltaTime)
        {
            var crouch = data.Crouch;
            _targetHeight = isCrouching ? crouch.CrouchHeight : crouch.StandHeight;
            _controller.height = Mathf.Lerp(_controller.height, _targetHeight, crouch.TransitionSpeed * deltaTime);
            _controller.center = new Vector3(0f, _controller.height * 0.5f, 0f);

            if (cameraPivot != null)
            {
                var pivotPosition = _standCameraLocalPosition;
                pivotPosition.y -= crouch.StandHeight - _controller.height;
                cameraPivot.localPosition = pivotPosition;
            }
        }

        // Adds a sine-based offset on top of whatever UpdateCrouch just set (so it must run after it), blended
        // in/out with HeadbobData.Smoothing so a sudden stop/start doesn't snap the camera. While grounded, exactly
        // one continuous state drives it: Walk/Run/Crouch while moving, or Idle's subtler "breathing" sway while
        // still; while airborne it settles to zero instead (amplitude 0) rather than swaying continuously. On top
        // of that, a vertical-only one-shot "bump" (see UpdateBump) plays at jump start and at landing - distinct
        // from every other state, which also sways left/right. No-ops entirely when headbobData is unassigned -
        // the field is optional by design.
        private void UpdateHeadbob(bool isMoving, bool isRunning, bool isCrouching, bool grounded, bool justJumped, bool justLanded, float deltaTime)
        {
            if (headbobData == null || cameraPivot == null)
            {
                return;
            }

            if (justJumped)
            {
                TriggerBump(headbobData.JumpStartCurve, headbobData.JumpStartAmplitude, headbobData.JumpStartDuration);
            }

            if (justLanded)
            {
                TriggerBump(headbobData.LandCurve, headbobData.LandAmplitude, headbobData.LandDuration);
            }

            float frequency;
            float amplitude;
            if (!grounded)
            {
                frequency = 0f;
                amplitude = 0f;
            }
            else if (isMoving)
            {
                frequency = headbobData.Frequency(isRunning, isCrouching);
                amplitude = headbobData.Amplitude(isRunning, isCrouching);
            }
            else
            {
                frequency = headbobData.IdleFrequency;
                amplitude = headbobData.IdleAmplitude;
            }

            _headbobTimer += deltaTime * frequency;
            var sin = Mathf.Sin(_headbobTimer);

            var targetOffset = new Vector3(Mathf.Cos(_headbobTimer * 0.5f) * amplitude * 0.5f, sin * amplitude, 0f);
            _headbobOffset = Vector3.Lerp(_headbobOffset, targetOffset, headbobData.Smoothing * deltaTime);

            cameraPivot.localPosition += _headbobOffset + Vector3.up * UpdateBump(deltaTime);
        }

        private void TriggerBump(AnimationCurve curve, float amplitude, float duration)
        {
            _bumpCurve = curve;
            _bumpAmplitude = amplitude;
            _bumpDuration = Mathf.Max(duration, 0.0001f);
            _bumpTime = 0f;
        }

        // Advances and evaluates whichever bump (Jump Start or Land) is currently playing, if any - a plain
        // one-shot curve sample, not blended through HeadbobData.Smoothing like the continuous sway above, since
        // the curve's own shape already defines the whole motion.
        private float UpdateBump(float deltaTime)
        {
            if (_bumpCurve == null || _bumpTime > _bumpDuration)
            {
                return 0f;
            }

            var value = _bumpCurve.Evaluate(_bumpTime / _bumpDuration) * _bumpAmplitude;
            _bumpTime += deltaTime;
            return value;
        }

        // Entirely independent of headbob/UpdateHeadbob - this is the only thing that decides footstep timing.
        private void UpdateFootsteps(bool isMoving, bool isRunning, bool isCrouching, bool justLanded, float deltaTime)
        {
            // Landing plays immediately regardless of movement input, even if you land standing still.
            if (justLanded)
            {
                _footstepTimer = 0f;
                PlayFootstep();
                return;
            }

            if (!isMoving)
            {
                _footstepTimer = 0f;
                return;
            }

            var footsteps = data.Footsteps;
            var interval = isCrouching ? footsteps.CrouchInterval : isRunning ? footsteps.RunInterval : footsteps.WalkInterval;

            _footstepTimer += deltaTime;
            if (_footstepTimer < interval)
            {
                return;
            }

            _footstepTimer -= interval;
            PlayFootstep();
        }

        private void SetCursorLocked(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // SurfaceSystem is an optional dependency (Editor/Setup/DependencyGuard.cs, HAS_SURFACE_SYSTEM) - both
        // branches keep the exact same shape so the rest of the class never needs its own #if.
#if HAS_SURFACE_SYSTEM
        private SurfaceSystem.SurfaceHandler _surfaceHandler;
        private void CacheSurfaceHandler() => _surfaceHandler = GetComponent<SurfaceSystem.SurfaceHandler>();
        private void PlayFootstep() => _surfaceHandler?.Footstep();
#else
        private void CacheSurfaceHandler() { }
        private void PlayFootstep() { }
#endif
    }
}
