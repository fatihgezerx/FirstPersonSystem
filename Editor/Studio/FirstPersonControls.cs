using Veneer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The two small controls the First Person System Inspectors need on top of the kit: a lower limit for a slider row's number
    /// field (the runtime fields carry no <c>[Min]</c>, and the old Inspector's sliders kept the typed value inside their range), and
    /// a two-handle slider for the pitch clamp, which is one <see cref="Vector2"/> in the data.
    /// </summary>
    internal static class FirstPersonControls
    {
        /// <summary>
        /// Adds a kit slider row whose range is the soft range of the old Inspector, with an optional lower limit on its number field.
        /// </summary>
        /// <param name="b">The builder; the row goes into its current container.</param>
        /// <param name="path">The float property path.</param>
        /// <param name="label">The row label.</param>
        /// <param name="leftEnd">Caption under the slider's start.</param>
        /// <param name="rightEnd">Caption under the slider's end.</param>
        /// <param name="low">Slider start.</param>
        /// <param name="high">Slider end.</param>
        /// <param name="unit">Unit text after the number.</param>
        /// <param name="minimum">Lowest value the number field accepts; NaN for no limit.</param>
        internal static void Slider(
            VeneerInspectorBuilder b, string path, string label, string leftEnd, string rightEnd, float low, float high, string unit, float minimum)
        {
            VisualElement row = b.Slider(path, label, leftEnd, rightEnd, low, high, unit);
            if (!float.IsNaN(minimum))
            {
                ClampMinimum(row, b.SerializedObject.FindProperty(path), minimum);
            }
        }

        /// <summary>
        /// Keeps the number field of a kit slider row (or number row) at or above <paramref name="minimum"/>: a smaller typed value is
        /// written back as the minimum. The slider's upper end stays soft, so a larger value can still be typed.
        /// </summary>
        /// <param name="row">A row made by <c>b.Slider</c> or <c>b.Number</c> for a float property.</param>
        /// <param name="property">The float property the row edits.</param>
        /// <param name="minimum">The lowest allowed value.</param>
        internal static void ClampMinimum(VisualElement row, SerializedProperty property, float minimum)
        {
            FloatField field = row.Q<FloatField>();
            if (field == null || property == null)
            {
                return;
            }

            SerializedProperty tracked = property.Copy();
            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue < minimum)
                {
                    tracked.floatValue = minimum;
                    tracked.serializedObject.ApplyModifiedProperties();
                }
            });
        }

        /// <summary>
        /// A two-handle slider that edits a <see cref="Vector2"/> property as a lowest and a highest value (the pitch clamp: how far the
        /// camera can look down and up). It follows Undo, Redo, prefab reverts and edits of several selected objects like the kit's rows.
        /// </summary>
        /// <param name="property">A Vector2 property (x = lowest, y = highest).</param>
        /// <param name="label">The row label.</param>
        /// <param name="limit">The slider runs from <c>-limit</c> to <c>limit</c>.</param>
        /// <param name="tooltip">The hover text of the row.</param>
        /// <param name="lowEnd">Caption under the slider's start.</param>
        /// <param name="highEnd">Caption under the slider's end.</param>
        internal static VisualElement MinMaxRow(
            SerializedProperty property, string label, float limit, string tooltip, string lowEnd, string highEnd)
        {
            SerializedProperty tracked = property.Copy();

            var block = new VisualElement();
            block.AddToClassList("vn-slider-row");
            block.tooltip = tooltip;

            var top = new VisualElement();
            top.AddToClassList("vn-slider-row__top");
            var title = new Label(label);
            title.AddToClassList("vn-field-row__label");
            top.Add(title);
            var readout = new Label();
            readout.AddToClassList("vn-label-muted");
            readout.AddToClassList("fps-readout");
            top.Add(readout);
            block.Add(top);

            Vector2 start = tracked.vector2Value;
            var slider = new MinMaxSlider(start.x, start.y, -limit, limit);
            slider.AddToClassList("vn-slider-row__slider");
            slider.tooltip = tooltip;
            slider.RegisterValueChangedCallback(evt =>
            {
                tracked.vector2Value = evt.newValue;
                tracked.serializedObject.ApplyModifiedProperties();
            });
            block.Add(slider);

            var ends = new VisualElement();
            ends.AddToClassList("vn-slider-row__ends");
            ends.Add(VeneerStyle.MutedLabel(lowEnd));
            ends.Add(VeneerStyle.MutedLabel(highEnd));
            block.Add(ends);

            VeneerBinding.Hook(block, tracked, title, block, () =>
            {
                Vector2 value = tracked.vector2Value;
                slider.SetValueWithoutNotify(value);
                readout.text = tracked.hasMultipleDifferentValues
                    ? "different values"
                    : value.x.ToString("0") + "° to " + value.y.ToString("0") + "°";
            });
            return block;
        }
    }
}
