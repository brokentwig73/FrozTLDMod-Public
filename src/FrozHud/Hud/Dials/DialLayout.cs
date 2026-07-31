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
        // Sizes and positions the transparent lower HUD container beneath the ToD widget.
        private void DrawLowerHud(
            Rect topHudRect,
            ClockLayout layout,
            float hudAlpha,
            bool drawDialClock,
            bool drawTemperature,
            bool drawFeelsOutside,
            bool drawStickCompass,
            bool drawWindCompass,
            bool drawScentMeter,
            bool drawBackpackWeight)
        {
            // The lower HUD is a transparent layout container. Its children decide
            // whether to draw, and the container grows enough to center the active
            // dial gauges without wasting space.
            var drawDialThermometer = drawTemperature;
            var dialCount = GetDialGaugeCount(drawStickCompass, drawWindCompass, drawDialThermometer, drawDialClock, drawScentMeter, drawBackpackWeight);
            var drawDials = dialCount > 0;
            var tuning = GetHudTuning();
            var hudInnerBoxSize = GetHudInnerBoxSize(tuning);
            var dialRowCount = GetDialGaugeRowCount(dialCount);
            var dialGaugesHeight = drawDials ? GetDialGaugesHeight(dialRowCount, hudInnerBoxSize) : 0f;
            var compassWidth = drawDials ? Mathf.Min(3, dialCount) * hudInnerBoxSize : 0f;
            var effectiveLowerWidth = Mathf.Max(layout.LowerWidth, compassWidth);
            var effectiveLowerHeight = Mathf.Max(layout.LowerHeight, dialGaugesHeight);
            var lowerRect = new Rect(
                topHudRect.x + (topHudRect.width - effectiveLowerWidth) * 0.5f + layout.LowerOffsetX,
                topHudRect.yMax + layout.LowerHudGap + layout.LowerOffsetY,
                effectiveLowerWidth,
                effectiveLowerHeight);

            DrawLowerHudItems(lowerRect, hudAlpha, drawDialThermometer, drawDialClock, drawTemperature, drawFeelsOutside, drawStickCompass, drawWindCompass, drawScentMeter, drawBackpackWeight);
        }

        // Draws enabled dial rows inside the lower container.
        private void DrawLowerHudItems(Rect lowerRect, float hudAlpha, bool drawDialThermometer, bool drawDialClock, bool drawTemperature, bool drawFeelsOutside, bool drawStickCompass, bool drawWindCompass, bool drawScentMeter, bool drawBackpackWeight)
        {
            var drawDials = GetDialGaugeCount(drawStickCompass, drawWindCompass, drawDialThermometer, drawDialClock, drawScentMeter, drawBackpackWeight) > 0;
            if (!drawDials)
            {
                return;
            }

            EnsureStyles();
            DrawDialGauges(lowerRect, hudAlpha, drawStickCompass, drawWindCompass, drawDialThermometer, drawDialClock, drawScentMeter, drawBackpackWeight);
        }

        // Splits up to six enabled gauges into centered rows of three boxes each.
        private void DrawDialGauges(Rect dialGaugesRect, float hudAlpha, bool drawStickCompass, bool drawWindCompass, bool drawDialThermometer, bool drawDialClock, bool drawScentMeter, bool drawBackpackWeight)
        {
            // Two-level layout: hudParentRow1 is centered under the ToD HUD,
            // then each active gauge gets one hudInnerBox. Per-gauge structured
            // offsets move the full drawn assembly inside its assigned inner box.
            var gaugeItems = GetDialGaugeItems(drawStickCompass, drawWindCompass, drawDialThermometer, drawDialClock, drawScentMeter, drawBackpackWeight);
            if (gaugeItems.Count == 0)
            {
                return;
            }

            var tuning = GetHudTuning();
            var hudInnerBoxSize = GetHudInnerBoxSize(tuning);
            var row1Count = Mathf.Min(3, gaugeItems.Count);
            var hudParentRow1Width = row1Count * hudInnerBoxSize;
            var hudParentRow1 = new Rect(
                dialGaugesRect.center.x - hudParentRow1Width * 0.5f + tuning.HudParentRow1Offset.X,
                dialGaugesRect.y + (Mathf.Min(DialGaugeHeight, dialGaugesRect.height) - hudInnerBoxSize) * 0.5f + tuning.HudParentRow1Offset.Y,
                hudParentRow1Width,
                hudInnerBoxSize);
            DrawDialGaugeRow(hudParentRow1, hudInnerBoxSize, gaugeItems, startIndex: 0, rowItemCount: row1Count, hudAlpha);

            if (gaugeItems.Count > 3)
            {
                var row2Count = gaugeItems.Count - 3;
                var hudParentRow2Width = row2Count * hudInnerBoxSize;
                var hudParentRow2 = new Rect(
                    dialGaugesRect.center.x - hudParentRow2Width * 0.5f + tuning.HudParentRow2Offset.X,
                    hudParentRow1.y + hudInnerBoxSize + tuning.HudParentRow2Offset.Y,
                    hudParentRow2Width,
                    hudInnerBoxSize);
                DrawDialGaugeRow(hudParentRow2, hudInnerBoxSize, gaugeItems, startIndex: 3, rowItemCount: row2Count, hudAlpha);
            }
        }

        // Assigns each gauge one equal-sized box in a row and applies its per-item offset.
        private void DrawDialGaugeRow(Rect hudParentRow, float hudInnerBoxSize, List<DialGaugeItem> gaugeItems, int startIndex, int rowItemCount, float hudAlpha)
        {
            var hudInnerBoxX = hudParentRow.x;
            for (var index = 0; index < rowItemCount; index++)
            {
                var gaugeItem = gaugeItems[startIndex + index];
                var hudInnerBox = GetHudInnerBoxRect(hudInnerBoxX, hudParentRow.y, hudInnerBoxSize, GetDialGaugeItemOffset(gaugeItem));
                DrawDialGaugeItem(gaugeItem, hudInnerBox, hudAlpha);
                hudInnerBoxX += hudInnerBoxSize;
            }
        }

        // Builds the ordered list of enabled gauges shown in the HUD rows.
        private List<DialGaugeItem> GetDialGaugeItems(bool drawStickCompass, bool drawWindCompass, bool drawDialThermometer, bool drawDialClock, bool drawScentMeter, bool drawBackpackWeight)
        {
            var gaugeItems = new List<DialGaugeItem>(6);
            if (drawStickCompass)
            {
                gaugeItems.Add(DialGaugeItem.StickCompass);
            }

            if (drawWindCompass)
            {
                gaugeItems.Add(DialGaugeItem.WindDial);
            }

            if (drawDialThermometer)
            {
                gaugeItems.Add(DialGaugeItem.DialThermometer);
            }

            if (drawDialClock)
            {
                gaugeItems.Add(DialGaugeItem.DialClock);
            }

            if (drawScentMeter)
            {
                gaugeItems.Add(DialGaugeItem.ScentMeter);
            }

            if (drawBackpackWeight)
            {
                gaugeItems.Add(DialGaugeItem.BackpackWeight);
            }

            return gaugeItems;
        }

        // Returns the compiled X/Y adjustment for one gauge assembly inside its box.
        private HudPoint GetDialGaugeItemOffset(DialGaugeItem gaugeItem)
        {
            var tuning = GetHudTuning();
            return gaugeItem switch
            {
                DialGaugeItem.StickCompass => tuning.HudInnerBoxStickCompassOffset,
                DialGaugeItem.WindDial => tuning.HudInnerBoxWindDialOffset,
                DialGaugeItem.DialThermometer => tuning.HudInnerBoxDialThermometerOffset,
                DialGaugeItem.DialClock => tuning.HudInnerBoxDialClockOffset,
                DialGaugeItem.ScentMeter => tuning.HudInnerBoxScentMeterOffset,
                DialGaugeItem.BackpackWeight => tuning.HudInnerBoxBackpackOffset,
                _ => null
            };
        }

        // Routes a gauge item to its specialized renderer.
        private void DrawDialGaugeItem(DialGaugeItem gaugeItem, Rect hudInnerBox, float hudAlpha)
        {
            switch (gaugeItem)
            {
                case DialGaugeItem.StickCompass:
                    DrawStickCompassItem(hudInnerBox, hudAlpha);
                    break;
                case DialGaugeItem.WindDial:
                    DrawWindAngleGauge(hudInnerBox, hudAlpha);
                    break;
                case DialGaugeItem.DialThermometer:
                    DrawDialThermometerItem(hudInnerBox, hudAlpha);
                    break;
                case DialGaugeItem.DialClock:
                    DrawDialClockItem(hudInnerBox, hudAlpha);
                    break;
                case DialGaugeItem.ScentMeter:
                    DrawScentMeterGauge(hudInnerBox, hudAlpha);
                    break;
                case DialGaugeItem.BackpackWeight:
                    DrawBackpackWeightGauge(hudInnerBox, hudAlpha);
                    break;
            }
        }

        // Counts enabled gauges without allocating the ordered item list.
        private static int GetDialGaugeCount(bool drawStickCompass, bool drawWindCompass, bool drawDialThermometer, bool drawDialClock, bool drawScentMeter, bool drawBackpackWeight)
        {
            return (drawStickCompass ? 1 : 0) + (drawWindCompass ? 1 : 0) + (drawDialThermometer ? 1 : 0) + (drawDialClock ? 1 : 0) + (drawScentMeter ? 1 : 0) + (drawBackpackWeight ? 1 : 0);
        }

        // Returns the number of three-item rows required for an enabled gauge count.
        private static int GetDialGaugeRowCount(int dialCount)
        {
            return dialCount <= 0 ? 0 : (dialCount + 2) / 3;
        }

        // Calculates lower-container height for one or more gauge rows.
        private static float GetDialGaugesHeight(int rowCount, float hudInnerBoxSize)
        {
            return rowCount <= 1 ? DialGaugeHeight : DialGaugeHeight + (rowCount - 1) * hudInnerBoxSize;
        }

        // Returns the configured square slot size with a minimum usable dimension.
        private static float GetHudInnerBoxSize(HudTuning tuning)
        {
            return Mathf.Max(20f, tuning.HudInnerBoxSize);
        }

        // Creates a square gauge box with its assembly offset already applied.
        private static Rect GetHudInnerBoxRect(float x, float y, float size, HudPoint offset)
        {
            offset ??= new HudPoint();
            return new Rect(x + offset.X, y + offset.Y, size, size);
        }

        // Draws only the shared circular background for placeholder or future dial content.
        private void DrawDialBackgroundOnlyItem(Rect itemRect, float hudAlpha)
        {
            var tuning = GetHudTuning();
            var circleRect = GetDialCircleRect(itemRect, tuning);
            var backTexture = GetDialBackgroundTexture();
            if (backTexture == null)
            {
                return;
            }

            var oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, tuning.DialOpacity * 0.01f * hudAlpha);
            GUI.DrawTexture(circleRect, backTexture, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
        }

        // Calculates a centered row width from item count, width, and inter-item gap.
        private static float GetCenteredRowWidth(int itemCount, float itemWidth, float gap)
        {
            return itemCount * itemWidth + Mathf.Max(0, itemCount - 1) * gap;
        }
    }
}
