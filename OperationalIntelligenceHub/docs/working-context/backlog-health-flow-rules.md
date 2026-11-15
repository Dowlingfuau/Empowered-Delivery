# Backlog Health — Flow Rules and Insight Triggers

## Purpose

Records the rules in `wwwroot/data/rules/flow_rules_v3.json` that decide when Backlog Health insights trigger, the numeric signals those rules read, and how the insights are placed in the UI.

This covers the rule and insight-engine layer only. The Backlog Health Monitor's calculations, gauges and readings are unchanged (see [backlog-health-signals.md](./backlog-health-signals.md)).

**Current versus target:** the rules and fixed interpretations below describe the current numeric runtime, not the redesigned intelligence model. Today the insight engine uses configured priority and confidence weight in its ranking. The agreed target evaluates confidence and priority dynamically and separately, preserves raw evidence and provenance, and can use cross-diagnostic evidence as support, challenge or context without assuming confirmation. No replacement formula or Backlog rule change is approved here. See [insight-engine-purpose.md](./insight-engine-purpose.md).

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

## Workbook review findings (not deployed rule changes)

The latest Rule Register Review workbook contains a separate **Flow Rule Register** with test observations and recommended resolutions. Several rows use older signal names or conditions that differ from the current runtime described above. This document continues to describe the deployed JSON and engine behavior; workbook review comments are not implementation instructions and do not replace those facts.

| Flow rule | Workbook review finding | Current documentation/runtime boundary |
|---|---|---|
| BacklogPressure | The register uses `WorkHorizon < 2`, `2–5`, and `>5`, and reports that the insight appears too early. Its recommendation is to align pressure bands with roughly 6, 8, and 12 weeks of work. | The current rule documented above uses the later 6/8/12-week bands. Confirm exact inclusive/exclusive boundaries before changing them. |
| BacklogStarvation | The workbook reports that the rule triggers as intended. | Its `WorkHorizon` bands are consistent with the current documentation. |
| IntakePressure | The workbook uses `FlowBalance` and reports the need for finer intensity boundaries, including a much longer horizon for high pressure. | The current runtime uses `BacklogGrowthWeeks`. Do not replace this mapping with `FlowBalance` without confirming the signal definition and desired bands. |
| DeliveryInstability | The workbook uses predictability bands at `0.7` and `0.4`. It also reports a low insight when throughput variation is zero and asks that stable delivery not trigger. | The current runtime gates on available predictability data and does not trigger at 20% variation or less. Reconcile the workbook's normalization/bands and the zero-variation example with the current signal calculation before any change. |
| BacklogAgingRisk | The workbook's reviewed condition uses `BacklogAge < 0.3` and reports that both a very fresh backlog and a very old backlog trigger an aging insight. It identifies fresh work as under-ripe, not aged. | The current implementation separates `OldWorkPercentage` from `FreshWorkPercentage` and has a distinct `BacklogUnderRipe` rule. Preserve that distinction. |
| IdleWorkRisk | The workbook says a fresh backlog should not be classified as idle and expects old work to be considered alongside sustainable capacity. | The current documented rule uses old-work share, work horizon, and predictability. The review identifies an important case to keep in its regression suite. |
| PrioritisationClarity | The workbook says one insight combines unprioritised work with concentration in a single priority band and recommends two separate insights. | The current implementation already has separate `UnprioritisedWorkRisk` and `PriorityDifferentiationRisk` rules/signals. |
| BacklogVolatility | The workbook requests distinct growing-versus-shrinking interpretations and flags impossible test data (for example, starting at zero, adding 20, completing 50, and ending at -30). | The current implementation has direction-specific signals and interpretation. Input consistency and realistic boundary tests remain a separate validation requirement; a negative backlog must not be presented as a meaningful scenario. |
| FlowDisruption | The workbook reports that its rule did not trigger and asks for better diagnostics. | `FlowDisruption` is intentionally removed from the current runtime because its categorical conditions are unsupported by the numeric engine and it duplicates `FlowOverloadSystem`. The workbook's rule should not be reintroduced without a new design decision. |
| FlowStabilityEmerging | The workbook says the tested state is already stable and the word “emerging” is misleading. | The current documentation records the updated stable-state wording. Keep this review case as a copy regression test. |
| BacklogBalance / StrongPrioritisation | The workbook reports these rules trigger as intended. | No change is proposed by this review entry; retain existing tests. |

### Flow decisions still requiring approval

- Confirm whether the workbook is a historical review snapshot or proposes changes to the current Flow rules. Its signal names and some thresholds do not match the deployed configuration.
- Approve any changes to `WorkHorizon`, `FlowBalance`, predictability, or backlog-age mappings and their exact boundary semantics before editing the JSON.
- Keep separate test cases for fresh versus old work, growing versus shrinking backlog, and unprioritised versus undifferentiated priority.
- Define valid backlog arithmetic and reject or explicitly explain inconsistent input scenarios such as a negative ending backlog.
- If `FlowDisruption` diagnostics are still needed, instrument the existing evaluation path rather than restoring a duplicate rule by default.
