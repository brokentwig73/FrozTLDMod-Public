using Il2Cpp;

namespace FrozTLDMods.FrozTLDMod
{
    internal static class PanelCache
    {
        // Returns a valid cached panel or asks the game's panel registry for the exact panel type.
        internal static T Get<T>(T cachedPanel) where T : Panel_Base
        {
            if (cachedPanel != null && cachedPanel.gameObject != null)
            {
                return cachedPanel;
            }

            if (InterfaceManager.TryGetPanel<T>(out var panel) &&
                panel != null &&
                panel.gameObject != null)
            {
                return panel;
            }

            return null;
        }
    }
}
