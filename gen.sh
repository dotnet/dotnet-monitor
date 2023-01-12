#!/bin/bash

# $1 = branch
# $2 = last release tag

# Use a non-capturing lookbehind and lookahead to ensure we're grabbing the final PR number, and not an origin PR number.
prs_to_consider=$(\
    { git log --no-merges $1 --not $2 --format="%aE %s" --extended-regexp --grep="\(#[[:digit:]]+\)$" & git log --merges $1 --not $2 --format="%aE %s" --extended-regexp --grep="^Merge pull request"; }\
    | grep -v "dotnet-maestro\[bot\]" \
    | grep -oP '#[[:digit:]]+' \
    | xargs \
    | tr ' ' ',')
echo "$prs_to_consider"

# git log upstream/release/6.x --not v6.3.0 --format=%H