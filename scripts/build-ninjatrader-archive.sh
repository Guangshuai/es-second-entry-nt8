#!/usr/bin/env sh
# Builds the exact source-archive layout accepted by NinjaTrader 8 Import.
set -eu

project_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
output_file="$project_dir/dist/SecondEntryES-NinjaTrader8.zip"
stage_dir=$(mktemp -d)

# A backslash is a legal macOS filename character.  Staging it this way causes
# zip to preserve the exact backslash path NT8's own exporter writes.
cp "$project_dir/src/Indicators/SecondEntryES.cs" "$stage_dir/Indicators\\SecondEntryES.cs"
# NT8's exporter writes an UTF-8 BOM and CRLF line endings for Info.xml.
{ printf '\357\273\277'; sed 's/$/\r/' "$project_dir/packaging/Info.xml"; } > "$stage_dir/Info.xml"

mkdir -p "$project_dir/dist"
archive_tmp="$stage_dir/SecondEntryES-NinjaTrader8.zip"
(cd "$stage_dir" && zip -q -X "$archive_tmp" 'Indicators\SecondEntryES.cs' 'Info.xml')
mv "$archive_tmp" "$output_file"
echo "Built $output_file"
