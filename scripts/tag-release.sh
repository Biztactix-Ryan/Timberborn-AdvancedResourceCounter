#!/usr/bin/env bash
# Tags the manifest version and pushes it; GitHub Actions then builds and publishes the release.
set -euo pipefail
cd "$(dirname "$0")/.."

version=$(python3 -c 'import json; print(json.load(open("Mod/manifest.json"))["Version"])')
tag="v$version"

[[ -z $(git status --porcelain) ]] || { echo "Working tree is dirty; commit first." >&2; exit 1; }
! git rev-parse -q --verify "refs/tags/$tag" >/dev/null ||
  { echo "Tag $tag already exists; bump Version in Mod/manifest.json." >&2; exit 1; }
[[ -n $(scripts/changelog-notes.sh "$version") ]] ||
  { echo "CHANGELOG.md has no section for $version." >&2; exit 1; }

git push origin HEAD
git tag "$tag"
git push origin "$tag"
echo "Pushed $tag - the release appears once the Build workflow finishes."
