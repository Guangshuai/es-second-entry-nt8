#!/usr/bin/env sh
# Regression test for the NT8 import archive contract.
set -eu

project_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
archive="$project_dir/dist/SecondEntryES-NinjaTrader8.zip"
[ -f "$archive" ] || { echo "FAIL: archive missing: $archive"; exit 1; }

expected=$(printf 'Indicators\\SecondEntryES.cs\nInfo.xml')
actual=$(unzip -Z1 "$archive" | sort)
[ "$actual" = "$expected" ] || { echo "FAIL: incorrect archive entries"; unzip -Z1 "$archive"; exit 1; }

manifest=$(unzip -p "$archive" Info.xml)
printf '%s' "$manifest" | rg -q '<Version>8\.0\.0\.13</Version>' || { echo "FAIL: invalid Info.xml version"; exit 1; }
printf '%s\n' "$manifest" | rg -q '<NinjaTrader>' || { echo "FAIL: invalid Info.xml root"; exit 1; }
echo "PASS: NT8 archive layout and manifest contract"
