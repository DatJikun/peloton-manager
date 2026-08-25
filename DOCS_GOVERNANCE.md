# Peloton Manager — Documentation Governance

**Status:** REVIEW

## 1. Cel
Projekt będzie rozwijany iteracyjnie z pomocą wielu sesji i modeli AI. Dokumentacja jest częścią architektury, bo musi zapobiegać sprzecznym wymaganiom, przypadkowym refactorom i implementowaniu starych pomysłów.

## 2. Hierarchy of truth
```text
VISION.md
↓
ACCEPTED ADR / DDR
↓
ARCHITECTURE.md
↓
ACCEPTED system design docs
↓
DATA_MODEL / GAME_STATES / RULESETS / CONTENT_FORMAT / SAVE_FORMAT
↓
HANDOFF.md
↓
ROADMAP / task specs
↓
implementation
```

## 3. Statusy
`DRAFT`, `REVIEW`, `ACCEPTED`, `IMPLEMENTED`, `DEPRECATED`, `ARCHIVED`.

## 4. Standardowy nagłówek
Każdy większy dokument posiada Title, Version, Status, Purpose, Authority/Owner, Supersedes, Superseded by, Last reviewed i Related ADRs.

## 5. Standardowe sekcje
Gdy pasują: `LOCKED DECISIONS`, `OPEN QUESTIONS`, `DEFERRED`, `NON-GOALS`, `IMPLEMENTATION NOTES`, `KNOWN DIFFERENCES FROM CODE`, `MIGRATION IMPACT`, `TEST / PLAYTEST CRITERIA`.

## 6. VISION.md
Krótki north star. Zawiera fantasy, priorytety i anti-goals. Nie przechowuje schematu bazy ani backlogu.

## 7. ARCHITECTURE.md
Zawiera boundaries, invariants, dependency rules, persistence, modularity, determinism, content composition i engineering constraints. Nie jest backlogiem.

## 8. HANDOFF.md
Jeden aktywny plik. Zawiera aktualny stan operacyjny, next task, test status, feedback właściciela i dokumenty do przeczytania. Nie zmienia po cichu designu.

## 9. DOCS.md
Indeks aktywnych dokumentów. AI nie powinno zgadywać, który markdown jest aktualny.

## 10. ADR i DDR
ADR dla trudnych decyzji technicznych, DDR dla najważniejszych decyzji gameplayowych. Format: Context, Decision, Consequences, Alternatives, Status.

## 11. Open questions
Nieznana odpowiedź ma być jawna. Open question może mieć decision deadline, np. „must decide before RaceDecisionRequest implementation”.

## 12. Zmiana zaakceptowanej decyzji
Wskaż starą decyzję, powód, skutki, wpływ na save/content compatibility, testy/golden simulations i aktualizuj dokumenty. Bez silent design drift.

## 13. Code vs docs mismatch
Jawna sekcja `KNOWN DIFFERENCE FROM CODE` opisuje design, current implementation, why i plan.

## 14. Playtest feedback
Po playteście zapisujemy co było fajne, nudne, oczywiste, nieczytelne i gdzie UI przeszkadzało. Nudnego systemu nie bronimy realizmem.

## 15. Dokumenty dla AI
Definiują terminy, stabilne nazwy modułów, rozdzielają locked/proposed, pokazują input/output, edge cases i acceptance criteria.

## 16. Archiwizacja
Stare dokumenty trafiają do `docs/archive/` i dostają `STATUS: DEPRECATED`, `SUPERSEDED BY` oraz `DO NOT IMPLEMENT FROM THIS DOCUMENT`.

## 17. Suggested docs tree
```text
docs/
├── VISION.md
├── DOCS.md
├── HANDOFF.md
├── architecture/
│   ├── ARCHITECTURE.md
│   └── adr/
├── design/
│   ├── UI_SITEMAP.md
│   ├── GAME_STATES.md
│   ├── RACE_ENGINE.md
│   ├── RECRUITMENT.md
│   ├── DEVELOPMENT.md
│   └── ECONOMY.md
├── data/
│   ├── DATA_MODEL.md
│   ├── CONTENT_FORMAT.md
│   ├── RULESETS.md
│   └── SAVE_FORMAT.md
├── development/
│   ├── TESTING.md
│   ├── AI_DEVELOPMENT_RULES.md
│   └── ROADMAP.md
└── archive/
```

## 18. Pre-code documentation gate
Przed większym gameplay codingiem co najmniej w `REVIEW`: VISION, ARCHITECTURE, DOCS, HANDOFF, UI_SITEMAP, GAME_STATES, DATA_MODEL, CONTENT_FORMAT, RULESETS, SAVE_FORMAT, TESTING i AI_DEVELOPMENT_RULES.

> **Dokument ma zmniejszać niepewność kolejnej osoby lub AI. Jeżeli ją zwiększa, ma zły status albo jest źle napisany.**


## Stable Decision IDs

Owner locks that affect multiple documents receive stable IDs in `DECISIONS.md`. Section numbers are navigation only and must remain unique inside each document.


## Code documentation boundary

Design/architecture docs explain durable contracts and reasons. They do not mirror implementation line-by-line. Private implementation detail belongs in readable code/tests unless it establishes a non-obvious invariant, workaround, numeric assumption or extension contract.
