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
      SelectedBehaviourHeadline
      BehaviouralNote
```

The current page validates one entry for each of the five themes within each lens. Thus the stored selection identity is `(Lens, Theme, Level)`: one behaviour level per lens/theme, not an arbitrary list of behaviour IDs. A zero level is missing evidence; it does not mean that the corresponding behaviour was rejected or absent from the organization.

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

## 5. Separate Flow rule documentation

Backlog Health rules use operational flow signals and remain distinct from Maturity behaviour patterns. Their current runtime is documented in [backlog-health-flow-rules.md](./backlog-health-flow-rules.md), including a separate section for review findings from the workbook that are not deployed changes.
