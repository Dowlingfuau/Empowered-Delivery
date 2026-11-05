# Backlog Health — Flow Rules and Insight Triggers

## Purpose

Records the rules in `wwwroot/data/rules/flow_rules_v3.json` that decide when Backlog Health insights trigger, the numeric signals those rules read, and how the insights are placed in the UI.

This covers the rule and insight-engine layer only. The Backlog Health Monitor's calculations, gauges and readings are unchanged (see [backlog-health-signals.md](./backlog-health-signals.md)).

## Rules

Rules are evaluated per condition in order and the first match wins, so conditions run High, then Moderate, then Low. A rule that matches no condition produces no insight.

| Rule | Reads | Triggers |
|---|---|---|
| BacklogPressure | `WorkHorizon` (weeks) | None below 6. Low 6 to under 8. Moderate 8 to 12. High over 12. |
| BacklogStarvation | `WorkHorizon` | Unchanged. High below 1, Moderate 1 to under 2, Low 2 or more. |
| IntakePressure | `BacklogGrowthWeeks` | None at 0 or below. Low above 0 to under 4. Moderate 4 to under 12. High 12 or more. |
| DeliveryInstability | `DeliveryPredictability`, `DeliveryPredictabilityDataAvailable` | None at 20% variation or less (predictability 0.8 or more). Moderate above 20% to 40%. High above 40%. Requires predictability data. |
| BacklogAgingRisk | `OldWorkPercentage` | Moderate at 25% or more old work. High above 50%. Old means over 4 weeks. |
| BacklogUnderRipe | `FreshWorkPercentage` | High when more than 50% of work is under 2 weeks old. Separate from ageing, so fresh and old work no longer share one insight. |
| IdleWorkRisk | `OldWorkPercentage`, `WorkHorizon`, `DeliveryPredictability` | High when over 50% is old, `WorkHorizon` is 6 or less, and variation is 20% or less. |
| UnprioritisedWorkRisk | `UnprioritisedWorkPercentage` | High when more than 50% of items have no priority. |
| PriorityDifferentiationRisk | `PriorityBandConcentration` | High when any single priority band holds more than 50% of items. |
| BacklogVolatility | `BacklogGrowthPercentage`, `BacklogShrinkagePercentage` | High when growth or shrinkage exceeds 30%. One rule, with direction-specific text. |
| FlowStabilityEmerging | Predictability, volatility and age signals | Conditions unchanged. Wording now describes an already stable state. |
| BacklogBalance, StrongPrioritisation | Unchanged | Unchanged. |

### Removed

`FlowDisruption` is removed. Its categorical `High/Moderate` conditions cannot be evaluated by the numeric-only `DerivedSignalEngine`, and it duplicated `FlowOverloadSystem`. `FlowOverloadSystem` in `system_rules.json` (`BacklogPressure = High AND IntakePressure = High`) is the single overload insight. No `FlowDisruption` template exists or should be added.

## Signals added for the rules

Built in `BacklogHealthSignalBuilder` as calculated signals. They feed rules only and do not change any gauge or the overall health score.

| Signal | Meaning |
|---|---|
| `FreshWorkPercentage` | Share of backlog under 2 weeks old. |
| `BacklogGrowthWeeks` | Net backlog growth divided by average throughput, floored at 0. |
| `BacklogGrowthPercentage` | Net growth as a share of the starting backlog, 0 when the backlog shrank. |
| `BacklogShrinkagePercentage` | Net shrinkage as a share of the starting backlog, 0 when the backlog grew. |
| `UnprioritisedWorkPercentage` | Items with no priority divided by total backlog. 0 when priority data is unavailable. |
| `PriorityBandConcentration` | Largest of the High, Medium and Low shares of total backlog. 0 when priority data is unavailable. |
| `DeliveryPredictabilityDataAvailable` | 1 when predictability has enough observations, otherwise 0. Gates the predictability rules. |

`ClassifyDeliveryRhythm` now treats a coefficient of variation of 0.2 or less as Stable (previously below 0.2), to match the 20% insight boundary.

## Engine changes

- `RuleCondition.Interpretation` allows text per condition. `DerivedSignalEngine` copies the matched condition's text onto `DerivedSignal.Interpretation`.
- `OperationalInsightEngine` uses the signal's interpretation when present and falls back to the rule-level text. This lets BacklogVolatility explain growth and shrinkage differently.
- Insights render the rule's fixed interpretation. They do not include live numbers.

## Placement

`diagnostic_anchors.json` maps contributing signals to UI anchors. The new signals are aliases of existing anchors, so no UI reading was added or split:

- Prioritisation Clarity (canonical `BacklogPrioritisation`): `UnprioritisedWorkPercentage`, `PriorityBandConcentration`. The two priority insights trigger separately and show different text, but both anchor here.
- Age: `FreshWorkPercentage`. Volatility and balance: the growth and shrinkage signals. Predictability: `DeliveryPredictabilityDataAvailable`.

An insight whose signals map to no anchor is placed at the overall level.

## Data file caching

The app fetches rule and insight JSON over HTTP. `DataFileNoCacheHandler` (attached to the shared `HttpClient` in `Program.cs`) makes the browser revalidate every request under `data/`. Edits to these files appear on a normal refresh, with no version suffix needed.

## Tests

- `FlowRuleConfigurationTests` loads the production rule JSON and checks the threshold bands, `FlowDisruption` removal, a single `FlowOverloadSystem`, volatility direction, and the separate priority insights.
- `BacklogHealthSignalBuilderTests` covers the new signals and the Stable rhythm boundary.
