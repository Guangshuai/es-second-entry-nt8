# ES Second Entry Indicator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a clean-room, importable NinjaTrader 8 indicator that marks ES 2,000-tick second-entry continuations and failed-second-entry reversals without placing orders.

**Architecture:** A self-contained NinjaScript indicator hosts small pure state/data helpers for pivot recognition, setup lifecycle, signal quality, and failed-setup tracking. The NinjaTrader adapter turns confirmed events into labels, guide lines, attachable plots, and deduplicated alerts; no strategy, account, or order API is used.

**Tech Stack:** C# / NinjaScript 8, .NET-compatible pure test harness, NinjaTrader NinjaScript export ZIP format, GitHub Releases.

**Spec:** `docs/superpowers/specs/2026-10-06-es-second-entry-design.md`

## Global Constraints

- Target NinjaTrader 8 and ES 2,000-tick charts; all numeric defaults remain editable.
- Detect 2EL, 2ES, F2EL, and F2ES; do not submit, alter, or cancel orders.
- Confirm only on post-signal-bar break; never repaint a confirmed label.
- Use only clean-room names, code, and visual assets.
- Ship source, direct-import ZIP, installation/settings docs, and deterministic test fixtures.

## Review Focus

- A doji/zero-range bar must not pass strict filtering; test in Task 2.
- Equal extreme must reset only when the configured switch is enabled; test in Task 3.
- A setup must not trigger on its own signal bar; test in Task 3.
- A failed-entry marker must be emitted only once and only inside its window; test in Task 4.
- All-day versus RTH session filters must give deterministic inclusion/exclusion; test in Task 2.

---

## File Structure

- `src/Indicators/SecondEntryES.cs` — NinjaScript 8 indicator, public properties, rendering, alerts, and package-facing entry point.
- `src/Core/SecondEntryModel.cs` — pure event/state types and configuration validation.
- `src/Core/SignalQualityFilter.cs` — pure trend, session, and signal-bar predicates.
- `src/Core/SecondEntryEngine.cs` — pure pivot/setup/failure state machine.
- `tests/SecondEntryEngineTests.cs` — deterministic engine/filter fixtures.
- `tests/run-tests.sh` — test-harness entry point when a local C# compiler exists; otherwise documents the NinjaTrader compile check.
- `docs/INSTALL.md` — GitHub download and NT8 Import NinjaScript Add-On steps.
- `docs/SETTINGS.md` — each property, ES defaults, visual labels, and replay workflow.
- `README.md` — project overview, disclaimer, release download/import quick start.
- `dist/SecondEntryES-NinjaTrader8.zip` — exported-source import package.

### Task 1: Scaffold the indicator and pure model

**Files:**
- Create: `src/Core/SecondEntryModel.cs`
- Create: `src/Indicators/SecondEntryES.cs`
- Create: `tests/SecondEntryEngineTests.cs`

**Interfaces:**
- Produces: `SetupDirection`, `SignalKind`, `SecondEntrySettings`, and `SignalEvent` used by all later tasks.

- [ ] **Step 1: Write the failing model-defaults test**

Assert defaults: EMA 20, pivot strength 2, break offset 1 tick, failure window 5 bars, minimum body 40%, strong close 30%, maximum opposite wick 40%, and target guide 8 points.

- [ ] **Step 2: Run the test to verify it fails**

Run: `tests/run-tests.sh`
Expected: FAIL because model/settings types are absent.

- [ ] **Step 3: Implement model types and a compiling NinjaScript shell**

Implement `SecondEntrySettings.CreateEs2000TickDefaults() -> SecondEntrySettings`; declare all public NinjaScript properties with matching values and no trading/order methods.

- [ ] **Step 4: Run the test to verify it passes**

Run: `tests/run-tests.sh`
Expected: PASS for default settings.

- [ ] **Step 5: Commit**

`git add src tests && git commit -m "feat: scaffold second-entry indicator"`

### Task 2: Implement signal-quality filtering

**Files:**
- Create: `src/Core/SignalQualityFilter.cs`
- Modify: `tests/SecondEntryEngineTests.cs`

**Interfaces:**
- Consumes: `SecondEntrySettings`, `SetupDirection`.
- Produces: `SignalQualityFilter.Passes(SignalBar, TrendSnapshot, SessionSnapshot, SetupDirection, SecondEntrySettings) -> bool`.

- [ ] **Step 1: Write failing quality-filter tests**

Cover a valid bullish/long signal bar, doji rejection, weak-close rejection, oversize-opposite-wick rejection, EMA trend mismatch, and RTH exclusion.

- [ ] **Step 2: Run tests to verify they fail**

Run: `tests/run-tests.sh`
Expected: FAIL because `SignalQualityFilter` is absent.

- [ ] **Step 3: Implement `SignalQualityFilter.Passes`**

Apply strict body/close/wick checks only when `StrictSignalBars` is true; validate range before division; apply trend and session conditions in all modes.

- [ ] **Step 4: Run tests to verify they pass**

Run: `tests/run-tests.sh`
Expected: PASS including zero-range and session cases.

- [ ] **Step 5: Commit**

`git add src/Core/SignalQualityFilter.cs tests && git commit -m "feat: add second-entry quality filters"`

### Task 3: Implement continuation setup lifecycle

**Files:**
- Create: `src/Core/SecondEntryEngine.cs`
- Modify: `tests/SecondEntryEngineTests.cs`

**Interfaces:**
- Consumes: finalized bars/pivots, `SecondEntrySettings`, and `SignalQualityFilter` result.
- Produces: `SecondEntryEngine.OnBar(BarSnapshot) -> IReadOnlyList<SignalEvent>` with `2EL` and `2ES` confirmation events.

- [ ] **Step 1: Write failing engine tests**

Create minimal bar fixtures for qualifying 2EL/2ES, non-trigger on the signal bar, one-tick break offset, equal-extreme reset enabled/disabled, and no duplicate trigger.

- [ ] **Step 2: Run tests to verify they fail**

Run: `tests/run-tests.sh`
Expected: FAIL because `SecondEntryEngine` is absent.

- [ ] **Step 3: Implement `SecondEntryEngine.OnBar`**

Use finalized two-bar pivots, keep a candidate with its originating bar/trigger, reset state exactly as configured, and emit only after a later bar crosses the correct break level.

- [ ] **Step 4: Run tests to verify they pass**

Run: `tests/run-tests.sh`
Expected: PASS for long, short, reset, offset, and duplicate cases.

- [ ] **Step 5: Commit**

`git add src/Core/SecondEntryEngine.cs tests && git commit -m "feat: detect confirmed second entries"`

### Task 4: Implement failed-second-entry tracking

**Files:**
- Modify: `src/Core/SecondEntryEngine.cs`
- Modify: `tests/SecondEntryEngineTests.cs`

**Interfaces:**
- Consumes: confirmed `2EL`/`2ES` event and later bar extremes.
- Produces: one `F2EL` or `F2ES` event before `FailureWindowBars` expiration.

- [ ] **Step 1: Write failing failure-tracker tests**

Cover F2EL after a 2EL signal-low break, F2ES after a 2ES signal-high break, expiration at bar six with default settings, and single emission.

- [ ] **Step 2: Run tests to verify they fail**

Run: `tests/run-tests.sh`
Expected: FAIL because failures are not emitted.

- [ ] **Step 3: Implement bounded failure tracking in `SecondEntryEngine`**

Store originating signal extremes and confirmation bar index; expire exactly after the configured number of subsequent bars; remove each tracker immediately after its event.

- [ ] **Step 4: Run tests to verify they pass**

Run: `tests/run-tests.sh`
Expected: PASS for both failure directions, expiry, and deduplication.

- [ ] **Step 5: Commit**

`git add src/Core/SecondEntryEngine.cs tests && git commit -m "feat: mark failed second entries"`

### Task 5: Integrate with NinjaTrader chart/alerts and package

**Files:**
- Modify: `src/Indicators/SecondEntryES.cs`
- Create: `docs/INSTALL.md`
- Create: `docs/SETTINGS.md`
- Create: `README.md`
- Create: `dist/SecondEntryES-NinjaTrader8.zip`

**Interfaces:**
- Consumes: `SecondEntryEngine.OnBar` events.
- Produces: stable draw objects, attachable values, once-only alerts, and a NinjaTrader import package.

- [ ] **Step 1: Write failing integration checks**

Add source-structure checks proving the four labels/properties exist, alerts deduplicate by signal ID, and the source excludes `EnterLong`, `EnterShort`, account, and order APIs.

- [ ] **Step 2: Run tests to verify they fail**

Run: `tests/run-tests.sh`
Expected: FAIL because chart integration and packaging checks are absent.

- [ ] **Step 3: Implement chart adapter and release assets**

Map events to stable draw tags/colors, projections, transparent direction plots, and user-toggleable sounds; write direct-import documentation and package source in NinjaTrader's expected archive structure.

- [ ] **Step 4: Run verification**

Run: `tests/run-tests.sh`
Expected: PASS. Also inspect archive contents and compile source with NinjaTrader 8 before release when a Windows/NinjaTrader host is available.

- [ ] **Step 5: Commit**

`git add src docs tests README.md dist && git commit -m "feat: package ES second-entry indicator"`

### Task 6: Publish GitHub release

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: validated `dist/SecondEntryES-NinjaTrader8.zip`.
- Produces: public GitHub repository and release asset download URL.

- [ ] **Step 1: Verify tracked release contents**

Run: `git status --short` and `unzip -l dist/SecondEntryES-NinjaTrader8.zip`
Expected: clean tracked source/docs and an archive containing only NinjaTrader import files.

- [ ] **Step 2: Create remote, push, and create release**

Create the `es-second-entry-nt8` repository under the authenticated user, push `main`, upload the ZIP to an initial GitHub Release, and record its browser-download URL in `README.md`.

- [ ] **Step 3: Verify the release asset URL**

Run: `gh release view v0.1.0 --repo Guangshuai/es-second-entry-nt8 --json url,assets`
Expected: one ZIP asset with a browser download URL.

- [ ] **Step 4: Commit release-link documentation update**

`git add README.md && git commit -m "docs: add release download link" && git push`
