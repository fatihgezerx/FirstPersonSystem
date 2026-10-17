using Veneer;
using UnityEditor;

namespace FirstPersonSystem
{
    /// <summary>
    /// Studio-look Inspector of <see cref="FirstPersonController"/>, drawn from a Veneer layout: the project's own copy made in the
    /// Inspector Designer when there is one, otherwise <see cref="FirstPersonControllerDefaultLayout"/> (a Data card and a Camera card).
    /// The Play Mode state pill and the pivot check are custom items (<see cref="FirstPersonControllerItems"/>).
    /// </summary>
    [CustomEditor(typeof(FirstPersonController))]
    [CanEditMultipleObjects]
    internal sealed class FirstPersonControllerEditor : VeneerLayoutDrivenInspector<FirstPersonController>
    {
        /// <inheritdoc />
        protected override VeneerBrand Brand => FirstPersonBrand.Value;
    }
}
