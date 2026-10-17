#if !HAS_VENEER
using UnityEditor;

namespace FirstPersonSystem
{
    /// <summary>
    /// The fallback Inspector of <see cref="HeadbobData"/>, used only while Veneer is not in the project: Unity's default
    /// drawing of the Idle, Walk, Run, Crouch, Jump Start, Land and Blend groups. With the kit installed,
    /// <c>HeadbobDataStudioEditor</c> (the studio-look Inspector with the live preview, in the <c>Studio</c> folder) replaces it.
    /// </summary>
    [CustomEditor(typeof(HeadbobData))]
    internal sealed class HeadbobDataEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI() => DrawDefaultInspector();
    }
}
#endif
