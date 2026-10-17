using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The live headbob preview of the <see cref="HeadbobData"/> Inspector: a dot that moves the way the camera would for the chosen
    /// state (a continuous sway for Idle, Walk, Run and Crouch, a vertical one-shot bump for Jump and Land), read straight from the
    /// asset so it follows every edit at once. It repaints about 30 times a second, and only while it is on screen.
    /// </summary>
    internal sealed class HeadbobPreview : VisualElement
    {
        /// <summary>Preview states in order: Idle, Walk, Run, Crouch, Jump start, Land.</summary>
        internal const int StateCount = 6;

        private const int JumpState = 4;
        private const int ContinuousStates = 4;
        private const int TrailSegments = 64;
        private const long RepaintMilliseconds = 33;

        // World-unit amplitude that fills half of the panel's height: keeps the dot on screen over the whole slider range.
        private const float AmplitudeScale = 0.12f;

        // A bump plays once per cycle and then rests for this long, so a very short bump is still readable.
        private const float BumpRest = 0.5f;
        private const float TwoPi = Mathf.PI * 2f;

        private readonly HeadbobData data;
        private readonly Color accent;
        private int state;
        private double startTime;
        private IVisualElementScheduledItem repaint;

        /// <summary>Creates the preview for one asset.</summary>
        /// <param name="data">The asset whose values are drawn.</param>
        /// <param name="accent">The colour of the dot.</param>
        internal HeadbobPreview(HeadbobData data, Color accent)
        {
            this.data = data;
            this.accent = accent;
            AddToClassList("fps-preview");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        /// <summary>The state being previewed (0 = Idle ... 5 = Land); setting it restarts the motion.</summary>
        internal int State
        {
            get => state;
            set
            {
                state = Mathf.Clamp(value, 0, StateCount - 1);
                startTime = EditorApplication.timeSinceStartup;
                MarkDirtyRepaint();
            }
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            startTime = EditorApplication.timeSinceStartup;
            repaint = schedule.Execute(MarkDirtyRepaint).Every(RepaintMilliseconds);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            if (repaint != null)
            {
                repaint.Pause();
                repaint = null;
            }
        }

        // The offset of the camera in world units for the given state and time, the same maths the controller uses.
        private void Sample(int which, float elapsed, out float x, out float y)
        {
            if (which < ContinuousStates)
            {
                float frequency;
                float amplitude;
                switch (which)
                {
                    case 0:
                        frequency = data.IdleFrequency;
                        amplitude = data.IdleAmplitude;
                        break;
                    case 1:
                        frequency = data.Frequency(false, false);
                        amplitude = data.Amplitude(false, false);
                        break;
                    case 2:
                        frequency = data.Frequency(true, false);
                        amplitude = data.Amplitude(true, false);
                        break;
                    default:
                        frequency = data.Frequency(false, true);
                        amplitude = data.Amplitude(false, true);
                        break;
                }

                float t = elapsed * frequency;
                y = Mathf.Sin(t) * amplitude;
                x = Mathf.Cos(t * 0.5f) * amplitude * 0.5f;
                return;
            }

            bool jump = which == JumpState;
            AnimationCurve curve = jump ? data.JumpStartCurve : data.LandCurve;
            float bumpAmplitude = jump ? data.JumpStartAmplitude : data.LandAmplitude;
            float duration = jump ? data.JumpStartDuration : data.LandDuration;
            x = 0f;
            y = 0f;
            if (curve == null || duration <= 0f)
            {
                return;
            }

            float local = Mathf.Repeat(elapsed, duration + BumpRest);
            if (local < duration)
            {
                y = curve.Evaluate(local / duration) * bumpAmplitude;
            }
        }

        private void Draw(MeshGenerationContext mgc)
        {
            Rect rect = contentRect;
            if (data == null || rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            Painter2D painter = mgc.painter2D;
            Vector2 middle = rect.center;
            float pixelsPerUnit = Mathf.Min(rect.width, rect.height) * 0.5f / AmplitudeScale;

            painter.lineWidth = 1f;
            painter.strokeColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, middle.y));
            painter.LineTo(new Vector2(rect.xMax, middle.y));
            painter.MoveTo(new Vector2(middle.x, rect.yMin));
            painter.LineTo(new Vector2(middle.x, rect.yMax));
            painter.Stroke();

            // The whole path of one sway cycle, faint, so the shape of the motion is visible besides the moving dot.
            if (state < ContinuousStates)
            {
                painter.strokeColor = new Color(accent.r, accent.g, accent.b, 0.35f);
                painter.BeginPath();
                for (int i = 0; i <= TrailSegments; i++)
                {
                    Sample(state, i / (float)TrailSegments * TwoPi * 2f / Mathf.Max(StateFrequency(), 0.0001f), out float tx, out float ty);
                    var point = new Vector2(middle.x + tx * pixelsPerUnit, middle.y - ty * pixelsPerUnit);
                    if (i == 0)
                    {
                        painter.MoveTo(point);
                    }
                    else
                    {
                        painter.LineTo(point);
                    }
                }

                painter.Stroke();
            }

            Sample(state, (float)(EditorApplication.timeSinceStartup - startTime), out float x, out float y);
            painter.fillColor = accent;
            painter.BeginPath();
            painter.Arc(new Vector2(middle.x + x * pixelsPerUnit, middle.y - y * pixelsPerUnit), 6f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.ClosePath();
            painter.Fill();
        }

        // The frequency of the state, used to stretch the trail over exactly one full cycle (two sine periods).
        private float StateFrequency()
        {
            switch (state)
            {
                case 0: return data.IdleFrequency;
                case 1: return data.Frequency(false, false);
                case 2: return data.Frequency(true, false);
                default: return data.Frequency(false, true);
            }
        }
    }
}
