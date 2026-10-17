using Veneer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The studio-look Inspector of <see cref="HeadbobData"/>: a header card, a live preview card (pick Idle, Walk, Run, Crouch, Jump or
    /// Land and watch the camera dot move as you edit) and two tabs. Moving holds the Walk, Run and Crouch sway plus the shared
    /// smoothing, Idle and air holds the breathing sway and the Jump start and Land bumps (curve, amplitude, duration).
    /// </summary>
    [CustomEditor(typeof(HeadbobData))]
    [CanEditMultipleObjects]
    internal sealed class HeadbobDataStudioEditor : VeneerInspector<HeadbobData>
    {
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

        private readonly VisualElement[] chips = new VisualElement[HeadbobPreview.StateCount];
        private HeadbobPreview preview;
        private Label caption;
        private bool selecting;

        protected override VeneerBrand Brand => FirstPersonBrand.Value;

        protected override void Build(VeneerInspectorBuilder b)
        {
            b.Header("Headbob Data", "Camera sway while walking, jumping and landing", null);

            b.Card("Preview", "play", () =>
            {
                VisualElement states = b.Row(true);
                states.AddToClassList("fps-preview-states");
                for (int i = 0; i < chips.Length; i++)
                {
                    int index = i;
                    chips[i] = VeneerStyle.Chip(StateNames[i], StateIcons[i], i == 0, on => SelectState(index), FirstPersonBrand.Value);
                    states.Add(chips[i]);
                }

                preview = new HeadbobPreview(Target, FirstPersonBrand.Value.Accent);
                b.Add(preview);
                caption = VeneerStyle.MutedLabel(string.Empty);
                caption.AddToClassList("fps-preview-caption");
                b.Add(caption);
                UpdateCaption();
                preview.TrackSerializedObjectValue(serializedObject, changed => UpdateCaption());
            });

            VisualElement[] tabs = b.Tabs(("Moving", "step"), ("Idle and air", "arrow-up"));
            using (b.Into(tabs[0]))
            {
                SwayCard(b, "Walk", "step", "walk", FrequencyMax, AmplitudeMax);
                SwayCard(b, "Run", "speed", "run", FrequencyMax, AmplitudeMax);
                SwayCard(b, "Crouch", "arrow-down", "crouch", FrequencyMax, AmplitudeMax);
                b.Card("Blend", "sliders", () =>
                    FirstPersonControls.Slider(b, "smoothing", "Smoothing", "Soft", "Snappy", 0f, SmoothingMax, "1/s", 0f));
            }

            using (b.Into(tabs[1]))
            {
                SwayCard(b, "Idle (breathing)", "user", "idle", IdleFrequencyMax, IdleAmplitudeMax);
                BumpCard(b, "Jump start", "arrow-up", "jumpStart");
                BumpCard(b, "Land", "anchor", "land");
            }
        }

        // One continuous state: how fast the camera bobs and how far.
        private static void SwayCard(VeneerInspectorBuilder b, string title, string icon, string prefix, float frequencyMax, float amplitudeMax)
        {
            b.Card(title, icon, () =>
            {
                FirstPersonControls.Slider(b, prefix + "Frequency", "Frequency", "Slow", "Fast", 0f, frequencyMax, "rad/s", 0f);
                FirstPersonControls.Slider(b, prefix + "Amplitude", "Amplitude", "Subtle", "Strong", 0f, amplitudeMax, "m", 0f);
            });
        }

        // One one-shot state: the curve (vertical only), its height and how long it plays.
        private static void BumpCard(VeneerInspectorBuilder b, string title, string icon, string prefix)
        {
            b.Card(title, icon, () =>
            {
                b.Curve(prefix + "Curve", "Curve");
                FirstPersonControls.Slider(b, prefix + "Amplitude", "Amplitude", "Subtle", "Strong", 0f, BumpAmplitudeMax, "m", 0f);
                FirstPersonControls.Slider(b, prefix + "Duration", "Duration", "Quick", "Slow", BumpDurationMin, BumpDurationMax, "s", BumpDurationMin);
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

        // Rewritten on data changes and state picks only, never per frame.
        private void UpdateCaption()
        {
            if (caption == null || preview == null)
            {
                return;
            }

            HeadbobData data = Target;
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

        private static string Sway(string name, float frequency, float amplitude) =>
            name + ": " + frequency.ToString("0.0") + " rad/s, " + amplitude.ToString("0.000") + " m (6.28 rad/s is one bob per second)";

        private static string Bump(string name, float amplitude, float duration) =>
            name + ": one vertical bump of " + amplitude.ToString("0.000") + " m over " + duration.ToString("0.00") + " s";
    }
}
