using System;
using Veneer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstPersonSystem
{
    /// <summary>
    /// The small controls the First Person System Inspectors need on top of the kit: a lower limit for a slider row's number
    /// field (the runtime fields carry no <c>[Min]</c>, and the old Inspector's sliders kept the typed value inside their range), and
    /// a two-handle slider for the pitch clamp, which is one <see cref="Vector2"/> in the data. Both are offered as custom widgets
    /// (<see cref="RegisterSlider"/>, <see cref="RegisterMinMax"/>) that read their label, captions, unit and tooltip from the layout entry
    /// of the field, so the Inspector Designer can still change those texts.
    /// </summary>
    internal static class FirstPersonControls
    {
        /// <summary>
        /// Registers a custom widget that draws the float at <paramref name="path"/> as a kit slider with a fixed soft range and an optional
        /// lower limit. The label, the two end captions and the unit come from the layout entry of the path (nothing when there is none).
        /// </summary>
        /// <param name="type">The edited type.</param>
        /// <param name="path">The float property path.</param>
        /// <param name="low">Slider start.</param>
        /// <param name="high">Slider end (soft: a larger value can still be typed).</param>
        /// <param name="minimum">Lowest value the number field accepts; NaN for no limit.</param>
        internal static void RegisterSlider(Type type, string path, float low, float high, float minimum)
        {
            VeneerCustomItems.RegisterWidget(type, path, (context, property) =>
            {
                VeneerLayoutEntry entry = EntryOf(context, path);
                VisualElement row = context.Builder.Slider(
                    path,
                    entry != null ? EmptyToNull(entry.Label) : null,
                    entry != null ? EmptyToNull(entry.SliderLeft) : null,
                    entry != null ? EmptyToNull(entry.SliderRight) : null,
                    low,
                    high,
                    entry != null ? EmptyToNull(entry.Unit) : null);
                if (!float.IsNaN(minimum))
                {
                    ClampMinimum(row, property, minimum);
                }

                return row;
            });
        }

        /// <summary>
        /// Registers a custom widget that draws the Vector2 at <paramref name="path"/> as a two-handle slider (<see cref="MinMaxRow"/>). The label
        /// and the tooltip come from the layout entry of the path.
        /// </summary>
        /// <param name="type">The edited type.</param>
        /// <param name="path">The Vector2 property path.</param>
        /// <param name="limit">The slider runs from <c>-limit</c> to <c>limit</c>.</param>
        /// <param name="lowEnd">Caption under the slider's start.</param>
        /// <param name="highEnd">Caption under the slider's end.</param>
        internal static void RegisterMinMax(Type type, string path, float limit, string lowEnd, string highEnd)
        {
            VeneerCustomItems.RegisterWidget(type, path, (context, property) =>
            {
                VeneerLayoutEntry entry = EntryOf(context, path);
                string label = entry != null ? EmptyToNull(entry.Label) : null;
                string tooltip = entry != null ? EmptyToNull(entry.Tooltip) : null;
                return MinMaxRow(property, label ?? property.displayName, limit, tooltip ?? property.tooltip, lowEnd, highEnd);
            });
        }

        /// <summary>The layout entry that holds <paramref name="path"/> in the layout being drawn, or null.</summary>
        /// <param name="context">The context a custom widget is built with.</param>
        /// <param name="path">The property path.</param>
        internal static VeneerLayoutEntry EntryOf(VeneerCustomItemContext context, string path)
        {
            VeneerLayout layout = context.Layout;
            return layout != null ? layout.FindEntry(path, out _, out _) : null;
        }

        private static string EmptyToNull(string text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
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
