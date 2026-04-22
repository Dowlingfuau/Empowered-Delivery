# Maturity Assessment — Signal Model (Working Context)

## Purpose

Defines how maturity signals are constructed from behavioural data and how they drive system insights.

This document is aligned to:

* behaviour catalogue
* behaviour → signal mapping
* derived signal rules
* insight rules

---

# 1. System Overview

Maturity Assessment pipeline:

Responses
→ Behaviour Identification
→ Behaviour Level (1–5)
→ Signal Mapping
→ Role-Based Signal Scores
→ Aggregation (Averages)
→ Disparity (Gaps)
→ Derived Signals
→ Insights

---

# 2. Behaviour Model (FOUNDATION)

Maturity is defined through behaviours.

Each behaviour:

* belongs to a **role (lens)**
* belongs to a **theme (Ownership, Trust, Value, Learning, System)**
* has a **level (1–5)**
* includes:

  * signals (indicators)
  * examples (real-world patterns)

---

## Example

From behaviour catalogue: 

Level 1 Ownership (Squad):

* “Work ownership sits with individuals”
* “Collaboration is limited”

Level 5 Ownership:

* “Team leads value creation”
* “Proactively improves product outcomes”

---

## Key Insight

Levels are not arbitrary:

👉 They represent **evolution of capability**

---

# 3. Behaviour → Signal Mapping

Each behaviour maps to a signal.

From mapping: 

Example:

```text
Squad_Ownership_L3 → TeamOwnershipLevel
Leader_Trust_L2 → LeaderTrustLevel
```

---

## Result

Signals are:

* theme-based
* role-specific

---

## Signal Structure

SignalName = <Role><Theme>Level

Examples:

* TeamOwnershipLevel
* LeaderTrustLevel
* POValueLevel

---

# 4. Role-Based Signal Scoring

Each role produces:

* one score per theme

---

## Example

```text
TeamOwnershipLevel = 0.6
LeaderOwnershipLevel = 0.3
```

---

## Meaning

* higher score → higher maturity level
* lower score → earlier stage behaviour

---

# 5. Aggregation

Signals are aggregated into:

* theme averages
* system-level indicators

---

## Example

```text
OwnershipAverage =
(TeamOwnership + POOwnership + LeaderOwnership) / N
```

---

# 6. Disparity (CRITICAL)

Disparity measures difference between roles.

---

## Example

```text
LeaderTrustLevel = 0.8
TeamTrustLevel = 0.4

TrustGap = 0.4
```

---

## Insight Rules Use This

From insights: 

```text
trust_gap >= 2 → alignment issue
```

---

## Meaning

* low disparity → aligned system
* high disparity → hidden dysfunction

---

## Key Principle

👉 Disparity often matters more than absolute score

---

# 7. Derived Signals

Derived signals combine multiple signals.

From rules: 

---

## Example

### AutonomyFriction

```text
TeamOwnershipLevel >= 0.7
AND LeaderTrustLevel <= 0.4
→ High
```

---

## Meaning

* teams are capable
* leadership is restrictive

---

## Structure

Each derived signal includes:

* source signals
* conditions
* intensity (Low / Moderate / High)
* interpretation

---

## Categories

* Positive
* Opportunity
* Constraint
* Risk

---

# 8. Insight Layer

Insights are triggered from:

* averages
* gaps
* derived signals

---

## Example

From insight rules: 

### Trust Alignment Gap

* metric: trust_gap
* threshold: 2

---

## Output

* title
* description
* recommendation

---

# 9. Continuum Model

Each signal exists on a maturity continuum:

---

## Ownership

Individual → Team → System Ownership

---

## Trust

Control → Collaboration → Autonomy

---

## Value

Output → Outcome → Value

---

## Learning

Compliance → Reflection → Evolution

---

## System

Local → Cross-team → Organisational

---

---

# 10. Interpretation Principles

---

## 1. Absolute Level

Indicates maturity stage

---

## 2. Disparity

Indicates alignment between roles

---

## 3. Derived Signals

Reveal system dynamics

---

## Combined Interpretation

| Condition                   | Meaning               |
| --------------------------- | --------------------- |
| High score + low disparity  | healthy maturity      |
| High score + high disparity | unstable / fragile    |
| Low score + low disparity   | consistent immaturity |
| Mixed signals               | transitional system   |

---

# 11. Trace Requirements (IMPORTANT)

Each maturity signal must expose:

---

## Inputs

* behaviours selected
* role responses

---

## Calculation

* level mapping
* scoring
* aggregation
* disparity

---

## Derived

* rule evaluation
* condition results

---

## Output

* final signal
* gap values
* derived signals
* triggered insights

---

# 12. Validation Guidance

---

## Check:

* behaviours match responses
* signal levels are correct
* gaps reflect real disagreement
* derived signals trigger correctly

---

## Red Flags

* identical scores across roles with different inputs
* no gaps when disagreement exists
* incorrect rule triggering
* signals not matching behaviour descriptions

---

# 13. Future Enhancements

* confidence scoring
* behavioural clustering
* trend analysis
* cross-diagnostic insights (with Backlog Health)

---
