# ES Second Entry for NinjaTrader 8 — Design

## Purpose

Create a clean-room NinjaTrader 8 indicator for the E-mini S&P 500 (ES) on a
2,000-tick chart. It helps a discretionary trader see high-quality second-entry
continuation setups and objectively defined failed-second-entry reversals. It
does not place, modify, or cancel orders.

The package must be downloadable from GitHub and importable through
**NinjaTrader 8 → Tools → Import → NinjaScript Add-On**.

## Scope

### Included in v1

- Second Entry Long (`2EL`) and Second Entry Short (`2ES`) setup detection.
- Failed Second Entry Long (`F2EL`) and Failed Second Entry Short (`F2ES`)
  reversal markers.
- Configurable quality filters, alerts, chart labels, and entry/stop/target
  guide lines.
- Historical and real-time operation with non-repainting confirmed signals.
- Source code, import ZIP, installation guide, settings guide, and test cases.

### Excluded from v1

- Automated order submission or account interaction.
- Profitability claims, optimized parameters, or a trading recommendation.
- Copying the referenced commercial product's code, UI assets, names, or
  proprietary implementation.

## Signal Definitions

### Common terms

- **Signal bar:** the bar that completes the second pullback attempt.
- **Break level:** signal-bar high plus `BreakOffsetTicks` for a long; signal-bar
  low minus the same offset for a short.
- **Confirmed signal:** emitted only when a bar after the completed signal bar
  trades through the break level. Labels never move to a different bar after
  confirmation.
- **Leg:** a directional swing between qualifying pivots. Pivots are detected
  with a configurable symmetric lookback and are only finalized after the
  requisite bars have closed.

### Continuation signals

`2EL` requires an uptrend filter, a pullback that produces two distinct
downward attempts, a qualifying bullish signal bar after the second attempt,
and a trade through the long break level.

`2ES` is the mirror condition: downtrend filter, two distinct upward attempts,
a qualifying bearish signal bar, and a trade through the short break level.

The count resets after a confirmed trigger, a configurable equal-high/equal-low
event when `ResetOnEqualExtremes` is true, or an opposite structural break.

### Failed second-entry reversals

After a confirmed `2EL`, an `F2EL` is emitted when price trades through that
signal bar's low before `FailureWindowBars` bars elapse. This is a bearish
reversal marker. After a confirmed `2ES`, an `F2ES` is emitted when price
trades through that signal bar's high within the same window. This is a bullish
reversal marker.

By default, the window is five bars. A failure is tied to the originating
confirmed second entry; an ordinary later opposite setup is not retrospectively
called a failure.

## Quality Filters and Defaults

Defaults are conservative starting points for ES 2,000-tick charts, not
guaranteed profitable settings:

| Filter | Default | Behavior |
|---|---:|---|
| EMA period | 20 | Longs require close at/above a rising EMA; shorts the mirror. |
| Pivot strength | 2 bars | Defines finalized swing pivots. |
| Break offset | 1 tick | Requires a true break beyond the signal bar. |
| Minimum body | 40% of range | Filters doji-like bars. |
| Strong close | 30% | Long closes in top 30% of range; short in bottom 30%. |
| Maximum opposite wick | 40% of range | Rejects strong rejection against the setup. |
| Failure window | 5 bars | Time available for a triggered setup to fail. |
| Target guide | 8 points | Visual planning line only. |
| Stop guide | opposite signal-bar extreme ± 1 tick | Visual planning line only. |

Every filter can be turned off or adjusted in the NinjaTrader indicator
properties. A `StrictSignalBars` switch applies the body, close-location, and
opposite-wick tests together. Optional regular-trading-hours start/end times
prevent alerts outside the selected session.

## Indicator Behavior and UI

- Long continuations: green `2EL` label below the signal bar.
- Short continuations: red `2ES` label above the signal bar.
- Failed longs: orange `F2EL` label above the failure bar.
- Failed shorts: blue `F2ES` label below the failure bar.
- Optional higher-low/lower-high context labels.
- Optional entry, stop, and target projection lines; these are informational,
  not orders.
- One optional transparent plot per direction provides an attachable Chart
  Trader reference point.
- Alerts are emitted once per confirmed signal and include instrument,
  direction, setup type, and price.
- Diagnostic mode shows pivot/leg/count state so the trader can compare the
  algorithm with their chart reading.

## Architecture

`SecondEntryES` will be a single NinjaScript indicator with focused internal
components:

1. **PivotTracker** finalizes pivots and reports leg direction.
2. **SecondEntryStateMachine** maintains candidate first/second attempts,
   structural reset rules, and trigger levels.
3. **SignalQualityFilter** evaluates trend, signal-bar, and session rules.
4. **FailureTracker** owns the bounded post-trigger window and emits a failed
   setup at most once.
5. **ChartRenderer** draws stable labels/projections and updates the optional
   current-bar diagnostic only.
6. **AlertPublisher** deduplicates audible and log alerts.

The state machine processes historical bars on close for stable backtesting.
In real time, it monitors each forming post-signal bar for a break and
confirms a signal at the first valid break; it never moves or withdraws that
confirmation afterward.

## Error Handling

- Insufficient bars produce no signal.
- Zero-range bars cannot pass strict signal-bar filtering.
- Invalid time ranges and non-positive numeric settings are normalized to safe
  minimums in `State.Configure`.
- A failed sound file never prevents chart calculations; the indicator logs a
  NinjaTrader warning and continues.
- The indicator explicitly checks its chart series and disables time-session
  filtering for non-time-based series only when the user selects all-day mode.

## Testing and Acceptance Criteria

### Unit-style deterministic checks

Replay fixtures will cover: valid 2EL, valid 2ES, equal-extreme reset enabled
and disabled, rejected weak signal bar, trigger offset handling, F2EL, F2ES,
failure-window expiration, session exclusion, and one-alert-only behavior.

### NinjaTrader acceptance checks

1. Import the ZIP into a clean NinjaTrader 8 installation without compile
   errors.
2. Add `SecondEntryES` to an ES 2,000-tick chart and verify properties appear.
3. On Market Replay/historical data, verify labels stay anchored after reload.
4. Enable diagnostic mode and confirm a known second-entry sequence is counted
   as documented.
5. Enable alerts and verify a single alert per confirmed setup.
6. Confirm that no strategy/order/account methods are present in the source.

## Delivery

The GitHub repository will contain:

- `src/Indicators/SecondEntryES.cs`
- `tests/` with documented replay fixtures and expected results
- `docs/INSTALL.md` and `docs/SETTINGS.md`
- `dist/SecondEntryES-NinjaTrader8.zip`
- `README.md` with direct download/import steps and risk disclaimer

The release ZIP will use NinjaTrader's expected NinjaScript export structure;
the user can download it from GitHub and import it directly in NinjaTrader 8.
