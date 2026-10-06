#!/usr/bin/env sh
set -eu
if command -v dotnet >/dev/null 2>&1 || command -v csc >/dev/null 2>&1 || command -v mcs >/dev/null 2>&1; then
  echo "A local C# compiler exists, but NinjaTrader 8 assemblies are required for the integration compile. Run the NT8 checklist in docs/INSTALL.md."
  exit 0
fi
echo "SKIP: no local C# compiler or NinjaTrader 8 installation is available; archive structure can still be verified."
exit 0
