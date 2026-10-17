using Veneer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The studio-look Inspector of <see cref="FPSData"/>: a header card with a walk / run speed pill and four tabs. Move holds the
    /// walk and run speeds and the footstep pacing, Look the sensitivities, the pitch clamp and the run FOV, Jump the height, the
    /// gravity and the ascent curve, Crouch the speed, the capsule heights and the stand-up check. Mistakes (run slower than walk,
    /// a crouch as tall as standing, a footstep every frame) show as hints. The number fields keep the lower limits the old sliders
    /// had; the slider ends are soft.
    /// </summary>
    [CustomEditor(typeof(FPSData))]
    [CanEditMultipleObjects]
    internal sealed class FPSDataStudioEditor : VeneerInspector<FPSData>
    {
        private const float PitchLimit = 90f;
        private const float MouseSensitivityMax = 1f;
        private const float GamepadSensitivityMax = 720f;
        private const float FovMin = 30f;
        private const float FovMax = 100f;
        private const float FovTransitionSpeedMax = 20f;
        private const float WalkSpeedMax = 10f;
        private const float RunSpeedMax = 15f;
        private const float CrouchSpeedMax = 8f;
        private const float JumpHeightMax = 4f;
        private const float JumpHeightMin = 0.1f;
        private const float GravityMin = -40f;
        private const float GravityMax = -1f;
        private const float MinimumInterval = 0.01f;

        // The pill text is rebuilt only when one of the two speeds changed, so the 200 ms tick allocates nothing in between.
        private float pillWalk = float.NaN;
        private float pillRun = float.NaN;
        private string pillText = string.Empty;

        protected override VeneerBrand Brand => FirstPersonBrand.Value;

        protected override void Build(VeneerInspectorBuilder b)
        {
            b.Header("FPS Data", "Look, move, jump, crouch and footsteps of one first person feel", null);
            b.StatePill(ReadPill);

            VisualElement[] tabs = b.Tabs(("Move", "speed"), ("Look", "eye"), ("Jump", "arrow-up"), ("Crouch", "arrow-down"));

            using (b.Into(tabs[0]))
            {
                BuildMove(b);
            }

            using (b.Into(tabs[1]))
            {
                BuildLook(b);
            }

            using (b.Into(tabs[2]))
            {
                BuildJump(b);
            }

            using (b.Into(tabs[3]))
            {
                BuildCrouch(b);
            }
        }

        private (string text, string kind) ReadPill()
        {
            MoveSettings move = Target.Move;
            if (move.WalkSpeed != pillWalk || move.RunSpeed != pillRun)
            {
                pillWalk = move.WalkSpeed;
                pillRun = move.RunSpeed;
                pillText = "Walk " + pillWalk.ToString("0.#") + " / Run " + pillRun.ToString("0.#") + " m/s";
            }

            return (pillText, pillRun < pillWalk ? "warn" : "info");
        }

        private void BuildMove(VeneerInspectorBuilder b)
        {
            b.Card("Speed", "speed", () =>
            {
                Slider(b, "move.walkSpeed", "Walk speed", "Slow", "Fast", 0f, WalkSpeedMax, "m/s", 0f);
                Slider(b, "move.runSpeed", "Run speed", "Slow", "Fast", 0f, RunSpeedMax, "m/s", 0f);
                b.ConditionalHint(
                    () => Target.Move.RunSpeed < Target.Move.WalkSpeed ? "Run speed is below walk speed, so holding Sprint slows the rig down." : null,
                    "warn");
            });

            b.Card("Footsteps", "step", () =>
            {
                b.Number("footsteps.walkInterval", "Walk interval", "s");
                b.Number("footsteps.runInterval", "Run interval", "s");
                b.Number("footsteps.crouchInterval", "Crouch interval", "s");
                b.ConditionalHint(
                    () => Target.Footsteps.WalkInterval <= 0f || Target.Footsteps.RunInterval <= 0f || Target.Footsteps.CrouchInterval <= 0f
                        ? "An interval of 0 plays a footstep on every frame. Use a few tenths of a second."
                        : null,
                    "warn");
                b.Hint("Footstep timing comes only from these intervals, whether or not headbob is used.", "info");
            });
        }

        private void BuildLook(VeneerInspectorBuilder b)
        {
            b.Card("Sensitivity", "sliders", () =>
            {
                Slider(b, "look.mouseSensitivity", "Mouse sensitivity", "Slow", "Fast", 0f, MouseSensitivityMax, "°/px", 0f);
                Slider(b, "look.gamepadSensitivity", "Gamepad sensitivity", "Slow", "Fast", 0f, GamepadSensitivityMax, "°/s", 0f);
            });

            b.Card("Pitch", "rotate", () =>
            {
                SerializedProperty pitch = b.SerializedObject.FindProperty("look.pitchClamp");
                b.Add(FirstPersonControls.MinMaxRow(
                    pitch, "Pitch clamp", PitchLimit,
                    "How far the camera can look down (left handle) and up (right handle), in degrees. Applied to Camera Pivot.",
                    "Look down", "Look up"));
            });

            b.Card("Run FOV", "camera", () =>
            {
                Slider(b, "look.runFov", "Run FOV", "Narrow", "Wide", FovMin, FovMax, "°", 0f);
                Slider(b, "look.fovTransitionSpeed", "FOV transition speed", "Slow", "Snappy", 0f, FovTransitionSpeedMax, "1/s", 0f);
            });
        }

        private void BuildJump(VeneerInspectorBuilder b)
        {
            b.Card("Jump", "arrow-up", () =>
            {
                Slider(b, "jump.jumpHeight", "Jump height", "Hop", "High", JumpHeightMin, JumpHeightMax, "m", JumpHeightMin);
                Slider(b, "jump.gravity", "Gravity", "Heavy", "Floaty", GravityMin, GravityMax, "m/s²", float.NaN);
                b.ConditionalHint(JumpSummary, "info");
            });

            b.Card("Rise shape", "graph", () =>
            {
                b.Curve("jump.ascentEase", "Ascent ease");
                b.Hint("Only reshapes how the rise looks (X: 0 = take-off, 1 = apex). Jump time and height stay the same.", "info");
            });
        }

        private string JumpSummary()
        {
            JumpSettings jump = Target.Jump;
            float gravity = Mathf.Abs(jump.Gravity);
            if (gravity < 0.01f || jump.JumpHeight <= 0f)
            {
                return null;
            }

            float velocity = Mathf.Sqrt(jump.JumpHeight * 2f * gravity);
            return "Takes off at " + velocity.ToString("0.0") + " m/s and reaches the top after " + (velocity / gravity).ToString("0.00") + " s.";
        }

        private void BuildCrouch(VeneerInspectorBuilder b)
        {
            b.Card("Crouch", "arrow-down", () =>
            {
                Slider(b, "crouch.crouchSpeed", "Crouch speed", "Slow", "Fast", 0f, CrouchSpeedMax, "m/s", 0f);
                b.Number("crouch.standHeight", "Stand height", "m");
                b.Number("crouch.crouchHeight", "Crouch height", "m");
                b.Number("crouch.transitionSpeed", "Transition speed", "1/s");
                b.ConditionalHint(
                    () => Target.Crouch.CrouchHeight >= Target.Crouch.StandHeight ? "Crouch height is not below stand height, so crouching does not lower the camera." : null,
                    "warn");
            });

            b.Card("Standing up", "shield", () =>
            {
                b.Field("crouch.standUpObstructionMask", "Obstruction layers");
                b.Hint("When Crouch is released, these layers are checked above the head. If something blocks, the rig stays crouched until it clears.", "info");
            });
        }

        // One slider row with the old Inspector's range as its soft ends; minimum limits the number field from below (NaN = no limit).
        private static void Slider(
            VeneerInspectorBuilder b, string path, string label, string leftEnd, string rightEnd, float low, float high, string unit, float minimum) =>
            FirstPersonControls.Slider(b, path, label, leftEnd, rightEnd, low, high, unit, minimum);
    }
}
