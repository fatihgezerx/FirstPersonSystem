using UnityEngine;

namespace FirstPersonSystem
{
    /// <summary>
    /// Optional camera headbob tuning: a sine-based local-position offset <see cref="FirstPersonController"/>
    /// layers on top of whatever it already computes for Camera Pivot (stand/crouch). One continuous state is
    /// always active while grounded - Walk/Run/Crouch while moving, or Idle's subtle "breathing" sway while
    /// still. On top of that, Jump Start and Land each fire a one-shot, vertical-only bump (no left/right sway,
    /// unlike every other state) at the instant they happen, instead of a continuous airborne sway. Leave the
    /// controller's Headbob Data field empty to disable headbob entirely - nothing else changes.
    /// </summary>
    [CreateAssetMenu(menuName = "First Person System/Headbob Data", fileName = "NewHeadbobData")]
    public sealed class HeadbobData : ScriptableObject
    {
        [Header("Idle (Breathing)")]
        [SerializeField] private float idleFrequency = 1.2f;
        [SerializeField] private float idleAmplitude = 0.01f;

        [Header("Walk")]
        [SerializeField] private float walkFrequency = 8f;
        [SerializeField] private float walkAmplitude = 0.035f;

        [Header("Run")]
        [SerializeField] private float runFrequency = 12f;
        [SerializeField] private float runAmplitude = 0.06f;

        [Header("Crouch")]
        [SerializeField] private float crouchFrequency = 6f;
        [SerializeField] private float crouchAmplitude = 0.02f;

        [Header("Jump Start (Anticipation)")]
        [Tooltip("Vertical-only one-shot offset played from the instant the jump begins, over Jump Start Duration. " +
                 "X: normalized time (0-1). Y: offset multiplier, scaled by Jump Start Amplitude.")]
        [SerializeField]
        private AnimationCurve jumpStartCurve = new(new Keyframe(0f, 0f), new Keyframe(0.3f, -1f), new Keyframe(1f, 0f));
        [SerializeField] private float jumpStartAmplitude = 0.05f;
        [SerializeField] private float jumpStartDuration = 0.15f;

        [Header("Land (Recoil)")]
        [Tooltip("Vertical-only one-shot offset played from the instant the controller lands, over Land Duration. " +
                 "X: normalized time (0-1). Y: offset multiplier, scaled by Land Amplitude.")]
        [SerializeField]
        private AnimationCurve landCurve = new(new Keyframe(0f, 0f), new Keyframe(0.2f, -1f), new Keyframe(1f, 0f));
        [SerializeField] private float landAmplitude = 0.08f;
        [SerializeField] private float landDuration = 0.25f;

        [Header("Blend")]
        [Tooltip("How quickly the Idle/Walk/Run/Crouch offset blends toward its target each second (Lerp factor). " +
                 "Higher = snappier. Does not affect Jump Start/Land, which follow their own curve/duration instead.")]
        [SerializeField] private float smoothing = 10f;

        /// <summary>Lerp factor used to blend the continuous (grounded) offset toward its target each frame.</summary>
        public float Smoothing => smoothing;

        /// <summary>Bob cycles per second while standing still (breathing sway).</summary>
        public float IdleFrequency => idleFrequency;

        /// <summary>Bob offset magnitude (world units) while standing still (breathing sway).</summary>
        public float IdleAmplitude => idleAmplitude;

        /// <summary>Bob cycles per second for the given grounded move state.</summary>
        public float Frequency(bool running, bool crouching) =>
            crouching ? crouchFrequency : running ? runFrequency : walkFrequency;

        /// <summary>Bob offset magnitude (world units) for the given grounded move state.</summary>
        public float Amplitude(bool running, bool crouching) =>
            crouching ? crouchAmplitude : running ? runAmplitude : walkAmplitude;

        /// <summary>Curve played once from the instant a jump starts.</summary>
        public AnimationCurve JumpStartCurve => jumpStartCurve;

        /// <summary>Multiplies <see cref="JumpStartCurve"/>'s value (world units).</summary>
        public float JumpStartAmplitude => jumpStartAmplitude;

        /// <summary>Seconds <see cref="JumpStartCurve"/> plays over.</summary>
        public float JumpStartDuration => jumpStartDuration;

        /// <summary>Curve played once from the instant the controller lands.</summary>
        public AnimationCurve LandCurve => landCurve;

        /// <summary>Multiplies <see cref="LandCurve"/>'s value (world units).</summary>
        public float LandAmplitude => landAmplitude;

        /// <summary>Seconds <see cref="LandCurve"/> plays over.</summary>
        public float LandDuration => landDuration;
    }
}
