using HarmonyLib;
using Il2Cpp;

namespace FrozTLDMods.FrozTLDMod
{
    // Harmony hook for the same action the game runs when the player presses Tab.
    // Normal Tab presses are consumed by FrozTimeHudController so the vanilla
    // Panel_Actions fade timer cannot fight us. Startup gets one explicit bypass
    // so vanilla can create the TimeWidget template we need to clone.
    internal static class VanillaHudSuppressor
    {
        private static bool _allowNextVanillaSurvivalPanelAction;

        private static bool ShouldToggleOverlay =>
            FrozTLDMod.Settings != null &&
            FrozTLDMod.Settings.Enabled &&
            FrozTLDMod.Settings.HasStickyElement();

        // Allows one native survival-panel action so a fresh session creates the TimeWidget clone template.
        public static void RunVanillaSurvivalPanelActionOnce()
        {
            // This is not a user-visible toggle. It is a bootstrap wake-up for a
            // fresh session where Panel_Actions has not created its TimeWidget yet.
            _allowNextVanillaSurvivalPanelAction = true;
            try
            {
                InputManager.ExecuteSurvivalPanelAction();
            }
            finally
            {
                _allowNextVanillaSurvivalPanelAction = false;
            }
        }

        [HarmonyPatch(typeof(InputManager), "ExecuteSurvivalPanelAction")]
        // Routes Tab to the owned sticky HUD while preserving the one internal bootstrap call.
        private static class InputManagerExecuteSurvivalPanelActionPatch
        {
            private static bool Prefix()
            {
                if (_allowNextVanillaSurvivalPanelAction)
                {
                    // Let exactly this internally-requested vanilla call pass
                    // through. The finally block above always drops the bypass.
                    return true;
                }

                if (!ShouldToggleOverlay)
                {
                    return true;
                }

                var handledByOwnedHud = FrozTLDMod.TimeHud?.ToggleFromHotkey() == true;
                FrozTLDMod.Overlay?.SetStickyDesired(FrozTLDMod.TimeHud?.StickyDesired == true);
                return !handledByOwnedHud;
            }
        }

        [HarmonyPatch(typeof(InterfaceManager), "SetTimeWidgetActive")]
        // Stops Panel_Actions from reactivating its source widget after the owned clone exists.
        private static class InterfaceManagerSetTimeWidgetActivePatch
        {
            private static bool Prefix(bool active)
            {
                if (!active ||
                    !ShouldToggleOverlay ||
                    FrozTLDMod.TimeHud?.HasClone != true)
                {
                    return true;
                }

                // Panel_Actions.Update() requests active=true throughout its fade.
                // Block only that exact shared-widget parent; other native
                // TimeWidget users, including rest and sleep, keep their calls.
                var timeWidget = InterfaceManager.m_TimeWidget;
                var parent = timeWidget != null ? timeWidget.transform?.parent : null;
                return parent == null || parent.name != "Panel_Actions";
            }
        }
    }
}
