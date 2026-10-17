using System;
using Veneer;

namespace FirstPersonSystem
{
    /// <summary>
    /// The design of the <see cref="HeadbobData"/> Inspector that First Person System ships: two tabs (Moving, Idle and air) with a sway
    /// card per continuous state, a bump card per one-shot state, the Blend card, and the live preview (a footer item, drawn under the
    /// tabs so it is visible whatever tab is open). With Veneer installed a project can copy it in the Inspector Designer and change the
    /// copy; this default is never saved as an asset.
    /// </summary>
    internal sealed class HeadbobDataDefaultLayout : IVeneerDefaultLayoutProvider
    {
        /// <inheritdoc />
        public Type TargetType => typeof(HeadbobData);

        /// <inheritdoc />
        public VeneerLayout CreateLayout()
        {
            VeneerLayoutBuilder builder = new VeneerLayoutBuilder(typeof(HeadbobData), "Headbob Data", "Camera sway while walking, jumping and landing")
                .Page("Moving", "step")
                .Card("Preview", "play")
                .Custom(HeadbobDataItems.PreviewId);
            Sway(builder, "Walk", "step", "walk");
            Sway(builder, "Run", "speed", "run");
            Sway(builder, "Crouch", "arrow-down", "crouch");
            builder.Card("Blend", "sliders")
                .Slider("smoothing", "Smoothing", "Soft", "Snappy", "1/s")
                .Page("Idle and air", "arrow-up");
            Sway(builder, "Idle (breathing)", "user", "idle");
            Bump(builder, "Jump start", "arrow-up", "jumpStart");
            Bump(builder, "Land", "anchor", "land");

            VeneerLayout layout = builder.Build();

            // The look of the system (accent, style sheet). UseThemeLogo stays off on purpose: the Inspector's builder already carries the
            // brand and shows its logo in the header.
            layout.ThemeBrand = FirstPersonBrand.Value.Name;
            return layout;
        }

        /// <inheritdoc />
        public void RegisterItems()
        {
            HeadbobDataItems.Register();
        }

        // One continuous state: how fast the camera bobs and how far.
        private static void Sway(VeneerLayoutBuilder builder, string title, string icon, string prefix)
        {
            builder.Card(title, icon)
                .Slider(prefix + "Frequency", "Frequency", "Slow", "Fast", "rad/s")
                .Slider(prefix + "Amplitude", "Amplitude", "Subtle", "Strong", "m");
        }

        // One one-shot state: the curve (vertical only), its height and how long it plays.
        private static void Bump(VeneerLayoutBuilder builder, string title, string icon, string prefix)
        {
            builder.Card(title, icon)
                .Field(prefix + "Curve", "Curve", widget: VeneerWidget.Curve)
                .Slider(prefix + "Amplitude", "Amplitude", "Subtle", "Strong", "m")
                .Slider(prefix + "Duration", "Duration", "Quick", "Slow", "s");
        }
    }
}
