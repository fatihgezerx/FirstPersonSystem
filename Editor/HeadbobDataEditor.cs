using UnityEditor;
using UnityEngine;

namespace FirstPersonSystem
{
    /// <summary>
    /// Custom Inspector for <see cref="HeadbobData"/>: an Idle/Walk/Run/Crouch/Jump Start/Land tab strip, every
    /// frequency/amplitude as a slider (Jump Start/Land get a curve field instead, since they're one-shot, not
    /// continuous), and a live bottom preview panel - the same kind of area Unity shows for a Material or
    /// AnimationClip - that simulates the bob for whichever tab is selected, updating immediately as values change.
    /// </summary>
    [CustomEditor(typeof(HeadbobData))]
    internal sealed class HeadbobDataEditor : Editor
    {
        private static readonly string[] TabNames = { "Idle", "Walk", "Run", "Crouch", "Jump Start", "Land" };
        private const float HeaderHeight = 26f;
        private const float TabHeight = 26f;
        private const float FrequencyMax = 20f;
        private const float AmplitudeMax = 0.2f;
        private const float IdleFrequencyMax = 5f;
        private const float IdleAmplitudeMax = 0.05f;
        private const float BumpAmplitudeMax = 0.3f;
        private const float BumpDurationMax = 1f;
        private const float SmoothingMax = 30f;

        // World-unit amplitude that fills half the preview panel's height - keeps the dot on-screen across the
        // whole slider range instead of clipping at the top amplitude values.
        private const float PreviewAmplitudeScale = 0.12f;

        private SerializedProperty _idleFrequency;
        private SerializedProperty _idleAmplitude;
        private SerializedProperty _walkFrequency;
        private SerializedProperty _walkAmplitude;
        private SerializedProperty _runFrequency;
        private SerializedProperty _runAmplitude;
        private SerializedProperty _crouchFrequency;
        private SerializedProperty _crouchAmplitude;
        private SerializedProperty _jumpStartCurve;
        private SerializedProperty _jumpStartAmplitude;
        private SerializedProperty _jumpStartDuration;
        private SerializedProperty _landCurve;
        private SerializedProperty _landAmplitude;
        private SerializedProperty _landDuration;
        private SerializedProperty _smoothing;
        private int _tab;
        private double _previewStartTime;

        private GUIStyle _headerStyle;
        private GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };

        private void OnEnable()
        {
            _idleFrequency = serializedObject.FindProperty("idleFrequency");
            _idleAmplitude = serializedObject.FindProperty("idleAmplitude");
            _walkFrequency = serializedObject.FindProperty("walkFrequency");
            _walkAmplitude = serializedObject.FindProperty("walkAmplitude");
            _runFrequency = serializedObject.FindProperty("runFrequency");
            _runAmplitude = serializedObject.FindProperty("runAmplitude");
            _crouchFrequency = serializedObject.FindProperty("crouchFrequency");
            _crouchAmplitude = serializedObject.FindProperty("crouchAmplitude");
            _jumpStartCurve = serializedObject.FindProperty("jumpStartCurve");
            _jumpStartAmplitude = serializedObject.FindProperty("jumpStartAmplitude");
            _jumpStartDuration = serializedObject.FindProperty("jumpStartDuration");
            _landCurve = serializedObject.FindProperty("landCurve");
            _landAmplitude = serializedObject.FindProperty("landAmplitude");
            _landDuration = serializedObject.FindProperty("landDuration");
            _smoothing = serializedObject.FindProperty("smoothing");
            _tab = EditorPrefs.GetInt(TabKey, 0);
            _previewStartTime = EditorApplication.timeSinceStartup;

            // The preview panel only repaints on its own schedule (mouse move, selection change...) otherwise,
            // so the bob would look frozen between those - force a steady redraw while this editor is open.
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        // Keyed per-asset, so two different HeadbobData assets remember their own open tab.
        private string TabKey => $"FirstPersonSystem.HeadbobDataEditor.Tab.{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target))}";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("HEADBOB DATA", HeaderStyle, GUILayout.Height(HeaderHeight));
            EditorGUILayout.Space(4);

            var newTab = GUILayout.Toolbar(_tab, TabNames, GUILayout.Height(TabHeight));
            if (newTab != _tab)
            {
                _tab = newTab;
                EditorPrefs.SetInt(TabKey, _tab);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            switch (_tab)
            {
                case 0:
                    EditorGUILayout.Slider(_idleFrequency, 0f, IdleFrequencyMax, new GUIContent("Frequency"));
                    EditorGUILayout.Slider(_idleAmplitude, 0f, IdleAmplitudeMax, new GUIContent("Amplitude"));
                    break;
                case 1:
                    EditorGUILayout.Slider(_walkFrequency, 0f, FrequencyMax, new GUIContent("Frequency"));
                    EditorGUILayout.Slider(_walkAmplitude, 0f, AmplitudeMax, new GUIContent("Amplitude"));
                    break;
                case 2:
                    EditorGUILayout.Slider(_runFrequency, 0f, FrequencyMax, new GUIContent("Frequency"));
                    EditorGUILayout.Slider(_runAmplitude, 0f, AmplitudeMax, new GUIContent("Amplitude"));
                    break;
                case 3:
                    EditorGUILayout.Slider(_crouchFrequency, 0f, FrequencyMax, new GUIContent("Frequency"));
                    EditorGUILayout.Slider(_crouchAmplitude, 0f, AmplitudeMax, new GUIContent("Amplitude"));
                    break;
                case 4:
                    EditorGUILayout.PropertyField(_jumpStartCurve, new GUIContent("Curve"));
                    EditorGUILayout.Slider(_jumpStartAmplitude, 0f, BumpAmplitudeMax, new GUIContent("Amplitude"));
                    EditorGUILayout.Slider(_jumpStartDuration, 0.01f, BumpDurationMax, new GUIContent("Duration"));
                    break;
                default:
                    EditorGUILayout.PropertyField(_landCurve, new GUIContent("Curve"));
                    EditorGUILayout.Slider(_landAmplitude, 0f, BumpAmplitudeMax, new GUIContent("Amplitude"));
                    EditorGUILayout.Slider(_landDuration, 0.01f, BumpDurationMax, new GUIContent("Duration"));
                    break;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.Slider(_smoothing, 0f, SmoothingMax, new GUIContent("Smoothing", "Shared across every state - how quickly the offset blends toward its target each second."));

            serializedObject.ApplyModifiedProperties();
        }

        public override bool HasPreviewGUI() => true;

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint || rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

            float offsetX;
            float offsetY;
            if (_tab <= 3)
            {
                var (frequency, amplitude) = _tab switch
                {
                    0 => (_idleFrequency.floatValue, _idleAmplitude.floatValue),
                    1 => (_walkFrequency.floatValue, _walkAmplitude.floatValue),
                    2 => (_runFrequency.floatValue, _runAmplitude.floatValue),
                    _ => (_crouchFrequency.floatValue, _crouchAmplitude.floatValue)
                };

                var t = (float)(EditorApplication.timeSinceStartup - _previewStartTime) * frequency;
                offsetY = Mathf.Sin(t) * amplitude;
                offsetX = Mathf.Cos(t * 0.5f) * amplitude * 0.5f;
            }
            else
            {
                // One-shot curves have no natural "ongoing" state to preview, so the preview just loops the curve
                // back-to-back every Duration - and stays vertical-only, exactly like the real bump.
                var (curve, amplitude, duration) = _tab == 4
                    ? (_jumpStartCurve.animationCurveValue, _jumpStartAmplitude.floatValue, _jumpStartDuration.floatValue)
                    : (_landCurve.animationCurveValue, _landAmplitude.floatValue, _landDuration.floatValue);

                var elapsed = (float)(EditorApplication.timeSinceStartup - _previewStartTime);
                var t = duration > 0f ? Mathf.Repeat(elapsed, duration) / duration : 0f;
                offsetY = curve.Evaluate(t) * amplitude;
                offsetX = 0f;
            }

            var pixelsPerUnit = Mathf.Min(rect.width, rect.height) * 0.5f / PreviewAmplitudeScale;
            var center = new Vector2(rect.center.x + offsetX * pixelsPerUnit, rect.center.y - offsetY * pixelsPerUnit);

            Handles.BeginGUI();
            Handles.color = new Color(0.4f, 0.4f, 0.4f);
            Handles.DrawLine(new Vector3(rect.xMin, rect.center.y), new Vector3(rect.xMax, rect.center.y));
            Handles.DrawLine(new Vector3(rect.center.x, rect.yMin), new Vector3(rect.center.x, rect.yMax));
            Handles.color = Color.cyan;
            Handles.DrawSolidDisc(center, Vector3.forward, 7f);
            Handles.EndGUI();

            GUI.Label(new Rect(rect.x + 6f, rect.yMax - 20f, rect.width - 12f, 18f), $"Previewing: {TabNames[_tab]}", EditorStyles.whiteMiniLabel);
        }
    }
}
