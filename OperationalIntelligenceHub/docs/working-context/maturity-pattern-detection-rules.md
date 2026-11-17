# Maturity Assessment — Pattern Detection Rules

## Status and scope

This is a documentation-only mapping of the `Pattern Detection Rules` sheet in **Pattern Detection Rules — Version 0.1** of the Rule Register Review workbook. Although the source rows are marked **Proposed**, the user has approved all 28 as a living rule catalogue. Their technical conditions, evidence sufficiency and scenarios are not yet an executable specification. The JSON is the intended source of truth for editable rules and narrative; this document does not change code or configuration.

The workbook contains 26 pattern IDs and 28 rule IDs. The 26 IDs are EM01, EM02, BF01–BF09, CC01–CC04, SC01–SC05, and PR01–PR06; EM01 and EM02 each have two rule rows. The earlier catalogue note that mentions a possible 27th pattern remains unresolved; no 27th ID is present in the current list.

The workbook says there is one selected behaviour per perspective/theme, defines no technical trigger syntax or numeric threshold, and treats evidence sufficiency as qualitative guidance. A later workbook's `Behaviour Test Inputs` sheet has one populated input row without an expected result; it is not an executable acceptance test.

## Application identifier mapping

The workbook references such as `SQ-OWN-L2` are proposed composite references, not stored application IDs. They have been cross-checked against the current `maturity.json` signal mappings and catalogue:

| Workbook prefix | Application lens |
|---|---|
| `SQ` | `Squad` |
| `SM` | `ScrumMaster` |
| `PO` | `ProductOwner` |
| `LD` | `Leader` |

| Workbook theme code | Application theme |
|---|---|
| `OWN` | `Ownership` |
| `TRU` | `Trust` |
| `VAL` | `Value` |
| `LRN` | `Learning` |
| `SYS` | `System` |

`L<n>` denotes level `<n>`. Thus `SQ-OWN-L2` resolves to the current mapping ID `Squad_Ownership_L2` and tuple `(Lens=Squad, Theme=Ownership, Level=2)`. The pattern rules must resolve against that tuple or the matching current `SignalMapping.BehaviourId`; they must not assume the workbook reference is persisted in an assessment.

The cross-check found 249 workbook reference occurrences representing 78 distinct IDs, all of which resolve to an existing Lens/Theme/Level tuple in the current mapping. No references were unresolvable. New selections persist their `BehaviourId` and catalogue version alongside the coordinate and selected content. The numeric signal builder validates these against the retained version, but does not use them to evaluate rule semantics. The IDs below are the current catalogue/mapping identifiers corresponding to the workbook references; historical selections must be interpreted using their saved version.

Some workbook labels do not match the current catalogue headline for the same tuple. For example, `SQ-OWN-L2` is described in the workbook as “Shared Responsibility Emerging” but the current catalogue headline is “Shared knowledge emerging”; `PO-VAL-L5` is called “Optimising Product Portfolio Value” in the workbook but the current headline is “Customer value leadership”. Tuple resolution is not proof of semantic equivalence. Review these labels before approving rule copy or expected outcomes; do not silently rename or remap a behaviour.

## One-selection-per-theme constraint

There are 92 within-rule coordinate collisions: references in a single rule include multiple levels for the same lens/theme. Those references cannot all be selected simultaneously in the current assessment. They must be interpreted as candidate alternatives, not as conjunctive evidence. The workbook does not yet identify which alternatives are equivalent, required, optional, supporting, or contradictory in a machine-evaluable way.

An unselected level is unknown, not evidence that the behaviour is absent. Any rule requiring two different levels for the same lens/theme is impossible under the current model and must be clarified or rejected during rule review.

## Rule reference map

The references in this table are the workbook's composite codes. Use the prefix/theme mapping above to resolve each code to the actual `BehaviourId` and Lens/Theme/Level tuple. The list captures the `Behaviour Reference(s)` cell for each rule; it does not assert that every listed reference is required at the same time.

| Rule ID | Pattern ID | Workbook behaviour references |
|---|---|---|
| BF01-R01 | BF01 | `SQ-VAL-L2`, `PO-VAL-L3`, `PO-VAL-L4`, `PO-OWN-L3`, `SQ-OWN-L3`, `SQ-VAL-L1` |
| BF02-R01 | BF02 | `LD-TRU-L1`, `SQ-OWN-L4`, `LD-OWN-L2`, `SQ-OWN-L3`, `LD-TRU-L2`, `SQ-TRU-L4`, `SQ-TRU-L5`, `LD-OWN-L1` |
| BF03-R01 | BF03 | `LD-TRU-L1`, `SQ-OWN-L4`, `LD-OWN-L2`, `SQ-OWN-L3`, `LD-TRU-L2`, `SQ-TRU-L4`, `SQ-TRU-L5` |
| BF04-R01 | BF04 | `PO-SYS-L3`, `PO-TRU-L1`, `PO-TRU-L2`, `PO-VAL-L3`, `PO-VAL-L4`, `PO-SYS-L4`, `PO-OWN-L4`, `PO-OWN-L3` |
| BF05-R01 | BF05 | `SM-TRU-L4`, `SQ-OWN-L2`, `SM-OWN-L3`, `SQ-OWN-L1`, `SM-TRU-L3`, `SQ-TRU-L2`, `SM-OWN-L4`, `SQ-TRU-L1`, `SQ-LRN-L2`, `SQ-LRN-L3` |
| BF06-R01 | BF06 | `SM-LRN-L3`, `LD-LRN-L2`, `LD-LRN-L1`, `SM-LRN-L4`, `LD-TRU-L1`, `SQ-LRN-L4`, `LD-OWN-L4`, `SQ-OWN-L4`, `SQ-LRN-L5`, `LD-TRU-L2` |
| BF07-R01 | BF07 | `SQ-VAL-L5`, `PO-VAL-L1`, `PO-LRN-L2`, `PO-VAL-L2`, `PO-LRN-L1`, `PO-LRN-L4`, `PO-VAL-L3`, `PO-LRN-L5`, `SQ-LRN-L4`, `SQ-LRN-L3` |
| BF08-R01 | BF08 | `LD-SYS-L2`, `SM-SYS-L4`, `SM-SYS-L5`, `SQ-SYS-L5`, `SQ-SYS-L4`, `LD-SYS-L1`, `SM-SYS-L3` |
| BF09-R01 | BF09 | `PO-VAL-L5`, `SQ-SYS-L3`, `SQ-VAL-L2`, `SQ-VAL-L3`, `SQ-SYS-L1`, `SQ-SYS-L2`, `PO-VAL-L4`, `PO-SYS-L4`, `SQ-SYS-L4`, `PO-SYS-L5`, `SQ-VAL-L1` |
| EM01-R01 | EM01 | `SQ-OWN-L2`, `SQ-VAL-L2`, `PO-VAL-L4`, `SQ-OWN-L1`, `PO-OWN-L4`, `SQ-VAL-L1` |
| EM01-R02 | EM01 | `SQ-OWN-L1`, `LD-OWN-L4`, `LD-TRU-L4`, `SQ-TRU-L3`, `LD-TRU-L5`, `SQ-TRU-L4` |
| EM02-R01 | EM02 | `LD-TRU-L1`, `SQ-OWN-L4`, `SQ-TRU-L4`, `SQ-TRU-L5`, `LD-OWN-L1` |
| EM02-R02 | EM02 | `SM-OWN-L3`, `SQ-OWN-L1`, `SM-TRU-L3`, `SQ-TRU-L3` |
| CC01-R01 | CC01 | `SQ-OWN-L5`, `LD-TRU-L1`, `LD-OWN-L4`, `LD-TRU-L4`, `SQ-OWN-L4`, `LD-OWN-L2`, `SQ-OWN-L3`, `LD-TRU-L2`, `SQ-TRU-L4`, `SQ-TRU-L5`, `LD-OWN-L1` |
| CC02-R01 | CC02 | `LD-SYS-L2`, `LD-VAL-L1`, `PO-VAL-L5`, `LD-VAL-L3`, `LD-VAL-L2`, `PO-OWN-L5`, `LD-SYS-L3`, `PO-VAL-L4`, `LD-SYS-L1`, `PO-OWN-L4`, `LD-VAL-L4` |
| CC03-R01 | CC03 | `SM-LRN-L5`, `LD-LRN-L4`, `LD-LRN-L2`, `LD-LRN-L1`, `LD-LRN-L5`, `SM-LRN-L4`, `LD-LRN-L3`, `SQ-LRN-L4`, `SQ-LRN-L5` |
| CC04-R01 | CC04 | `LD-SYS-L2`, `SM-SYS-L4`, `SM-SYS-L5`, `LD-SYS-L4`, `SQ-SYS-L5`, `SQ-SYS-L4`, `LD-SYS-L1`, `LD-SYS-L5`, `SM-SYS-L3` |
| SC01-R01 | SC01 | `LD-VAL-L3`, `SQ-VAL-L3`, `LD-VAL-L5`, `SQ-VAL-L4`, `LD-TRU-L1`, `LD-VAL-L4`, `LD-OWN-L2`, `LD-TRU-L2`, `LD-OWN-L1` |
| SC02-R01 | SC02 | `LD-OWN-L5`, `SQ-OWN-L5`, `LD-TRU-L1`, `LD-OWN-L4`, `SQ-OWN-L4`, `LD-TRU-L2`, `SQ-TRU-L4`, `SQ-TRU-L5` |
| SC03-R01 | SC03 | `LD-LRN-L4`, `LD-LRN-L5`, `LD-TRU-L1`, `LD-LRN-L3`, `SQ-LRN-L4`, `LD-OWN-L4`, `LD-TRU-L4`, `SQ-LRN-L5`, `LD-TRU-L2` |
| SC04-R01 | SC04 | `PO-VAL-L5`, `PO-TRU-L1`, `PO-TRU-L2`, `PO-OWN-L5`, `PO-VAL-L4`, `PO-SYS-L4`, `PO-SYS-L5`, `PO-OWN-L4` |
| SC05-R01 | SC05 | `LD-SYS-L2`, `SM-SYS-L4`, `SM-SYS-L5`, `LD-SYS-L4`, `SQ-SYS-L5`, `LD-SYS-L3`, `SQ-SYS-L4`, `LD-SYS-L1`, `LD-SYS-L5` |
| PR01-R01 | PR01 | `LD-OWN-L5`, `SM-OWN-L3`, `SQ-OWN-L5`, `LD-OWN-L4`, `LD-TRU-L4`, `SM-OWN-L4`, `SQ-OWN-L4`, `SQ-OWN-L3`, `LD-TRU-L5` |
| PR02-R01 | PR02 | `SQ-VAL-L5`, `PO-VAL-L5`, `LD-VAL-L3`, `SQ-VAL-L3`, `LD-VAL-L5`, `SQ-VAL-L4`, `PO-VAL-L3`, `PO-VAL-L4`, `LD-VAL-L4`, `PO-OWN-L3`, `SQ-OWN-L3` |
| PR03-R01 | PR03 | `SM-LRN-L3`, `SM-SYS-L5`, `SM-LRN-L5`, `LD-LRN-L4`, `LD-LRN-L5`, `SM-LRN-L4`, `LD-LRN-L3`, `SQ-LRN-L4`, `LD-TRU-L4`, `SQ-LRN-L5`, `SQ-LRN-L3` |
| PR04-R01 | PR04 | `PO-SYS-L3`, `SM-TRU-L4`, `PO-TRU-L3`, `SM-TRU-L5`, `SM-OWN-L3`, `PO-TRU-L4`, `PO-TRU-L5`, `SM-TRU-L3`, `LD-TRU-L4`, `SQ-TRU-L3`, `LD-TRU-L5`, `SQ-TRU-L4`, `SQ-TRU-L5` |
| PR05-R01 | PR05 | `PO-SYS-L3`, `SM-SYS-L4`, `SM-SYS-L5`, `LD-SYS-L4`, `SQ-SYS-L5`, `LD-SYS-L3`, `PO-VAL-L4`, `PO-SYS-L4`, `SQ-SYS-L4`, `PO-SYS-L5`, `LD-SYS-L5`, `LD-VAL-L4`, `SM-SYS-L3` |
| PR06-R01 | PR06 | `SM-LRN-L3`, `LD-LRN-L4`, `SM-OWN-L3`, `SQ-OWN-L5`, `SM-LRN-L4`, `LD-LRN-L3`, `SQ-LRN-L4`, `LD-OWN-L4`, `SM-OWN-L4`, `SQ-OWN-L4`, `SQ-LRN-L5` |

## Detection specification still required

The workbook's prose is useful design input, but it does not itself establish machine-evaluable conditions. The separate [BF01 revised draft](../../wwwroot/data/rules/maturity_pattern_rules_BF01_revised.json) specifies conditions for the direct-evidence contract described in the [implementation plan](./maturity-pattern-implementation-plan.md). An isolated semantic evaluator executes BF01; it is not integrated into the current results page or automatically substituted for the workbook. Other rows still need approved definitions of:

- which references are required, optional, or alternatives;
- AND/OR structure and the direction of any role-to-role relationship;
- which supporting, contradictory, and excluded evidence is actually observable from assessment selections;
- what constitutes a triggered rule versus a sufficiently supported pattern;
- how missing or incomplete selections affect evaluation;
- how multiple rules for a pattern aggregate and how overlapping patterns coexist;
- approved user-facing observation, hypothesis, implication, validation question, and conditional action.

The workbook's “do not infer” text is a guardrail, not automatically a machine-detectable exclusion. Context such as authority boundaries, capacity, stakeholder decisions, motivation, and external constraints is not captured as a structured assessment input. It must not be inferred from an unselected behaviour or fabricated by the engine.

## Relationship to existing flow rules

Maturity behaviour patterns use selected assessment behaviours; Backlog Health flow rules use operational flow signals. Keep their rule schemas and evaluation paths distinct. The latest workbook's Flow Rule Register contains review findings and candidate changes that differ from some deployed flow rules; those findings are recorded separately in [backlog-health-flow-rules.md](./backlog-health-flow-rules.md) and do not redefine current runtime behaviour.

## Related implementation plan

The current data model, pattern aggregation proposal, insight narrative, UX, test matrix, implementation sequence, risks, and approvals are documented in [maturity-pattern-implementation-plan.md](./maturity-pattern-implementation-plan.md).
