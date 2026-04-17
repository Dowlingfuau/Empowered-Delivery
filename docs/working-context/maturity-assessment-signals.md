# Maturity Assessment — Signal Definitions (Working Context)

## Purpose

This document defines how Maturity Assessment signals are constructed, interpreted, and used.

Unlike Backlog Health, Maturity signals are:

* qualitative
* behaviour-driven
* role-dependent
* comparative (disparity-based)

This document provides the source of truth for:

* signal meaning
* scoring logic
* interpretation guidance

---

# 1. Core Model

Maturity signals follow:

Responses → Behaviour Mapping → Role Scores → Aggregation → Disparity → Normalised Signal

---

## Key Difference from Backlog Health

Backlog Health:

* data-driven (facts, metrics)

Maturity Assessment:

* behaviour-driven (perception, practice, alignment)

---

# 2. Input Structure

## Inputs

* Responses (survey or assessment answers)
* Role (e.g. Leader, Product Owner, Team)
* Behaviour Indicators (mapped from responses)

---

## Example

```text
Leader → “Decisions are centralised”
Team → “We self-organise”
```

---

👉 These inputs may conflict

This is intentional.

---

# 3. Behaviour Mapping

Each response maps to:

* one or more behaviours
* within a specific domain (e.g. Trust, Ownership, Flow)

---

## Example

```text
Response: “Work is assigned by leadership”
→ Behaviour: Low Ownership
→ Domain: Team Autonomy
```

---

## Purpose

To translate subjective responses into structured indicators.

---

# 4. Role-Based Scoring

Each role produces:

* a score per domain
* based on behaviour indicators

---

## Example

```text
Leader Trust Score: 0.8
Team Trust Score: 0.4
```

---

## Meaning

* high score → stronger maturity in that domain
* low score → weaker maturity

---

# 5. Aggregation

Scores are aggregated to form:

* overall signal score
* domain-level maturity

---

## Methods

* average scoring
* weighted scoring (if roles differ in importance)

---

# 6. Disparity (CRITICAL CONCEPT)

## What it represents

Difference between roles’ perception of the same domain.

---

## Calculation

Disparity = difference between role scores

Example:

```text
Leader: 0.8
Team: 0.4

Disparity = 0.4
```

---

## Meaning

* low disparity → aligned understanding
* high disparity → misalignment / dysfunction

---

## Continuum

Aligned ← → Misaligned

* 0.0 → perfect agreement
* 1.0 → complete disagreement

---

## Notes

Disparity is often more important than absolute score.

---

# 7. Signal Formation

Each maturity signal includes:

* aggregated score
* disparity
* contributing behaviours

---

## Example Signal

### Team Autonomy

Inputs:

* behavioural indicators
* role responses

Outputs:

* AutonomyScore
* DisparityScore

---

# 8. Continuum Model

Each maturity signal exists on a continuum.

---

## Example Continua

### Trust

Low Trust ← → High Trust

---

### Ownership

Command-Control ← → Self-Organising

---

### Decision Making

Centralised ← → Distributed

---

### Flow

Fragmented ← → Optimised

---

## Important

Signals are not “good or bad” in isolation.

They describe:

* system state
* tendencies
* operating model

---

# 9. Interpretation Principles

---

## 1. Absolute Score

Indicates strength of maturity.

---

## 2. Disparity

Indicates alignment between roles.

---

## 3. Combined Meaning

Example:

* High score + High disparity → unstable maturity
* Low score + Low disparity → consistently immature
* High score + Low disparity → healthy maturity

---

# 10. Signal Interaction

Signals combine to form deeper insights.

---

## Examples

* Trust + Autonomy → empowerment
* Ownership + Flow → delivery effectiveness
* Decision Making + Disparity → governance issues

---

# 11. Trace Requirements

Each maturity signal must be traceable.

---

## Inputs

* responses per role
* behaviour indicators

---

## Calculation

* role scores
* aggregation logic
* disparity calculation

---

## Output

* final score
* disparity
* interpretation context

---

# 12. Validation Guidance

When testing maturity signals:

---

## Check:

1. Responses map correctly to behaviours
2. Role scores are accurate
3. Disparity is calculated correctly
4. Signal meaning aligns with real-world expectations

---

## Red Flags

* identical scores across roles when responses differ
* no disparity when disagreement exists
* overly sensitive scoring
* signals not reflecting behaviours

---

# 13. Future Enhancements

* confidence scoring (response consistency)
* pattern detection across roles
* behavioural clustering
* cross-domain maturity insights
* integration with operational signals (Backlog Health)

---
