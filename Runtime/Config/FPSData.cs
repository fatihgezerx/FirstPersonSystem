using System;
using UnityEngine;

namespace FirstPersonSystem
{
    /// <summary>Mouse look sensitivity, the pitch clamp applied to Camera Pivot, and the run FOV effect.</summary>
    [Serializable]
    public sealed class LookSettings
    {
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private Vector2 pitchClamp = new(-85f, 85f);

        [Tooltip("Field of View while running. The controller eases toward this and back using whatever FOV is " +
                 "already set on Player Camera as the \"normal\" value - there is no separate base FOV field here.")]
        [SerializeField] private float runFov = 55f;
        [SerializeField] private float fovTransitionSpeed = 8f;

        /// <summary>Mouse delta multiplier.</summary>
        public float MouseSensitivity => mouseSensitivity;

        /// <summary>Lowest pitch angle (looking down), in degrees.</summary>
        public float MinPitch => pitchClamp.x;

        /// <summary>Highest pitch angle (looking up), in degrees.</summary>
        public float MaxPitch => pitchClamp.y;

        /// <summary>Field of View eased toward while running.</summary>
        public float RunFov => runFov;

        /// <summary>Lerp factor used to blend Player Camera's field of view toward its target each frame.</summary>
        public float FovTransitionSpeed => fovTransitionSpeed;
    }

    /// <summary>Walk/run speed.</summary>
    [Serializable]
    public sealed class MoveSettings
    {
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 7f;

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
    }

    /// <summary>
    /// Jump height, gravity, and the curve that shapes the feel of the rise only (the fall is always plain
    /// gravity). The curve's X axis is 0 at the moment of the jump and 1 at the apex; its Y value multiplies that
    /// frame's upward velocity before it's applied to the CharacterController, so it reshapes how the rise *looks*
    /// without changing how long the jump takes or how high it goes - both of those stay governed by JumpHeight/
    /// Gravity alone. A flat curve at 1 (the default) means no change from plain physics.
    /// </summary>
    [Serializable]
    public sealed class JumpSettings
    {
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;

        [Tooltip("Multiplies the rising half of the jump only (X: 0 = jump start, 1 = apex). The fall always uses " +
                 "plain gravity. Default is a flat 1 - shape it to taste, e.g. a slower rise near the apex.")]
        [SerializeField] private AnimationCurve ascentEase = AnimationCurve.Constant(0f, 1f, 1f);

        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public AnimationCurve AscentEase => ascentEase;
    }

    /// <summary>Crouch speed, capsule heights and the stand/crouch transition speed.</summary>
    [Serializable]
    public sealed class CrouchSettings
    {
        [SerializeField] private float crouchSpeed = 2f;
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float transitionSpeed = 8f;

        [Tooltip("Layers checked when Crouch is released, to see if something above would block standing up.")]
        [SerializeField] private LayerMask standUpObstructionMask = ~0;

        public float CrouchSpeed => crouchSpeed;
        public float StandHeight => standHeight;
        public float CrouchHeight => crouchHeight;
        public float TransitionSpeed => transitionSpeed;
        public LayerMask StandUpObstructionMask => standUpObstructionMask;
    }

    /// <summary>
    /// Time between footstep calls, per move state (seconds). Entirely independent of HeadbobData/headbob - this
    /// is the only thing that decides footstep timing, always, regardless of whether headbob is in use.
    /// </summary>
    [Serializable]
    public sealed class FootstepSettings
    {
        [SerializeField] private float walkInterval = 0.5f;
        [SerializeField] private float runInterval = 0.35f;
        [SerializeField] private float crouchInterval = 0.7f;

        public float WalkInterval => walkInterval;
        public float RunInterval => runInterval;
        public float CrouchInterval => crouchInterval;
    }

    /// <summary>
    /// Every tunable value <see cref="FirstPersonController"/> reads: look sensitivity/pitch clamp/run FOV,
    /// walk/run speed, jump/gravity/ascent curve, crouch heights, and footstep pacing per move state. Author once
    /// as an asset and assign it to the controller's Data field - swap assets to get a differently-feeling rig
    /// (heavier, floatier, slower...) without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "First Person System/FPS Data", fileName = "NewFPSData")]
    public sealed class FPSData : ScriptableObject
    {
        [SerializeField] private LookSettings look = new();
        [SerializeField] private MoveSettings move = new();
        [SerializeField] private JumpSettings jump = new();
        [SerializeField] private CrouchSettings crouch = new();
        [SerializeField] private FootstepSettings footsteps = new();

        public LookSettings Look => look;
        public MoveSettings Move => move;
        public JumpSettings Jump => jump;
        public CrouchSettings Crouch => crouch;
        public FootstepSettings Footsteps => footsteps;
    }
}
