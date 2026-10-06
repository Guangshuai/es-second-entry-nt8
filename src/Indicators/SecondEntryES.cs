// SecondEntryES - clean-room NinjaTrader 8 discretionary price-action indicator.
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class SecondEntryES : Indicator
    {
        private EMA trendEma;
        private int downAttempts, upAttempts;
        private double lastPullbackLow, lastPullbackHigh;
        private int pendingLongBar = -1, pendingShortBar = -1;
        private double pendingLongLow, pendingShortHigh;
        private int longConfirmedBar = -1, shortConfirmedBar = -1;
        private double longSignalLow, shortSignalHigh;

        [NinjaScriptProperty]
        [Range(2, 200)]
        [Display(Name = "EMA Period", GroupName = "Filters", Order = 0)]
        public int EmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Pivot Strength", GroupName = "Structure", Order = 0)]
        public int PivotStrength { get; set; }

        [NinjaScriptProperty]
        [Range(0, 20)]
        [Display(Name = "Break Offset (Ticks)", GroupName = "Structure", Order = 1)]
        public int BreakOffsetTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Strict Signal Bars", GroupName = "Filters", Order = 1)]
        public bool StrictSignalBars { get; set; }

        [NinjaScriptProperty]
        [Range(0.05, 1.0)]
        [Display(Name = "Minimum Body %", GroupName = "Filters", Order = 2)]
        public double MinimumBodyPercent { get; set; }

        [NinjaScriptProperty]
        [Range(0.05, 0.50)]
        [Display(Name = "Strong Close Zone %", GroupName = "Filters", Order = 3)]
        public double StrongCloseZonePercent { get; set; }

        [NinjaScriptProperty]
        [Range(0.05, 1.0)]
        [Display(Name = "Max Opposite Wick %", GroupName = "Filters", Order = 4)]
        public double MaxOppositeWickPercent { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Reset On Equal Extremes", GroupName = "Structure", Order = 2)]
        public bool ResetOnEqualExtremes { get; set; }

        [NinjaScriptProperty]
        [Range(1, 30)]
        [Display(Name = "Failure Window Bars", GroupName = "Failed Entries", Order = 0)]
        public int FailureWindowBars { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Target Guide (Points)", GroupName = "Display", Order = 0)]
        public int TargetGuidePoints { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Projections", GroupName = "Display", Order = 1)]
        public bool ShowProjections { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Diagnostics", GroupName = "Display", Order = 2)]
        public bool ShowDiagnostics { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Alerts Enabled", GroupName = "Alerts", Order = 0)]
        public bool AlertsEnabled { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Marks second-entry continuations and failed second-entry reversals. It never submits orders.";
                Name = "SecondEntryES";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                EmaPeriod = 20;
                PivotStrength = 2;
                BreakOffsetTicks = 1;
                StrictSignalBars = true;
                MinimumBodyPercent = 0.40;
                StrongCloseZonePercent = 0.30;
                MaxOppositeWickPercent = 0.40;
                ResetOnEqualExtremes = true;
                FailureWindowBars = 5;
                TargetGuidePoints = 8;
                ShowProjections = true;
                ShowDiagnostics = false;
                AlertsEnabled = true;
                AddPlot(Brushes.Transparent, "LongEntry");
                AddPlot(Brushes.Transparent, "ShortEntry");
            }
            else if (State == State.DataLoaded)
                trendEma = EMA(EmaPeriod);
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(EmaPeriod + PivotStrength + 2, 10)) return;
            Values[0][0] = double.NaN;
            Values[1][0] = double.NaN;

            bool upTrend = Close[0] >= trendEma[0] && trendEma[0] > trendEma[1];
            bool downTrend = Close[0] <= trendEma[0] && trendEma[0] < trendEma[1];
            bool downBar = Close[0] < Open[0];
            bool upBar = Close[0] > Open[0];

            // Count directional pullback attempts, resetting after a meaningful opposite bar.
            if (downBar && upTrend)
            {
                if (downAttempts == 0 || Low[0] < lastPullbackLow - TickSize || (!ResetOnEqualExtremes && Low[0] <= lastPullbackLow + TickSize))
                    downAttempts++;
                lastPullbackLow = Low[0];
            }
            else if (upBar && upTrend && downAttempts > 0 && QualifiesSignalBar(true))
            {
                if (downAttempts >= 2) { pendingLongBar = CurrentBar; pendingLongLow = Low[0]; }
            }

            if (upBar && downTrend)
            {
                if (upAttempts == 0 || High[0] > lastPullbackHigh + TickSize || (!ResetOnEqualExtremes && High[0] >= lastPullbackHigh - TickSize))
                    upAttempts++;
                lastPullbackHigh = High[0];
            }
            else if (downBar && downTrend && upAttempts > 0 && QualifiesSignalBar(false))
            {
                if (upAttempts >= 2) { pendingShortBar = CurrentBar; pendingShortHigh = High[0]; }
            }

            // A candidate cannot trigger on its own signal bar.
            if (pendingLongBar >= 0 && CurrentBar > pendingLongBar && High[0] >= High[CurrentBar - pendingLongBar] + BreakOffsetTicks * TickSize)
            {
                ConfirmLong(pendingLongBar, pendingLongLow, High[CurrentBar - pendingLongBar] + BreakOffsetTicks * TickSize);
                pendingLongBar = -1; downAttempts = 0;
            }
            if (pendingShortBar >= 0 && CurrentBar > pendingShortBar && Low[0] <= Low[CurrentBar - pendingShortBar] - BreakOffsetTicks * TickSize)
            {
                ConfirmShort(pendingShortBar, pendingShortHigh, Low[CurrentBar - pendingShortBar] - BreakOffsetTicks * TickSize);
                pendingShortBar = -1; upAttempts = 0;
            }

            if (longConfirmedBar >= 0 && CurrentBar - longConfirmedBar <= FailureWindowBars && Low[0] < longSignalLow)
            { MarkFailure(false); longConfirmedBar = -1; }
            else if (longConfirmedBar >= 0 && CurrentBar - longConfirmedBar > FailureWindowBars) longConfirmedBar = -1;
            if (shortConfirmedBar >= 0 && CurrentBar - shortConfirmedBar <= FailureWindowBars && High[0] > shortSignalHigh)
            { MarkFailure(true); shortConfirmedBar = -1; }
            else if (shortConfirmedBar >= 0 && CurrentBar - shortConfirmedBar > FailureWindowBars) shortConfirmedBar = -1;

            if (ShowDiagnostics)
                Draw.TextFixed(this, "SecondEntryES.Debug", "Down attempts: " + downAttempts + " | Up attempts: " + upAttempts, TextPosition.TopLeft, Brushes.Gray, new SimpleFont("Arial", 12), Brushes.Transparent, Brushes.Transparent, 0);
        }

        private bool QualifiesSignalBar(bool isLong)
        {
            if (!StrictSignalBars) return true;
            double range = High[0] - Low[0];
            if (range <= 0) return false;
            double body = Math.Abs(Close[0] - Open[0]) / range;
            if (body < MinimumBodyPercent) return false;
            if (isLong) return (High[0] - Close[0]) / range <= StrongCloseZonePercent && (Open[0] - Low[0]) / range <= MaxOppositeWickPercent;
            return (Close[0] - Low[0]) / range <= StrongCloseZonePercent && (High[0] - Open[0]) / range <= MaxOppositeWickPercent;
        }

        private void ConfirmLong(int bar, double stop, double entry)
        {
            int barsAgo = CurrentBar - bar;
            Values[0][0] = entry;
            Draw.Text(this, "2EL." + bar, "2EL", barsAgo, Low[barsAgo] - 2 * TickSize, Brushes.LimeGreen);
            if (ShowProjections) { Draw.HorizontalLine(this, "2EL.E." + bar, entry, Brushes.LimeGreen); Draw.HorizontalLine(this, "2EL.S." + bar, stop - TickSize, Brushes.OrangeRed); Draw.HorizontalLine(this, "2EL.T." + bar, entry + TargetGuidePoints, Brushes.DodgerBlue); }
            if (AlertsEnabled) Alert("2EL." + bar, Priority.Medium, "SecondEntryES 2EL " + Instrument.FullName, NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 0, Brushes.LimeGreen, Brushes.Black);
            longConfirmedBar = CurrentBar; longSignalLow = stop;
        }

        private void ConfirmShort(int bar, double stop, double entry)
        {
            int barsAgo = CurrentBar - bar;
            Values[1][0] = entry;
            Draw.Text(this, "2ES." + bar, "2ES", barsAgo, High[barsAgo] + 2 * TickSize, Brushes.Red);
            if (ShowProjections) { Draw.HorizontalLine(this, "2ES.E." + bar, entry, Brushes.Red); Draw.HorizontalLine(this, "2ES.S." + bar, stop + TickSize, Brushes.OrangeRed); Draw.HorizontalLine(this, "2ES.T." + bar, entry - TargetGuidePoints, Brushes.DodgerBlue); }
            if (AlertsEnabled) Alert("2ES." + bar, Priority.Medium, "SecondEntryES 2ES " + Instrument.FullName, NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 0, Brushes.Red, Brushes.White);
            shortConfirmedBar = CurrentBar; shortSignalHigh = stop;
        }

        private void MarkFailure(bool failedShort)
        {
            string label = failedShort ? "F2ES" : "F2EL";
            double price = failedShort ? Low[0] - 2 * TickSize : High[0] + 2 * TickSize;
            Draw.Text(this, label + "." + CurrentBar, label, 0, price, failedShort ? Brushes.DodgerBlue : Brushes.Orange);
            if (AlertsEnabled) Alert(label + "." + CurrentBar, Priority.Medium, "SecondEntryES " + label + " " + Instrument.FullName, NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert2.wav", 0, Brushes.Gold, Brushes.Black);
        }
    }
}
