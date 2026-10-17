using System;
using Veneer;

namespace FirstPersonSystem
{
    /// <summary>
    /// The design of the <see cref="FPSData"/> Inspector that First Person System ships: four tabs (Move, Look, Jump, Crouch) with their
    /// cards, sliders, number rows, notes and the system's own items (walk / run pill and the computed hints). With Veneer installed a
    /// project can copy it in the Inspector Designer and change the copy; this default is never saved as an asset.
    /// </summary>
    internal sealed class FPSDataDefaultLayout : IVeneerDefaultLayoutProvider
    {
        /// <inheritdoc />
        public Type TargetType => typeof(FPSData);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            VeneerLayout layout = new VeneerLayoutBuilder(typeof(FPSData), "FPS Data", "Look, move, jump, crouch and footsteps of one first person feel")
                .Page("Move", "speed")
                .Card("Speed", "speed")
                .Custom(FPSDataItems.StateId)
                .Slider("move.walkSpeed", "Walk speed", "Slow", "Fast", "m/s")
                .Slider("move.runSpeed", "Run speed", "Slow", "Fast", "m/s")
                .Custom(FPSDataItems.RunBelowWalkId)
                .Card("Footsteps", "step")
                .Field("footsteps.walkInterval", "Walk interval", unit: "s", widget: VeneerWidget.Number)
                .Field("footsteps.runInterval", "Run interval", unit: "s", widget: VeneerWidget.Number)
                .Field("footsteps.crouchInterval", "Crouch interval", unit: "s", widget: VeneerWidget.Number)
                .Custom(FPSDataItems.FootstepsId)
                .Note("Footstep timing comes only from these intervals, whether or not headbob is used.")
                .Page("Look", "eye")
                .Card("Sensitivity", "sliders")
                .Slider("look.mouseSensitivity", "Mouse sensitivity", "Slow", "Fast", "°/px")
                .Slider("look.gamepadSensitivity", "Gamepad sensitivity", "Slow", "Fast", "°/s")
                .Card("Pitch", "rotate")
                .Field("look.pitchClamp", "Pitch clamp", FPSDataItems.PitchTooltip)
                .Card("Run FOV", "camera")
                .Slider("look.runFov", "Run FOV", "Narrow", "Wide", "°")
                .Slider("look.fovTransitionSpeed", "FOV transition speed", "Slow", "Snappy", "1/s")
                .Page("Jump", "arrow-up")
                .Card("Jump", "arrow-up")
                .Slider("jump.jumpHeight", "Jump height", "Hop", "High", "m")
                .Slider("jump.gravity", "Gravity", "Heavy", "Floaty", "m/s²")
                .Custom(FPSDataItems.JumpSummaryId)
                .Card("Rise shape", "graph")
                .Field("jump.ascentEase", "Ascent ease", widget: VeneerWidget.Curve)
                .Note("Only reshapes how the rise looks (X: 0 = take-off, 1 = apex). Jump time and height stay the same.")
                .Page("Crouch", "arrow-down")
                .Card("Crouch", "arrow-down")
                .Slider("crouch.crouchSpeed", "Crouch speed", "Slow", "Fast", "m/s")
                .Field("crouch.standHeight", "Stand height", unit: "m", widget: VeneerWidget.Number)
                .Field("crouch.crouchHeight", "Crouch height", unit: "m", widget: VeneerWidget.Number)
                .Field("crouch.transitionSpeed", "Transition speed", unit: "1/s", widget: VeneerWidget.Number)
                .Custom(FPSDataItems.CrouchHeightId)
                .Card("Standing up", "shield")
                .Field("crouch.standUpObstructionMask", "Obstruction layers")
                .Note("When Crouch is released, these layers are checked above the head. If something blocks, the rig stays crouched until it clears.")
                .Build();

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header.
            layout.ThemeBrand = FirstPersonBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            FPSDataItems.Register();
        }
    }
}
