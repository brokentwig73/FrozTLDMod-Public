using Il2Cpp;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal static partial class DragFuelIntoFireController
    {
        // Caches the prompt title and projected time/temperature values at a low refresh rate.
        private static void UpdatePromptValueCache(GearItem fuelGear, Fire fire)
        {
            if (_promptValueFuel == fuelGear &&
                _promptValueFire == fire &&
                Time.unscaledTime < _nextPromptValueRefreshTime)
            {
                return;
            }

            _promptValueFuel = fuelGear;
            _promptValueFire = fire;
            _nextPromptValueRefreshTime = Time.unscaledTime + PromptValueRefreshSeconds;
            _promptTitleText = "Add " + fuelGear.DisplayName;

            var fuelSource = fuelGear.m_FuelSourceItem;
            var addedDurationHours = fuelSource.GetModifiedBurnDurationHours(
                fuelGear.GetNormalizedCondition());
            var addedDurationSeconds = addedDurationHours * 3600f;
            var addedTime = Utils.GetDurationString(Mathf.CeilToInt(
                addedDurationSeconds / 60f));
            var combinedTime = Utils.GetDurationString(Mathf.CeilToInt(
                (fire.GetRemainingLifeTimeSeconds() + addedDurationSeconds) / 60f));
            var addedTemp = Utils.GetTemperatureString(
                fuelSource.m_HeatIncrease,
                true,
                true,
                true);
            var projectedFinalTempIncrease = fire.m_FuelHeatIncrease + fuelSource.m_HeatIncrease;
            var combinedTemp = Utils.GetTemperatureString(
                projectedFinalTempIncrease,
                true,
                true,
                true);

            _promptTimeText = "+" + addedTime + " (" + combinedTime + ")";
            _promptTempText = addedTemp + " (" + combinedTemp + ")";

        }

        // Reuses the native fire hover widgets while replacing their text with projected add-fuel values.
        private static void ApplyNativeHoverText(
            Panel_HUD panelHud,
            GearItem fuelGear,
            Fire fire)
        {
            if (panelHud == null)
            {
                return;
            }

            UpdatePromptValueCache(fuelGear, fire);
            if (panelHud.m_HoverTextObject != null)
            {
                panelHud.m_HoverTextObject.SetActive(true);
            }

            if (panelHud.m_Label_ObjectName != null)
            {
                panelHud.m_Label_ObjectName.text = _promptTitleText;
            }

            if (panelHud.m_Label_FireTime != null)
            {
                panelHud.m_Label_FireTime.gameObject.SetActive(true);
                panelHud.m_Label_FireTime.transform.parent.gameObject.SetActive(true);
                panelHud.m_Label_FireTime.text = _promptTimeText;
            }

            if (panelHud.m_Label_FireTemp != null)
            {
                panelHud.m_Label_FireTemp.gameObject.SetActive(true);
                panelHud.m_Label_FireTemp.transform.parent.gameObject.SetActive(true);
                panelHud.m_Label_FireTemp.text = _promptTempText;
            }

            if (panelHud.m_Label_SubText != null)
            {
                panelHud.m_Label_SubText.gameObject.SetActive(false);
            }

            if (panelHud.m_HoverTextBG != null)
            {
                panelHud.m_HoverTextBG.gameObject.SetActive(true);
                panelHud.m_HoverTextBG.enabled = true;
                panelHud.m_HoverTextBG.height = panelHud.m_HoverTextBGHeightWithFire;
            }

            if (panelHud.m_HoverTextLinebreak != null)
            {
                panelHud.m_HoverTextLinebreak.gameObject.SetActive(true);
                panelHud.m_HoverTextLinebreak.enabled = true;
            }

            panelHud.m_HoverTextGrid?.Reposition();
        }

        // Invalidates projected prompt values when the fuel or target session ends.
        private static void ClearPromptValueCache()
        {
            _promptValueFuel = null;
            _promptValueFire = null;
            _nextPromptValueRefreshTime = 0f;
            _promptTitleText = string.Empty;
            _promptTimeText = string.Empty;
            _promptTempText = string.Empty;
        }

        // Hides the held preview without disabling colliders needed by vanilla's continuous targeting.
        private static void HideFuelForPrompt(PlayerManager playerManager, GearItem fuelGear)
        {
            HideFuelRenderers(playerManager, fuelGear);
            // Keep the placement colliders enabled. Vanilla needs them to
            // refresh the exact fire hit continuously; hiding only renderers
            // avoids the old cached-hit expiry loop.
        }

        // Caches all renderer states for the held fuel, then keeps those renderers disabled.
        private static void HideFuelRenderers(PlayerManager playerManager, GearItem fuelGear)
        {
            if (_hiddenFuel != fuelGear)
            {
                RestoreFuelRenderers();
                _hiddenFuel = fuelGear;

                var childRenderers = fuelGear.gameObject.GetComponentsInChildren<Renderer>(true);
                for (var index = 0; index < childRenderers.Length; index++)
                {
                    CacheFuelRenderer(childRenderers[index]);
                }

                CacheFuelRenderers(playerManager.m_ObjectToPlaceRenderers);
                CacheFuelRenderers(playerManager.m_TintedRenderers);
            }

            for (var index = 0; index < HiddenRendererStates.Count; index++)
            {
                var renderer = HiddenRendererStates[index].Renderer;
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }

        // Adds every renderer in a native IL2CPP list to the restoration cache.
        private static void CacheFuelRenderers(Il2CppSystem.Collections.Generic.List<Renderer> renderers)
        {
            if (renderers == null)
            {
                return;
            }

            for (var index = 0; index < renderers.Count; index++)
            {
                CacheFuelRenderer(renderers[index]);
            }
        }

        // Records one renderer once, preserving its original enabled state.
        private static void CacheFuelRenderer(Renderer renderer)
        {
            if (renderer == null || !HiddenRendererIds.Add(renderer.GetInstanceID()))
            {
                return;
            }

            HiddenRendererStates.Add(new RendererState(renderer, renderer.enabled));
        }

        // Restores all renderer states captured while the custom add-fuel prompt was visible.
        private static void RestoreFuelRenderers()
        {
            for (var index = 0; index < HiddenRendererStates.Count; index++)
            {
                var state = HiddenRendererStates[index];
                if (state.Renderer != null)
                {
                    state.Renderer.enabled = state.Enabled;
                }
            }

            HiddenRendererStates.Clear();
            HiddenRendererIds.Clear();
            _hiddenFuel = null;
        }

        // Clears the active fireplace target and restores the fuel preview.
        private static void ClearTarget()
        {
            RestoreFuelRenderers();
            _targetFuel = null;
        }
    }
}
