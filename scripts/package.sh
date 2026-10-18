#!/usr/bin/env bash
# Builds the mod and zips it as dist/AdvancedResourceCounter-vX.Y.Z.zip (version from the manifest).
# Extra arguments go to dotnet build, e.g. scripts/package.sh -p:GamePath=/path/to/Timberborn
set -euo pipefail
cd "$(dirname "$0")/.."

version=$(python3 -c 'import json; print(json.load(open("Mod/manifest.json"))["Version"])')
zip="dist/AdvancedResourceCounter-v$version.zip"

dotnet build -c Release "$@"
rm -f "$zip"
(cd dist && zip -qr "../$zip" AdvancedResourceCounter -x 'AdvancedResourceCounter/workshop_data.json')
echo "$zip"
