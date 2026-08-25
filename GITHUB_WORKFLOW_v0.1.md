# Peloton Manager — GitHub Workflow

**Version:** 0.1  
**Status:** REVIEW

## Main rule

`main` should remain buildable and testable.

Work happens in short-lived branches and is merged through reviewable pull requests once active development begins.

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

Before merge:

```text
build passes
relevant tests pass
no unexplained architecture violation
migration exists if schema changed
World Spy trace added for important automation
owner manual test done when gameplay feel matters
```

## Reverts

If a merged change badly breaks an invariant, prefer a clear Git revert over stacking emergency patches when practical. History should show what happened.
