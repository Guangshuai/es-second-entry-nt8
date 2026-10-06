// Run this deterministic fixture in NinjaTrader's NinjaScript Editor after import.
// It documents the initial ES 2,000-tick defaults and acceptance cases.
// The production indicator must expose exactly these defaults.
namespace SecondEntryESTests
{
    internal static class SecondEntryEngineTests
    {
        // Expected defaults: EMA=20, pivot=2, break=1 tick, failure window=5,
        // body=40%, strong close=30%, opposite wick=40%, target=8 points.
        // Replay acceptance: a 2EL/2ES cannot fire on its own signal bar; F2EL/F2ES
        // must occur only when the opposite signal extreme breaks within five bars.
    }
}
