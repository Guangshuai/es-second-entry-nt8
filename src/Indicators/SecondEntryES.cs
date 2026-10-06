// SecondEntryES - clean-room NinjaTrader 8 price-action study. Never submits orders.
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class SecondEntryES : Indicator
    {
        private int longCount, shortCount;
        private int longAnchorBar = -1, shortAnchorBar = -1;
        private double longAnchorLow = double.NaN, shortAnchorHigh = double.NaN;
        private bool longPullbackActive, shortPullbackActive;
        private double longPullbackLow, shortPullbackHigh;
        private double firstLongPullbackLow, firstShortPullbackHigh;
        private int longConfirmedBar = -1, shortConfirmedBar = -1;
        private double longSignalLow, shortSignalHigh;

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Pivot Strength", GroupName = "Structure", Order = 0)]
        public int PivotStrength { get; set; }

        [NinjaScriptProperty]
        [Range(0, 20)]
        [Display(Name = "Break Offset (Ticks)", GroupName = "Structure", Order = 1)]
        public int BreakOffsetTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Reset On Equal Extremes", GroupName = "Structure", Order = 2)]
        public bool ResetOnEqualExtremes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Strict Signal Bars", GroupName = "Filters", Order = 0)]
        public bool StrictSignalBars { get; set; }

        [NinjaScriptProperty]
        [Range(0.05, 1.0)]
        [Display(Name = "Minimum Body %", GroupName = "Filters", Order = 1)]
        public double MinimumBodyPercent { get; set; }

        [NinjaScriptProperty]
        [Range(0.05, 0.50)]
        [Display(Name = "Strong Close Zone %", GroupName = "Filters", Order = 2)]
        public double StrongCloseZonePercent { get; set; }

        [NinjaScriptProperty]
        [Range(1, 30)]
        [Display(Name = "Failure Window Bars", GroupName = "Failed Entries", Order = 0)]
        public int FailureWindowBars { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Diagnostics", GroupName = "Display", Order = 0)]
        public bool ShowDiagnostics { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Alerts Enabled", GroupName = "Alerts", Order = 0)]
        public bool AlertsEnabled { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Marks pivot-anchored first and second entries, HL/LH context, and failed second entries. It never submits orders.";
                Name = "SecondEntryES";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                PivotStrength = 2;
                BreakOffsetTicks = 1;
                ResetOnEqualExtremes = true;
                StrictSignalBars = true;
                MinimumBodyPercent = 0.40;
                StrongCloseZonePercent = 0.30;
                FailureWindowBars = 5;
                ShowDiagnostics = false;
                AlertsEnabled = true;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2 * PivotStrength + 2)
                return;

            DetectConfirmedSwingAnchors();
            TrackLongSequence();
            TrackShortSequence();
            DetectFailedSecondEntries();

            if (ShowDiagnostics)
                Draw.TextFixed(this, "SecondEntryES.Debug",
                    "Long pivot: " + longAnchorLow + " | long count: " + longCount +
                    "\nShort pivot: " + shortAnchorHigh + " | short count: " + shortCount,
                    TextPosition.TopLeft, Brushes.Gray, new SimpleFont("Arial", 12),
                    Brushes.Transparent, Brushes.Transparent, 0);
        }

        // A pivot is confirmed only after PivotStrength later bars.  Counts are
        // anchored to the most recently confirmed low/high rather than an EMA.
        private void DetectConfirmedSwingAnchors()
        {
            int barsAgo = PivotStrength;
            if (Low[barsAgo] <= MIN(Low, 2 * PivotStrength + 1)[barsAgo])
            {
                longAnchorLow = Low[barsAgo];
                longAnchorBar = CurrentBar - barsAgo;
                longCount = 0;
                longPullbackActive = false;
            }

            if (High[barsAgo] >= MAX(High, 2 * PivotStrength + 1)[barsAgo])
            {
                shortAnchorHigh = High[barsAgo];
                shortAnchorBar = CurrentBar - barsAgo;
                shortCount = 0;
                shortPullbackActive = false;
            }
        }

        private void TrackLongSequence()
        {
            if (longAnchorBar < 0 || CurrentBar <= longAnchorBar)
                return;

            if (Low[0] <= longAnchorLow + (ResetOnEqualExtremes ? 0 : -TickSize))
            {
                longAnchorLow = Low[0];
                longAnchorBar = CurrentBar;
                longCount = 0;
                longPullbackActive = false;
                return;
            }

            if (Close[0] < Open[0])
            {
                if (!longPullbackActive)
                    longPullbackLow = Low[0];
                else
                    longPullbackLow = Math.Min(longPullbackLow, Low[0]);
                longPullbackActive = true;
                return;
            }

            if (longPullbackActive && BreaksPriorHigh() && QualifiesSignalBar(true))
            {
                longCount++;
                if (longCount == 1)
                {
                    firstLongPullbackLow = longPullbackLow;
                    Mark("1EL", true, 0, Low[0] - 2 * TickSize, Brushes.DodgerBlue);
                }
                else if (longCount == 2)
                {
                    Mark("2EL", true, 0, Low[0] - 2 * TickSize, Brushes.LimeGreen);
                    if (longPullbackLow > firstLongPullbackLow + TickSize)
                        Mark("HL", true, 0, longPullbackLow - TickSize, Brushes.Gold);
                    else
                        Mark("DT", true, 0, longPullbackLow - TickSize, Brushes.Orange);
                    longConfirmedBar = CurrentBar;
                    longSignalLow = longPullbackLow;
                }
                longPullbackActive = false;
            }
        }

        private void TrackShortSequence()
        {
            if (shortAnchorBar < 0 || CurrentBar <= shortAnchorBar)
                return;

            if (High[0] >= shortAnchorHigh - (ResetOnEqualExtremes ? 0 : -TickSize))
            {
                shortAnchorHigh = High[0];
                shortAnchorBar = CurrentBar;
                shortCount = 0;
                shortPullbackActive = false;
                return;
            }

            if (Close[0] > Open[0])
            {
                if (!shortPullbackActive)
                    shortPullbackHigh = High[0];
                else
                    shortPullbackHigh = Math.Max(shortPullbackHigh, High[0]);
                shortPullbackActive = true;
                return;
            }

            if (shortPullbackActive && BreaksPriorLow() && QualifiesSignalBar(false))
            {
                shortCount++;
                if (shortCount == 1)
                {
                    firstShortPullbackHigh = shortPullbackHigh;
                    Mark("1ES", false, 0, High[0] + 2 * TickSize, Brushes.MediumVioletRed);
                }
                else if (shortCount == 2)
                {
                    Mark("2ES", false, 0, High[0] + 2 * TickSize, Brushes.Red);
                    if (shortPullbackHigh < firstShortPullbackHigh - TickSize)
                        Mark("LH", false, 0, shortPullbackHigh + TickSize, Brushes.Gold);
                    else
                        Mark("DT", false, 0, shortPullbackHigh + TickSize, Brushes.Orange);
                    shortConfirmedBar = CurrentBar;
                    shortSignalHigh = shortPullbackHigh;
                }
                shortPullbackActive = false;
            }
        }

        private void DetectFailedSecondEntries()
        {
            if (longConfirmedBar >= 0 && CurrentBar - longConfirmedBar <= FailureWindowBars && Low[0] < longSignalLow)
            {
                Mark("F2EL", false, 0, High[0] + 2 * TickSize, Brushes.Orange);
                longConfirmedBar = -1;
            }
            else if (longConfirmedBar >= 0 && CurrentBar - longConfirmedBar > FailureWindowBars)
                longConfirmedBar = -1;

            if (shortConfirmedBar >= 0 && CurrentBar - shortConfirmedBar <= FailureWindowBars && High[0] > shortSignalHigh)
            {
                Mark("F2ES", true, 0, Low[0] - 2 * TickSize, Brushes.DodgerBlue);
                shortConfirmedBar = -1;
            }
            else if (shortConfirmedBar >= 0 && CurrentBar - shortConfirmedBar > FailureWindowBars)
                shortConfirmedBar = -1;
        }

        private bool BreaksPriorHigh() { return High[0] >= High[1] + BreakOffsetTicks * TickSize; }
        private bool BreaksPriorLow() { return Low[0] <= Low[1] - BreakOffsetTicks * TickSize; }

        private bool QualifiesSignalBar(bool isLong)
        {
            if (!StrictSignalBars) return true;
            double range = High[0] - Low[0];
            if (range <= 0) return false;
            double body = Math.Abs(Close[0] - Open[0]) / range;
            if (body < MinimumBodyPercent) return false;
            return isLong
                ? (High[0] - Close[0]) / range <= StrongCloseZonePercent
                : (Close[0] - Low[0]) / range <= StrongCloseZonePercent;
        }

        private void Mark(string label, bool isLong, int barsAgo, double price, Brush color)
        {
            Draw.Text(this, label + "." + CurrentBar + "." + barsAgo, label, barsAgo, price, color);
            if (AlertsEnabled && (label == "2EL" || label == "2ES" || label == "F2EL" || label == "F2ES"))
                Alert(label + "." + CurrentBar, Priority.Medium, "SecondEntryES " + label + " " + Instrument.FullName,
                    NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 0, color, Brushes.Black);
        }
    }
}
