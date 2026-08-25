# Peloton Manager — Determinism, Events & Information Contracts

**Wersja:** 0.1  
**Status:** REVIEW  
**Authority:** refines `ARCHITECTURE.md`; must not contradict `DECISIONS.md`.

## Determinism guarantee

```text
same simulation build
+ same resolved content/rules
+ same initial state
+ same ordered commands
= same gameplay result
```

Cross-version bit-identical replay is not promised unless a future ADR explicitly adds versioned simulation routing.

## Canonical order

Work is ordered by:

```text
SimulationTimestamp
→ ProcessingPhase
→ AuthorityAssignedSequence
→ StableWorkId / CommandId tie-break
```

Runtime-dependent collection iteration order is never business ordering.

## RNG

- no gameplay `new Random()`;
- no `.GetHashCode()` / `HashCode.Combine()` in persistent seed derivation;
- stable versioned seed derivation;
- isolated domains/scopes;
- cosmetic RNG cannot affect gameplay RNG;
- save/load restores or reconstructs RNG state deterministically.

## Event taxonomy

```text
ScheduledWork
CommandEnvelope
DomainEvent
ObservationSignal
DecisionRequest
DecisionRecord
HistoricalRecord
NotificationProjection
```

They are separate contracts, not one generic Event DTO.

## Information boundary

```text
Truth / Domain change
→ publication & observation rules
→ signal
→ knowledge store
→ interpretation / forecast
→ decision
```

AI never subscribes to hidden truth as knowledge.

## Forecast purity

Forecasts are:
- read-only,
- RNG-neutral,
- knowledge-bounded,
- uncertainty-capable.

Opening a preview twice cannot change the world or reveal a new hidden Monte-Carlo draw.

## Barrier semantics

A persistent `DecisionRequest` pauses only at a deterministic boundary. Processing rules define whether all work in the same timestamp/phase completes before the pause.

Future hotseat may create multiple human-owned DecisionRequests; the current single-player implementation must not assume the decision owner is globally unique.

## Numeric policy

Exact domains use exact representations (integer money units, IDs, counters, dates/ticks).

Race numeric representation is OPEN until race-engine spike. Fixed-point is an option, not a blanket pre-code mandate. The selected policy must pass deterministic regression tests on supported platforms/build constraints.


---

## Diagnostic tracing contract

World Spy / Decision Trace emissions are observational side effects only.

They must not:
- mutate authoritative World State,
- consume gameplay RNG,
- influence event/command ordering,
- alter actor knowledge,
- change deterministic checksums of gameplay state.

A regression test must compare Spy OFF and Spy DECISIONS for identical gameplay outcomes.
