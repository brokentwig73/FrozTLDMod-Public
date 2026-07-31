using Il2Cpp;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Draws every Froz-owned IMGUI element below the cloned time-of-day dial.
    // The top sundial is an NGUI TimeWidget clone; the lower HUD contains the
    // analog clock and the other enabled dial or icon elements.
    internal sealed partial class FrozTLDModOverlay
    {
        private enum DialGaugeItem
        {
            StickCompass,
            WindDial,
            DialThermometer,
            DialClock,
            ScentMeter,
            BackpackWeight
        }

        private const float OutdoorCompassSettleSeconds = 0.75f;
        private const float DialGaugeHeight = 78f;
        private const float InventoryWeightRefreshIntervalSeconds = 0.1f;
        private const float ScentRefreshIntervalSeconds = 0.25f;
        private const float StickImageNorthRotationDegrees = 135f;
        private const float MaxScentRange = 80f;
        private const float FreezingCelsius = 0f;
        private const float FreezingFahrenheit = 32f;
        private const float ThermometerScaleStartDegrees = 220f;
        private const float ThermometerScaleEndDegrees = 140f;
        private const float ThermometerScaleSweepDegrees = 360f - ThermometerScaleStartDegrees + ThermometerScaleEndDegrees;
        private const int ThermometerRangeLabelCount = 8;

        private bool _stickyDesired;
        private int _lastToggleFrame = -1;
        private float _outdoorCompassVisibleAfter = -1f;
        private GUIStyle _lowerHudValueStyle;
        private GUIStyle _dialOverlayFontStyle;
        private Texture2D _stickCompassTexture;
        private Texture2D _clockDialHandTexture;
        private Texture2D _dialBackgroundTexture;
        private Texture2D _stickCompassBackTexture;
        private Texture2D _stickCompassDisabledTexture;
        private Texture2D _defaultBackpackTexture;
        private Texture2D _scentLitTexture;
        private Texture2D _scentDimTexture;
        private static Texture2D _windSpeedBadgeFillTexture;
        private static Texture2D _windSpeedBadgeBorderTexture;
        private static float _windSpeedBadgeBorderThicknessPercent = -1f;
        private static Texture2D _triangleMarkerInTexture;
        private static Texture2D _triangleMarkerOutTexture;
        private static Texture2D _thermometerValueBorderTexture;
        private static float _thermometerValueBorderSize = -1f;
        private static Panel_Inventory _cachedPanelInventory;
        private float _cachedInventoryWeight;
        private float _cachedMaxCarryCapacity;
        private float _nextInventoryWeightRefreshTime;
        private float _cachedScentIntensity;
        private float _cachedScentPercent;
        private int _cachedScentBars;
        private float _nextScentRefreshTime;

        public bool Visible => FrozTLDMod.ShouldRenderClock(_stickyDesired) ||
                               FrozTLDMod.ShouldRenderLowerHudContainer(_stickyDesired);

        // Maintains sticky state and refreshes cached data used by enabled HUD gauges.
        public void Update()
        {
            if (!FrozTLDMod.IsStickyHudEnabled())
            {
                _stickyDesired = false;
                return;
            }

            UpdateInventoryWeightCacheIfNeeded();
            UpdateScentCacheIfNeeded();
        }

        // Toggles the shared sticky request once per input frame and returns the resulting visibility.
        public bool ToggleFromHotkey()
        {
            if (!IsEnabled())
            {
                _stickyDesired = false;
                return false;
            }

            if (_lastToggleFrame == Time.frameCount)
            {
                return Visible;
            }

            _lastToggleFrame = Time.frameCount;
            _stickyDesired = !_stickyDesired;
            return Visible;
        }

        // Synchronizes lower HUD elements with the owned time-of-day controller's sticky state.
        public void SetStickyDesired(bool stickyDesired)
        {
            _stickyDesired = stickyDesired;
        }

        // Draws enabled lower HUD rows in front of the game's IMGUI layer.
        public void Draw()
        {
            // Each element has its own ModSettings checkbox, but all elements use
            // the same sticky desired state and HUD alpha from FrozTLDMod.
            var drawClock = FrozTLDMod.ShouldRenderClock(_stickyDesired);
            var drawLowerContainer = FrozTLDMod.ShouldRenderLowerHudContainer(_stickyDesired);
            var drawTemperature = FrozTLDMod.ShouldRenderTemperature(_stickyDesired);
            var drawFeelsOutside = FrozTLDMod.ShouldRenderFeelsOutside(_stickyDesired);
            var drawStickCompass = FrozTLDMod.ShouldRenderStickCompass(_stickyDesired);
            var drawWindCompass = FrozTLDMod.ShouldRenderWindCompass(_stickyDesired);
            var drawScentMeter = FrozTLDMod.ShouldRenderScentMeter(_stickyDesired);
            var drawBackpackWeight = FrozTLDMod.ShouldRenderBackpackWeight(_stickyDesired);
            if (!drawClock && !drawLowerContainer)
            {
                return;
            }

            var hudAlpha = FrozTLDMod.GetHudAlpha(_stickyDesired);
            if (hudAlpha <= 0.01f)
            {
                return;
            }

            if (!TryGetHudRect(out var rect))
            {
                return;
            }

            var oldDepth = GUI.depth;
            // Lower depth draws in front in IMGUI. Restore it afterward so we do
            // not affect other mods or game UI drawn later.
            GUI.depth = -1000;
            try
            {
                var layout = FrozTLDMod.ClockLayout.Values;

                if (drawLowerContainer)
                {
                    DrawLowerHud(rect, layout, hudAlpha, drawClock, drawTemperature, drawFeelsOutside, drawStickCompass, drawWindCompass, drawScentMeter, drawBackpackWeight);
                }
            }
            finally
            {
                GUI.depth = oldDepth;
            }
        }

        // Reports whether the game currently permits custom HUD rendering.
        private static bool IsEnabled()
        {
            return FrozTLDMod.IsHudAllowed();
        }

        // Returns the compiled HUD tuning values initialized before the overlay is created.
        private static HudTuning GetHudTuning()
        {
            return FrozTLDMod.HudTuning.Values;
        }

        // Lazily creates the reusable IMGUI styles shared by dial text.
        private void EnsureStyles()
        {
            _lowerHudValueStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Overflow,
                wordWrap = false
            };

            _dialOverlayFontStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Overflow,
                wordWrap = false
            };
        }

    }
}
