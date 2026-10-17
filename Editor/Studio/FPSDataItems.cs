using System;
using Veneer;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The pieces of the <see cref="FPSData"/> Inspector that are not plain fields, registered with Veneer so the default layout (and a
    /// user's copy of it) can place them: the walk / run pill, the computed hints, and the widgets of the sliders (soft ranges and lower
    /// limits the runtime fields do not carry) and of the pitch clamp. The items can be moved or hidden in the Inspector Designer, not changed.
    /// </summary>
    internal static class FPSDataItems
    {
        /// <summary>Id of the header pill with the walk and run speeds (header badge).</summary>
        public const string StateId = "fps.state";

        /// <summary>Id of the "run speed is below walk speed" hint (card item).</summary>
        public const string RunBelowWalkId = "fps.run-below-walk";

        /// <summary>Id of the "a footstep interval of 0" hint (card item).</summary>
        public const string FootstepsId = "fps.footstep-intervals";

        /// <summary>Id of the take-off speed and rise time summary (card item).</summary>
        public const string JumpSummaryId = "fps.jump-summary";

        /// <summary>Id of the "crouch is not lower than standing" hint (card item).</summary>
        public const string CrouchHeightId = "fps.crouch-height";

        /// <summary>The tooltip of the pitch clamp row.</summary>
        public const string PitchTooltip =
            "How far the camera can look down (left handle) and up (right handle), in degrees. Applied to Camera Pivot.";

        private const float PitchLimit = 90f;

        /// <summary>Registers the custom items and widgets of <see cref="FPSData"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(FPSData);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = StateId,
                Label = "Speed pill",
                Icon = "speed",
                Description = "A pill in the header: the walk and run speeds, in warning colour while run is slower than walk.",
                Slot = VeneerCustomItemSlot.HeaderBadge,
                Build = BuildState,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = RunBelowWalkId,
                Label = "Run slower than walk check",
                Icon = "warning",
                Description = "A warning while Run speed is below Walk speed.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildRunBelowWalk,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = FootstepsId,
                Label = "Footstep interval check",
                Icon = "warning",
                Description = "A warning while a footstep interval is 0 or less (a footstep on every frame).",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildFootsteps,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = JumpSummaryId,
                Label = "Jump summary",
                Icon = "arrow-up",
                Description = "A line with the take-off speed and the rise time worked out from the jump height and the gravity.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildJumpSummary,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = CrouchHeightId,
                Label = "Crouch height check",
                Icon = "warning",
                Description = "A warning while Crouch height is not below Stand height.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildCrouchHeight,
            });

            // The soft slider ends of the old Inspector; the number field keeps the lower limit (NaN: none).
            FirstPersonControls.RegisterSlider(type, "move.walkSpeed", 0f, 10f, 0f);
            FirstPersonControls.RegisterSlider(type, "move.runSpeed", 0f, 15f, 0f);
            FirstPersonControls.RegisterSlider(type, "look.mouseSensitivity", 0f, 1f, 0f);
            FirstPersonControls.RegisterSlider(type, "look.gamepadSensitivity", 0f, 720f, 0f);
            FirstPersonControls.RegisterSlider(type, "look.runFov", 30f, 100f, 0f);
            FirstPersonControls.RegisterSlider(type, "look.fovTransitionSpeed", 0f, 20f, 0f);
            FirstPersonControls.RegisterSlider(type, "jump.jumpHeight", 0.1f, 4f, 0.1f);
            FirstPersonControls.RegisterSlider(type, "jump.gravity", -40f, -1f, float.NaN);
            FirstPersonControls.RegisterSlider(type, "crouch.crouchSpeed", 0f, 8f, 0f);
            FirstPersonControls.RegisterMinMax(type, "look.pitchClamp", PitchLimit, "Look down", "Look up");
        }

        private static FPSData DataOf(VeneerCustomItemContext context)
        {
            return context.SerializedObject.targetObject as FPSData;
        }

        private static VisualElement BuildState(VeneerCustomItemContext context)
        {
            FPSData data = DataOf(context);
            Label pill = VeneerStyle.Pill(string.Empty, "info");

            // The text is rebuilt only when one of the two speeds changed, so the 200 ms tick allocates nothing in between.
            float shownWalk = float.NaN;
            float shownRun = float.NaN;
            string text = string.Empty;
            string kind = "info";
            Action refresh = () =>
            {
                if (data == null)
                {
                    return;
                }

                MoveSettings move = data.Move;
                if (move.WalkSpeed != shownWalk || move.RunSpeed != shownRun)
                {
                    shownWalk = move.WalkSpeed;
                    shownRun = move.RunSpeed;
                    text = "Walk " + shownWalk.ToString("0.#") + " / Run " + shownRun.ToString("0.#") + " m/s";
                    kind = shownRun < shownWalk ? "warn" : "info";
                }

                VeneerStyle.SetPill(pill, kind, text);
            };

            // The first run of Poll is on the first tick: show the right state at once.
            refresh();
            context.Poll(refresh);
            return pill;
        }

        private static VisualElement BuildRunBelowWalk(VeneerCustomItemContext context)
        {
            FPSData data = DataOf(context);
            context.Builder.ConditionalHint(
                () => data != null && data.Move.RunSpeed < data.Move.WalkSpeed
                    ? "Run speed is below walk speed, so holding Sprint slows the rig down."
                    : null,
                "warn");
            return null;
        }

        private static VisualElement BuildFootsteps(VeneerCustomItemContext context)
        {
            FPSData data = DataOf(context);
            context.Builder.ConditionalHint(
                () => data != null
                    && (data.Footsteps.WalkInterval <= 0f || data.Footsteps.RunInterval <= 0f || data.Footsteps.CrouchInterval <= 0f)
                        ? "An interval of 0 plays a footstep on every frame. Use a few tenths of a second."
                        : null,
                "warn");
            return null;
        }

        private static VisualElement BuildJumpSummary(VeneerCustomItemContext context)
        {
            FPSData data = DataOf(context);
            context.Builder.ConditionalHint(() => JumpSummary(data), "info");
            return null;
        }

        private static string JumpSummary(FPSData data)
        {
            if (data == null)
            {
                return null;
            }

            JumpSettings jump = data.Jump;
            float gravity = Mathf.Abs(jump.Gravity);
            if (gravity < 0.01f || jump.JumpHeight <= 0f)
            {
                return null;
            }

            float velocity = Mathf.Sqrt(jump.JumpHeight * 2f * gravity);
            return "Takes off at " + velocity.ToString("0.0") + " m/s and reaches the top after " + (velocity / gravity).ToString("0.00") + " s.";
        }

        private static VisualElement BuildCrouchHeight(VeneerCustomItemContext context)
        {
            FPSData data = DataOf(context);
            context.Builder.ConditionalHint(
                () => data != null && data.Crouch.CrouchHeight >= data.Crouch.StandHeight
                    ? "Crouch height is not below stand height, so crouching does not lower the camera."
                    : null,
                "warn");
            return null;
        }
    }
}
