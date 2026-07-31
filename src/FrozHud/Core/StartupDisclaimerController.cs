using System;
using HarmonyLib;
using Il2Cpp;

namespace FrozTLDMods.FrozTLDMod
{
    // Routes the optional setting through the game's native -skipintro path so
    // Panel_Boot remains responsible for its own loading and scene transitions.
    internal static class StartupDisclaimerController
    {
        [HarmonyPatch(
            typeof(Utils),
            nameof(Utils.IsCommandLineArgumentPresent),
            new Type[] { typeof(string) })]
        private static class UtilsIsCommandLineArgumentPresentPatch
        {
            private static bool Prefix(string __0, ref bool __result)
            {
                if (__0 == "-skipintro" &&
                    FrozTLDMod.Settings != null &&
                    FrozTLDMod.Settings.Enabled &&
                    FrozTLDMod.Settings.SkipStartupDisclaimers)
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }
    }
}
