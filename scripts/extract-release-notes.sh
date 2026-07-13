#!/usr/bin/env bash
set -euo pipefail

TAG="${1:?Usage: extract-release-notes.sh vX.Y.Z [CHANGELOG.md]}"
CHANGELOG_PATH="${2:-CHANGELOG.md}"

awk -v header="## $TAG" '
  $0 == header {
    found = 1
    next
  }

  found && /^## / {
    exit
  }

  found {
    print
  }

  END {
    if (!found) {
      exit 3
    }
  }
' "$CHANGELOG_PATH"
