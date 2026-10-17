#if !HAS_VENEER
using UnityEditor;

namespace FirstPersonSystem
{
    /// <summary>
    /// The fallback Inspector of <see cref="FPSData"/>, used only while Veneer is not in the project: Unity's default
    /// drawing of the Look, Move, Jump, Crouch and Footsteps groups. With the kit installed, <c>FPSDataStudioEditor</c> (the
    /// studio-look Inspector in the <c>Studio</c> folder) replaces it.
    /// </summary>
    [CustomEditor(typeof(FPSData))]
    internal sealed class FPSDataEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI() => DrawDefaultInspector();
    }
}
#endif
