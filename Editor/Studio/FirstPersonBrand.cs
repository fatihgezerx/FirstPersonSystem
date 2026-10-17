using Veneer;
using UnityEngine;

namespace FirstPersonSystem
{
    /// <summary>The First Person System brand for the Veneer look: sky blue accent, logo and banner from <c>Editor/Brand</c>.</summary>
    internal static class FirstPersonBrand
    {
        /// <summary>The accent class the brand style sheet answers to (the exact sky blue, instead of a kit preset).</summary>
        internal const string AccentClass = "fps-accent";

        /// <summary>The cached brand instance; use it for every Inspector of the system.</summary>
        internal static readonly VeneerBrand Value = VeneerBrand.FromAssets(
            name: "First Person System",
            subtitle: "A first person rig for keyboard, mouse and gamepad",
            accent: new Color32(0x00, 0xA2, 0xC7, 255),
            logoPath: "Assets/Scripts/FirstPersonSystem/Editor/Brand/logo.png",
            bannerPath: "Assets/Scripts/FirstPersonSystem/Editor/Brand/banner.png",
            iconFolder: null,
            styleSheetPath: "Assets/Scripts/FirstPersonSystem/Editor/Brand/FirstPersonBrand.uss",
            accentClass: AccentClass);
    }
}
