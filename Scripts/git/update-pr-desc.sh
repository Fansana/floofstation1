#!/bin/bash

set -e

PRNUM=$1 # First arg to a shell script

pr_info=$(gh pr view https://github.com/simple-station/einstein-engines/pull/$PRNUM --json body)

body=$(echo "$pr_info" | jq -r .body)
body1=$(echo "$body" | sed 's/:cl:/:cl: EinsteinEngines/g')
body2=$(printf "Upstream PR: https://github.com/simple-station/einstein-engines/pull/%s\n%s" "$PRNUM" "$body1")
gh pr edit upstream/$PRNUM --title "Upstream Merge #$PRNUM" --body "$body2"

