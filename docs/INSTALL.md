# Install and test

1. Download `SecondEntryES-NinjaTrader8.zip` to Desktop. Do not unzip it; a valid NT8 archive contains `Info.xml` and `Indicators\\SecondEntryES.cs`.
2. In NinjaTrader 8 Control Center: **Tools → Import → NinjaScript Add-On**.
3. Select the ZIP and allow NinjaTrader to compile it.
4. Open an ES 2,000-tick chart; right-click → **Indicators** → **SecondEntryES**.
5. Enable **Show Diagnostics** and test first with Playback/Market Replay.

If NinjaTrader reports a compile error, open **New → NinjaScript Editor → Compile** and send the exact Errors tab output. Do not use it for live decisions until you have compared its labels against your own replay review.

## Updating an existing installation

The indicator's stable identity is `SecondEntryES` (`SecondEntryES.cs`). NinjaTrader can reject a ZIP import when that source file already exists, so use the replacement path for updates:

1. Close NinjaTrader 8.
2. Download the release asset named `SecondEntryES.cs`.
3. Replace `Documents\\NinjaTrader 8\\bin\\Custom\\Indicators\\SecondEntryES.cs` with that file.
4. Restart NinjaTrader, open **New → NinjaScript Editor**, and select **Compile**.
5. Remove and add the indicator again on an open chart (or open a fresh chart) to clear prior drawing objects.
