#!/bin/bash
set -eu

# This script updates the changelog by replacing unreleased release notes with versioned ones,
# marking them with current date as release date.
# It also re-adds the empty unreleased block.

if [[ $# -ne 1 ]]; then
    echo Needs the release version
    exit 1
fi

VERSION="$1"

if [[ ! -f CHANGELOG.md ]]; then
    echo The changelog does not exist
    exit 1
fi

DATE="$(date "+%Y-%m-%d")"

sed -i -r -e "s/^##\s*\[Unreleased\]/## [Unreleased]\n\n## [$VERSION] $DATE/" CHANGELOG.md
