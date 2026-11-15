# Operational Dimension Model Specification

The filename is retained for existing links; “Lens” in the filename is legacy terminology. In the Maturity Assessment, a lens is a role perspective such as Scrum Master, Product Owner, Squad or Leader. It is not an Operational MRI dimension.

## Purpose

The Operational Dimension Model provides a consistent, cross-platform taxonomy for organising signals, insights, diagnostics, and improvement actions.

Operational dimensions are not calculated scores.

They group related system behaviours to help users:
- understand what type of problem they are facing
- connect insights across diagnostics
- navigate to relevant actions, plays, and perspectives
- form their own hypotheses about system behaviour

---

## Core Principle

Operational dimensions do not introduce new interpretation logic into the system.

They:
- DO group signals, derived signals, and insights
- DO NOT calculate scores
- DO NOT override the Insight Engine
- DO NOT act as a second interpretation layer

**Target boundary:** the five dimensions are the top-level operational dimensions of the shared Operational MRI taxonomy. They group and navigate insight associations rather than calculate or evaluate intelligence. Their flexible hierarchies may contain categories and elements at different depths; an insight can relate to multiple dimensions, optionally with a primary one. Diagnostic, team-system and multi-team-system intelligence scopes are independent of dimension classification. Operational MRI evaluates relationships across dimensions; they are not separate engines. These are target capabilities, not claims about the existing implementation; see [insight-engine-purpose.md](./insight-engine-purpose.md).

- Signals = standardised observable measures or characteristics
- Derived Signals = calculated or rule-derived observations, with provenance
- Insights = evidence-grounded interpretation or hypothesis
- Operational dimensions = how to classify and organise intelligence

---

## Operational Dimension Definitions

### Clarity

**Represents:**
Understanding of what to do and why it matters.

**Failure Mode:**
Teams are active but lack clear direction or shared understanding of value.

**Contributing Signals:**
- BacklogPrioritisation (Flow)
- ValueFocus (Maturity)
- Purpose (Team Health)

**Typical Derived Signals:**
- ValueClarityRisk
- PriorityNoise

---

### Performance

**Represents:**
The system’s ability to deliver work effectively and sustainably.

**Failure Mode:**
Work is not flowing effectively, or delivery outcomes are inconsistent.

**Contributing Signals:**
- BacklogSize
- BacklogVolatility
- DeliveryPredictability

**Typical Derived Signals:**
- BacklogPressure
- FlowInstability
- DeliveryInstability

---

### Leadership

**Represents:**
How decisions are made and how authority and accountability are structured.

**Failure Mode:**
Decision-making is slow, centralised, or misaligned with team capability.

**Contributing Signals:**
- LeadershipTrust (Maturity)
- ScrumMasterEffectiveness (Maturity)
- TeamOwnership (Maturity)

**Typical Derived Signals:**
- EmpowermentConstraint
- DecisionLatencyRisk

---

### Culture

**Represents:**
How people interact, learn, and operate together.

**Failure Mode:**
Low trust, poor collaboration, or lack of psychological safety limits system performance.

**Contributing Signals:**
- Trust (Maturity)
- Learning (Maturity)
- Interpersonal Dynamics (Team Health)
- Safety & Trust (Team Health)

**Typical Derived Signals:**
- CollaborationBreakdownRisk
- LearningConstraint

---

### Foundation

**Represents:**
The structural and capability baseline required for effective delivery.

**Failure Mode:**
Teams lack the foundational practices or discipline required to sustain delivery.

**Contributing Signals:**
- DeliveryPractices (Maturity)
- ContinuousImprovement (Maturity)
- BacklogAge

**Typical Derived Signals:**
- DeliveryDisciplineRisk
- StagnationRisk

---

## Operational Dimension Mapping Rules

Operational dimensions are defined through associations, not calculations.

Each operational dimension may be associated with:

- Signals (atomic inputs)
- Derived Signals (system meaning)
- Insight tags (used for grouping in UI)

Example structure:


---

## System Usage

Operational dimensions can be used across the platform to:

### 1. Group Insights
- Coach Hub can organise insights by operational dimension
- Users can navigate by problem type instead of signal type

### 2. Tag Plays and Actions
- Plays can be associated with relevant operational dimensions
- Users can take action based on the type of problem

### 3. Tag Perspectives / Content
- Articles and guidance can be organised by operational dimension
- Enables deeper learning aligned to system issues

### 4. Group Diagnostic Information
- Signals and insights from multiple tools may be presented under the same operational dimension for navigation and comparison.
- This grouping does not evaluate evidence relationships, corroborate insights or generate system intelligence. Those responsibilities belong to Operational MRI.

---

## UX Intent

The Operational Dimension Model supports:

- A consistent mental model across the platform
- Aggregated views of system health without collapsing into scores
- Visibility of where issues originate:
  - Behaviour (Team Health)
  - Structure (Backlog / Flow)
  - Capability (Maturity)

Users should be able to:

- Identify a problem category (e.g. Culture)
- See all related signals and insights
- Navigate to relevant actions
- Form their own system-level hypotheses

---

## Non-Goals

The Operational Dimension Model must not:

- Produce a combined score per operational dimension
- Replace or duplicate derived signal logic
- Hide underlying signals or system behaviour
- Introduce hidden weighting or aggregation

---

## Future Considerations

The Operational Dimension Model may later support:

- Dynamic filtering of insights
- Personalised recommendations
- Visual aggregation layers (e.g. system radar views)

Any future enhancements must maintain:
- transparency
- traceability back to signals
- separation from core signal logic