using System;
using UnityEditor;
using UnityEngine;

namespace FirstPersonSystem
{
    /// <summary>
    /// Custom Inspector for <see cref="FPSData"/>: Look/Move/Jump/Crouch, each its own bold-headed box stacked
    /// vertically (no tab strip). Speeds and sensitivity are sliders; Look's pitch clamp is a single
    /// <see cref="EditorGUILayout.MinMaxSlider(ref float, ref float, float, float, GUILayoutOption[])"/> instead
    /// of two separate min/max float fields; Jump's ascent ease is Unity's own curve field.
    /// </summary>
    [CustomEditor(typeof(FPSData))]
    internal sealed class FPSDataEditor : Editor
    {
        private const float HeaderHeight = 26f;
        private const float PitchLimit = 90f;
        private const float MouseSensitivityMax = 1f;
        private const float FovMin = 30f;
        private const float FovMax = 100f;
        private const float FovTransitionSpeedMax = 20f;
        private const float WalkSpeedMax = 10f;
        private const float RunSpeedMax = 15f;
        private const float CrouchSpeedMax = 8f;
        private const float JumpHeightMax = 4f;
        private const float GravityMin = -40f;
        private const float GravityMax = -1f;

        private SerializedProperty _look;
        private SerializedProperty _move;
        private SerializedProperty _jump;
        private SerializedProperty _crouch;
        private SerializedProperty _footsteps;

        private GUIStyle _headerStyle;
        private GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };

        private void OnEnable()
        {
            _look = serializedObject.FindProperty("look");
            _move = serializedObject.FindProperty("move");
            _jump = serializedObject.FindProperty("jump");
            _crouch = serializedObject.FindProperty("crouch");
            _footsteps = serializedObject.FindProperty("footsteps");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("FPS DATA", HeaderStyle, GUILayout.Height(HeaderHeight));
            EditorGUILayout.Space(6);

            DrawSection("Look", DrawLook);
            EditorGUILayout.Space(8);
            DrawSection("Move", DrawMove);
            EditorGUILayout.Space(8);
            DrawSection("Jump", DrawJump);
            EditorGUILayout.Space(8);
            DrawSection("Crouch", DrawCrouch);
            EditorGUILayout.Space(8);
            DrawSection("Footsteps", DrawFootsteps);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSection(string title, Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            content();
            EditorGUILayout.EndVertical();
        }

        private void DrawLook()
        {
            EditorGUILayout.Slider(_look.FindPropertyRelative("mouseSensitivity"), 0f, MouseSensitivityMax, new GUIContent("Mouse Sensitivity"));

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Pitch Clamp");

            var pitchClamp = _look.FindPropertyRelative("pitchClamp");
            var range = pitchClamp.vector2Value;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{range.x:F0}°", GUILayout.Width(36));
            EditorGUILayout.MinMaxSlider(ref range.x, ref range.y, -PitchLimit, PitchLimit);
            EditorGUILayout.LabelField($"{range.y:F0}°", GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();

            pitchClamp.vector2Value = range;

            EditorGUILayout.Space(10);
            EditorGUILayout.Slider(_look.FindPropertyRelative("runFov"), FovMin, FovMax, new GUIContent("Run FOV"));
            EditorGUILayout.Slider(_look.FindPropertyRelative("fovTransitionSpeed"), 0f, FovTransitionSpeedMax, new GUIContent("Fov Transition Speed"));
        }

        private void DrawMove()
        {
            EditorGUILayout.Slider(_move.FindPropertyRelative("walkSpeed"), 0f, WalkSpeedMax, new GUIContent("Walk Speed"));
            EditorGUILayout.Slider(_move.FindPropertyRelative("runSpeed"), 0f, RunSpeedMax, new GUIContent("Run Speed"));
        }

        private void DrawJump()
        {
            EditorGUILayout.Slider(_jump.FindPropertyRelative("jumpHeight"), 0.1f, JumpHeightMax, new GUIContent("Jump Height"));
            EditorGUILayout.Slider(_jump.FindPropertyRelative("gravity"), GravityMin, GravityMax, new GUIContent("Gravity"));
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_jump.FindPropertyRelative("ascentEase"), new GUIContent("Ascent Ease"));
        }

        private void DrawCrouch()
        {
            EditorGUILayout.Slider(_crouch.FindPropertyRelative("crouchSpeed"), 0f, CrouchSpeedMax, new GUIContent("Crouch Speed"));
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_crouch.FindPropertyRelative("standHeight"), new GUIContent("Stand Height"));
            EditorGUILayout.PropertyField(_crouch.FindPropertyRelative("crouchHeight"), new GUIContent("Crouch Height"));
            EditorGUILayout.PropertyField(_crouch.FindPropertyRelative("transitionSpeed"), new GUIContent("Transition Speed"));
            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_crouch.FindPropertyRelative("standUpObstructionMask"), new GUIContent("Stand Up Obstruction Mask"));
        }

        private void DrawFootsteps()
        {
            EditorGUILayout.PropertyField(_footsteps.FindPropertyRelative("walkInterval"), new GUIContent("Walk Interval"));
            EditorGUILayout.PropertyField(_footsteps.FindPropertyRelative("runInterval"), new GUIContent("Run Interval"));
            EditorGUILayout.PropertyField(_footsteps.FindPropertyRelative("crouchInterval"), new GUIContent("Crouch Interval"));
        }
    }
}
