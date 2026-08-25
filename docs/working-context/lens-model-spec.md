# Lens Model Specification

## Purpose

The Lens Model provides a consistent, cross-platform way to organise and interpret signals, insights, diagnostics, and improvement actions.

Lenses are not calculated scores.

They are **perspectives** that group related system behaviours to help users:
- understand what type of problem they are facing
- connect insights across diagnostics
- navigate to relevant actions, plays, and perspectives
- form their own hypotheses about system behaviour

---

## Core Principle

Lenses do not introduce new logic into the system.

They:
- DO group signals, derived signals, and insights
- DO NOT calculate scores
- DO NOT override the Insight Engine
- DO NOT act as a second interpretation layer

Signals = what is happening  
Derived Signals = what it means  
Insights = explanation  
Lenses = how to view and organise the system

---

## Lens Definitions

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

## Lens Mapping Rules

Lenses are defined through associations, not calculations.

Each lens maps to:

- Signals (atomic inputs)
- Derived Signals (system meaning)
- Insight tags (used for grouping in UI)

Example structure:


---

## System Usage

Lenses are used across the platform to:

### 1. Group Insights
- Coach Hub organises insights by lens
- Users can navigate by problem type instead of signal type

### 2. Tag Plays and Actions
- Plays are tagged with relevant lenses
- Users can take action based on the type of problem

### 3. Tag Perspectives / Content
- Articles and guidance are organised by lens
- Enables deeper learning aligned to system issues

### 4. Aggregate Diagnostics
- Signals from multiple tools contribute to the same lens
- Enables cross-diagnostic understanding

---

## UX Intent

The Lens Model enables:

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

The Lens Model must not:

- Produce a combined score per lens
- Replace or duplicate derived signal logic
- Hide underlying signals or system behaviour
- Introduce hidden weighting or aggregation

---

## Future Considerations

The Lens Model may later support:

- Dynamic filtering of insights
- Personalised recommendations
- Visual aggregation layers (e.g. system radar views)

Any future enhancements must maintain:
- transparency
- traceability back to signals
- separation from core signal logic