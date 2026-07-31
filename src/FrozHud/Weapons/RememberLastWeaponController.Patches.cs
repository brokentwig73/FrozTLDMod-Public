using HarmonyLib;
using Il2Cpp;
using System;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class RememberLastWeaponController
    {
        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.UseWeaponInventoryItem), new Type[] { typeof(GearItem), typeof(bool) })]
        // Intercepts the shared inventory, radial, and hotkey weapon equip boundary.
        private static class PlayerManagerUseWeaponInventoryItemPatch
        {
            private static void Prefix(PlayerManager __instance, ref GearItem gi)
            {
                HandleWeaponSelection(__instance, ref gi);
            }
        }
    }
}
