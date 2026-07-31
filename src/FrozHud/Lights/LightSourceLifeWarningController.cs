using Il2Cpp;
using System;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // The equipped torch/flare/lantern popup uses Panel_HUD.m_EquipItemPopup
    // and a Duration/Foreground UISprite for the white burn-life bar. Vanilla
    // leaves it white until burnout; this tints it like the game's other
    // danger HUDs.
    internal sealed class LightSourceLifeWarningController
    {
        private static readonly Color NormalColor = new(0.98f, 0.98f, 0.98f, 1f);
        private static readonly Color WarningColor = new(0.95f, 0.08f, 0.08f, 1f);
        private const float WarningThresholdPercent = 0.05f;
        private const float RefreshIntervalSeconds = 0.25f;

        private Panel_HUD _panelHud;
        private UISprite _foreground;
        private UISprite _background;
        private UISprite _lightSourceIcon;
        private bool _hasAppliedTint;
        private float _nextRefreshTime;
        private Color _cachedTintColor = NormalColor;
        private Color _cachedBackgroundColor = NormalColor;

        // Updates the equipped light's life bar and icon tint at a throttled interval while it is active.
        public void Update()
        {
            if (FrozTLDMod.Settings == null ||
                !FrozTLDMod.Settings.Enabled ||
                !FrozTLDMod.Settings.RedTorchFlareLifeBar)
            {
                RestoreNormalColor();
                return;
            }

            try
            {
                var heldGear = GetHeldGear();
                var remainingPercent = GetRemainingLifePercent(heldGear);
                if (remainingPercent < 0f)
                {
                    // Throwing/dropping a nearly-dead torch can clear the held
                    // item before the vanilla duration popup finishes fading.
                    // Keep the last tint until the bar is hidden so it does not
                    // flash back to white at the most visible moment.
                    if (_hasAppliedTint && !IsDurationBarVisible())
                    {
                        RestoreNormalColor();
                    }

                    return;
                }


                if (Time.unscaledTime < _nextRefreshTime)
                {
                    return;
                }

                _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
                RefreshCachedTint(remainingPercent);
                EnsureSprites();
                if (_foreground == null)
                {
                    return;
                }

                _foreground.color = _cachedTintColor;
                TintLightSourceIcon(_cachedTintColor);
                _hasAppliedTint = true;
                if (_background != null)
                {
                    _background.color = _cachedBackgroundColor;
                }
            }
            catch
            {
                RestoreNormalColor();
            }
        }

        // Converts remaining life into cached foreground and background warning colors.
        private void RefreshCachedTint(float remainingPercent)
        {
            var warningStrength = GetWarningStrength(remainingPercent);
            _cachedTintColor = Color.Lerp(NormalColor, WarningColor, warningStrength);
            _cachedBackgroundColor = Color.Lerp(NormalColor, new Color(0.5f, 0.03f, 0.03f, 1f), warningStrength * 0.55f);
        }

        // Switches directly to full warning red at five percent remaining.
        private static float GetWarningStrength(float remainingPercent)
        {
            return remainingPercent <= WarningThresholdPercent ? 1f : 0f;
        }

        // Restores every tinted widget after the feature or light source becomes inactive.
        private void RestoreNormalColor()
        {
            if (!_hasAppliedTint)
            {
                return;
            }

            if (_foreground != null)
            {
                _foreground.color = NormalColor;
            }

            if (_background != null)
            {
                _background.color = NormalColor;
            }

            TintLightSourceIcon(NormalColor);
            _hasAppliedTint = false;
        }

        // Applies the current warning tint to the light-source icon without changing its native alpha.
        private void TintLightSourceIcon(Color color)
        {
            // This icon row is shared by the equipped-item/ammo popup. Torch,
            // flare, and lantern use Sprite_Ammo10 with ico_lightSource_*
            // sprites; weapons appear to use the same EquipItemPopup/AmmoWidget
            // area.
            if (_lightSourceIcon == null || _lightSourceIcon.gameObject == null)
            {
                return;
            }

            if (IsLightSourceIcon(_lightSourceIcon))
            {
                var adjusted = color;
                adjusted.a = _lightSourceIcon.alpha;
                _lightSourceIcon.color = adjusted;
            }
        }

        // Reports whether the native duration foreground is still visibly fading on screen.
        private bool IsDurationBarVisible()
        {
            return _foreground != null &&
                   _foreground.gameObject != null &&
                   _foreground.gameObject.activeInHierarchy &&
                   _foreground.alpha > 0.01f;
        }

        // Resolves and caches the duration bar, background, and exact light-source icon from Panel_HUD.
        private void EnsureSprites()
        {
            if (AreSpritesCached())
            {
                return;
            }

            _panelHud = PanelCache.Get(_panelHud);
            var popup = Il2CppReflection.GetObjectMember(_panelHud, "m_EquipItemPopup");
            var popupRoot = GetPopupRoot(popup);
            var ammoWidget = FindDescendant(popupRoot, "AmmoWidget");
            var bottomRight = FindDescendant(ammoWidget, "BottomRight");
            var duration = FindDescendant(bottomRight, "Duration");
            if (_foreground == null || _foreground.gameObject == null)
            {
                _foreground = FindChild(duration, "Foreground")?.gameObject.GetComponent<UISprite>();
            }

            if (_background == null || _background.gameObject == null)
            {
                _background = FindChild(duration, "Background")?.gameObject.GetComponent<UISprite>();
            }

            // The popup can retain duplicate AmmoWidget/BottomRight branches.
            // Search the confirmed EquipItemPopup root for the exact live
            // Sprite_Ammo10 instance displaying light-source artwork.
            _lightSourceIcon = FindLightSourceIconInSubtree(popupRoot);
        }

        // Validates all cached sprites, including the icon's current light-source artwork.
        private bool AreSpritesCached()
        {
            return _foreground != null &&
                   _foreground.gameObject != null &&
                   _background != null &&
                   _background.gameObject != null &&
                   _lightSourceIcon != null &&
                   _lightSourceIcon.gameObject != null &&
                   IsLightSourceIcon(_lightSourceIcon);
        }

        // Converts the reflected equip-popup component to its transform root.
        private static Transform GetPopupRoot(object popup)
        {
            return popup is Component component && component.gameObject != null
                ? component.gameObject.transform
                : null;
        }

        // Recognizes torch, flare, and lantern variants by their shared icon sprite names.
        private static bool IsLightSourceIcon(UISprite sprite)
        {
            if (sprite == null)
            {
                return false;
            }

            var spriteName = sprite.spriteName ?? string.Empty;
            return spriteName.Equals("ico_lightSource_torch", StringComparison.OrdinalIgnoreCase) ||
                   spriteName.Equals("ico_lightSource_flare", StringComparison.OrdinalIgnoreCase) ||
                   spriteName.Equals("ico_lightSource_lantern", StringComparison.OrdinalIgnoreCase);
        }

        // Finds the exact Sprite_Ammo10 instance currently displaying light-source artwork.
        private static UISprite FindLightSourceIconInSubtree(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            var sprite = root.gameObject != null ? root.gameObject.GetComponent<UISprite>() : null;
            if (sprite != null &&
                root.gameObject.name == "Sprite_Ammo10" &&
                IsLightSourceIcon(sprite))
            {
                return sprite;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var icon = FindLightSourceIconInSubtree(root.GetChild(i));
                if (icon != null)
                {
                    return icon;
                }
            }

            return null;
        }

        // Finds a direct child by native object name.
        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child != null && child.gameObject != null && child.gameObject.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        // Recursively finds a named descendant within a known HUD subtree.
        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.gameObject != null && root.gameObject.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        // Returns the gear currently held by the local player.
        private static GearItem GetHeldGear()
        {
            var playerManager = GameManager.GetPlayerManagerComponent();
            return Il2CppReflection.GetObjectMember(playerManager, "m_ItemInHands") as GearItem;
        }

        // Routes torch, flare, and lantern gear to the correct remaining-life calculation.
        private static float GetRemainingLifePercent(GearItem gear)
        {
            if (gear == null)
            {
                return -1f;
            }

            var torch = gear.GetComponent<TorchItem>();
            if (torch != null)
            {
                return GetRemainingLifePercent(torch);
            }

            var flare = gear.GetComponent<FlareItem>();
            if (flare != null)
            {
                return GetRemainingLifePercent(flare);
            }

            var lantern = gear.GetComponent<Il2CppTLD.Gear.KeroseneLampItem>();
            return lantern != null ? GetRemainingFuelPercent(lantern) : -1f;
        }

        // Calculates torch/flare life from elapsed and total burn minutes.
        private static float GetRemainingLifePercent(object torchOrFlare)
        {
            var lifetime = GetFloatMember(torchOrFlare, "m_BurnLifetimeMinutes");
            var elapsed = GetFloatMember(torchOrFlare, "m_ElapsedBurnMinutes");
            if (lifetime <= 0f || elapsed < 0f)
            {
                return -1f;
            }

            return Mathf.Clamp01((lifetime - elapsed) / lifetime);
        }

        // Calculates lantern life from TLD's fixed-point current and maximum fuel values.
        private static float GetRemainingFuelPercent(Il2CppTLD.Gear.KeroseneLampItem lantern)
        {
            var currentFuel = GetRawUnitsMember(lantern, "m_CurrentFuelLiters");
            var maxFuel = GetRawUnitsMember(lantern, "m_MaxFuel");
            if (currentFuel < 0 || maxFuel <= 0)
            {
                return -1f;
            }

            return Mathf.Clamp01((float)currentFuel / maxFuel);
        }

        // Reads a reflected float field, returning a negative sentinel when unavailable.
        private static float GetFloatMember(object target, string memberName)
        {
            var value = Il2CppReflection.GetObjectMember(target, memberName);
            return value is float floatValue ? floatValue : -1f;
        }

        // Reads the raw fixed-point units stored inside a reflected quantity field.
        private static long GetRawUnitsMember(object target, string memberName)
        {
            var value = Il2CppReflection.GetObjectMember(target, memberName);
            var units = Il2CppReflection.GetObjectMember(value, "m_Units");
            return GearItemInterop.TryGetInt64(units, out var rawUnits) ? rawUnits : -1;
        }
    }
}
