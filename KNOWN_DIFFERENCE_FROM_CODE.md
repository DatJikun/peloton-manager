# Known Difference From Code

## Stub race versus `RACE_ENGINE_DESIGN_v0.2.md`

Milestone 0 contains `StubRaceEngine` only to prove deterministic headless execution, GameState isolation, pre-race autosave, and result persistence.

It ranks a supplied start list using a versioned seed derivation plus the route ID and race number. It does not implement the canonical race model: physics, physiology, positioning, drafting, wind, terrain, information, tactics, DecisionTrace, or Race Spy are absent.

This is an explicit prototype boundary, not an accepted simplification of the future Race Engine. The dedicated Race Engine prototype remains a later task and must satisfy `RACE_ENGINE_DESIGN_v0.2.md` and `TESTING_v0.1.md` §9.

The Milestone 0 checksum and shared high-water ID allocator are likewise skeleton contracts. They do not close OQ-TS-001 or OQ-DM-001; changing their durable representation later requires normal schema/version review and migration.
