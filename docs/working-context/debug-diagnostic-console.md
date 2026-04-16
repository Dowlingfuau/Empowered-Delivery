# Debug Diagnostic Console — Working Context (v4)

## Purpose

Provide end-to-end observability across the diagnostic and Insight Engine pipeline.

The console must allow users to trace:

Inputs → Signals → Derived Signals → Insights

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

"No data available"

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

1. Introduce Level 1 structure (Inputs, Signals, Derived, Insights)
2. Move existing content into correct layers
3. Add Level 2 expansion per entity
4. Retain existing trace rendering inside each entity
