# Settings

- **EMA Period 20:** trend context; rising/above for longs, falling/below for shorts.
- **Pivot Strength 2:** reserved for later pivot refinement; v0.1 uses pullback attempts.
- **Break Offset 1 tick:** break required after the completed signal bar.
- **Strict Signal Bars:** requires body ≥40%, close in the directional 30% zone, and opposite wick ≤40%.
- **Reset On Equal Extremes:** equal extreme is treated as a reset when enabled.
- **Failure Window 5 bars:** a triggered 2EL/2ES becomes failed only if its opposite signal extreme breaks in this window.
- **Target Guide 8 points:** visual guide only; no orders are generated.
