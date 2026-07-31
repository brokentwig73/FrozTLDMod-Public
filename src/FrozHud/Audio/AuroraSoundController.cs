using System;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // AuroraManager drives the continuous sky ambience through AuroraStrength.
    // Powered lights and machinery use the separate, object-scoped
    // AuroraElectrolizer RTPC so the two sound families can be tuned separately.
    internal static class AuroraSoundController
    {
        private const uint AuroraStrengthRtpcId = 3174855862u;
        private const uint AuroraElectrolizerRtpcId = 991664965u;

        // Reapplies both Aurora sound families immediately after a settings change.
        internal static void ApplyCurrentSetting()
        {
            var auroraManager = GameManager.GetAuroraManager();
            if (auroraManager != null)
            {
                var originalStrength = Mathf.Clamp01(auroraManager.m_NormalizedActive) * 100f;
                GameAudioManager.SetRTPCValue(AuroraStrengthRtpcId, originalStrength, null);
            }

            ApplyCurrentElectricalSetting();
        }

        // Re-emits each powered object's last unscaled intensity so the new electrical volume takes effect.
        private static void ApplyCurrentElectricalSetting()
        {
            var electrolizers = AuroraManager.m_AuroraElectrolizerList;
            if (electrolizers != null)
            {
                for (var index = 0; index < electrolizers.Count; index++)
                {
                    var electrolizer = electrolizers[index];
                    if (electrolizer != null)
                    {
                        GameAudioManager.SetRTPCValue(
                            AuroraElectrolizerRtpcId,
                            Mathf.Clamp01(electrolizer.m_IntensitySentToWise) * 100f,
                            electrolizer.gameObject);
                    }
                }
            }

            var simpleLights = AuroraManager.m_AuroraLightSimpleList;
            if (simpleLights == null)
            {
                return;
            }

            for (var index = 0; index < simpleLights.Count; index++)
            {
                var simpleLight = simpleLights[index];
                if (simpleLight != null)
                {
                    GameAudioManager.SetRTPCValue(
                        AuroraElectrolizerRtpcId,
                        Mathf.Clamp01(simpleLight.m_IntensitySentToWise) * 100f,
                        simpleLight.gameObject);
                }
            }
        }

        // Scales only the two known Aurora RTPC values while leaving every other game sound untouched.
        private static void ApplyConfiguredVolume(uint rtpcId, ref float rtpcValue)
        {
            var settings = FrozTLDMod.Settings;
            if (settings == null || !settings.Enabled)
            {
                return;
            }

            if (rtpcId == AuroraStrengthRtpcId)
            {
                rtpcValue *= Mathf.Clamp01(settings.AuroraAmbienceVolumePercent / 100f);
            }
            else if (rtpcId == AuroraElectrolizerRtpcId)
            {
                rtpcValue *= Mathf.Clamp01(settings.AuroraElectricalVolumePercent / 100f);
            }
        }

        [HarmonyPatch(
            typeof(GameAudioManager),
            nameof(GameAudioManager.SetRTPCValue),
            new Type[] { typeof(uint), typeof(float), typeof(GameObject) })]
        // Intercepts Aurora RTPC writes at the game's audio boundary and applies the configured volume.
        private static class GameAudioManagerSetRtpcValuePatch
        {
            // Harmony binds this argument by name, so rtpcID must match the game's parameter exactly.
            private static void Prefix(uint rtpcID, ref float rtpcValue)
            {
                if (rtpcID == AuroraStrengthRtpcId || rtpcID == AuroraElectrolizerRtpcId)
                {
                    ApplyConfiguredVolume(rtpcID, ref rtpcValue);
                }
            }
        }
    }
}
