name: 'Submit linter suggestions'

on:
  workflow_run:
    workflows: ["C# linting"]
    types:
      - completed

permissions:
  pull-requests: write

jobs:
  submit-linter-suggestions:
    name: 'Submit linter suggestions'
    runs-on: ubuntu-latest
    if: >
      ${{ github.event.workflow_run.event == 'pull_request' &&
      github.event.workflow_run.conclusion == 'success' }}

    steps:
      - name: Checkout
        uses: actions/checkout@v3

      # Download the artifact from the workflow that kicked off this one.
      # The default artifact download action doesn't support cross-workflow
      # artifacts, so use a 3rd party one.
      - name: 'Download linting results'
        uses: dawidd6/action-download-artifact@v2
        with:
          workflow: lint-csharp.yml
          run_id: ${{github.event.workflow_run.id }}
          name: pr-linter
          path: ./pr-linter

      - name: 'Setup reviewdog'
        uses: reviewdog/action-setup@v1

      # Manually supply the triggering PR event information since when a PR is from a fork,
      # this workflow running in the base repo will not be given information about it.
      #
      # Also patch the fork's owner id in the event file, since reviewdog has as fail-fast path that
      # checks the head vs base repo owner id to determine if the PR is from a fork.
      # If so, it assumes that it doesn't have permissions to write comments on the PR.
      #
      # This isn't the case in our setup since we are using two workflows (lint-csharp and this one)
      # to enable write permissions on fork PRs.
      - name: Submit formatting suggestions
        run: |
          new_event_file=${{github.workspace}}/reviewdog_event.json
          new_event_name=$(cat ./pr-linter/pr-event-name)
          jq -j ".${new_event_name}.head.repo.owner.id = .${new_event_name}.base.repo.owner.id" ./pr-linter/pr-event.json > ${new_event_file}
          GITHUB_EVENT_NAME="${new_event_name}" GITHUB_EVENT_PATH="${new_event_file}" REVIEWDOG_GITHUB_API_TOKEN="${{ secrets.GITHUB_TOKEN }}" reviewdog \
              -name="dotnet format" \
              -f=diff \
              -f.diff.strip=1 \
              -reporter="github-pr-review" \
              -filter-mode="diff_context" \
              -fail-on-error="false" \
              -level="warning" \
              < "./pr-linter/linter.diff"
