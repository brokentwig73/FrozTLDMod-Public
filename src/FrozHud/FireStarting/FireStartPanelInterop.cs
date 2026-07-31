using Il2Cpp;

namespace FrozTLDMods.FrozTLDMod
{
    internal static class FireStartPanelInterop
    {
        // Reports whether a native panel is enabled and visible in the current hierarchy.
        internal static bool IsPanelActive(Panel_Base panel)
        {
            return panel != null &&
                   panel.gameObject != null &&
                   panel.gameObject.activeInHierarchy &&
                   panel.IsEnabled();
        }

    }
}
