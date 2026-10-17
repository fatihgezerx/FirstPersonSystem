using System;
using Veneer;

namespace FirstPersonSystem
{
    /// <summary>
    /// The design of the <see cref="FirstPersonController"/> Inspector that First Person System ships: a Data card (FPS Data, optional
    /// Headbob Data) and a Camera card (Camera pivot, Player camera) with their notes, plus the system's own items (the Play Mode state
    /// pill and the pivot check). With Veneer installed a project can copy it in the Inspector Designer and change the copy.
    /// </summary>
    internal sealed class FirstPersonControllerDefaultLayout : IVeneerDefaultLayoutProvider
    {
        /// <inheritdoc />
        public Type TargetType => typeof(FirstPersonController);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            VeneerLayout layout = new VeneerLayoutBuilder(typeof(FirstPersonController), "First Person Controller", "Walk, look, jump and crouch with keyboard, mouse or gamepad")
                .Page("Controller")
                .Card("Data", "database")
                .Custom(FirstPersonControllerItems.StateId)
                .Field("data", "FPS Data", widget: VeneerWidget.Object)
                .Note(
                    "FPS Data is not assigned: the controller logs an error and switches itself off. Create one with Create > First Person System > FPS Data.",
                    VeneerNoteKind.Error)
                .ShowIf("data", VeneerCondition.IsNull)
                .Field("headbobData", "Headbob Data", widget: VeneerWidget.Object)
                .Note("Headbob Data is optional. Leave it empty to turn camera headbob off.")
                .Card("Camera", "camera")
                .Field("cameraPivot", "Camera pivot", widget: VeneerWidget.Object)
                .Custom(FirstPersonControllerItems.PivotCheckId)
                .Field("playerCamera", "Player camera", widget: VeneerWidget.Object)
                .Note("Empty fields fall back to Camera.main. The pivot receives the pitch; the camera's field of view is eased while running.")
                .Build();

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header.
            layout.ThemeBrand = FirstPersonBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            FirstPersonControllerItems.Register();
        }
    }
}
