# Diagnostics Console upgrade report

Additive change to the existing `DiagnosticsConsole.razor`. The console only renders trace data emitted by the builders and engines. It performs no calculation of its own.

## Decisions (agreed)
| Topic | Decision |
|---|---|
| Insight sort order | Engine's actual Score order (intensity score × priority × confidence weight), not raw priority |
| Trace capture | Builders and engines emit structured trace data via additive methods |
| Scope | Backlog Health and Maturity builders (Team Health has no builder and no console) |
| Formula rendering | HTML/CSS stacked fractions, no dependency |

## What changed
**Models** (`Models\Debug`): `CalculationStep` gains `Stage` and `Formula`. New `FormulaTrace`, `TraceNumber`, `ConditionClause`. `RuleEvaluation` gains `Layer` and `Clauses`. New `InsightEvaluationTrace` and `InsightTraceEntry`.

**Engines:** `DerivedSignalEngine` and `SystemRuleEngine` record each clause (subject, operator, threshold, actual, matched). Decision logic is unchanged. `OperationalInsightEngine.GenerateInsightsWithTrace` returns the insights plus the trace. `GenerateInsights` delegates to it, and the scoring now uses the shared `IntensityScore` helper so the trace cannot drift from the real score.

**Pipeline:** `BuildSignalsAndDerivedDetailed` and `ProcessSignalsDetailed` are new. The old methods delegate to them with unchanged return shapes.

**Builders:** `BacklogHealthSignalBuilder` traces every signal with staged steps and substituted formulas. `MaturitySignalBuilder.BuildSignalsWithTrace` and `SignalNormaliser.NormaliseTraced` cover Maturity.

**Pages:** `BacklogHealthMonitor` uses the detailed pipeline and reports the rendering stage via `InsightEvaluation.ApplyRendering`. It now also passes derived signals and rule evaluations, so the Derived Signals section populates. `MaturityAssessment` passes traces and the insight trace. It does not report a rendering stage, and the console says so.

**Console:** Signals show steps grouped by stage (Intermediate calculations, Calculated outputs, Normalisation, Derived signals), with stacked-fraction formulas. Rule Evaluation shows condition, actual, threshold and matched per clause. A new "Insight Evaluation" section shows the count summary and an ordered list. Each entry has separate Triggered, Prioritised and Rendered stages, with the reason if it was not selected or not rendered. CSS is appended to `wwwroot\css\app.css`.

## Verification
`dotnet build` succeeds. All 22 existing tests pass. I added no new tests.

## Findings (not changed, out of scope)
- **No period conversion exists.** The three throughput buckets (Under 2 weeks, 2–4 weeks, Over 4 weeks) are averaged as equal periods, then WeeksOfWork = TotalItems ÷ average. The UI labels the buckets as different lengths. The console shows the real formula, so this is now visible.
- "Prioritisation" is just a `Take(5)` of the ranked list. `InsightPrioritiser.cs`, `InsightGenerator.cs` and `TeamHealthSignalBuilderService.cs` are empty stubs.
- Rendering depends on page layout and placement: an insight whose anchor isn't a displayed area is not rendered.
- The builder parameter `unprioritisedItems` is unused, and `highPenalty` in the prioritisation maths is computed but never used.
- `OperationalInsight.Category` and `Domain` are not set by the engine. The trace takes them from the rule.

## Limitations and follow-ups
- Per-period labels are not shown in the sum formulas, only the values.
- The Derived and Insights console sections still use the original step lists. Only the Signals section has the staged formula view.
- No unit tests cover the new traces yet.
- Not visually checked in a browser.
