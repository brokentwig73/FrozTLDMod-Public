using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // vp_FPSPlayer.InputWalk() queries GetButtonPressed(AutoWalk) directly.
    // Reintroduce the missing Windows Z-key press only for that action so the
    // game's existing context checks, toggle behavior, and cancellation rules
    // remain responsible for AutoWalk.
    internal static class AutoWalkController
    {
        [HarmonyPatch(typeof(InputSystemRewired), nameof(InputSystemRewired.GetButtonPressed), new[] { typeof(InputManager.InputAction) })]
        private static class InputSystemRewiredGetButtonPressedPatch
        {
            private static void Postfix(InputManager.InputAction action, ref bool __result)
            {
                if (__result ||
                    action != InputManager.InputAction.AutoWalk ||
                    Application.platform != RuntimePlatform.WindowsPlayer ||
                    FrozTLDMod.Settings == null ||
                    !FrozTLDMod.Settings.Enabled ||
                    !FrozTLDMod.Settings.FixAutoWalk)
                {
                    return;
                }

                __result = Input.GetKeyDown(KeyCode.Z);
            }
        }
    }
}
