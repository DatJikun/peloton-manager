# Peloton Manager: Content Format

**Title:** Content Format

**Version:** 0.1

**Status:** DRAFT

**Purpose:** Define the JSON content-pack contract, stable content identity, deterministic composition, validation, overrides, and compatibility rules used to create a world.

**Authority/Owner:** Project owner (content and simulation architecture)

**Supersedes:** none

**Superseded by:** none

**Last reviewed:** 2026-08-25

**Related decisions/ADRs:** D-001, D-002, D-007, D-013, D-015, D-028, D-029

---

## 1. Purpose and scope

Peloton Manager loads static game definitions from versioned JSON content packs. The loader resolves dependencies and explicit overrides before a world is created. The result is a validated, ordered content set with a reproducible identity.

The format serves official databases, historical presets, fictional databases, custom scenarios, challenge overlays, and data-only mods. These sources use the same loader and validation rules.

### In scope

- pack manifests and payload resources;
- namespaced `ContentDefinitionId` values;
- schema and pack versions;
- dependencies, capabilities, incompatibilities, and overrides;
- deterministic composition of scenarios and era modules;
- structural, semantic, and reference validation;
- resolved-content identity for new worlds and saves;
- content schema compatibility and migration policy;
- data-only mod safety boundaries.

### Out of scope

- SQLite persistence and world serialization: `SAVE_FORMAT_v0.1.md`;
- rules execution and season transitions: `RULESETS_v0.1.md`;
- full rider, race, physiology, economy, contract, or calendar schemas;
- editor and Workshop user experience;
- runtime scripting or executable mods;
- download, trust, signing, and community moderation services.

---

## 2. Terms

| Term | Meaning |
|---|---|
| Content pack | A versioned manifest plus JSON resources distributed as one logical unit. |
| Content definition | A static definition addressed by `ContentDefinitionId`. |
| Content module | A definition set that fills one scenario slot, such as riders, calendar, equipment, or economy context. |
| Rules module definition | Content that configures a rules contract implemented by the simulation build. |
| Scenario recipe | A definition selecting compatible content and rules modules for world creation. |
| Resolved content | The exact ordered pack set, definitions, overrides, and schema interpretations accepted by the loader. |
| World entity | A save-local instance created from content or simulation, addressed by `WorldEntityId`. |

Content definitions are inputs. World entities are historical instances. A definition can create many entities in different saves without sharing their world identity.

---

## 3. Identity contract

### 3.1 ContentDefinitionId

`ContentDefinitionId` is a stable namespaced string.

Recommended shape:

```text
<domain>.<publisher-or-pack>.<local-name>
```

Examples:

```text
scenario.peloton.modern_default
organization.vistula.vistula_racing
race.peloton.tour_of_poland
calendar.peloton.modern_world_tour
rules.peloton.road_2026
equipment.community.steel_future
```

Rules:

- IDs use a documented restricted character set and ordinal comparison.
- Case normalization is not implicit. IDs that differ only by case are rejected to avoid filesystem-dependent behavior.
- Display name, sponsor name, localization key, and filename are not identity.
- An ID keeps the same meaning across compatible versions. A definition with materially different semantics receives a new ID or a declared migration.
- A pack cannot claim another publisher namespace without an explicit override declaration.
- Runtime hashes, object hash codes, and load positions are not IDs.

### 3.2 ContentDefinitionId versus WorldEntityId

```text
ContentDefinitionId = organization.vistula.vistula_racing
WorldEntityId       = OrganizationId 1042 in one save
```

An entity may retain `OriginDefinitionId` for provenance. Its `WorldEntityId` remains authoritative after renames, world evolution, content upgrades, retirement, or compaction.

`WorldEntityId` values are never allocated by content packs and never reused in a save (D-007).

### 3.3 Pack identity

A pack has:

```text
PackId
PackVersion
ContentSchemaVersion
Cryptographic content hash
```

Changing bytes covered by the pack hash without changing the recorded hash produces a different pack artifact, even if `PackId` and `PackVersion` are unchanged.

---

## 4. Pack boundary and layout

A pack contains one root manifest and one or more JSON resources. A typical layout may be:

```text
pack-root/
├── pack.json
├── scenarios/
├── riders/
├── organizations/
├── calendars/
├── rules/
└── localization/
```

Directory names are packaging conventions, not domain identity. The manifest declares every resource that participates in resolution. Unlisted files do not silently enter the world.

All content JSON uses UTF-8. The production schema will define exact number, date, enum, nullability, and unknown-field policies. Implementations must not depend on object-property order from source files.

---

## 5. Manifest contract

Minimum manifest semantics:

```text
PackManifest
    PackId
    PackVersion
    ContentSchemaVersion
    DisplayMetadata
    Resources[]
    Dependencies[]
    OptionalDependencies[]?
    ProvidesCapabilities[]?
    RequiresCapabilities[]?
    IncompatibleWith[]?
    Overrides[]?
```

### 5.1 Required fields

| Field | Contract |
|---|---|
| `packId` | Stable namespaced pack identity. |
| `packVersion` | Declared semantic release version of this pack. |
| `contentSchemaVersion` | Version of the JSON contract used to interpret its resources. |
| `resources` | Explicit resource list with resource kind and relative path. |
| `dependencies` | Required pack constraints needed before this pack can resolve. Empty is valid. |

Display metadata may contain localization keys, author attribution, license metadata, and description. It cannot affect simulation unless a gameplay field references it through a defined contract.

### 5.2 Resource declaration

Each resource entry declares at least:

```text
ResourceKind
RelativePath
ResourceSchemaVersion?  // only if a domain schema versions independently
ExpectedHash?           // distribution/integrity aid
```

Paths are normalized and confined to the pack root. Absolute paths, parent traversal, device paths, and links escaping the pack root are rejected.

### 5.3 Dependencies

A required dependency identifies a `PackId` and a supported version constraint. Resolution fails when no installed artifact satisfies it.

Optional dependencies cannot change the meaning of existing fields merely by appearing. A pack that uses an optional integration declares the capability or definition it consumes, so the resolved result remains auditable.

Dependency cycles are invalid.

### 5.4 Capabilities and incompatibilities

Capabilities describe contracts, not brand names. Examples:

```text
contracts.multi_year
race.communication.radio
equipment.carbon_frames
antidoping.testing
```

A scenario is valid only when all required capabilities have one compatible provider under the rules-module contract. Explicit incompatibility entries may reject known combinations that cannot be described by capabilities alone.

Capabilities do not permit arbitrary code dispatch. The simulation build recognizes supported capability and rules contract versions.

---

## 6. Deterministic dependency resolution

Content resolution is a pure pre-world operation. Given the same installed pack artifacts and scenario recipe, it returns the same result or the same validation failure.

Required phases:

```text
1. Read and structurally validate manifests
2. Select exact dependency versions
3. Build dependency graph
4. Reject missing dependencies, cycles, and incompatible constraints
5. Produce topological order with ordinal PackId tie-breaks
6. Read and validate declared resources
7. Register definitions
8. Apply explicit overrides in resolved order
9. Validate references, capabilities, ranges, and composition
10. Produce ResolvedContentIdentity
```

Filesystem enumeration order, archive entry order, locale, OS path comparison, and dictionary iteration never decide resolution order.

The resolver records the chosen dependency version for every constraint. A later run does not select a newer installed version when loading an existing save unless an explicit compatibility/migration flow permits it.

---

## 7. Definitions and references

Every gameplay definition has:

```text
ContentDefinitionId
DefinitionKind
DefinitionSchemaVersion or inherited content schema version
Payload defined by its domain contract
```

References use `ContentDefinitionId`, not display names or relative filenames.

Validation distinguishes:

- required reference: target must exist and have a compatible kind;
- optional reference: absent value is legal, but a supplied value must resolve;
- set reference: duplicates and ordering semantics are defined explicitly;
- effective-dated reference: target and transition policy must cover the effective period.

A missing or wrong-kind reference is a creation-time error. The loader does not replace it with the first matching name or a convenient default.

---

## 8. Overrides

Overrides are explicit patch declarations. Installing a second definition with the same ID is not enough to override the first.

An override identifies:

```text
Target ContentDefinitionId
Expected target kind
Operation
Patch or replacement resource
Expected source pack/version/hash constraint?
```

Initial operation vocabulary should stay small:

- `replace`: replace one complete definition;
- `merge`: apply a schema-defined field merge where that definition kind explicitly supports it;
- `remove`: allowed only where the scenario and reference graph remain valid.

Rules:

- duplicate definitions without an override declaration fail;
- two unrelated overrides of the same target fail unless their ordering is explicitly derivable and the target schema allows composition;
- array merge behavior is never guessed;
- an override cannot change the target's definition kind;
- reference and range validation runs after all overrides;
- the resolver records which pack changed which target and in what order;
- official content and community content use the same mechanics, though distribution trust policy may differ later.

There is no general "last file wins" rule.

---

## 9. Scenario and era composition

A scenario is a recipe, not a monolithic era switch. It selects independent module slots such as:

```text
riders
organizations
staff
races
calendar
competition rules
transfer rules
registration and roster rules
equipment and technology
medicine
anti-doping
organization structure
economy and sponsor market
media and information environment
training knowledge
race communication and safety
```

A historical preset supplies a tested combination. A custom scenario may mix modules from different periods or publishers.

Example supported intent:

```text
riders = modern
competition rules = historical
equipment = custom future
anti-doping = off
economy = late 1990s
```

The composition is accepted when schemas, references, capabilities, and rules contracts are compatible. The engine does not infer rules from the calendar year and does not correct an unusual but valid combination toward real history.

Scenario defaults such as difficulty, history mode, or attribute visibility are explicit recipe fields. Difficulty cannot change content visibility through an undocumented UI trap.

### 9.1 Composition output

Before `CreateWorld`, the loader produces:

```text
ResolvedScenario
    ScenarioDefinitionId
    Exact scenario artifact identity
    Ordered pack identities
    Selected module definitions by slot
    Resolved rules module definitions
    Applied override log
    Capability set
    Content and rules schema versions
    Aggregate resolved-content hash/identity
```

World creation consumes this frozen result. Human and AI actors use the same definitions and rules.

---

## 10. Validation of untrusted input

All external packs are untrusted input, including locally edited files and official content damaged in transit.

Validation layers:

### 10.1 Packaging safety

- pack paths cannot escape the pack root;
- duplicate normalized paths are rejected;
- resource size, nesting, collection, and string limits are enforced before large allocations;
- compressed packages are checked against expansion and entry-count limits;
- only declared data formats are read;
- content cannot request file, process, network, environment, or reflection access.

### 10.2 Structural validation

- required fields and supported schema versions;
- exact field types and numeric representations;
- enum and discriminator values;
- unknown-field policy;
- unique IDs within the resource and pack.

### 10.3 Semantic validation

- explicit units and valid ranges;
- date and effective-range consistency;
- mutually exclusive fields;
- domain invariants defined by the owning system contract;
- no NaN, Infinity, or locale-dependent number interpretation where numeric JSON is accepted.

### 10.4 Graph validation

- every required reference resolves to the correct definition kind;
- no forbidden dependency or inheritance cycle;
- no ambiguous provider or override;
- required capabilities are present and compatible;
- scenario slots are complete for the selected world recipe.

### 10.5 Simulation boundary

Validation may construct temporary resolved definitions. It does not create authoritative World State, allocate `WorldEntityId` values, consume gameplay RNG, run world Commands, or mutate an existing save.

---

## 11. Error contract

Validation returns structured issues rather than only free text.

```text
ContentIssue
    Stable issue code
    Severity
    PackId and artifact identity
    Resource path
    JSON path or definition ID?
    Related definition/pack IDs[]
    Human-readable message key and arguments
```

Errors prevent resolution. Warnings describe valid but suspicious or untested combinations. A warning cannot silently repair content.

Useful issue families include:

```text
MANIFEST_SCHEMA_UNSUPPORTED
DEPENDENCY_MISSING
DEPENDENCY_CYCLE
PACK_HASH_MISMATCH
DEFINITION_ID_DUPLICATE
REFERENCE_MISSING
REFERENCE_KIND_MISMATCH
VALUE_OUT_OF_RANGE
CAPABILITY_UNSATISFIED
OVERRIDE_AMBIGUOUS
PATH_OUTSIDE_PACK
SCENARIO_SLOT_MISSING
```

The same invalid pack set should produce issues in stable sort order.

---

## 12. Minimal illustrative pack

The example is deliberately small. Production domain schemas will add fields without changing the manifest, identity, dependency, and validation contracts here.

`pack.json`:

```json
{
  "packId": "peloton.example.fictional_2030",
  "packVersion": "1.0.0",
  "contentSchemaVersion": 1,
  "display": {
    "nameKey": "pack.example.fictional_2030.name"
  },
  "dependencies": [
    {
      "packId": "peloton.core",
      "version": ">=1.0.0 <2.0.0"
    }
  ],
  "resources": [
    {
      "kind": "organizations",
      "path": "organizations/example.json"
    },
    {
      "kind": "scenarios",
      "path": "scenarios/example.json"
    }
  ],
  "providesCapabilities": [
    "database.fictional"
  ]
}
```

`organizations/example.json`:

```json
{
  "definitions": [
    {
      "id": "organization.example.north_sea_cycling",
      "kind": "organization",
      "nameKey": "organization.example.north_sea_cycling.name",
      "countryId": "country.core.nl"
    },
    {
      "id": "organization-set.example.fictional_2030",
      "kind": "organization-set",
      "organizations": [
        "organization.example.north_sea_cycling"
      ]
    }
  ]
}
```

`scenarios/example.json`:

```json
{
  "definitions": [
    {
      "id": "scenario.example.fictional_2030",
      "kind": "scenario",
      "startDate": "2030-01-01",
      "baseScenario": "scenario.core.modern_default",
      "modules": {
        "organizations": "organization-set.example.fictional_2030"
      }
    }
  ]
}
```

The base scenario supplies the remaining slots through the declared core dependency. A standalone scenario must resolve every required slot itself.

---

## 13. ResolvedContentIdentity

The resolver produces a durable identity suitable for a save manifest:

```text
ResolvedContentIdentity
    Resolver contract version
    Content schema version(s)
    Scenario definition and artifact identity
    Ordered PackIdentity[]
        PackId
        PackVersion
        Cryptographic content hash
    Resolved dependency edges/order
    Selected module IDs by slot
    Applied override identities/order
    Rules module identities and contract versions
    Aggregate identity/hash
```

The aggregate identity is an integrity shortcut, not a replacement for the itemized list. Diagnostics must be able to say which pack, module, or override differs.

Opening a save resolves against the recorded identity, not merely the currently selected scenario name.

---

## 14. Content schema evolution

`PackVersion` and `ContentSchemaVersion` answer different questions:

- `PackVersion` identifies the publisher's release.
- `ContentSchemaVersion` tells the loader how to parse and validate fields.

Every content schema change requires the steps in `AI_DEVELOPMENT_RULES_v0.1.md` §25:

```text
schema version change
validator update
migration or compatibility policy
sample content update
tests
```

### 14.1 Change classes

| Change | Required policy |
|---|---|
| Optional additive field with defined default | Loader compatibility may accept the older artifact without rewriting it. |
| New required field | Pack migration or an explicit compatibility adapter is required. |
| Renamed field or enum value | Versioned migration; no alias that silently changes meaning forever. |
| Changed units, range, or semantics | Breaking schema change with explicit conversion and validation. |
| Removed definition | Reference impact and save compatibility must be declared. |
| Changed definition meaning under the same ID | Migration or new ID; silent reinterpretation is forbidden. |

Migrations operate on pack artifacts or an immutable resolved snapshot before world creation/load. They do not consume gameplay RNG or partially mutate an attached world.

### 14.2 Compatibility declarations

A pack may declare supported simulation/content contract ranges. The loader rejects a pack whose required contract is unknown to the current build.

Compatibility does not mean balance. Official combinations receive balance probes. Arbitrary custom combinations may be valid and poorly balanced without being corrupted.

---

## 15. Existing saves and changed content

An existing save never accepts changed content solely because `PackId` and `PackVersion` match.

Load checks:

1. recorded pack and module identities;
2. cryptographic hashes;
3. dependency and override order;
4. content and rules contract versions;
5. save schema compatibility;
6. availability of a declared migration or compatible immutable artifact.

Possible outcomes are defined by `SAVE_FORMAT_v0.1.md`: exact match, compatible migration, recoverable missing content, or hard incompatibility. The loader must not quietly substitute a similarly named definition.

Long-lived saves require an immutable content cache or a minimal resolved snapshot policy. The exact storage split remains open, but reproducibility is mandatory (ARCHITECTURE §115).

---

## 16. Mods in MVP

MVP modding is data-only.

A mod may:

- add packs and definitions;
- select existing supported rules contracts;
- provide values within schemas;
- declare dependencies, capabilities, and explicit overrides;
- provide localization and presentation assets under later asset rules.

A mod may not:

- execute arbitrary C#, GDScript, JavaScript, native code, shell commands, or macros;
- load arbitrary assemblies or plugins;
- access the network, environment, filesystem outside its package, or process APIs;
- introduce a new gameplay rule contract that the simulation build does not recognize;
- bypass validation by writing directly to a save.

Data-only does not mean trusted. Values and references still pass all validation layers.

---

## 17. Locked decisions

| Decision | Content consequence |
|---|---|
| D-001 | Content describes starting conditions and rules inputs; it does not script winners. |
| D-002 | Human and AI worlds resolve the same content and rules. |
| D-007 | Content IDs never replace or allocate save-local WorldEntityIds. |
| D-013 | Exact resolved content/rules identity is part of the determinism contract. |
| D-015 | Content migration cannot discard a future causal hook from an existing world. |
| D-028 | Content schema changes follow the mandatory migration and test workflow. |
| D-029 | Content contract changes use scoped commits and reviewable migration impact. |

---

## 18. Open questions

| ID | Question | Decision deadline |
|---|---|---|
| OQ-CF-001 | Exact cryptographic hash algorithm and canonical artifact-byte policy | Before Content loader implementation |
| OQ-CF-002 | Exact semantic-version constraint grammar and prerelease handling | Before dependency resolver implementation |
| OQ-CF-003 | Whether resource schemas reject all unknown fields or allow namespaced extension fields | Before first public mod schema |
| OQ-CF-004 | Which definition kinds support field-level `merge` rather than whole-definition `replace` | Before override implementation |
| OQ-CF-005 | Immutable local content cache versus embedding a minimal resolved snapshot in each save | Before `SAVE_FORMAT_v0.1.md` is accepted |
| OQ-CF-006 | Trust/signature policy for official and community artifacts | Before external pack distribution |

---

## 19. Deferred

- full schemas for riders, staff, races, physiology, training, contracts, economy, sponsors, and equipment;
- binary assets and localization bundle details;
- graphical Database Editor;
- mod browser, Workshop integration, installation UX, and signatures;
- procedural content generation authoring format;
- remote dependency repositories and automatic downloads;
- executable extension API.

---

## 20. Non-goals

- an editor-only content format;
- executable scripts or arbitrary mod code;
- hard-coded behavior selected by a famous race, rider, organization name, or calendar year;
- `PlayerTeam`, `IsHumanTeam`, or different content for human and AI simulation;
- storing WorldEntityId values inside static pack definitions;
- filesystem order or "last file wins" resolution;
- silent fixes for broken references or ranges;
- a complete production JSON Schema for every gameplay domain in v0.1;
- a guarantee that every valid custom module combination is balanced.

---

## 21. Implementation notes

- Keep the resolver headless and independent from Godot.
- Resolve and validate content before allocating WorldEntityIds or attaching World State.
- Use ordinal, culture-invariant comparison for IDs and deterministic sort keys.
- Report all independent validation issues in stable order when safe, rather than stopping at the first typo.
- Keep schema migration separate from gameplay Commands.
- Store applied override provenance so debug reports can identify the source of a value.
- Treat sample JSON as a contract test once production schemas exist.

---

## 22. Migration impact

This DRAFT introduces no implemented content schema and migrates no existing files. Its first implementation will establish schema version 1.

Later changes must describe:

- affected pack/resource versions;
- whether old artifacts remain directly readable;
- conversion behavior and failure handling;
- effect on `ResolvedContentIdentity` and existing saves;
- validator, sample, and compatibility-test updates.

---

## 23. Test criteria

### Identity and resolution

- Two definitions cannot share an ID without a legal explicit override.
- IDs that differ only by case are rejected.
- Dependency graph resolution returns the same order across repeated runs and supported platforms.
- Missing dependencies and cycles fail before world creation.
- Filesystem and JSON property order do not change the resolved identity.
- A pack byte change under the same ID/version produces a content identity mismatch.

### Validation and safety

- Broken, wrong-kind, and cyclic references produce stable issue codes and paths.
- Invalid ranges, dates, enums, units, and duplicate IDs are rejected.
- Path traversal and undeclared resources cannot escape the pack boundary.
- Oversized or deeply nested untrusted input fails within configured limits.
- Loading/validating a pack leaves World State and gameplay RNG unchanged.
- A data-only mod cannot request code, process, network, or unrestricted filesystem execution.

### Composition and overrides

- A tested historical preset and a compatible mixed-era scenario both resolve.
- An incompatible capability set fails with the conflicting modules named.
- Ambiguous overrides fail; source/archive order cannot choose a winner.
- Override provenance appears in the resolved identity/report.
- Human and AI actors created in the same world reference the same resolved definitions.

### Compatibility

- Each content schema fixture declares its schema version.
- A compatible additive change keeps its documented default semantics.
- A renamed or reinterpreted field requires a versioned migration test.
- Loading an existing save never substitutes a changed artifact with the same PackId/PackVersion and a different hash.
