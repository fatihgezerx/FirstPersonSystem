using Veneer;
using UnityEditor;

namespace FirstPersonSystem
{
    /// <summary>
    /// Studio-look Inspector of <see cref="HeadbobData"/>, drawn from a Veneer layout: the project's own copy made in the Inspector
    /// Designer when there is one, otherwise <see cref="HeadbobDataDefaultLayout"/> (tabs Moving and Idle and air). The live preview is a
    /// custom item drawn under the tabs (<see cref="HeadbobDataItems"/>, <see cref="HeadbobPreview"/>).
    /// </summary>
    [CustomEditor(typeof(HeadbobData))]
    [CanEditMultipleObjects]
    internal sealed class HeadbobDataStudioEditor : VeneerLayoutDrivenInspector<HeadbobData>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => FirstPersonBrand.Value;
    }
}
