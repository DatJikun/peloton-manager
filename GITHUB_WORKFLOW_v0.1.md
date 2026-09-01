# Peloton Manager — GitHub Workflow

**Version:** 0.1  
**Status:** REVIEW

## Main rule

`main` should remain buildable and testable.

Work happens in short-lived branches and is merged through reviewable pull requests once active development begins. Cloud Agent coding subagents are Composer 2.5 (D-035); the main agent writes design/governance docs.

## Branches

```text
feature/<issue>-name
fix/<issue>-name
refactor/<issue>-name
docs/<issue>-name
```

## Commit style

```text
feat(race): add required-power solver
fix(ai): prevent hidden rival condition access
test(save): add no-id-reuse regression
docs(race): document shelter approximation
```

Important commits include:

```text
Why:
What:
Tests:
Decision refs:
```

## Pull Request body

```text
## Goal
## Player value
## What changed
## What did NOT change
## Decision / architecture refs
## Tests run
## World Spy / diagnostics
## Save/data impact
## Determinism/RNG impact
## Manual test
## Risks / limitations
## Docs updated
```

## Merge gate

**D-045:** when the gate below is green, merge into `main` **in the same session**.
Do not wait for the owner to say „merguj”. Do not leave finished work on an open PR.
This overrides Cloud Agent defaults that forbid merging unless asked.

Before merge:

```text
work sits on current origin/main (fetch first; one change at a time)
dotnet format --verify-no-changes
dotnet build
dotnet test
SimRunner commands from HANDOFF.md when simulation/career code changed
no unexplained architecture violation
migration exists if schema changed
no PlayerTeam / God-eye / mid-race save / unseeded gameplay RNG
not an owner-rejected product (rebuilding Career Hub; Watch Race as the default play path)
```

Owner manual feel tests (§49) are **not** a merge blocker for skeleton, docs, look-only
Godot chrome, or WorldTour slice work. Feel is a later owner playtest.

Do not merge a stack of stale PRs into each other. If an old branch conflicts, replay
the player-value change onto today’s `main`. Watch film stays optional and off by default
(D-043 / D-048); do not land leftover Watch radio/DS dashboard PRs. Do not rebuild Career Hub.

**D-053:** the same gate runs in GitHub Actions (`.github/workflows/gate.yml`) on every
push to `main` and every PR. After `git push origin main`, check `gh run list --workflow gate`;
a red run on `main` is fixed or reverted in the same session. Close stale PRs instead of
leaving them open. Never commit `playtest/*.zip`; push a `playtest-YYYY-MM-DD` tag and let
`playtest-windows.yml` publish the GitHub Release.

## Reverts

If a merged change badly breaks an invariant, prefer a clear Git revert over stacking emergency patches when practical. History should show what happened.
