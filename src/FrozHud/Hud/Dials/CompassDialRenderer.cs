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
        // Converts a flattened world direction into clockwise compass degrees.
        private static float GetDegreesFromFlatDirection(Vector3 direction)
        {
            var flatDirection = new Vector3(direction.x, 0f, direction.z);
            if (flatDirection.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            flatDirection.Normalize();
            var degrees = Mathf.Atan2(flatDirection.x, flatDirection.z) * Mathf.Rad2Deg;
            return Mathf.Repeat(degrees, 360f);
        }

        // Draws the rotating compass face and calibrated stick, or the disabled indoor appearance.
        private void DrawStickCompassItem(Rect itemRect, float hudAlpha)
        {
            EnsureStyles();

            // Indoor scenes use a different orientation frame, so stick north is
            // misleading there. During loading, keep the stick hidden until the
            // outdoor scene has settled to avoid a one-frame flash.
            var showStick = IsCompassDialRotationEnabled();
            var texture = GetStickCompassTexture();
            if (texture == null)
            {
                return;
            }

            var imageArea = itemRect;
            var tuning = GetHudTuning();
            var circleSize = Mathf.Min(imageArea.width, imageArea.height) * tuning.DialSizeScale * 0.01f;
            var circleRect = new Rect(
                imageArea.x + (imageArea.width - circleSize) * 0.5f,
                imageArea.y + (imageArea.height - circleSize) * 0.5f,
                circleSize,
                circleSize);
            var oldColor = GUI.color;
            var oldMatrix = GUI.matrix;
            var backTexture = GetDialBackgroundTexture();
            var dialRotation = GetCompassDialRotationDegreesForHud(tuning);

            if (backTexture != null)
            {
                if (showStick)
                {
                    // Background letters and stick rotate together. The marker at
                    // the top remains fixed, making it easy to see current north.
                    GUIUtility.RotateAroundPivot(dialRotation, circleRect.center);
                }

                GUI.color = showStick
                    ? new Color(1f, 1f, 1f, tuning.DialOpacity * 0.01f * hudAlpha)
                    : GetDisabledCompassTint(tuning, hudAlpha);
                GUI.DrawTexture(circleRect, backTexture, ScaleMode.ScaleToFit, true);
                DrawCompassDirectionLetters(circleRect, tuning, hudAlpha);

                GUI.matrix = oldMatrix;
            }

            if (!showStick)
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
                return;
            }

            GUI.color = new Color(1f, 1f, 1f, 0.95f * hudAlpha);
            GUIUtility.RotateAroundPivot(dialRotation, circleRect.center);
            GUI.DrawTexture(circleRect, texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = oldMatrix;
            DrawCompassReferenceDot(circleRect, tuning, hudAlpha);
            GUI.color = oldColor;
        }

        // Draws the fixed top marker that shows the player's forward reference.
        private static void DrawCompassReferenceDot(Rect circleRect, HudTuning tuning, float hudAlpha)
        {
            DrawTriangleMarker(
                circleRect,
                0f,
                tuning.DialCompassArrowRadius,
                tuning.DialCompassArrowSize,
                GetCompassArrowColor(tuning, 0.95f * hudAlpha),
                pointsInward: false,
                "Compass.Reference");
        }

        // Draws N/E/S/W on the rotating dial using the configured radius and style.
        private void DrawCompassDirectionLetters(Rect circleRect, HudTuning tuning, float hudAlpha)
        {
            var radius = circleRect.width * tuning.DialOverlayFontRadius * 0.01f;
            var fontSize = Mathf.Max(4, tuning.DialOverlayFontSize);
            var labelSize = fontSize * 2f;
            var alpha = tuning.DialOverlayFontOpacity * 0.01f * hudAlpha;

            _dialOverlayFontStyle.fontSize = fontSize;
            _dialOverlayFontStyle.fontStyle = GetDialOverlayFontStyle(tuning);
            _dialOverlayFontStyle.normal.textColor = GetDialOverlayFontColor(tuning, alpha);

            GUI.color = Color.white;
            DrawCompassDirectionLetter("N", circleRect.center.x, circleRect.center.y - radius, labelSize);
            DrawCompassDirectionLetter("E", circleRect.center.x + radius, circleRect.center.y, labelSize);
            DrawCompassDirectionLetter("S", circleRect.center.x, circleRect.center.y + radius, labelSize);
            DrawCompassDirectionLetter("W", circleRect.center.x - radius, circleRect.center.y, labelSize);
        }

        // Draws one centered direction label at a precomputed point.
        private void DrawCompassDirectionLetter(string text, float centerX, float centerY, float labelSize)
        {
            GUI.Label(
                new Rect(centerX - labelSize * 0.5f, centerY - labelSize * 0.5f, labelSize, labelSize),
                text,
                _dialOverlayFontStyle);
        }

        // Converts the configured compass font-weight text into a Unity style.
        private static FontStyle GetDialOverlayFontStyle(HudTuning tuning)
        {
            return string.Equals(tuning.DialOverlayFontWeight, "Normal", StringComparison.OrdinalIgnoreCase)
                ? FontStyle.Normal
                : FontStyle.Bold;
        }

        // Applies the configured compass label color and opacity.
        private static Color GetDialOverlayFontColor(HudTuning tuning, float alpha)
        {
            var colorText = tuning.DialOverlayFontColor;
            if (!string.IsNullOrEmpty(colorText) && colorText[0] != '#')
            {
                colorText = "#" + colorText;
            }

            if (ColorUtility.TryParseHtmlString(colorText, out var color))
            {
                color.a = alpha;
                return color;
            }

            return new Color(0.282f, 0.282f, 0.282f, alpha);
        }

        // Returns the gray indoor/loading tint applied to the entire compass assembly.
        private static Color GetDisabledCompassTint(HudTuning tuning, float hudAlpha)
        {
            var alpha = tuning.DialCompassDisabledOpacity * 0.01f * hudAlpha;
            var colorText = tuning.DialCompassDisabledTintColor;
            if (!string.IsNullOrEmpty(colorText) && colorText[0] != '#')
            {
                colorText = "#" + colorText;
            }

            if (ColorUtility.TryParseHtmlString(colorText, out var color))
            {
                color.a = alpha;
                return color;
            }

            return new Color(0f, 0f, 0f, alpha);
        }

        // Returns the configured color for the fixed compass reference marker.
        private static Color GetCompassArrowColor(HudTuning tuning, float alpha)
        {
            var colorText = tuning.DialCompassArrowColor;
            if (!string.IsNullOrEmpty(colorText) && colorText[0] != '#')
            {
                colorText = "#" + colorText;
            }

            ColorUtility.TryParseHtmlString(colorText, out var color);
            color.a = alpha;
            return color;
        }

        // Combines player heading with the community stick-north calibration offset.
        private static float GetCompassDialRotationDegrees(HudTuning tuning, float compassHeading)
        {
            // StickNorthOffsetDegrees is the community "stick north" adjustment.
            // CompassBackgroundOffsetDegrees is only for visual background art.
            return tuning.CompassBackgroundOffsetDegrees + tuning.StickNorthOffsetDegrees - compassHeading;
        }

        // Keeps the dial fixed indoors and during loading transitions where scene headings are unreliable.
        private bool IsCompassDialRotationEnabled()
        {
            if (IsIndoorEnvironment() || IsLoadingPanelEnabledOrLoading())
            {
                _outdoorCompassVisibleAfter = Time.unscaledTime + OutdoorCompassSettleSeconds;
                return false;
            }

            return Time.unscaledTime >= _outdoorCompassVisibleAfter;
        }

        // Returns the final HUD rotation, or zero while compass rotation is disabled.
        private float GetCompassDialRotationDegreesForHud(HudTuning tuning)
        {
            return IsCompassDialRotationEnabled()
                ? GetCompassDialRotationDegrees(tuning, GetCompassHeadingDegrees())
                : 0f;
        }

        // Formats the current player heading as whole compass degrees.
        private static string GetCompassHeadingText()
        {
            try
            {
                return Mathf.RoundToInt(GetCompassHeadingDegrees()).ToString("0");
            }
            catch
            {
                return string.Empty;
            }
        }

        // Reads the player's world-forward heading in clockwise degrees.
        private static float GetCompassHeadingDegrees()
        {
            var playerTransform = GameManager.GetPlayerTransform();
            if (playerTransform == null)
            {
                return 0f;
            }

            var forward = playerTransform.forward;
            var flatForward = new Vector3(forward.x, 0f, forward.z);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            flatForward.Normalize();
            var degrees = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            if (degrees < 0f)
            {
                degrees += 360f;
            }

            return degrees;
        }

        // Lazily loads the calibrated stick overlay artwork.
        private Texture2D GetStickCompassTexture()
        {
            if (_stickCompassTexture != null)
            {
                return _stickCompassTexture;
            }

            _stickCompassTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.dialCompassStick.png");
            return _stickCompassTexture;
        }

        // Lazily loads the compass background artwork.
        private Texture2D GetStickCompassBackTexture()
        {
            if (_stickCompassBackTexture != null)
            {
                return _stickCompassBackTexture;
            }

            _stickCompassBackTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.stick_compass_back.png");
            return _stickCompassBackTexture;
        }

        // Lazily creates or loads the disabled compass appearance.
        private Texture2D GetStickCompassDisabledTexture()
        {
            if (_stickCompassDisabledTexture != null)
            {
                return _stickCompassDisabledTexture;
            }

            _stickCompassDisabledTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.stick_compass_disabled.png");
            return _stickCompassDisabledTexture;
        }

        // Reads the game's authoritative indoor-environment state.
        private static bool IsIndoorEnvironment()
        {
            var weather = GameManager.GetWeatherComponent();
            return weather != null && weather.IsIndoorEnvironment();
        }

        // Detects scene loading so the outgoing scene's heading does not flash briefly.
        private static bool IsLoadingPanelEnabledOrLoading()
        {
            return InterfaceManager.IsPanelLoadingEnabledOrLoading();
        }
    }
}
