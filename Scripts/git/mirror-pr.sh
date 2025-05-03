#!/bin/bash
set -e

PRNUM=$1 # First arg to a shell script

pr_info=$(gh pr view https://github.com/simple-station/einstein-engines/pull/$PRNUM --json number,title,headRefName,mergeCommit,body)
merge_commit=$(echo "$pr_info" | jq -r .mergeCommit.oid)
title=$(echo "$pr_info" | jq -r .title)
body=$(echo "$pr_info" | jq -r .body)

echo "Creating PR for: $title"


target_branch="upstream/$PRNUM"
current_branch=$(git rev-parse --abbrev-ref HEAD)
if [ "$current_branch" != "$target_branch" ]; then
    echo "Resetting current branch"
    git checkout master
    git pull

    parent_count=$(git rev-list --parents -n 1 "$merge_commit" | wc -w)

    echo "Creating branch"
    git checkout -b upstream/$PRNUM
    if [ "$parent_count" -gt 2 ]; then
        if ! git cherry-pick -m 1 $merge_commit; then
            read -r
            git cherry-pick --continue --no-edit
        fi
    else
        if ! git cherry-pick $merge_commit; then
            read -r
            git cherry-pick --continue --no-edit
        fi
    fi
    git push
fi

if [ -f .git/CHERRY_PICK_HEAD ]; then
    git cherry-pick --continue --no-edit
    git push
fi

body=$(echo "$pr_info" | jq -r .body)
body1=$(echo "$body" | sed 's/:cl:/:cl: EinsteinEngines/g')
body2=$(printf "Upstream PR: https://github.com/simple-station/einstein-engines/pull/%s\n%s" "$PRNUM" "$body1")
gh pr create --base master --head "upstream/$PRNUM" --title "Upstream Merge #$PRNUM | $title" --body "$body2"


