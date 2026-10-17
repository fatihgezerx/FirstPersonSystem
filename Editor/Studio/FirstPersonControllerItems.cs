using System;
using Veneer;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The pieces of the <see cref="FirstPersonController"/> Inspector that are computed, registered with Veneer so the default layout
    /// (and a user's copy of it) can place them: the Play Mode state pill and the "pivot is not a child" hint. They can be moved or hidden
    /// in the Inspector Designer, not changed.
    /// </summary>
    internal static class FirstPersonControllerItems
    {
        /// <summary>Id of the header pill that shows what the rig is doing in Play Mode (header badge).</summary>
        public const string StateId = "fps.controller-state";

        /// <summary>Id of the "Camera Pivot is not a child of this object" hint (card item).</summary>
        public const string PivotCheckId = "fps.pivot-check";

        /// <summary>Registers the custom items of <see cref="FirstPersonController"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(FirstPersonController);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = StateId,
                Label = "State pill",
                Icon = "check-circle",
                Description = "A pill in the header: Edit mode, or in Play Mode what the rig is doing (On foot, Sprinting, Crouching, Jumping, Falling, Off).",
                Slot = VeneerCustomItemSlot.HeaderBadge,
                Build = BuildState,
            });

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = PivotCheckId,
                Label = "Camera pivot check",
                Icon = "warning",
                Description = "A warning while Camera Pivot is not a child of this object.",
                Slot = VeneerCustomItemSlot.Item,
                Build = BuildPivotCheck,
            });
        }

        private static VisualElement BuildState(VeneerCustomItemContext context)
        {
            var controller = context.SerializedObject.targetObject as FirstPersonController;
            Label pill = VeneerStyle.Pill(string.Empty, "idle");
            Action refresh = () =>
            {
                if (controller == null)
                {
                    return;
                }

                (string text, string kind) state = ReadState(controller);
                VeneerStyle.SetPill(pill, state.kind, state.text);
            };

            // The first run of Poll is on the first tick: show the right state at once.
            refresh();
            context.Poll(refresh);
            return pill;
        }

        private static (string text, string kind) ReadState(FirstPersonController controller)
        {
            if (!Application.isPlaying)
            {
                return ("Edit mode", "idle");
            }

            if (!controller.isActiveAndEnabled)
            {
                return ("Off", "warn");
            }

            if (!controller.IsGrounded)
            {
                return controller.IsJumping ? ("Jumping", "info") : ("Falling", "info");
            }

            if (controller.IsCrouching)
            {
                return ("Crouching", "info");
            }

            return controller.IsSprinting ? ("Sprinting", "ok") : ("On foot", "ok");
        }

        private static VisualElement BuildPivotCheck(VeneerCustomItemContext context)
        {
            var controller = context.SerializedObject.targetObject as FirstPersonController;
            var pivot = context.SerializedObject.FindProperty("cameraPivot");
            context.Builder.ConditionalHint(
                () =>
                {
                    var pivotTransform = pivot.objectReferenceValue as Transform;
                    return controller != null && pivotTransform != null && !pivotTransform.IsChildOf(controller.transform)
                        ? "Camera Pivot is not a child of this object, so the camera will not follow the body."
                        : null;
                },
                "warn");
            return null;
        }
    }
}
