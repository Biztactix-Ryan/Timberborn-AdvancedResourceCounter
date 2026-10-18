#!/usr/bin/env bash
# Prints the CHANGELOG.md section for a version (default: the manifest version).
set -euo pipefail
cd "$(dirname "$0")/.."
version=${1:-$(python3 -c 'import json; print(json.load(open("Mod/manifest.json"))["Version"])')}
awk -v v="$version" '/^## /{p = index($0, "## " v) == 1; next} p' CHANGELOG.md
