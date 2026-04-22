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
