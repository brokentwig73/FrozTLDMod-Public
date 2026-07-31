using Il2Cpp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class FrozTLDModOverlay
    {
        private const float NativeScentSpriteWidth = 30f;
        private const float NativeScentSpriteHeight = 50f;
        private const float NativeScentHorizontalOffset = 12f;
        private const float NativeScentMiddleVerticalOffset = 7f;
        private const float NativeScentLayoutWidth = 54f;
        private const float NativeScentLayoutHeight = 57f;
        private const float NativeScentVisualCenterOffset = 3.5f;

        // Draws the three native-style scent squiggles inside one gauge box.
        private void DrawScentMeterGauge(Rect itemRect, float hudAlpha)
        {
            DrawScentSprites(itemRect, hudAlpha);
        }

        // Reproduces the serialized vanilla HUD layout: three 30x50 sprites at
        // X offsets -12/0/+12, with the middle sprite raised by seven units.
        private void DrawScentSprites(Rect itemRect, float hudAlpha)
        {
            var litTexture = GetScentLitTexture();
            var dimTexture = GetScentDimTexture();
            if (litTexture == null || dimTexture == null)
            {
                return;
            }

            var tuning = GetHudTuning();
            var fitScale = Mathf.Min(
                itemRect.width / NativeScentLayoutWidth,
                itemRect.height / NativeScentLayoutHeight);
            var scale = fitScale * tuning.ScentMeterScale * 0.01f;
            var spriteWidth = NativeScentSpriteWidth * scale;
            var spriteHeight = NativeScentSpriteHeight * scale;
            var rootCenter = new Vector2(
                itemRect.center.x + tuning.ScentMeterPosition.X,
                itemRect.center.y + NativeScentVisualCenterOffset * scale + tuning.ScentMeterPosition.Y);
            var oldColor = GUI.color;

            for (var index = 0; index < 3; index++)
            {
                var localX = (index - 1) * NativeScentHorizontalOffset * scale;
                var localY = index == 1 ? -NativeScentMiddleVerticalOffset * scale : 0f;
                var drawRect = new Rect(
                    rootCenter.x + localX - spriteWidth * 0.5f,
                    rootCenter.y + localY - spriteHeight * 0.5f,
                    spriteWidth,
                    spriteHeight);
                GUI.color = new Color(1f, 1f, 1f, hudAlpha);
                GUI.DrawTexture(drawRect, dimTexture, ScaleMode.StretchToFill, true);

                if (index < _cachedScentBars)
                {
                    GUI.DrawTexture(drawRect, litTexture, ScaleMode.StretchToFill, true);
                }
            }

            GUI.color = oldColor;
        }

        // Refreshes live scent intensity and bar count at a throttled interval, preserving the last good value.
        private void UpdateScentCacheIfNeeded()
        {
            if (Time.unscaledTime < _nextScentRefreshTime)
            {
                return;
            }

            _nextScentRefreshTime = Time.unscaledTime + ScentRefreshIntervalSeconds;
            if (FrozTLDMod.Settings == null ||
                !FrozTLDMod.Settings.Enabled ||
                !FrozTLDMod.Settings.ScentMeter)
            {
                return;
            }

            if (!TryGetLiveScentIntensity(out var scentIntensity))
            {
                return;
            }

            _cachedScentIntensity = Mathf.Max(0f, scentIntensity);
            _cachedScentPercent = Mathf.Clamp01(_cachedScentIntensity / MaxScentRange);
            _cachedScentBars = GetScentBarCount(_cachedScentIntensity);
        }

        // Reads the player's persistent live scent range without requiring the Status panel.
        private static bool TryGetLiveScentIntensity(out float scentIntensity)
        {
            scentIntensity = 0f;

            try
            {
                var inventory = GameManager.GetInventoryComponent();
                if (inventory == null)
                {
                    return false;
                }

                scentIntensity = inventory.GetCurrentTotalScentIntensity();
                return !float.IsNaN(scentIntensity) && !float.IsInfinity(scentIntensity);
            }
            catch
            {
                return false;
            }
        }

        // Converts scent range to the same zero-to-three indicator levels used by the game.
        private static int GetScentBarCount(float scentIntensity)
        {
            if (scentIntensity >= 80f)
            {
                return 3;
            }

            if (scentIntensity >= 45f)
            {
                return 2;
            }

            return scentIntensity >= 15f ? 1 : 0;
        }

        // Lazily loads the bright native scent squiggle asset.
        private Texture2D GetScentLitTexture()
        {
            if (_scentLitTexture != null)
            {
                return _scentLitTexture;
            }

            _scentLitTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.scentActive.png");
            return _scentLitTexture;
        }

        // Lazily loads the dim native scent squiggle asset.
        private Texture2D GetScentDimTexture()
        {
            if (_scentDimTexture != null)
            {
                return _scentDimTexture;
            }

            _scentDimTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.scentBackground.png");
            return _scentDimTexture;
        }
    }
}
