# Maturity Assessment — Current Data and Signal Model

## Purpose and status

This document records the current Maturity Assessment data paths. BF01 is evaluated independently of the numeric pipeline during saved assessment refreshes; its candidate trace is visible in the Diagnostics Console, not in the existing result cards. The pattern mapping, implementation design, user experience, and tests are covered in the [pattern implementation plan](./maturity-pattern-implementation-plan.md) and [pattern detection rules](./maturity-pattern-detection-rules.md).

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
      SelectedBehaviourHeadline
      BehaviouralNote (catalogue-generated content)
      SelectedBehaviour
        BehaviourId
        CatalogueVersion
        Headline
        Signals[]
        Examples[]
```

The user selects one behaviour per lens/theme; the page validates one entry for each of the five themes within each lens. The coordinate `(Lens, Theme, Level)` and the selected behaviour's identity, version and content are saved together. A zero level is missing evidence; it does not mean that a behaviour was rejected or absent from the organization.

The current assessment does **not** collect user-entered free-text behavioural evidence. The selected behaviour's headline and catalogue signals/examples provide supporting context for the selection, not independent observations. On selection, `SaveBehaviourAsync` assembles catalogue signals and examples into `BehaviouralNote`; `SessionService.UpdateThemeScoreAsync` saves that generated value with the level, and draft transfer carries it forward. A legacy or generated note must not be treated as independent user evidence.

The selection and draft-transfer flows populate `SelectedBehaviourHeadline` and the selected-content snapshot. Results display the saved snapshot for the selected level, while unselected choices use the current catalogue. Legacy sessions with selected levels but without provenance cannot be reopened safely and are not automatically migrated or deleted.

## 2. Catalogue and mapping

`wwwroot/data/diagnostics/maturity_behaviours.json` provides the behaviour content by lens, theme, and level. `wwwroot/data/signals/maturity.json` maps each coordinate to a current `BehaviourId`, such as `Squad_Ownership_L3`, and an output signal.

`wwwroot/data/maturity/current.json` identifies the current combined catalogue/mapping version. Immutable copies of both files are retained in `wwwroot/data/maturity/versions/<version>/`; `v1` is the initial version. The current files must match the retained version or the assessment reports an error. When changing either current file, add a new version directory containing copies of both updated files and update the manifest; never edit or remove a retained version while assessments may reference it.

On selection, the displayed content is checked against the retained version, then captured with the matching ID. On reopening or signal construction, the saved coordinate, ID, headline, signals and examples are checked against the selected version. Missing or inconsistent provenance is reported rather than silently resolved against current content. `MaturitySignalBuilderService` uses the selected version's mapping and records its ID in the session-only signal trace; it does not evaluate a semantic relationship. Workbook references such as `SQ-OWN-L3` are proposed composite references, not application IDs. See the [complete rule reference map](./maturity-pattern-detection-rules.md).

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
| `Components/Debug/DiagnosticsConsole.razor` | Provides numeric evaluation diagnostics and a separate BF01 semantic trace for saved Maturity sessions. Existing numeric Pattern Detection placeholders remain unchanged. |

`BehaviourRuleEngine` is registered but is not called by the main signal pipeline. It contains experimental average-ownership examples, not the new patterns. `InsightGenerator`, `InsightPrioritiser`, and `DomainProfileBuilder` remain TODO stubs; the related domain-profile path is not implemented. `InsightResult` is not the active result type for the Maturity pipeline.

## 4. Proposed pattern boundary

The new workbook describes semantic relationships among selected behaviours, including reinforcing and friction patterns. Individual maturity signals retain their raw selected level, while the session retains the selected behaviour evidence; numeric averages alone cannot establish these relationships. The numeric pipeline does not evaluate semantic rules; BF01 runs separately for saved Maturity sessions.

The workbook's 28 rule rows reference 26 patterns. Cross-checking found 249 reference occurrences representing 78 distinct IDs with current Lens/Theme/Level mappings and no unresolved tuples. The 92 within-rule coordinate collisions represent multiple candidate levels for the same lens/theme; because only one level can be selected at a time, these must be treated as alternatives, not simultaneous requirements. Some workbook labels differ from current catalogue headlines, so tuple resolution alone does not confirm semantic equivalence.

The 28 workbook rules have been approved as a living document, although Version 0.1 still marks its rows Proposed. The workbook itself supplies no technical trigger syntax or approved numeric threshold; a later workbook contains one populated behaviour test input without an expected result. The separate BF01 JSON has a typed direct-evidence contract implemented by an evaluator independent of the numeric path. Therefore:

- an individual rule match must remain distinct from a sufficiently supported pattern;
- incomplete coordinates are unknown, not negative evidence;
- hypotheses and delivery impacts must remain conditional and user-validated;
- the existing numeric insight ranking must not be reused as a pattern confidence model;
- positive patterns must be evaluated alongside friction patterns under the same evidence standard.

The new pattern evaluation must be deterministic, preserve selected-coordinate provenance, aggregate rules by pattern ID, and compose user-facing insights only after an explicit support decision. The initial proposed engine directly evaluates selected behaviours independently of numeric signals. A future Role Profile Builder can generate a user-facing role profile and interpreted evidence from each role's five selections; the engine will consume both profiles and the original evidence without treating profile absence as negative evidence. Neither profiles nor their UI are part of the initial build. The [implementation plan](./maturity-pattern-implementation-plan.md) specifies the proposed BF01 contract, component responsibilities, tests and open decisions.

### Evidence preservation and remaining target

The coordinate, resolved `BehaviourId`, catalogue version and selected content are now preserved together, with retained catalogue and mapping versions used on reopening and numeric evaluation. Existing saved assessments without this provenance are not migrated or deleted.

Preserving the selected behaviour does not by itself reproduce an old insight if rule definitions or evaluation logic change. Rule/evaluation versioning and whether to replay an earlier evaluation or run a new one are separate decisions; the trace must distinguish them.

The BF01 semantic run preserves its selected evidence and evaluation trace in memory; it does not persist evaluation history or combine its evidence with the numeric pipeline's signals. The current session-only trace is not durable evidence history. Semantic selections may be evaluated directly rather than forced into numeric signals. Normalisation is optional and equal ranges do not establish comparability.

The JSON is the intended source of truth for editable rule definitions and narrative. The separate BF01 draft specifies machine-evaluable conditions for the independent evaluator; other conditions and sufficiency criteria remain incomplete. A matched rule is a candidate, not an automatic insight. Dynamic confidence and priority are separate evaluations, with support, challenge, missing evidence, relationships and shared provenance recorded; neither has an approved numeric formula. Diagnostic insights remain distinct from cross-diagnostic MRI hypotheses. See [insight-engine-purpose.md](./insight-engine-purpose.md).

## 5. Separate Flow rule documentation

Backlog Health rules use operational flow signals and remain distinct from Maturity behaviour patterns. Their current runtime is documented in [backlog-health-flow-rules.md](./backlog-health-flow-rules.md), including a separate section for review findings from the workbook that are not deployed changes.
