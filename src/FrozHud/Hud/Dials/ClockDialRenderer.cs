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
        // Draws the analog clock face, numerals, dots, and hands inside one gauge box.
        private void DrawDialClockItem(Rect itemRect, float hudAlpha)
        {
            var tuning = GetHudTuning();
            var circleRect = GetDialCircleRect(itemRect, tuning);
            var backTexture = GetDialBackgroundTexture();
            var oldColor = GUI.color;

            if (backTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, tuning.DialOpacity * 0.01f * hudAlpha);
                GUI.DrawTexture(circleRect, backTexture, ScaleMode.ScaleToFit, true);
            }

            DrawClockDialDots(circleRect, tuning, hudAlpha);
            DrawClockDialNumbers(circleRect, tuning, hudAlpha);
            DrawClockDialHands(circleRect, tuning, hudAlpha);
            GUI.color = oldColor;
        }

        // Draws 12, 3, 6, and 9 around the configured clock radius.
        private void DrawClockDialNumbers(Rect circleRect, HudTuning tuning, float hudAlpha)
        {
            EnsureStyles();

            var radius = circleRect.width * tuning.DialClockMarkerRadius * 0.01f;
            var fontSize = Mathf.Max(4, tuning.DialClockNumberFontSize);
            var labelSize = fontSize * 2.2f;
            _dialOverlayFontStyle.fontSize = fontSize;
            _dialOverlayFontStyle.fontStyle = FontStyle.Bold;
            _dialOverlayFontStyle.normal.textColor = GetHudColor(tuning.DialClockNumberColor, tuning.DialClockNumberOpacity * 0.01f * hudAlpha);
            GUI.color = Color.white;

            DrawClockDialNumber(circleRect, "12", 0f, radius, labelSize);
            DrawClockDialNumber(circleRect, "3", 90f, radius, labelSize);
            DrawClockDialNumber(circleRect, "6", 180f, radius, labelSize);
            DrawClockDialNumber(circleRect, "9", 270f, radius, labelSize);
        }

        // Draws one centered clock numeral at an angle and radius.
        private void DrawClockDialNumber(Rect circleRect, string label, float angleDegrees, float radius, float labelSize)
        {
            var point = GetCirclePoint(circleRect, angleDegrees, radius);
            GUI.Label(
                new Rect(point.x - labelSize * 0.5f, point.y - labelSize * 0.5f, labelSize, labelSize),
                label,
                _dialOverlayFontStyle);
        }

        // Draws dot markers for the eight hours that do not use numerals.
        private static void DrawClockDialDots(Rect circleRect, HudTuning tuning, float hudAlpha)
        {
            var radius = circleRect.width * tuning.DialClockMarkerRadius * 0.01f;
            var dotSize = Mathf.Max(1f, tuning.DialClockDotSize);
            var dotTexture = GetWindSpeedBadgeFillTexture();
            var oldColor = GUI.color;
            GUI.color = GetHudColor(tuning.DialClockDotColor, tuning.DialClockNumberOpacity * 0.01f * hudAlpha);

            for (var hour = 1; hour <= 12; hour++)
            {
                if (hour == 3 || hour == 6 || hour == 9 || hour == 12)
                {
                    continue;
                }

                var point = GetCirclePoint(circleRect, hour * 30f, radius);
                GUI.DrawTexture(
                    new Rect(point.x - dotSize * 0.5f, point.y - dotSize * 0.5f, dotSize, dotSize),
                    dotTexture,
                    ScaleMode.StretchToFill,
                    true);
            }

            GUI.color = oldColor;
        }

        // Converts a clockwise clock angle and radius into an IMGUI point.
        private static Vector2 GetCirclePoint(Rect circleRect, float angleDegrees, float radius)
        {
            var radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(
                circleRect.center.x + Mathf.Sin(radians) * radius,
                circleRect.center.y - Mathf.Cos(radians) * radius);
        }

        // Calculates current game-time angles and draws hour first, then minute on top.
        private void DrawClockDialHands(Rect circleRect, HudTuning tuning, float hudAlpha)
        {
            if (!TryGetClockTotalMinutes(out var totalMinutes))
            {
                return;
            }

            var minutes = totalMinutes % 60;
            var hours = totalMinutes / 60;
            var hourAngle = Mathf.Repeat((hours % 12 + minutes / 60f) * 30f, 360f);
            var minuteAngle = minutes * 6f;

            DrawClockDialHand(circleRect, hourAngle, tuning.DialClockHourHandLengthScale, tuning.DialClockHourHandWidthScale, tuning.DialClockHourHandColor, hudAlpha);
            DrawClockDialHand(circleRect, minuteAngle, tuning.DialClockMinuteHandLengthScale, tuning.DialClockMinuteHandWidthScale, tuning.DialClockMinuteHandColor, hudAlpha);
        }

        // Scales, pivots, colors, and rotates one hand around the clock center.
        private void DrawClockDialHand(Rect circleRect, float angleDegrees, float lengthScalePercent, float widthScalePercent, string colorText, float hudAlpha)
        {
            var texture = GetClockDialHandTexture();
            if (texture == null)
            {
                return;
            }

            var handHeight = texture.height * Mathf.Max(1f, lengthScalePercent) * 0.01f;
            var handWidth = texture.width * Mathf.Max(1f, widthScalePercent) * 0.01f;
            var handRect = new Rect(
                circleRect.center.x - handWidth * 0.5f,
                circleRect.center.y - handHeight * 0.5f,
                handWidth,
                handHeight);
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;

            GUI.color = GetHudColor(colorText, 0.95f * hudAlpha);
            GUIUtility.RotateAroundPivot(angleDegrees, circleRect.center);
            GUI.DrawTexture(handRect, texture, ScaleMode.StretchToFill, true);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        // Lazily loads the embedded clock-hand artwork.
        private Texture2D GetClockDialHandTexture()
        {
            if (_clockDialHandTexture != null)
            {
                return _clockDialHandTexture;
            }

            _clockDialHandTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.clockDialHand.png");
            return _clockDialHandTexture;
        }

        // Reads the current game clock and returns normalized minutes since midnight.
        private static bool TryGetClockTotalMinutes(out int totalMinutes)
        {
            var timeOfDay = TimeOfDay.Instance;
            if (timeOfDay == null)
            {
                totalMinutes = 0;
                return false;
            }

            var normalizedTime = Mathf.Repeat(timeOfDay.GetNormalizedTime(), 1f);
            totalMinutes = Mathf.RoundToInt(normalizedTime * 24f * 60f);
            var layout = FrozTLDMod.ClockLayout?.Values;
            if (layout != null)
            {
                totalMinutes += Mathf.RoundToInt(layout.TimeOffsetHours * 60f);
            }

            totalMinutes = NormalizeClockMinutes(totalMinutes);
            return true;
        }

        // Wraps arbitrary minute counts into a single 24-hour day.
        private static int NormalizeClockMinutes(int totalMinutes)
        {
            return ((totalMinutes % 1440) + 1440) % 1440;
        }
    }
}
