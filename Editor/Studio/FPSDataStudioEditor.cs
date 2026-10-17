using Veneer;
using UnityEditor;

namespace FirstPersonSystem
{
    /// <summary>
    /// Studio-look Inspector of <see cref="FPSData"/>, drawn from a Veneer layout: the project's own copy made in the Inspector Designer
    /// when there is one, otherwise <see cref="FPSDataDefaultLayout"/> (tabs Move, Look, Jump and Crouch). The walk / run pill, the
    /// computed hints and the slider and pitch widgets are custom items (<see cref="FPSDataItems"/>).
    /// </summary>
    [CustomEditor(typeof(FPSData))]
    [CanEditMultipleObjects]
    internal sealed class FPSDataStudioEditor : VeneerLayoutDrivenInspector<FPSData>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => FirstPersonBrand.Value;
    }
}
