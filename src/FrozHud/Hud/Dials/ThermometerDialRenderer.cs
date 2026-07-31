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
        // Draws the dynamic temperature scale, current/outdoor markers, freezing marker, and center value.
        private void DrawDialThermometerItem(Rect itemRect, float hudAlpha)
        {
            if (!MeasurementUnitProvider.TryGetCurrent(out var measurementUnits) ||
                !TryGetCurrentFeelsLike(measurementUnits, out var currentFeelsLike))
            {
                return;
            }

            var tuning = GetHudTuning();
            var circleRect = GetDialCircleRect(itemRect, tuning);
            var backTexture = GetDialBackgroundTexture();
            var oldColor = GUI.color;
            var oldMatrix = GUI.matrix;

            if (backTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, tuning.DialOpacity * 0.01f * hudAlpha);
                GUI.DrawTexture(circleRect, backTexture, ScaleMode.ScaleToFit, true);
            }

            var freezingTemperature = GetFreezingTemperature(measurementUnits);
            var outdoorFeelsLike = currentFeelsLike;
            var drawOutdoorNeedle = IsIndoorEnvironment() && TryGetOutsideFeelsLike(measurementUnits, out outdoorFeelsLike);
            GetThermometerScale(
                currentFeelsLike,
                drawOutdoorNeedle ? outdoorFeelsLike : currentFeelsLike,
                freezingTemperature,
                measurementUnits,
                out var scaleMinTemp,
                out var scaleMaxTemp,
                out var labelStartTemp,
                out var labelIncrement);

            DrawThermometerRangeLabels(circleRect, tuning, hudAlpha, scaleMinTemp, scaleMaxTemp, labelStartTemp, labelIncrement);
            DrawThermometerFreezingMarker(circleRect, tuning, hudAlpha, freezingTemperature, scaleMinTemp, scaleMaxTemp);

            DrawThermometerFeelsLikeMarker(circleRect, tuning, hudAlpha, currentFeelsLike, scaleMinTemp, scaleMaxTemp);
            if (drawOutdoorNeedle)
            {
                DrawThermometerOutdoorMarker(circleRect, tuning, hudAlpha, outdoorFeelsLike, scaleMinTemp, scaleMaxTemp);
            }

            DrawThermometerFeelsLikeValue(circleRect, tuning, hudAlpha, currentFeelsLike);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        // Draws eight evenly spaced temperature labels across the fixed dial sweep.
        private void DrawThermometerRangeLabels(Rect circleRect, HudTuning tuning, float hudAlpha, int scaleMinTemp, int scaleMaxTemp, int labelStartTemp, int labelIncrement)
        {
            EnsureStyles();

            var radius = circleRect.width * tuning.DialThermometerRangeRadius * 0.01f;
            var fontSize = Mathf.Max(4, tuning.DialThermometerRangeFontSize);
            var labelSize = fontSize * 2.7f;
            _dialOverlayFontStyle.fontSize = fontSize;
            _dialOverlayFontStyle.fontStyle = FontStyle.Bold;
            _dialOverlayFontStyle.normal.textColor = GetHudColor(tuning.DialThermometerRangeFontColor, 0.9f * hudAlpha);
            GUI.color = Color.white;

            for (var index = 0; index < ThermometerRangeLabelCount; index++)
            {
                var value = labelStartTemp + labelIncrement * index;
                var point = GetThermometerPoint(circleRect, value, scaleMinTemp, scaleMaxTemp, radius);
                GUI.Label(
                    new Rect(point.x - labelSize * 0.5f, point.y - labelSize * 0.5f, labelSize, labelSize),
                    value.ToString("0"),
                    _dialOverlayFontStyle);
            }
        }

        // Places the outward freezing marker at 32 F or 0 C on the current dynamic scale.
        private static void DrawThermometerFreezingMarker(Rect circleRect, HudTuning tuning, float hudAlpha, float freezingTemperature, int minTemp, int maxTemp)
        {
            DrawTriangleMarker(
                circleRect,
                GetThermometerAngle(freezingTemperature, minTemp, maxTemp),
                tuning.DialThermometerFreezingMarkerRadius,
                tuning.DialThermometerFreezingMarkerSize,
                GetHudColor(tuning.DialThermometerFreezingColor, 0.95f * hudAlpha),
                pointsInward: false,
                "Thermometer.Freezing");
        }

        // Places the inward current Feels Like marker on the dynamic scale.
        private static void DrawThermometerFeelsLikeMarker(Rect circleRect, HudTuning tuning, float hudAlpha, float currentFeelsLike, int minTemp, int maxTemp)
        {
            DrawThermometerInwardMarker(
                circleRect,
                GetThermometerAngle(currentFeelsLike, minTemp, maxTemp),
                tuning.DialThermometerFeelsLikeMarkerRadius,
                tuning.DialThermometerFeelsLikeMarkerSize,
                GetHudColor(tuning.DialThermometerFeelsLikeColor, 0.95f * hudAlpha),
                "Thermometer.FeelsLike");
        }

        // Places the inward outdoor estimate marker when the player is indoors.
        private static void DrawThermometerOutdoorMarker(Rect circleRect, HudTuning tuning, float hudAlpha, float outdoorFeelsLike, int minTemp, int maxTemp)
        {
            DrawThermometerInwardMarker(
                circleRect,
                GetThermometerAngle(outdoorFeelsLike, minTemp, maxTemp),
                tuning.DialThermometerOutdoorMarkerRadius,
                tuning.DialThermometerOutdoorMarkerSize,
                GetHudColor(tuning.DialThermometerOutdoorColor, 0.9f * hudAlpha),
                "Thermometer.OutdoorFeelsLike");
        }

        // Draws a named inward triangle using the shared marker asset and geometry.
        private static void DrawThermometerInwardMarker(Rect circleRect, float angle, float radiusPercent, float sizePercent, Color color, string markerName)
        {
            DrawTriangleMarker(circleRect, angle, radiusPercent, sizePercent, color, pointsInward: true, markerName);
        }

        // Draws the current Feels Like number inside the bordered center badge.
        private void DrawThermometerFeelsLikeValue(Rect circleRect, HudTuning tuning, float hudAlpha, float currentFeelsLike)
        {
            EnsureStyles();

            var badgeSize = tuning.DialThermometerFontCircleSize;
            var containerRect = new Rect(
                circleRect.center.x - badgeSize * 0.5f,
                circleRect.center.y - badgeSize * 0.5f,
                badgeSize,
                badgeSize);
            var borderColor = GetHudColor(tuning.DialThermometerFeelsLikeColor, 0.95f * hudAlpha);
            var fontColor = GetHudColor(tuning.DialThermometerFeelsLikeFontColor, 0.95f * hudAlpha);

            GUI.color = new Color(1f, 1f, 1f, 0.95f * hudAlpha);
            GUI.DrawTexture(containerRect, GetWindSpeedBadgeFillTexture(), ScaleMode.ScaleToFit, true);
            GUI.color = borderColor;
            GUI.DrawTexture(containerRect, GetThermometerValueBorderTexture(tuning), ScaleMode.ScaleToFit, true);

            _lowerHudValueStyle.fontSize = tuning.DialThermometerFeelsLikeFontSize;
            _lowerHudValueStyle.normal.textColor = fontColor;
            GUI.color = Color.white;
            GUI.Label(containerRect, Mathf.RoundToInt(currentFeelsLike).ToString("0"), _lowerHudValueStyle);
        }

        // Chooses unit-appropriate increments so eight labels contain current, outdoor, and freezing values.
        private static void GetThermometerScale(
            float currentFeelsLike,
            float outdoorFeelsLike,
            float freezingTemperature,
            MeasurementUnits measurementUnits,
            out int scaleMinTemp,
            out int scaleMaxTemp,
            out int labelStartTemp,
            out int labelIncrement)
        {
            var imperial = measurementUnits == MeasurementUnits.Imperial;
            var smallestIncrement = imperial ? 10 : 5;
            var mediumIncrement = imperial ? 15 : 10;
            var largestIncrement = imperial ? 20 : 15;
            var scaleMargin = imperial ? 20f : 10f;
            var minValue = Mathf.Min(currentFeelsLike, outdoorFeelsLike, freezingTemperature);
            var maxValue = Mathf.Max(currentFeelsLike, outdoorFeelsLike, freezingTemperature);
            var requiredMin = Mathf.FloorToInt((minValue - scaleMargin) / smallestIncrement) * smallestIncrement;
            var requiredMax = Mathf.CeilToInt((maxValue + scaleMargin) / smallestIncrement) * smallestIncrement;
            var requiredSpanPerSlot = (requiredMax - requiredMin) / (float)ThermometerRangeLabelCount;

            if (requiredSpanPerSlot <= smallestIncrement)
            {
                labelIncrement = smallestIncrement;
            }
            else if (requiredSpanPerSlot <= mediumIncrement)
            {
                labelIncrement = mediumIncrement;
            }
            else
            {
                labelIncrement = largestIncrement;
            }

            scaleMinTemp = Mathf.FloorToInt(requiredMin / (float)labelIncrement) * labelIncrement;
            scaleMaxTemp = scaleMinTemp + labelIncrement * (ThermometerRangeLabelCount - 1);

            while (requiredMax > scaleMaxTemp)
            {
                scaleMinTemp += labelIncrement;
                scaleMaxTemp = scaleMinTemp + labelIncrement * (ThermometerRangeLabelCount - 1);
            }

            while (requiredMin < scaleMinTemp)
            {
                scaleMinTemp -= labelIncrement;
                scaleMaxTemp = scaleMinTemp + labelIncrement * (ThermometerRangeLabelCount - 1);
            }

            labelStartTemp = scaleMinTemp;
        }

        // Converts a temperature on the active scale into a point around the dial sweep.
        private static Vector2 GetThermometerPoint(Rect circleRect, float value, int minTemp, int maxTemp, float radius)
        {
            var angle = GetThermometerAngle(value, minTemp, maxTemp);
            var radians = angle * Mathf.Deg2Rad;
            return new Vector2(
                circleRect.center.x + Mathf.Sin(radians) * radius,
                circleRect.center.y - Mathf.Cos(radians) * radius);
        }

        // Maps a temperature linearly onto the hard-coded 220-to-140 degree display sweep.
        private static float GetThermometerAngle(float value, int minTemp, int maxTemp)
        {
            var range = Mathf.Max(1f, maxTemp - minTemp);
            var fraction = Mathf.Clamp01((value - minTemp) / range);
            return Mathf.Repeat(ThermometerScaleStartDegrees + fraction * ThermometerScaleSweepDegrees, 360f);
        }

        // Reads the game's current body-temperature calculation in selected display units.
        private static bool TryGetCurrentFeelsLike(MeasurementUnits measurementUnits, out float value)
        {
            var freezing = GameManager.GetFreezingComponent();
            if (freezing == null)
            {
                value = 0f;
                return false;
            }

            value = ConvertCelsiusForDisplay(freezing.CalculateBodyTemperature(), measurementUnits);
            return true;
        }

        // Calculates the estimated outdoor Feels Like value in selected display units.
        private static bool TryGetOutsideFeelsLike(MeasurementUnits measurementUnits, out float value)
        {
            var outsideFeelsLike = CalculateOutsideFeelsLikeCelsius();
            if (float.IsNaN(outsideFeelsLike))
            {
                value = 0f;
                return false;
            }

            value = ConvertCelsiusForDisplay(outsideFeelsLike, measurementUnits);
            return true;
        }

        // Converts a Celsius value only when the player selected Imperial units.
        private static float ConvertCelsiusForDisplay(float valueCelsius, MeasurementUnits measurementUnits)
        {
            return measurementUnits == MeasurementUnits.Imperial
                ? CelsiusToFahrenheit(valueCelsius)
                : valueCelsius;
        }

        // Returns the freezing point in the player's selected display units.
        private static float GetFreezingTemperature(MeasurementUnits measurementUnits)
        {
            return measurementUnits == MeasurementUnits.Imperial
                ? FreezingFahrenheit
                : FreezingCelsius;
        }

        // Converts an absolute Celsius temperature to Fahrenheit.
        private static float CelsiusToFahrenheit(float valueCelsius)
        {
            return valueCelsius * 9f / 5f + 32f;
        }

        // Reconstructs outdoor Feels Like from weather day curve, wind chill, and clothing bonuses while indoors.
        private static float CalculateOutsideFeelsLikeCelsius()
        {
            var weather = GameManager.GetWeatherComponent();
            var wind = GameManager.GetWindComponent();
            var timeOfDay = TimeOfDay.Instance;
            var experienceMode = GameManager.GetExperienceModeManagerComponent();
            var playerManager = GameManager.GetPlayerManagerComponent();
            if (weather == null || wind == null || timeOfDay == null || experienceMode == null || playerManager == null)
            {
                return float.NaN;
            }

            var hour = timeOfDay.GetHour() + timeOfDay.GetMinutes() / 60f;
            var outdoorAir = CalculateLinearDayTemperature(
                weather.m_TempLow,
                weather.m_TempHigh,
                hour,
                weather.m_HourWarmingBegins,
                weather.m_HourCoolingBegins);
            outdoorAir -= experienceMode.GetOutdoorTempDropCelcius(timeOfDay.GetDayNumber());

            var exposedWindchill = Mathf.Min(0f, wind.GetBaseWindChill() + playerManager.m_WindproofBonusFromClothing);
            return outdoorAir + playerManager.m_WarmthBonusFromClothing + exposedWindchill;
        }

        // Interpolates the game's low/high daily temperature curve across wrapped warming and cooling periods.
        private static float CalculateLinearDayTemperature(float lowCelsius, float highCelsius, float hour, int warmingBeginsHour, int coolingBeginsHour)
        {
            if (warmingBeginsHour == coolingBeginsHour)
            {
                return float.NaN;
            }

            var warmingStart = NormalizeHour(warmingBeginsHour);
            var coolingStart = NormalizeHour(coolingBeginsHour);
            var currentHour = NormalizeHour(hour);

            if (IsHourInWrappedRange(currentHour, warmingStart, coolingStart))
            {
                return Mathf.Lerp(lowCelsius, highCelsius, FractionInWrappedRange(currentHour, warmingStart, coolingStart));
            }

            return Mathf.Lerp(highCelsius, lowCelsius, FractionInWrappedRange(currentHour, coolingStart, warmingStart));
        }

        // Wraps an arbitrary hour into the 0-24 range.
        private static float NormalizeHour(float hour)
        {
            hour %= 24f;
            return hour < 0f ? hour + 24f : hour;
        }

        // Tests membership in a time range that may cross midnight.
        private static bool IsHourInWrappedRange(float hour, float start, float end)
        {
            return start <= end ? hour >= start && hour <= end : hour >= start || hour <= end;
        }

        // Returns normalized progress through a time range that may cross midnight.
        private static float FractionInWrappedRange(float hour, float start, float end)
        {
            var length = end >= start ? end - start : end + 24f - start;
            var elapsed = hour >= start ? hour - start : hour + 24f - start;
            return length <= 0f ? 0f : Mathf.Clamp01(elapsed / length);
        }
    }
}
