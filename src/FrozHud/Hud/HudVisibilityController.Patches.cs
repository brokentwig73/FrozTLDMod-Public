using HarmonyLib;
using Il2Cpp;

namespace FrozTLDMods.FrozTLDMod
{
    public sealed partial class FrozTLDMod
    {
        [HarmonyPatch(typeof(Rest), nameof(Rest.BeginSleeping), new System.Type[] { typeof(Bed), typeof(int), typeof(int) })]
        // Marks the simple Rest.BeginSleeping flow as sleep/rest active.
        private static class RestBeginSleepingSimplePatch
        {
            private static void Postfix()
            {
                SetSleepingActive(true, "Rest.BeginSleeping");
                SetRestOrPassTimeActive(true, "Rest.BeginSleeping");
            }
        }

        [HarmonyPatch(typeof(Rest), nameof(Rest.BeginSleeping), new System.Type[] { typeof(Bed), typeof(int), typeof(int), typeof(float), typeof(Rest.PassTimeOptions), typeof(Il2CppSystem.Action) })]
        // Marks the extended Rest.BeginSleeping flow as sleep/rest active.
        private static class RestBeginSleepingExtendedPatch
        {
            private static void Postfix()
            {
                SetSleepingActive(true, "Rest.BeginSleepingExtended");
                SetRestOrPassTimeActive(true, "Rest.BeginSleepingExtended");
            }
        }

        [HarmonyPatch(typeof(Rest), nameof(Rest.EndSleeping))]
        // Clears sleep/rest state and starts the short post-sleep HUD recovery delay.
        private static class RestEndSleepingPatch
        {
            private static void Postfix()
            {
                SetSleepingActive(false, "Rest.EndSleeping");
                SetRestOrPassTimeActive(false, "Rest.EndSleeping");
                DelayHudAfterSleep("Rest.EndSleeping");
            }
        }

        [HarmonyPatch(typeof(PassTime), nameof(PassTime.Begin))]
        // Marks accelerated pass-time as active.
        private static class PassTimeBeginPatch
        {
            private static void Postfix()
            {
                SetRestOrPassTimeActive(true, "PassTime.Begin");
            }
        }

        [HarmonyPatch(typeof(PassTime), nameof(PassTime.End))]
        // Clears the explicit accelerated pass-time state.
        private static class PassTimeEndPatch
        {
            private static void Postfix()
            {
                SetRestOrPassTimeActive(false, "PassTime.End");
            }
        }

        [HarmonyPatch(typeof(PassTime), nameof(PassTime.UpdatePassingTime))]
        // Extends the pass-time continuity signal while native updates continue.
        private static class PassTimeUpdatePassingTimePatch
        {
            private static void Postfix(PassTime __instance)
            {
                RecordPassTimeUpdate(__instance);
            }
        }
    }
}
