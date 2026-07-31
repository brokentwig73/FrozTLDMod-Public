using Il2Cpp;
using Il2CppTLD.SaveState;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class FrozTLDModOverlay
    {
        private const float KilometersPerMile = 1.609344f;

        // Draws the compass-oriented wind dial, direction marker, and center speed badge.
        private void DrawWindAngleGauge(Rect itemRect, float hudAlpha)
        {
            // The wind dial rotates with the final stick-compass dial so both
            // dials share the same player-relative frame. The arrow then points
            // within that rotated frame.
            var wind = GameManager.GetWindComponent();
            if (wind == null)
            {
                return;
            }

            var tuning = GetHudTuning();
            var circleSize = Mathf.Min(itemRect.width, itemRect.height) * tuning.DialSizeScale * 0.01f;
            var circleRect = new Rect(
                itemRect.x + (itemRect.width - circleSize) * 0.5f,
                itemRect.y + (itemRect.height - circleSize) * 0.5f,
                circleSize,
                circleSize);
            var oldColor = GUI.color;
            var oldMatrix = GUI.matrix;
            var dialRotation = GetCompassDialRotationDegreesForHud(tuning);
            var backTexture = GetDialBackgroundTexture();
            if (backTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, tuning.DialOpacity * 0.01f * hudAlpha);
                GUIUtility.RotateAroundPivot(dialRotation, circleRect.center);
                GUI.DrawTexture(circleRect, backTexture, ScaleMode.ScaleToFit, true);
                DrawCompassDirectionLetters(circleRect, tuning, hudAlpha);
                GUI.matrix = oldMatrix;
            }

            DrawWindDirectionMarker(
                circleRect,
                tuning,
                hudAlpha,
                Mathf.Repeat(wind.GetWindAngle() + dialRotation, 360f));

            GUI.matrix = oldMatrix;
            DrawWindSpeedValue(circleRect, wind, tuning, hudAlpha);
            GUI.color = oldColor;
        }

        // Places the outward wind-direction marker at the corrected world angle.
        private static void DrawWindDirectionMarker(Rect circleRect, HudTuning tuning, float hudAlpha, float rotationDegrees)
        {
            DrawWindTriangleMarker(
                circleRect,
                rotationDegrees,
                tuning.DialWindArrowRadius,
                tuning.DialWindArrowSize,
                GetDialWindColor(tuning, 0.98f * hudAlpha),
                "Wind.Direction");
        }

        // Draws rounded wind speed over a white circle with a marker-colored border.
        private void DrawWindSpeedValue(Rect circleRect, Wind wind, HudTuning tuning, float hudAlpha)
        {
            // Keep speed stationary in the middle of the dial so the number stays
            // readable while the background/arrow rotate around it.
            if (!TryGetWindSpeedForDisplay(wind, out var windSpeed))
            {
                return;
            }

            EnsureStyles();

            var badgeSize = tuning.DialWindFontCircleRadius * 2f;
            var badgeRect = new Rect(
                circleRect.center.x - badgeSize * 0.5f,
                circleRect.center.y - badgeSize * 0.5f,
                badgeSize,
                badgeSize);

            GUI.color = new Color(1f, 1f, 1f, 0.95f * hudAlpha);
            GUI.DrawTexture(badgeRect, GetWindSpeedBadgeFillTexture(), ScaleMode.ScaleToFit, true);
            GUI.color = GetDialWindColor(tuning, 0.98f * hudAlpha);
            GUI.DrawTexture(badgeRect, GetWindSpeedBadgeBorderTexture(tuning), ScaleMode.ScaleToFit, true);

            _lowerHudValueStyle.fontSize = tuning.DialThermometerFeelsLikeFontSize;
            _lowerHudValueStyle.normal.textColor = new Color(0f, 0f, 0f, 0.95f * hudAlpha);
            GUI.Label(badgeRect, Mathf.RoundToInt(windSpeed).ToString("0"), _lowerHudValueStyle);
        }

        // Returns MPH for Imperial or converts the game's MPH source to km/h for Metric.
        private static bool TryGetWindSpeedForDisplay(Wind wind, out float windSpeed)
        {
            if (wind == null || !MeasurementUnitProvider.TryGetCurrent(out var measurementUnits))
            {
                windSpeed = 0f;
                return false;
            }

            var milesPerHour = wind.GetSpeedMPH();
            windSpeed = measurementUnits == MeasurementUnits.Metric
                ? milesPerHour * KilometersPerMile
                : milesPerHour;
            return true;
        }

        // Returns the shared configured color for the wind marker, border, and speed text.
        private static Color GetDialWindColor(HudTuning tuning, float alpha)
        {
            var colorText = tuning.DialWindArrowColor;
            if (!string.IsNullOrEmpty(colorText) && colorText[0] != '#')
            {
                colorText = "#" + colorText;
            }

            ColorUtility.TryParseHtmlString(colorText, out var color);
            color.a = alpha;
            return color;
        }
    }
}
