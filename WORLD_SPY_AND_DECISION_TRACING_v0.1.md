# Peloton Manager — World Spy & Decision Trace Framework

**Version:** 0.1  
**Status:** REVIEW  
**Authority:** Cross-system diagnostic contract under `DECISIONS.md`, `ARCHITECTURE.md`, `DETERMINISM_AND_EVENT_CONTRACTS_v0.1.md`  
**Purpose:** provide one shared, passive, deterministic diagnostic framework for explaining important decisions and state transitions across the entire game.

---

## 1. North star

> **Every important automated decision in Peloton Manager must be explainable from the actor's knowledge, goals, constraints and options at the moment it was made.**

World Spy exists so developers can answer:

```text
WHAT HAPPENED?
WHO ACTED?
WHAT DID THEY KNOW?
WHAT DID THEY BELIEVE?
WHAT DID THEY WANT?
WHAT CONSTRAINTS APPLIED?
WHAT OPTIONS DID THEY CONSIDER?
WHY DID THEY CHOOSE THIS?
WHAT COMMAND WAS EMITTED?
WHAT HAPPENED NEXT?
```

This applies to the whole world, not only races.

---

## 2. Why this is a core architectural system

Peloton Manager deliberately relies on:

- organization-scoped knowledge,
- imperfect information,
- many AI managers with different traits,
- common Human/AI commands,
- emergent markets,
- delegation,
- long-term world evolution,
- 100-year simulations,
- explainable mistakes rather than arbitrary bad RNG.

Without a shared diagnostic framework, a strange result becomes:

> "Somewhere in the AI code it decided this."

That is unacceptable for a simulation this interconnected.

---

## 3. World Spy vs player-facing Why

These are separate products built from related structured data.

### World Spy

Developer-only.

May inspect:
- Simulation Truth,
- private actor knowledge,
- utility breakdown,
- deterministic RNG keys,
- hidden traits,
- internal constraints,
- debug assertions.

### Player-facing Why

Uses only information legally available to the player's current `AccessContext`.

It may explain:
- why staff recommended something,
- why an agent rejected an offer,
- why a sponsor prefers another team,
- why the DS used a domestique,
- why a rider is unhappy.

It may NOT leak:
- rival hidden offers,
- true potential,
- internal rival strategy,
- debug utility numbers,
- hidden physiological truth.

---

## 4. Hard invariants

### WS-001 — Passive observer

World Spy never mutates gameplay state.

### WS-002 — RNG neutral

Enabling/disabling tracing cannot consume gameplay RNG or change outcomes.

### WS-003 — Same system headless and client-side

Tracing works in:
- SimRunner,
- automated tests,
- normal game with debug tools,
- reproducibility runs.

### WS-004 — Decision-time truth

Explanations are recorded at decision time.

Never reconstruct "why" afterward using later knowledge.

### WS-005 — Truth and knowledge remain distinct

Every trace that can compare them uses explicit fields:

```text
SimulationTruthContext
ActorKnownInputs
ActorInterpretations
```

### WS-006 — Structured first

Core traces are structured records.

Human-readable text/Markdown is a projection.

### WS-007 — Bounded retention

Verbose diagnostics are not permanent 100-year history.

---

## 5. Common Decision Trace

Canonical cross-system structure:

```text
DecisionTrace
{
    DecisionId
    SimulationTime

    Domain
    DecisionType

    ActorPersonId?
    ActingOrganizationId?
    ActorRole?
    DecisionAuthority?

    Trigger

    Goals[]
    Constraints[]
    ActorKnownInputs[]
    ActorInterpretations[]
    Confidence

    ConsideredOptions[]
    OptionEvaluations[]

    SelectedOption
    SelectionReasons[]

    StochasticResolution?
    RandomDomain?
    RandomKey?

    CommandsEmitted[]

    RelatedEntities[]
    RelatedCases[]
    RelatedEvents[]

    OutcomeLinks[]
    TruthDebugRef?
}
```

Individual domains may extend this structure but must not invent incompatible tracing concepts.

---

## 6. Generic option evaluation

An option may contain structured evaluation dimensions such as:

```text
SportingValue
FinancialValue
StrategicValue
RelationshipValue
InformationValue
Risk
ResourceCost
TimeCost
OpportunityCost
RuleLegality
Confidence
```

Not every domain uses every dimension.

The point is not to create one universal magic utility equation.

The point is to preserve explainable reasons.

---

## 7. Recruitment / transfer spy

Must answer questions such as:

> Why did Team A scout Rider X?

> Why did Team B offer €2.4m?

> Why did Team C stop pursuing him?

> Why did this AI team sign a rider who later flopped?

Example:

```text
Decision:
Submit rider contract offer.

Known:
- climbing ability estimate: strong, medium confidence
- development outlook: promising, low confidence
- agent asks for leadership role
- two competitors reportedly interested
- roster needs GC support
- budget room limited

Manager interpretation:
high upside, expensive, strategic fit

Options:
A. Offer 2 years / €1.8m
B. Offer 3 years / €2.2m
C. Wait
D. Close case

Selected:
B

Reasons:
- competition likely
- long-term planning trait
- current roster age profile
- manager willing to accept uncertainty
```

Truth comparison may later show:

```text
true future development was mediocre
```

This proves the AI made a plausible scouting mistake rather than cheating.

---

## 8. Agent / negotiation spy

Must trace:

- agent statements,
- reliability/confidence,
- deadlines,
- competing signals,
- role expectations,
- salary expectations,
- relationship state,
- offer history,
- reasons for counteroffer/rejection/acceptance.

Example:

```text
Agent rejected offer.

Actor-known reasons:
- salary below preferred range
- promised role insufficient
- competing project stronger
- client values GT leadership

Hidden developer truth:
competitor offer exists and is higher
```

Player-facing Why only shows what the agent/player organization legitimately knows.

---

## 9. Sponsor spy

Must answer:

> Why did this company sponsor Team X?

> Why did Sponsor Y leave cycling?

> Why did they pay Team A more than Team B?

Inputs may include:

```text
target markets
cycling popularity by country/era
brand strategy
team nationality/reach
sporting visibility
reputation
scandal risk
calendar exposure
budget
existing sponsorship commitments
manager/team relationships
```

This is especially important because sponsor economics should emerge from different countries/eras rather than a global balancing tax.

Example:

```text
Sponsor 91 selected Team 12.

Reasons:
- Polish market strategic priority: HIGH
- cycling popularity in Poland: RISING
- Team 12 has two Polish riders
- planned Tour + Tour de Pologne exposure
- cost lower than WorldTour competitor
- reputation risk acceptable
```

---

## 10. Sponsor-market world trace

World Spy should also explain macro movement:

```text
Why did French cycling sponsorship decline from 2042–2048?
```

Possible structured causal contributors:

- sponsor exits,
- scandal exposure,
- economic downturn,
- lower domestic audience,
- competing sports investment,
- regulation change,
- loss of star riders,
- new sponsor entrants elsewhere.

Do not reduce this to:
`FranceSponsorMultiplier -= 0.2`.

---

## 11. Staff hiring/firing spy

Must answer:

- why organization hired a DS,
- why coach was fired,
- why staff member refused an offer,
- why former employee followed a manager,
- why AI poached staff.

Example:

```text
Hire DS Marta Rossi

Reasons:
- cobbled classics expertise
- manager trusts former colleague
- current DS contract ending
- organization strategy shifting toward classics
- salary within staff budget

Trade-off:
weaker GC management than alternative candidate
```

---

## 12. Manager job-market spy

Must answer:

- why board fired a manager,
- why organization approached a candidate,
- why manager accepted/rejected,
- why AI manager changed teams,
- why human manager received an offer.

Important dimensions:

```text
results vs expectations
budget performance
sponsor relations
youth development
staff stability
strategic fit
manager reputation
contract/autonomy
organization prestige
career ambition
personal relationships
```

---

## 13. Race Spy specialization

`RACE_SPY_DEBUGGING_v0.1.md` remains the race-domain implementation of World Spy.

It adds race-specific:
- rider truth samples,
- group state,
- shelter,
- power demand,
- W' state,
- DecisionRequest gates,
- briefing rules.

It must reuse the common Decision Trace identity and semantics.

---

## 14. Calendar / race-entry spy

Must answer:

> Why is this race on Team X's calendar?

Existing provenance becomes traceable:

```text
Mandatory
SponsorPriority
SeasonObjectivePreparation
WildcardAccepted
ManagerAdded
StaffRecommendation
```

Also:

> Why did AI decline this invitation?

Possible reasons:
- race-day overload,
- travel cost,
- roster conflict,
- sponsor value low,
- preparation conflict,
- injury shortage,
- expected sporting value low.

---

## 15. Rider selection spy

Must answer:

> Why did the DS select Rider A instead of Rider B?

Inputs may include:
- organization estimates,
- current condition,
- role promises,
- course suitability,
- workload,
- next objectives,
- rider morale,
- sponsor pressure,
- DS traits.

No hidden `trueOverall`.

---

## 16. Training / development spy

Must answer:

- why coach changed training,
- why rider progress estimate changed,
- why staff believes rider stagnated,
- why training load was reduced,
- why camp was recommended.

Developer truth may compare:
- actual adaptation state,
- illness,
- accumulated fatigue,
- hidden development factors.

Player-facing explanation remains staff interpretation.

---

## 17. Medical spy

Must separate:

```text
actual medical truth
observed symptoms
doctor assessment
confidence
recommended action
organization decision
```

This prevents:
- doctors magically knowing exact hidden truth,
- player receiving omniscient diagnosis,
- AI teams responding to injuries they should not know exist.

---

## 18. Finance spy

Must answer:

> Why can't the team afford this?

Not:

```text
Insufficient budget.
```

But:

```text
Cash available: sufficient
Future payroll headroom: insufficient
Guaranteed sponsor income: X
Existing committed contracts: Y
Regulatory roster/payroll constraint: Z
```

Also traces important automated finance decisions.

---

## 19. Equipment / R&D spy

Must answer:

- why AI chose a partner,
- why project priority changed,
- why equipment recommendation changed,
- why a team accepted cash instead of better technology.

Inputs include:
- cash offer,
- technical capability,
- discipline fit,
- staff trust,
- current weakness,
- contractual length,
- future strategy.

---

## 20. Scouting spy

Must answer:

> Why does Organization A think Rider X is excellent while Organization B thinks he is average?

Compare:

```text
evidence sets
source quality
scout skill
regional expertise
sample size
observation dates
biases
confidence
staleness
```

Developer truth may display actual capability separately.

This is a primary test of:

> Truth belongs to Simulation. Knowledge belongs to organizations.

---

## 21. Knowledge lifecycle spy

World Spy should trace:

- creation of knowledge subject,
- important new observations,
- estimate changes,
- confidence changes,
- staleness,
- compaction,
- portability on staff/manager movement.

This is critical for catching:
- impossible retained private information,
- amnesia after staff changes,
- cross-organization leaks,
- runaway 100-year knowledge growth.

---

## 22. AI organization strategy spy

At an organization level World Spy should answer:

> Why is this team rebuilding?

> Why is it suddenly recruiting youth?

> Why did it cut expensive veterans?

Example:

```text
Current strategic state:
Financial Rebuild

Drivers:
- sponsor revenue down
- payroll commitments high
- manager financial discipline high
- recent sporting return on salary poor
- several prospects internally rated promising

Resulting policy shifts:
- reduce veteran extensions
- prioritize U23 scouting
- avoid bidding wars
```

---

## 23. Relationship / promise spy

Important relationship events should expose:

- promise made,
- expectation,
- actor interpretation,
- violation/satisfaction event,
- memory update,
- consequence.

Example:

```text
Rider morale decreased.

Reason:
leadership promise expected 2 GT opportunities
actual assignment provided 0
manager explanation judged insufficient
```

No mysterious `Morale -8`.

---

## 24. Doping/integrity spy

Developer diagnostics may trace:

```text
true illegal behavior
who knows
who suspects
evidence
investigation probability/trigger
actor ethics/willingness
sponsor knowledge
```

Normal actors only react to information they possess.

This is particularly important for delayed investigations years later.

---

## 25. World-event spy

For emergent events:

> Why did organization collapse?

> Why did sponsor market move?

> Why did this rivalry emerge?

World Spy should create a causal graph or trace linking important DomainEvents and decisions.

Not every tiny event requires a permanent graph.

Important outliers do.

---

## 26. Causal links

Decision and event traces should support links:

```text
Decision A
→ Command B
→ DomainEvent C
→ Observation D
→ Decision E
→ HistoricalRecord F
```

This allows a debugging question to move backward and forward in causality.

---

## 27. Spy domains

Canonical domain enumeration should eventually include at least:

```text
Race
Recruitment
Negotiation
Contracts
StaffMarket
ManagerMarket
Sponsors
Finance
Calendar
Selection
Training
Development
Medical
Scouting
Knowledge
Equipment
Integrity
OrganizationStrategy
WorldEvolution
```

Domains may add specialized trace payloads.

---

## 28. Spy levels

A common tracing level model:

### OFF
Only critical assertions/checksums.

### IMPORTANT
Only major decisions and abnormal events.

### DECISIONS
All meaningful automated decisions.

### VERBOSE
Detailed domain-specific state samples.

Domains can interpret VERBOSE differently.

---

## 29. Assertions

World Spy should support automatic invariant failures such as:

```text
HiddenTruthLeak
UnauthorizedKnowledgeAccess
UnexplainedAutomatedDecision
InvalidKnowledgeTimestamp
DuplicateResolution
IllegalCommand
InvalidStateTransition
NonCanonicalOrder
GameplayRNGConsumedByQuery
GameplayRNGConsumedBySpy
CompactionChangedCausality
StableIdReused
ActorWithoutAuthority
NaNOrInvalidUtility
```

---

## 30. 100-year World Spy strategy

Never keep verbose logs for the whole world for 100 years.

Long simulations retain:

- aggregate metrics,
- assertion failures,
- sampled decision traces,
- outlier traces,
- selected causal chains,
- reproducibility bundles.

Automatic outlier examples:

```text
team signs six expensive sprinters
manager trait dominates 40 years
sponsor market in one country collapses unexpectedly
transfer salary outlier
AI repeatedly renews clearly unwanted rider
organization goes bankrupt despite strong income
favorite loses bizarre race
knowledge table growth spikes
```

---

## 31. Cross-era analysis

World Spy makes manager/ruleset balance measurable.

Example query:

```text
Trait: DataReliance

1965 rules:
- frequently low impact
- information environment sparse

2026 rules:
- high positive impact in recruitment
- moderate race impact

Custom high-tech:
- potentially dominant
```

The tool should reveal *why* the trait matters in each environment, not merely win-rate correlation.

---

## 32. Reproducibility bundle

Any serious outlier should be exportable with:

```text
SimulationBuildVersion
ResolvedContentHashes
RulesetHashes
MasterSeed / relevant derived keys
StartStateSnapshot
CommandSequence
DecisionTraces
RelevantKnowledgeSnapshots
Assertion failures
Expected/actual checksums
```

This is designed for handoff between AI development sessions.

---

## 33. Query examples

Developer tooling should eventually allow queries such as:

```text
why decision <id>
why did org <id> sign rider <id>
why did org <id> reject sponsor <id>
why did manager <id> get fired
why is rider <id> unhappy
why did team <id> chase
compare knowledge orgA orgB riderX
show causal chain event <id>
show decisions affected by trait <trait>
show hidden-truth leak assertions
```

UI can be a developer console/table later.

The data contract matters first.

---

## 34. Report format

Preferred output:

1. structured JSON/JSONL for machines,
2. Markdown/HTML projection for humans.

A human report should avoid dumping hundreds of raw fields.

It should summarize:
- trigger,
- actor,
- known facts,
- beliefs,
- constraints,
- options,
- selected action,
- reasons,
- consequences,
- suspicious conditions.

---

## 35. Performance

World Spy must not force all systems to serialize huge objects each tick.

Guidelines:
- trace only meaningful decisions by default,
- use stable IDs and compact structured values,
- lazily project prose,
- sample verbose state,
- allow domain filters,
- benchmark Spy OFF vs DECISIONS.

---

## 36. Architecture integration

World Spy receives structured diagnostic emissions from domain/application/simulation systems.

It does not become the owner of gameplay decisions.

Conceptually:

```text
Domain/Application/Simulation
    executes normal logic
        ↓
Structured Trace Emission
        ↓
WorldSpySink
        ↓
Assertions / Report / Export / Metrics
```

Gameplay code does not query World Spy to decide what to do.

---

## 37. Implementation sequence

Before broad AI system implementation:

```text
1. Common DecisionTrace schema
2. Trace sink interface
3. Deterministic trace IDs
4. Race Spy adapter
5. Recruitment/contract trace
6. Sponsor trace
7. Manager/organization strategy trace
8. Generic assertions
9. Reproducibility export
```

Other domains add adapters as they are implemented.

---

## 38. Anti-patterns

Never:

```text
if debug:
    use extra AI information
```

Never:

```text
Spy explains decision by recalculating it after the fact
```

Never:

```text
Every domain invents its own incompatible log format
```

Never:

```text
Store 100 years of verbose World Spy in the normal save
```

Never:

```text
AI decided this because score=73.42
```

without explaining what meaningful reasons created that evaluation.

---

## 39. Definition of success

World Spy is successful when any important unexpected automated behavior can be investigated without guessing.

For example:

> Why did this AI team spend €8m on this rider?

The answer can reconstruct:

```text
what it knew
what it estimated
what roster need existed
what the agent said
what competitors were believed to be doing
what manager traits mattered
what budget constraints existed
what alternatives existed
why the offer won
```

And if the outcome was terrible, developers can distinguish:

```text
bad information
bad but plausible judgement
bad calibration
rule bug
knowledge leak
implementation bug
```

That distinction is the entire reason the framework exists.
