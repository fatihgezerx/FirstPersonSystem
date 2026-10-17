using System;
using Veneer;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The pieces of the <see cref="HeadbobData"/> Inspector that are not plain fields, registered with Veneer so the default layout (and a
    /// user's copy of it) can place them: the live preview card and the widgets of the sliders (soft ranges and lower limits the runtime
    /// fields do not carry). The preview can be moved or hidden in the Inspector Designer, not changed.
    /// </summary>
    internal static class HeadbobDataItems
    {
        /// <summary>Id of the live preview card (footer, so it stays visible under the tabs).</summary>
        public const string PreviewId = "headbob.preview";

        private const float FrequencyMax = 20f;
        private const float AmplitudeMax = 0.2f;
        private const float IdleFrequencyMax = 5f;
        private const float IdleAmplitudeMax = 0.05f;
        private const float BumpAmplitudeMax = 0.3f;
        private const float BumpDurationMax = 1f;
        private const float BumpDurationMin = 0.01f;
        private const float SmoothingMax = 30f;

        private static readonly string[] StateNames = { "Idle", "Walk", "Run", "Crouch", "Jump", "Land" };
        private static readonly string[] StateIcons = { "user", "step", "speed", "arrow-down", "arrow-up", "anchor" };

        /// <summary>Registers the custom items and widgets of <see cref="HeadbobData"/>. Safe to call again (items are replaced).</summary>
        public static void Register()
        {
            Type type = typeof(HeadbobData);

            VeneerCustomItems.Register(type, new VeneerCustomItem
            {
                Id = PreviewId,
                Label = "Live preview",
                Icon = "play",
                Description = "A card with Idle, Walk, Run, Crouch, Jump and Land buttons and a dot that moves like the camera while you edit. Drawn at the bottom, whatever tab is open.",
                Slot = VeneerCustomItemSlot.Footer,
                Build = BuildPreview,
            });

            // The soft slider ends of the old Inspector; the number field keeps the lower limit.
            FirstPersonControls.RegisterSlider(type, "walkFrequency", 0f, FrequencyMax, 0f);
            FirstPersonControls.RegisterSlider(type, "walkAmplitude", 0f, AmplitudeMax, 0f);
            FirstPersonControls.RegisterSlider(type, "runFrequency", 0f, FrequencyMax, 0f);
            FirstPersonControls.RegisterSlider(type, "runAmplitude", 0f, AmplitudeMax, 0f);
            FirstPersonControls.RegisterSlider(type, "crouchFrequency", 0f, FrequencyMax, 0f);
            FirstPersonControls.RegisterSlider(type, "crouchAmplitude", 0f, AmplitudeMax, 0f);
            FirstPersonControls.RegisterSlider(type, "idleFrequency", 0f, IdleFrequencyMax, 0f);
            FirstPersonControls.RegisterSlider(type, "idleAmplitude", 0f, IdleAmplitudeMax, 0f);
            FirstPersonControls.RegisterSlider(type, "jumpStartAmplitude", 0f, BumpAmplitudeMax, 0f);
            FirstPersonControls.RegisterSlider(type, "jumpStartDuration", BumpDurationMin, BumpDurationMax, BumpDurationMin);
            FirstPersonControls.RegisterSlider(type, "landAmplitude", 0f, BumpAmplitudeMax, 0f);
            FirstPersonControls.RegisterSlider(type, "landDuration", BumpDurationMin, BumpDurationMax, BumpDurationMin);
            FirstPersonControls.RegisterSlider(type, "smoothing", 0f, SmoothingMax, 0f);
        }

        private static VisualElement BuildPreview(VeneerCustomItemContext context)
        {
            var data = context.SerializedObject.targetObject as HeadbobData;
            if (data != null)
            {
                new PreviewCard(context, data).Build();
            }

            return null;
        }

        private static string Sway(string name, float frequency, float amplitude) =>
            name + ": " + frequency.ToString("0.0") + " rad/s, " + amplitude.ToString("0.000") + " m (6.28 rad/s is one bob per second)";

        private static string Bump(string name, float amplitude, float duration) =>
            name + ": one vertical bump of " + amplitude.ToString("0.000") + " m over " + duration.ToString("0.00") + " s";

        /// <summary>
        /// One built preview card: the state chips (radio buttons), the <see cref="HeadbobPreview"/> and a caption that is rewritten on data
        /// changes and state picks only, never per frame. It holds its own state, so every Inspector gets its own instance.
        /// </summary>
        private sealed class PreviewCard
        {
            private readonly VeneerCustomItemContext context;
            private readonly HeadbobData data;
            private readonly VisualElement[] chips = new VisualElement[HeadbobPreview.StateCount];
            private HeadbobPreview preview;
            private Label caption;
            private bool selecting;

            public PreviewCard(VeneerCustomItemContext context, HeadbobData data)
            {
                this.context = context;
                this.data = data;
            }

            public void Build()
            {
                VeneerInspectorBuilder b = context.Builder;
                b.Card("Preview", "play", () =>
                {
                    VisualElement states = b.Row(true);
                    states.AddToClassList("fps-preview-states");
                    for (int i = 0; i < chips.Length; i++)
                    {
                        int index = i;
                        chips[i] = VeneerStyle.Chip(StateNames[i], StateIcons[i], i == 0, on => SelectState(index), context.Brand);
                        states.Add(chips[i]);
                    }

                    preview = new HeadbobPreview(data, context.Brand.Accent);
                    b.Add(preview);
                    caption = VeneerStyle.MutedLabel(string.Empty);
                    caption.AddToClassList("fps-preview-caption");
                    b.Add(caption);
                    UpdateCaption();
                    preview.TrackSerializedObjectValue(context.SerializedObject, changed => UpdateCaption());
                });
            }

            private void SelectState(int index)
            {
                if (selecting)
                {
                    return;
                }

                // Chips act as radio buttons: clicking the lit one must not switch it off.
                selecting = true;
                for (int i = 0; i < chips.Length; i++)
                {
                    VeneerStyle.SetChipOn(chips[i], i == index);
                }

                selecting = false;
                preview.State = index;
                UpdateCaption();
            }

            private void UpdateCaption()
            {
                if (caption == null || preview == null || data == null)
                {
                    return;
                }

                switch (preview.State)
                {
                    case 0:
                        caption.text = Sway("Idle", data.IdleFrequency, data.IdleAmplitude);
                        break;
                    case 1:
                        caption.text = Sway("Walk", data.Frequency(false, false), data.Amplitude(false, false));
                        break;
                    case 2:
                        caption.text = Sway("Run", data.Frequency(true, false), data.Amplitude(true, false));
                        break;
                    case 3:
                        caption.text = Sway("Crouch", data.Frequency(false, true), data.Amplitude(false, true));
                        break;
                    case 4:
                        caption.text = Bump("Jump start", data.JumpStartAmplitude, data.JumpStartDuration);
                        break;
                    default:
                        caption.text = Bump("Land", data.LandAmplitude, data.LandDuration);
                        break;
                }
            }
        }
    }
}
