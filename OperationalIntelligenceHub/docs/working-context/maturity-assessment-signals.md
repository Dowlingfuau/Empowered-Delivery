# Maturity Assessment — Current Data and Signal Model

## Purpose and status

This document records the current Maturity Assessment data path and distinguishes it from the proposed semantic behaviour-pattern work. The pattern mapping, implementation design, user experience, and tests are covered in the [pattern implementation plan](./maturity-pattern-implementation-plan.md) and [pattern detection rules](./maturity-pattern-detection-rules.md). No runtime pattern detector exists for those workbook rules today.

## 1. Assessment responses

Maturity responses are stored as an `AssessmentSession` in browser localStorage under `assessments_maturity`.

```text
AssessmentSession
  AssessmentId
  WorkspaceId
  SquadId
  LensAssessments[]
    Lens
    Themes[]
      Theme
      Level (0 = unanswered; 1–5 = selected)
      SelectedBehaviourHeadline (model field; not populated by current selection flow)
      BehaviouralNote (model field; catalogue-generated content in current selection flow)
```

The user selects one behaviour per lens/theme; the page validates one entry for each of the five themes within each lens. The canonical selection is that behaviour, resolved through the catalogue from the currently stored `(Lens, Theme, Level)` coordinate, not an arbitrary list of behaviour IDs. A zero level is missing evidence; it does not mean that a behaviour was rejected or absent from the organization.

The current assessment does **not** collect user-entered free-text behavioural evidence. The selected behaviour's headline and catalogue signals/examples provide supporting context for the selection, not independent observations. On selection, `SaveBehaviourAsync` assembles catalogue signals and examples into `BehaviouralNote`; `SessionService.UpdateThemeScoreAsync` saves that generated value with the level, and draft transfer carries it forward. A legacy or generated note must not be treated as independent user evidence.

`SelectedBehaviourHeadline` is a model property included in session serialization, but the current selection and draft-transfer flows do not populate it; a newly created session therefore ordinarily saves its default empty value. Results obtain the headline from the catalogue using the selected coordinate. Existing saved sessions may contain other values; the field must not be assumed populated or authoritative. The repository serializes the session to localStorage rather than separately persisting a resolved `BehaviourId`.

## 2. Catalogue and mapping

`wwwroot/data/diagnostics/maturity_behaviours.json` provides the behaviour content by lens, theme, and level. `wwwroot/data/signals/maturity.json` maps each coordinate to a current `BehaviourId`, such as `Squad_Ownership_L3`, and an output signal.

The assessment session does not persist `BehaviourId`. `MaturitySignalBuilderService` loads signal mappings and matches by Lens, Theme, and Level; it records the resolved `BehaviourId` in the session-only signal trace, but does not use it to evaluate a semantic relationship. Workbook references such as `SQ-OWN-L3` are proposed composite references. They can be cross-checked against the current tuple and mapping, but they are not application IDs saved on the session. See the [complete rule reference map](./maturity-pattern-detection-rules.md).

## 3. Current runtime pipeline

```text
AssessmentSession
  -> MaturitySignalBuilderService
  -> DerivedSignalEngine (numeric behaviour and flow rules)
  -> SystemRuleEngine
  -> OperationalInsightEngine
  -> MaturityAssessment results and diagnostics
```

| Component | Current responsibility |
|---|---|
| `MaturitySignalBuilderService` | Converts selected nonzero lens/theme/level coordinates to normalized numeric signals and signal traces. |
| `DerivedSignalEngine` | Evaluates configured `DerivedSignalRule` expressions against numeric signals. It combines behavior and flow rule definitions; it does not evaluate workbook prose or selected-behaviour relationships. |
| `SystemRuleEngine` | Evaluates system rules against derived-signal intensities. |
| `OperationalInsightEngine` | Creates insights from derived signals, scores them with intensity, configured priority, and confidence weight, and selects at most five. |
| `MaturityInsightAttribution` | Traces existing numeric-rule evidence back to Maturity coordinates when the evaluation evidence resolves uniquely. |
| `MaturityAssessment.razor` | Presents the four-lens/five-theme results grid, radar profile, and expandable insight cards. Current cards expose interpretation, description, intensity, source, supporting signals, and rule reasons. |
| `Components/Debug/DiagnosticsConsole.razor` | Provides evaluation diagnostics. Its Pattern Detection area is currently a placeholder with no data. |

`BehaviourRuleEngine` is registered but is not called by the main signal pipeline. It contains experimental average-ownership examples, not the new patterns. `InsightGenerator`, `InsightPrioritiser`, and `DomainProfileBuilder` remain TODO stubs; the related domain-profile path is not implemented. `InsightResult` is not the active result type for the Maturity pipeline.

## 4. Proposed pattern boundary

The new workbook describes semantic relationships among selected behaviours, including reinforcing and friction patterns. Individual maturity signals retain their raw selected level, and the mapping can resolve a behaviour ID; numeric averages alone cannot preserve the individual evidence or establish these relationships. The current pipeline does not evaluate the new semantic rules.

The workbook's 28 rule rows reference 26 patterns. Cross-checking found 249 unique references with current Lens/Theme/Level mappings and no unresolved tuples. The 92 within-rule coordinate collisions represent multiple candidate levels for the same lens/theme; because only one level can be selected at a time, these must be treated as alternatives, not simultaneous requirements. Some workbook labels differ from current catalogue headlines, so tuple resolution alone does not confirm semantic equivalence.

The 28 workbook rules have been approved as a living document, although Version 0.1 still marks its rows Proposed. The workbook supplies no technical trigger syntax, no approved numeric threshold, and no raw behaviour test cases. Therefore:

- an individual rule match must remain distinct from a sufficiently supported pattern;
- incomplete coordinates are unknown, not negative evidence;
- hypotheses and delivery impacts must remain conditional and user-validated;
- the existing numeric insight ranking must not be reused as a pattern confidence model;
- positive patterns must be evaluated alongside friction patterns under the same evidence standard.

The new pattern evaluation must be deterministic, preserve selected-coordinate provenance, aggregate rules by pattern ID, and compose user-facing insights only after an explicit support decision. Extending the existing `BehaviourRuleEngine` is an option; the redesigned pipeline and rule configuration remain to be decided. The [implementation plan](./maturity-pattern-implementation-plan.md) records the earlier proposed components, tests, UX, sequence, and open decisions.

### Agreed target — not current runtime

For each selected behaviour, persist the coordinate `(Lens, Theme, Level)`, resolved `BehaviourId`, and catalogue version with the assessment evidence. Retain the referenced catalogue content and the mapping needed to interpret that selection as it was presented. Reopening or re-evaluating an assessment must use that historical version, not silently reinterpret its coordinate against today's catalogue. If the referenced version or mapping is unavailable or inconsistent, report the limitation explicitly rather than substitute the current version. Version identifiers, storage and retention mechanisms remain to be designed; this is an agreed target, not a current capability. Existing saved assessments are test data and may be discarded, but this documentation does not delete or migrate them.

Preserving the selected behaviour does not by itself reproduce an old insight if rule definitions or evaluation logic change. Rule/evaluation versioning and whether to replay an earlier evaluation or run a new one are separate decisions; the trace must distinguish them.

Preserve this evidence alongside raw levels, calculated signals, optional normalised values, derived results and evaluation history. The current session-only trace is not durable evidence history. Semantic selections may be evaluated directly rather than forced into numeric signals. Normalisation is optional and equal ranges do not establish comparability.

The JSON is the source of truth for editable rule definitions and narrative, while its machine-evaluable conditions and sufficiency criteria remain incomplete. A matched rule is a candidate, not an automatic insight. Dynamic confidence and priority are separate evaluations, with support, challenge, missing evidence, relationships and shared provenance recorded; neither has an approved numeric formula. Diagnostic insights remain distinct from cross-diagnostic MRI hypotheses. See [insight-engine-purpose.md](./insight-engine-purpose.md).

## 5. Separate Flow rule documentation

Backlog Health rules use operational flow signals and remain distinct from Maturity behaviour patterns. Their current runtime is documented in [backlog-health-flow-rules.md](./backlog-health-flow-rules.md), including a separate section for review findings from the workbook that are not deployed changes.
