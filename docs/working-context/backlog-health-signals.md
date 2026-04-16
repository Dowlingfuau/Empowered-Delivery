# Backlog Health — Signal Definitions (Working Context)

## Purpose

This document defines all Backlog Health signals, including:

* what each signal represents
* how it is calculated
* how it behaves across its continuum
* how it contributes to system interpretation

This is the source of truth for understanding signal meaning and validating system behaviour.

---

# 1. Signal Model Overview

All signals follow:

Inputs → Calculation → Raw Value → Normalisation → Continuum Position

Each signal is:

* measurable (quantitative or derived)
* normalised to a 0–1 range
* positioned on a meaningful continuum

---

# 2. Backlog Size

## What it represents

The amount of work in the backlog relative to delivery capacity.

---

## Inputs

* TotalItems
* ThroughputHistory
* AverageThroughput

---

## Calculation

WeeksOfWork = TotalItems / AverageThroughput

---

## Meaning

* Low value → starvation risk (not enough work)
* High value → overload risk (too much work)

---

## Continuum

Starvation ← → Overload

* 0.0 → no backlog
* ~0.5 → balanced
* 1.0 → excessive backlog

---

## Notes

* Highly sensitive to throughput accuracy
* Breaks if throughput = 0 (must be handled explicitly)

---

# 3. Backlog Age

## What it represents

The distribution of work by age.

---

## Inputs

* FreshItems
* MidAgeItems
* OldItems
* TotalAgeItems

---

## Calculation

* FreshPercent = FreshItems / Total
* MidPercent = MidItems / Total
* OldPercent = OldItems / Total

DistributionDistance = deviation from ideal distribution
AgeScore = mapped score from distance

Bias = FreshPercent - OldPercent

Shape = classification of distribution

---

## Meaning

* Balanced → healthy flow
* Old-heavy → stagnation / risk
* Fresh-heavy → churn / instability

---

## Continuum

Under-ripe ← → Over-ripe

* 0.0 → entirely fresh
* 0.5 → balanced
* 1.0 → entirely old

---

## Shape Types

* Balanced
* Fresh-skewed
* Old-skewed
* Bimodal

---

# 4. Backlog Volatility

## What it represents

How much the backlog is changing over time.

---

## Inputs

* ItemsAdded
* ItemsCompleted
* TotalItems

---

## Calculation

Ratio = ItemsAdded / ItemsCompleted

BalanceRatio = symmetric mapping of ratio
RelativeDelta = change relative to backlog size
VolatilityContinuum = mapped value

---

## Meaning

* Low → stable system
* High → unpredictable system

---

## Continuum

Stable ← → Chaotic

* 0.0 → no change
* 0.5 → manageable fluctuation
* 1.0 → high instability

---

## Notes

* Ratio > 1 → growth
* Ratio < 1 → reduction

---

# 5. Backlog Prioritisation

## What it represents

Clarity and distribution of prioritisation.

---

## Inputs

* HighPriority
* MediumPriority
* LowPriority
* TotalItems

---

## Calculation

* HighPercent
* MediumPercent
* LowPercent
* UnprioritisedPercent

Coverage = 1 - UnprioritisedPercent
DistributionScore = balance across priorities

PrioritisationScore = combined score

---

## Meaning

* High clarity → clear decision-making
* Low clarity → confusion / inefficiency

---

## Continuum

No Clarity ← → Strong Clarity

* 0.0 → no prioritisation
* 0.5 → partial clarity
* 1.0 → strong prioritisation

---

# 6. Delivery Predictability

## What it represents

Consistency of delivery output.

---

## Inputs

* ThroughputHistory
* AverageThroughput

---

## Calculation

* Variance
* StandardDeviation
* CoefficientOfVariation (CV)

CV = StandardDeviation / Mean

---

## Meaning

* Low CV → stable delivery
* High CV → unpredictable delivery

---

## Continuum

Unpredictable ← → Predictable

* 0.0 → highly variable
* 0.5 → moderate consistency
* 1.0 → stable delivery

---

## Notes

* Requires sufficient data
* CV = 0 means perfectly consistent delivery

---

# 7. Flow Balance

## What it represents

Balance between incoming and completed work.

---

## Inputs

* ItemsAdded
* ItemsCompleted

---

## Calculation

Ratio = ItemsAdded / ItemsCompleted

BalanceScore = mapped from ratio

---

## Meaning

* > 1 → intake exceeds delivery (pressure)
* <1 → backlog being reduced

---

## Continuum

Draining ← → Overloaded

* 0.0 → backlog shrinking
* 0.5 → balanced
* 1.0 → overload

---

# 8. Work Horizon

## What it represents

Forward visibility of work.

---

## Inputs

* TotalItems
* AverageThroughput

---

## Calculation

WeeksOfWork = TotalItems / AverageThroughput

---

## Meaning

* Short horizon → risk of running out of work
* Long horizon → potential inefficiency

---

## Continuum

Short ← → Long

---

# 9. Items Added / Items Completed

## What they represent

Raw operational inputs.

---

## Calculation

Direct mapping from input.

---

## Purpose

* feed other signals
* enable flow and volatility analysis

---

# 10. Old Work Percentage

## What it represents

Proportion of aged work.

---

## Inputs

* OldItems
* TotalAgeItems

---

## Calculation

OldPercent = OldItems / TotalAgeItems

---

## Meaning

* high → stagnation risk
* low → fresh flow

---

# 11. Signal Interaction Principles

Signals are not independent.

They interact to form system understanding:

---

## Examples

* Backlog Size + Flow Balance → pressure
* Volatility + Predictability → delivery risk
* Age + Prioritisation → decision effectiveness

---

## Important

Insights are derived from:

* combinations of signals
* not individual signals in isolation

---

# 12. Validation Guidance

When testing signals, always check:

1. Inputs are correct
2. Calculations match expectations
3. Continuum position makes sense
4. Behaviour aligns with real-world intuition

---

## Red Flags

* values that don’t change when inputs change
* division by zero cases
* extreme values without explanation
* conflicting signals

---

# 13. Future Extensions

* pattern detection (cyclical, chaotic, stable)
* constraint identification
* cross-diagnostic signal relationships
* confidence scoring

---
