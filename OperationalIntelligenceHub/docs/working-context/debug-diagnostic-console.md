# Debug Diagnostic Console — Working Context (v4)

## Current trace implementation

The shared `SignalTraceService` connects recorded rule evaluations to exact signal traces and their recorded inputs and calculations. It reports missing or ambiguous links instead of guessing. The console displays the evidence path alongside existing prioritisation and rendering information.

* Backlog Health supplies signal and insight traces as before.
* Maturity supplies selected levels, resolved behaviour IDs, signal calculations and insight placement status. Unanswered coordinates are shown as unanswered, not as negative evidence.
* Team Health shows its current eight answers and existing overall calculation, including unanswered scores currently counted as zero. Team Health signal building, rule evaluation and insight generation are not yet implemented.

Traces are held in the current page session, not persisted. Assessment free-text notes are excluded. The saved Maturity assessment evaluates BF01 on a separate semantic path and passes the resulting structured run to a dedicated Maturity pattern detection section. It shows original evidence and statuses, definition provenance, groups, conditions, classifications, pending relationships, provisional eligibility, narrative, errors and presentation status. A qualifying core match creates a provisional diagnostic finding; incomplete assessments do not show results. Reopening recalculates with current rules and narrative rather than replaying historical output. The older numeric Pattern Detection placeholders do not represent this semantic stage.

## Purpose

Provide end-to-end observability across the diagnostic and Insight Engine pipeline.

The console must allow users to trace:

Inputs and evidence → measurements and signals → derived signals → diagnostic insights → relationships and intelligence evaluation → composition and presentation

The goal is to understand:

* what triggered an insight
* which signals contributed
* how signals were formed
* where issues occur in the pipeline

---

## 1. Core Constraint (CRITICAL)

The console must NOT introduce any new logic.

It must:

* only render existing data
* not simulate pipeline behaviour
* not infer missing relationships

If data is missing, show:

The recorded reason or state; do not turn an absent result into a negative result. Where the current trace has no further detail, show "No data available".

### Agreed target: shared Intelligence Trace — not yet implemented

The conceptual **Intelligence Trace** covers the entire intelligence lifecycle, not only signals. One shared, diagnostic-agnostic contract should support Backlog Health, Maturity, Team Health and future diagnostics. Diagnostic and intelligence pipeline stages produce records; the console consumes and renders them, without interpreting diagnostic-specific inputs or inferring relationships. Diagnostic-specific detail may accompany the common records where needed. This is a conceptual contract, not a required new service or a rename of the existing `SignalTraceService`.

The target lifecycle covers:

1. Original inputs, selected evidence, identity and provenance.
2. Diagnostic-specific evidence quality, completeness and measurement reliability.
3. Calculations, transformations, signal construction and optional normalisation.
4. Rule, derived-signal and pattern evaluation.
5. Diagnostic insight generation with its evidence.
6. Cross-diagnostic relationships, corroboration, challenge and context.
7. Confidence, significance and priority evaluation.
8. Insight composition and final output and presentation decisions.

The shared contract should allow each stage to identify its producer and evaluation context, its outcome and reasons, and links to source records and dependencies; diagnostic-specific detail should preserve the facts needed to explain that stage. Record the intelligence scope and operational dimension associations where evaluated, without treating classification as evidence. Where comparisons across evaluations are available, records should link the relevant versions or evaluations so changes can be explained. Exact record shapes, identifiers, versioning, access, retention and integration remain open design decisions.

Distinguish no evidence, available but not evaluated, evaluated with no match, support, challenge, insufficient evidence and evaluation failure. Include related but insufficiently relevant and contextual evidence as well as missing/unavailable evidence. Trace shared provenance and dependencies so derived outputs are not mistaken for independent sources.

The console should expose recorded measurement reliability, completeness and quality, supporting and challenging evidence, relationship relevance, independence, confidence, significance and priority and their contributing factors. It should explain the relationship between confidence and priority, why an insight was generated, suppressed or changed, and how additional assessments affected each outcome. Show internal values and states with their actual derivation when available; these are not automatically end-user scores or labels. Changes across evaluations require recorded history; its retention and comparison policy is unresolved.

The present service does not yet record this full lifecycle. Any required evaluation data must be produced by the pipeline and passed to the console; the console must not invent it. Session-only traces and exclusion of free-text notes describe the current implementation, not a decision on future persistence. Maturity does not currently collect user-entered behavioural notes: its `BehaviouralNote` field is populated from catalogue content and is not independent user evidence.

---

## 2. Structure — Progressive Trace

The console must support multi-level expansion.

---

### Level 1 — System Layers

Top-level sections:

* Inputs
* Signals
* Derived Signals
* Insights

---

### Level 2 — Entity Expansion

Each item within a layer must be expandable.

Examples:

Signals:

* Backlog Size
* Predictability
* Leader Trust

Derived Signals:

* Backlog Pressure
* Decision Centralisation

Insights:

* Delivery instability
* Decision bottleneck

---

### Level 3 — Trace Detail

Each expanded item must show:

1. Inputs
2. Calculation / Formation
3. Rule Evaluation
4. Pattern Detection

Only if data exists.

---

## 3. Data Binding (CRITICAL)

The UI must use existing data sources:

* Signals
* DerivedSignals
* Insights
* RuleEvaluations
* SignalTraces

No fallback logic.

No filtering logic in UI.

No inference.

---

## 4. Linking Between Layers

The console must show relationships where available:

* Derived Signals → contributing Signals
* Insights → contributing Derived Signals or Signals

These relationships must come from data.

Do NOT derive them in UI.

---

## 5. Maturity-Specific Support

Signals must support:

* role-based inputs
* disparity between roles

Trace must show:

* individual role values
* comparison logic (if provided)
* resulting signal

---

## 6. Backlog-Specific Support

Signals must support:

* data-based inputs
* statistical calculations

Trace must show:

* raw inputs
* interpretation steps
* normalisation outcome

---

## 7. UI Behaviour

* each level must be collapsible
* expansion must be independent
* multiple items can be expanded

The UI must prioritise:

* clarity
* navigation
* traceability

---

## 8. Implementation Constraints

The instructions below describe the earlier console refactor, not a prohibition on new trace models or evaluation data required by the agreed target.

Refactor DiagnosticsConsole.razor

Do:

* remove duplication
* remove fallback rule logic
* organise into layered structure

Do NOT:

* add new data models
* create new calculation logic
* hardcode diagnostic-specific behaviour

---

## 9. Immediate Next Step

Historical refactor checklist; the target lifecycle and outstanding decisions above supersede it as architectural direction.

1. Introduce Level 1 structure (Inputs, Signals, Derived, Insights)
2. Move existing content into correct layers
3. Add Level 2 expansion per entity
4. Retain existing trace rendering inside each entity
