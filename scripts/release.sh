#!/usr/bin/env bash
# Builds the mod, zips it and publishes a GitHub release for the version in Mod/manifest.json.
# Usage: scripts/release.sh [--dry-run]   (extra dotnet args via BUILD_ARGS, e.g. -p:GamePath=...)
set -euo pipefail
cd "$(dirname "$0")/.."

version=$(python3 -c 'import json; print(json.load(open("Mod/manifest.json"))["Version"])')
tag="v$version"
zip="dist/AdvancedResourceCounter-$tag.zip"

if [[ -n $(git status --porcelain) ]]; then
  echo "Working tree is dirty; commit first." >&2
  exit 1
fi
if git rev-parse -q --verify "refs/tags/$tag" >/dev/null; then
  echo "Tag $tag already exists; bump Version in Mod/manifest.json." >&2
  exit 1
fi

dotnet build -c Release ${BUILD_ARGS:-}
rm -f "$zip"
(cd dist && zip -qr "../$zip" AdvancedResourceCounter -x 'AdvancedResourceCounter/workshop_data.json')
echo "Packaged $zip"

# Release notes: the CHANGELOG section for this version.
notes=$(awk -v v="$version" '/^## /{p = index($0, "## " v) == 1; next} p' CHANGELOG.md)

if [[ ${1:-} == --dry-run ]]; then
  printf 'Would release %s with notes:\n%s\n' "$tag" "$notes"
  exit 0
fi
gh release create "$tag" "$zip" --title "$tag" --notes "$notes"
