using Veneer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The studio-look Inspector of <see cref="FirstPersonController"/> (it had no custom Inspector before): a header card with a pill
    /// that shows what the rig is doing in Play Mode, a Data card (FPS Data, optional Headbob Data) and a Camera card (Camera Pivot,
    /// Player Camera), with hints for a missing Data asset and for a pivot that is not a child of the rig.
    /// </summary>
    [CustomEditor(typeof(FirstPersonController))]
    [CanEditMultipleObjects]
    internal sealed class FirstPersonControllerEditor : VeneerInspector<FirstPersonController>
    {
        protected override VeneerBrand Brand => FirstPersonBrand.Value;

        protected override void Build(VeneerInspectorBuilder b)
        {
            b.Header("First Person Controller", "Walk, look, jump and crouch with keyboard, mouse or gamepad", null);
            b.StatePill(ReadState);

            SerializedProperty data = b.SerializedObject.FindProperty("data");
            SerializedProperty pivot = b.SerializedObject.FindProperty("cameraPivot");

            b.Card("Data", "database", () =>
            {
                b.Object("data", "FPS Data", typeof(FPSData));
                b.ConditionalHint(
                    () => data.objectReferenceValue == null
                        ? "FPS Data is not assigned: the controller logs an error and switches itself off. Create one with Create > First Person System > FPS Data."
                        : null,
                    "error");
                b.Object("headbobData", "Headbob Data", typeof(HeadbobData));
                b.Hint("Headbob Data is optional. Leave it empty to turn camera headbob off.", "info");
            });

            b.Card("Camera", "camera", () =>
            {
                b.Object("cameraPivot", "Camera pivot", typeof(Transform));
                b.ConditionalHint(
                    () =>
                    {
                        var pivotTransform = pivot.objectReferenceValue as Transform;
                        return pivotTransform != null && !pivotTransform.IsChildOf(Target.transform)
                            ? "Camera Pivot is not a child of this object, so the camera will not follow the body."
                            : null;
                    },
                    "warn");
                b.Object("playerCamera", "Player camera", typeof(Camera));
                b.Hint("Empty fields fall back to Camera.main. The pivot receives the pitch; the camera's field of view is eased while running.", "info");
            });
        }

        private (string text, string kind) ReadState()
        {
            FirstPersonController controller = Target;
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
    }
}
