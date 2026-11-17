# Maturity Assessment — Behaviour Pattern Implementation Plan

## Status and guardrails

This plan describes the isolated BF01 implementation and the remaining Maturity architecture work.

The source workbook's `Pattern Detection Rules` sheet is Version 0.1. Its 28 rule rows for 26 patterns are marked **Proposed** in the workbook, but the user has since approved them as a living catalogue. JSON is the intended source of truth for editable rules and narrative. The separate revised BF01 draft specifies a proposed contract; other executable conditions and sufficiency need further specification. Workbook-derived examples below remain provisional acceptance-test fixtures.

**Scope and dependency:** The immediate target is an independently testable Maturity semantic engine, not a platform-wide rule framework. It can evaluate original evidence before future shared contracts or Role Profiles are implemented, provided its output retains provenance and a clear diagnostic-to-platform boundary. Shared integration and historical replay remain separate decisions. The agreed redesign preserves evidence throughout the pipeline and keeps confidence and priority distinct; see [insight-engine-purpose.md](./insight-engine-purpose.md).

The 26 pattern IDs are EM01, EM02, BF01–BF09, CC01–CC04, SC01–SC05, and PR01–PR06. EM01 and EM02 each have two rule rows. A prior catalogue note mentions 27 patterns, but no 27th ID is listed; this discrepancy remains an approval question.

This plan does not alter the agreed pattern definitions. Where the workbook is ambiguous, internally conflicting, or unobservable in the current assessment model, the gap is called out rather than resolved by invention. The separate Flow Rule Register is not mixed into the Maturity behaviour-pattern evaluation; its differences from current flow runtime documentation are recorded in [backlog-health-flow-rules.md](./backlog-health-flow-rules.md).

## Target architecture and BF01 executable contract

**Status:** the first independently testable Maturity semantic evaluator, validator and structured trace are implemented. The [BF01 revised draft](../../wwwroot/data/rules/maturity_pattern_rules_BF01_revised.json) uses `maturity-semantic-v1-draft` and is loaded by the new Maturity-specific repository, not the existing numeric rule repository. It is not connected to the results page or platform pipeline. Other workbook rows are not converted; their semantics remain unresolved.

### Initial and enriched evidence paths

```text
Initial target:
saved selections -> acquire and validate -> project evidence
                 -> configurable semantic evaluation -> candidate + trace
                 -> assess support and compose diagnostic finding when eligible

Future enrichment, not a prerequisite:
saved selections -> deterministic Role Profile Builder -> structured Role Profiles
       |                                                   |
       +------------------ original evidence --------------+-> evidence projection
                                                              -> same semantic engine
```

The initial evaluator uses original selections, not numeric scores. It resolves each saved role/theme/level, BehaviourId, catalogue version and snapshot using retained catalogue and mapping content. `MaturityEvidenceService` already checks this for valid saved selections; a projection must retain the selections and separately represent missing, unavailable and invalid evidence rather than reducing them to the returned numeric mapping. Each selection is checked against its own version. No substitution with the current catalogue is allowed.

Later, a deterministic Role Profile Builder will interpret the five selected behaviours of each Product Owner, Squad, Scrum Master and Leader role into that role's current state, operating characteristics and growth focus. Profiles are planned both as a dedicated user-facing experience and as structured interpreted evidence. Each profile must link to its original selections and versioned interpretation; it cannot replace them. The semantic engine, not a profile, evaluates tensions between roles; a profile does not model a role in tension with itself. Original selections remain available alongside profile attributes. Unavailable future attributes are unknown or unavailable, not negative evidence. Optional future profile dependencies must be explicit in definition versions. Do not implement profile models, services, rules, persistence, UI or speculative placeholder abstractions now.

Separate acquisition/projection from evaluation so future evidence can be added without rewriting the evaluator; do not assume normalisation means numeric scoring. The proposed responsibilities are definition loading and validation; evidence acquisition and projection; condition and relationship evaluation; contextual contribution evaluation; finding composition; and trace recording. These are responsibilities, not a mandatory class or deployment layout. The existing numeric Maturity path and shared Backlog Health processing remain unchanged in the first engine implementation.

### JSON contract: `maturity-semantic-v1-draft`

The executable inputs are `evaluation.contract`, `semanticDefinition.coreRelationship`, `catalogueEvidence`, `supportingEvidenceProfiles`, `contextRelationships` and `executionPolicy`. Other fields are authored context and guardrails, not executable expressions. Only BF01 uses this contract; an unsupported contract or expression must be rejected explicitly, never treated as a non-match. `candidateBehaviourIds` is an index, not an AND condition. Narrative text is not parsed as logic. The canonical retained catalogue and mapping are authoritative for behaviour identity and content; duplicate IDs, levels and headlines in the draft are review aids and must be validated against them, never treated as an alternative catalogue.

| Construct | Proposed evaluator meaning |
|---|---|
| `groupDefinitions` | A unique `groupId`, `coordinate` and nonempty set of catalogue levels. Resolve the saved BehaviourId and coordinate against its compatible archived catalogue/mapping first; membership uses the resolved level, not a numeric score. Validate each level and its unique behaviour identity. |
| `{ "hasGroup": "groupId" }` | True if the valid selected behaviour at the group's coordinate belongs to that group; false if valid but outside it; unknown if the evidence is missing or unavailable. |
| `{ "hasBehaviour": { "coordinate": "...", "behaviourIds": ["..."] } }` | True if a valid selection's exact ID is in the list, false if valid and not in it, unknown if missing or unavailable. Entries in the list are alternatives, not simultaneous selections. |
| `{ "all": [expression, ...] }` | AND: false if any operand is false; true if all are true; otherwise unknown. A nonempty array is required. |
| `{ "any": [expression, ...] }` | OR: true if any operand is true; false if all are false; otherwise unknown. A nonempty array is required. BF01 currently does not use this operator, but the contract supports it for later definitions. |
| `branches[].when` and `result` | Evaluate all core branches, retaining every operand and result. With both core inputs valid, exactly one branch must match; A and B produce a candidate with their explicit `direction`, and both shared-orientation branches produce a non-match. `ChallengesBF01` records the shared-value reason; it does not create an alignment insight. Zero or multiple matches are definition errors. |
| `supportingEvidenceProfiles[].contributions` | After a core match, use the valid selected level and explicit `DirectionA` or `DirectionB` key to select Supporting, Challenging, Qualifying or Neutral. Each profile is pattern-specific; retain its principle and source selection. A classification does not itself prove a cause or change the core outcome. |
| `contextRelationships[].when` / `effect` | On a core match, evaluate each expression with the same three-valued logic. `ContextOnly` adds the authored conditional interpretation without replacing individual contributions or changing the match. False and unknown relationship results remain in the trace. |
| `executionPolicy` | Defines the initial direct evidence source, gating and unassessed fields. Its enumerated values are part of this contract, not arbitrary fallback strings. |

Both core coordinates must be present, valid and catalogue-compatible before deciding a BF01 match or non-match. Missing core is `Unknown`; invalid, unavailable archived content or incompatible core is `Unavailable` with the reason. Missing or unavailable supporting inputs are recorded separately and lead to unknown dependent conditions; they do not prevent reporting a core candidate, change it to a non-match or fabricate a contribution. Supporting evidence and contextual relationships are not evaluated when the core is unknown, unavailable or non-matching; trace them as skipped, not neutral. Out-of-scope coordinates are not read and are not labelled missing.

BF01 declares compatible version `v1` for every selection. Reject incompatible historical versions rather than infer compatibility from an ID or level. An expression has exactly one supported operator; `all`/`any` operands are evaluated without short-circuiting the trace. Validation rejects unknown operators, duplicate IDs, unknown or ambiguous groups, wrong-coordinate behaviour IDs, missing directions or levels, duplicate/overlapping core coverage, incomplete core coverage, invalid effects and references, malformed expressions and impossible conjunctions. The contract does not offer general scripting, weights, thresholds, or automatic priority/confidence.

The trace links each selected snapshot, resolved version, group, classification, expression operand and relationship outcome. Definition provenance includes a fingerprint of the loaded executable definition, so changes to executable content can be distinguished even if a version is accidentally reused; it is not a substitute for immutable version retention. Retain supporting and challenging evidence together; neutral is trace-only. A relationship links to its source selections and is not independent corroboration. Evidence strength, reliability, confidence and priority remain separately **not assessed** until policies are agreed. A core match yields a candidate; eligibility and user-facing composition are separate, still-unresolved decisions.

### Implementation and test boundary

The first implementation evaluates BF01 direct evidence only, without modifying UI, numeric flow, Backlog Health or MRI integration. Focused automated tests exercise all 25 core level pairs, the 60 directional supporting classifications, all four contextual relationships, missing evidence, three-valued logic, invalid definitions, provenance, isolation and existing numeric pipelines. The trace is returned as structured data to its caller; it is not persisted or shown in the Diagnostics Console. The static evaluator accepts already validated projected evidence independently of session acquisition; callers must not label unvalidated evidence as valid.

Later integrate findings and Intelligence Trace into the existing results experience, then agree a platform-facing diagnostic-finding contract before changing existing outputs. A future adapter must preserve diagnostic ownership, evidence and unknown assessments, and must not invent numeric intensity for the current `OperationalInsight` model. Operational MRI may consume diagnostic findings and their evidence for separately evaluated broader relationships; it must not regenerate Maturity interpretation. Role Profiles and their UI are later work.

**Decisions:** refine combined-context effects and classification explanations; decide what partial supporting evidence means for presentation, eligibility and priority; determine immutable rule/evaluator retention and replay policy; agree shared output/trace contracts and transition of existing platform consumers. Separate candidates such as shared low orientation need independent definitions, not an automatic new insight.

The workbook-oriented sections below are historical reference and broader planning material. Their all-rules-first sequence and earlier component alternatives are not prerequisites for the BF01-first engine target above. Where they refer to BF01 conditions as unspecified, the proposed draft contract above is the current preparation; other pattern semantics remain unresolved.

## 1. Existing architecture and data model

### Assessment response storage

| Concern | Current implementation |
|---|---|
| Session | `AssessmentSession` stores `AssessmentId`, `WorkspaceId`, `SquadId`, creation/status fields, and `LensAssessments`. |
| Lens | `MaturityLensAssessment` stores a lens name and a list of themes. Current lens keys include `ScrumMaster`, `ProductOwner`, `Squad`, and `Leader`. |
| Theme selection | The user selects one catalogue behaviour per lens/theme. The saved coordinate includes `Theme` and `Level` under the lens (0 means unselected; 1–5 selectable). New selections retain the behaviour ID, catalogue version and content snapshot, including `SelectedBehaviourHeadline`. `BehaviouralNote` is generated from catalogue signals/examples, not entered by the user or independent evidence. Legacy sessions without provenance are not migrated. |
| Cardinality | The session has one `MaturityTheme` per theme for each lens. Page validation expects each of the five themes exactly once per lens. Therefore, it can store only one level for a given lens/theme. It cannot represent two candidate levels at that coordinate simultaneously. |
| Persistence | `AssessmentRepositoryService` serializes sessions to browser localStorage under `assessments_maturity`. The session is the persisted assessment input; pattern evaluation results are currently recomputed and are not persisted as an assessment result. |
| Incomplete data | A level of 0 is an unanswered coordinate. The page blocks its normal results action until the session is complete. The signal builder skips levels at or below 0. An evaluator must treat such a coordinate as unknown, never as evidence against a pattern. |

The workbook references (`SQ-OWN-L2`, `PO-VAL-L5`, etc.) are not stored application IDs. New selections persist mapping IDs such as `Squad_Ownership_L2` with their coordinate and catalogue version; catalogue headlines and content provide supporting context, not separate observations.

### Catalogue and mapping

`wwwroot/data/diagnostics/maturity_behaviours.json` supplies the current catalogue content for each lens/theme/level. `wwwroot/data/signals/maturity.json` contains the current `BehaviourId`, Lens/Theme/Level mapping, and output signal. `MaturitySignalBuilderService` loads the mappings and selects a mapping by matching Lens, Theme, and Level. It builds a numeric signal from the selected level; it does not use the workbook's composite code or the `BehaviourId` to establish a semantic relationship.

The workbook-to-current-map cross-check found 249 reference occurrences representing 78 distinct IDs and no missing Lens/Theme/Level tuples. This confirms tuple resolution only. It does not confirm that every workbook behaviour label is semantically identical to the current catalogue label. Examples of label mismatches are documented in [maturity-pattern-detection-rules.md](./maturity-pattern-detection-rules.md); those need editorial approval before the rule copy or expected test outcomes are treated as final.

The workbook contains 92 within-rule coordinate collisions. These are multiple levels at the same perspective/theme in one rule's reference list. They must be interpreted as candidate alternatives, not simultaneous requirements. For example, a rule cannot require both `Leader/Ownership=L1` and `Leader/Ownership=L4` in one assessment.

### Existing signal, rule, insight, and presentation flow

| Stage | Current component and behavior | Consequence for pattern work |
|---|---|---|
| Base signal construction | `MaturitySignalBuilderService` maps selected tuples to signals with raw selected levels, normalized values and traces. | Individual raw levels can often be resolved back through the mapping, but a durable, explicit semantic evidence/provenance contract is missing. Pattern evaluation needs the session or a projection preserving coordinates and catalogue labels. |
| Numeric behaviour and flow rules | `DerivedSignalEngine` loads `DerivedSignalRule` definitions from `RuleRepositoryService` and evaluates expression conditions against numeric signals. It combines behavior and flow rule definitions. | This is not a semantic behaviour-combination evaluator. Do not translate workbook prose into numeric signal thresholds or treat the existing `behaviour_rules.json` rules as implementation of these patterns. |
| Experimental engine | `BehaviourRuleEngine` is registered in `Program.cs`, but the main `SignalPipelineService` does not call it. Its current implementation contains sample ownership averages, not workbook pattern logic. | It could be redesigned and integrated for semantic patterns; a separate evaluator is another option, not an established requirement. |
| System rules | `SystemRuleEngine` evaluates derived-signal intensity relationships after the numeric rule stage. | These are a separate existing layer, not a substitute for behaviour-reference relationships. |
| Insight generation | `OperationalInsightEngine` converts derived signals into `OperationalInsight`, ranks by `IntensityScore × Priority × ConfidenceWeight`, and renders at most five. | Its numeric score and five-item cap are not justified for the workbook's qualitative evidence sufficiency. Avoid routing a matched rule directly into this engine as an insight. |
| Domain profiles and generators | `DomainProfileBuilder`, `InsightGenerator`, and `InsightPrioritiser` are TODO stubs; related domain model types are not a completed profile pipeline. `InsightResult` is not used in the active Maturity flow. | Do not describe domain profiles, generated recommendations, or an alternate insight generator as existing capabilities. |
| Maturity results | `MaturityAssessment.razor` shows the four-lens/five-theme result grid and radar profile, then renders existing insights as expandable cards. `MaturityInsightAttribution` traces numeric insight evidence back to coordinates when it can resolve them. | Reuse the current result experience and anchor concepts; pattern evidence should directly carry its selected lens/theme coordinates instead of attempting to infer them from aggregate signals. |
| User validation | Current insight cards show interpretation, description, intensity, source, supporting signals, and rule reasons. There is no validate/reject interaction or four-part narrative model. | A pattern-specific narrative and explicit validation affordance are new UX. Whether validation feedback is persisted is an approval decision. |
| Diagnostics | `Components/Debug/DiagnosticsConsole.razor` currently shows “No data available” for its Pattern Detection section. | Pattern evaluation should expose rule-trigger and pattern-support traces there, so operators can explain why a result did or did not become an insight. |

The `SignalPipelineService` currently builds Maturity signals, evaluates numeric behaviour/flow rules, evaluates system rules, and calls the existing insight engine. It does not evaluate the workbook's semantic pattern rules.

## 2. Workbook rule-to-model mapping

The complete 28-rule composite-reference list and its tuple-to-application-ID translation are in [maturity-pattern-detection-rules.md](./maturity-pattern-detection-rules.md). Examples:

| Workbook reference | Current mapping ID | Stored assessment coordinate | Mapping result |
|---|---|---|---|
| `SQ-OWN-L2` | `Squad_Ownership_L2` | `Squad / Ownership = 2` | Tuple exists; workbook label needs semantic review. |
| `PO-VAL-L5` | `ProductOwner_Value_L5` | `ProductOwner / Value = 5` | Tuple exists; workbook label needs semantic review. |
| `LD-TRU-L1` | `Leader_Trust_L1` | `Leader / Trust = 1` | Tuple exists. |

Every listed reference can be resolved to a current tuple, so the **selection values** are representable. The **rule meanings** are not yet fully evaluable:

| Requirement | Current-data assessment |
|---|---|
| Match a selected behaviour | New selections retain their exact Lens/Theme/Level, `BehaviourId`, version and content; historical lookup uses the retained catalogue and mapping. Semantic rule matching remains unimplemented. |
| Combine source behaviours | Technically possible only after approved AND/OR/alternative semantics are supplied. The workbook's reference list alone does not define the Boolean expression. |
| Supporting/contradictory evidence | Selected behaviours at other coordinates can be collected. The workbook does not consistently define which references play each role, and same-coordinate alternatives cannot co-occur. |
| Exclusions and context | Many exclusions are prose such as “do not assume” or mention conditions (stakeholder authority, capacity, motivation) that are not structured assessment inputs. These cannot be enforced as data exclusions today. |
| Sufficiency | Qualitative phrases such as “moderate when…” do not provide approved thresholds or a complete decision table. No numeric threshold or percentage should be introduced. |
| Directionality | The narrative sometimes implies role-to-role direction, but no complete typed direction model is supplied. Direction must be explicit per rule; it must not be inferred from reference ordering. |
| Incomplete assessments | The data model represents an unanswered coordinate as level 0. That is unknown evidence; it must not count as contradictory or negative evidence. |
| One selection per theme | The model enforces one selected level per lens/theme. Rule references with multiple levels at one coordinate are alternatives unless workbook owners explicitly revise them. |

Some pattern claims refer to evidence outside the assessment. For example, a rule may hypothesize that stakeholders constrain a Product Owner, but the session contains the Product Owner's selected behaviours, not stakeholder decisions or stakeholder responses. Such a pattern can only be framed as a hypothesis for user validation, not as a directly observed fact.

## 3. Detection and aggregation design

### Recommended evaluation stages

1. **Resolve selected evidence.** For each nonzero selection, use its retained catalogue version and mapping to resolve the saved Lens, Theme, Level and `BehaviourId` to the content presented at selection time. Preserve that version and source coordinate; report unavailable or inconsistent historical references instead of silently substituting current content. The generated or legacy `BehaviouralNote` is not a user-entered observation or a substitute for the selected behaviour. Versioned selection capture is implemented; carrying this evidence through semantic evaluation remains future work.
2. **Evaluate approved rule clauses.** A typed rule should distinguish required references from alternatives, supporting evidence, contradictory evidence, and exclusions. Compare only with selected evidence. Return a rule evaluation with `Matched` state and exact evidence references; missing/unselected coordinates remain `Unknown`.
3. **Aggregate by pattern ID.** Combine all evaluations for the same Pattern ID into one candidate pattern, preserving the contributing Rule IDs and the evidence each rule contributed. Do not create duplicate cards for multiple rules describing the same pattern.
4. **Assess support separately.** Return an explicit support state (for example: insufficient, qualified, or supported) based on approved qualitative criteria. A rule match is a trigger candidate, not an automatic user-facing insight. Contradictions or exclusions may qualify or block support only when their semantics are explicitly approved.
5. **Keep distinct overlapping patterns distinct.** Do not suppress a different Pattern ID simply because it shares evidence with another. Group related results in the UI and avoid duplicate copy within a pattern. Any cross-pattern merge or suppression policy requires approved examples.
6. **Preserve direction.** Store relationship direction explicitly (for example, role A's selected evidence in relation to role B's selected evidence). Reversed evidence should not match a directional rule unless the rule is explicitly bidirectional or a separate rule covers the reverse.
7. **Evaluate positive patterns identically.** PR01–PR06 use the same evidence, support, contradiction, and sufficiency pipeline as friction patterns. Do not use lower evidence standards or reduce improvement visibility to make room for friction cards.

No arbitrary scoring, confidence percentage, evidence count, or numeric threshold is recommended. The target evaluates confidence and priority dynamically using justified evidence and context, with the method still unresolved. Until sufficiency statements are made testable, the evaluator must not claim that a user-facing pattern is sufficiently supported merely because a rule triggered.

### Proposed rule record

Use a typed rule definition rather than trying to interpret free-form prose at runtime. At minimum it needs stable Pattern ID and Rule ID, canonical evidence references, required/alternative clause groups, relationship direction, supporting and contradicting references, observable exclusions, qualitative support guidance, and authored narrative fields. This is a conceptual contract, not an approved JSON schema.

Before configuration is written, resolve each workbook collision as an alternative and define exactly which rule references are mandatory. The workbook's `Source Evidence`, `Related Evidence`, `Supporting Evidence`, `Contradictory Evidence`, `Exclusions`, `Evidence Sufficiency`, `Test Scenario`, and `Expected Interpretation` columns should be retained as authored source material; free-text columns should not be parsed as executable conditions.

## 4. Insight composition

Use a deterministic, rule-authored composition. No generative model should invent evidence, motivations, organizational conditions, impacts, or recommendations. The four narrative parts should remain visibly distinct:

| Narrative part | Proposed source and behavior |
|---|---|
| **What we observed** | Render the actual selected catalogue headlines, role/lens, theme, and level that matched the rule. Clearly label these as assessment selections, not independently verified organizational facts. |
| **What we suspect** | Use an authored, conditional relationship hypothesis from the approved pattern/rule content. Do not turn the hypothesis into a diagnosis or claim of cause. |
| **What it might mean** | Use conditional impact copy from approved pattern content. Include relevant alternatives and uncertainty; do not assert a delivery effect that was not measured. |
| **What to do** | Present an authored validation question and a practical next step only as “if this fits your situation.” Do not present an action as a conclusion of the rule. |

The current workbook's `Expected Interpretation` often includes a validation question, but it has no separate `What To Do` field and does not consistently supply all four narrative parts. Those fields need review and authoring before insight composition is implementable. Reuse authored copy and template fields; do not generate generic AI commentary from matching combinations.

Evidence and relationship direction should be visible in the card. Show contradictions or alternatives as context, not as hidden score adjustments. Ask the user to confirm or reject the hypothesis. Persisting that response requires a decision: session-only state is simplest; localStorage persistence would require a versioned feedback shape and a policy for invalidating feedback when selections or rule definitions change.

Deduplicate by Pattern ID for multiple matched rules. For distinct overlapping Pattern IDs, present each with its own evidence and relationship; do not silently merge or suppress. If similarity-based suppression is later desired, define test examples and explain the suppression to diagnostics.

## 5. Technical changes likely required

The following is an earlier candidate Maturity design, not an approved file list or a replacement for shared architecture contracts. Names and responsibilities must be reassessed against those contracts before implementation. No data-model or runtime changes are made in this planning task.

| Component/file candidate | Responsibility | Status / dependencies | Data model, configuration, or persistence |
|---|---|---|---|
| `wwwroot/data/rules/maturity_pattern_rules.json` | Store editable behavior-pattern rules and narrative separately from numeric derived-signal rules. | Existing candidate configuration; executable conditions and mapping semantics need completion and approval. | Configuration; policy for versions and provenance remains open. |
| `Models/Patterns/MaturityPatternRule.cs` and evaluation/evidence result types | Typed rule clauses, evidence provenance, rule match, pattern support state, and directional relationship. | Candidate; based on the shared and approved rule contracts. | Evaluation results may be transient; the assessment evidence must persist selected coordinate, resolved `BehaviourId` and catalogue version. Retain the referenced content and mapping. |
| `Services/Patterns/MaturityPatternRuleRepository.cs` | Load and validate pattern config and resolve its references against existing catalogue/mapping entries. | New; may reuse existing data loading conventions, but should not deserialize pattern records as `DerivedSignalRule`. | Config validation should fail explicitly for unknown IDs, impossible same-coordinate conjunctions, missing narrative, or duplicate IDs. |
| `Services/Patterns/MaturityPatternEngine.cs` | Project a session into evidence, evaluate individual rules, then aggregate by Pattern ID and compute support state. | New; depends on repository and catalogue/mapping. Keep matching and support decisions deterministic. | No database. Direct session input avoids lossy numeric aggregation. |
| `Services/Patterns/MaturityPatternInsightComposer.cs` | Compose the four authored narrative parts for supported patterns and attach evidence/validation/action fields. | New, or a small cohesive part of the engine if the final implementation stays simple. | No LLM-generated text. |
| `Pages/Hub/Diagnostics/MaturityAssessment.razor` | Present diagnostic pattern insights without duplicating the current grid and radar. | Existing; integration depends on the shared architecture and selected evaluator/composer. | The UI does not own persistence; the session model and repository must persist versioned selection evidence. |
| `Components/Debug/DiagnosticsConsole.razor` | Show evaluated rule status, source evidence, alternatives, contradictions, support decision, and suppression reason in its existing Pattern Detection section. | Existing; depends on structured evaluation trace. | Diagnostic-only; no persistence. |
| `OperationalIntelligenceHub.Tests/Tests/MaturityPatternEngineTests.cs` | Focused rule, support, contradiction, missing-data, and aggregation tests. | New; depends on approved rule fixtures. | Test-only. Add config validation tests in the same or a focused test file. |
| `AssessmentSession` / `AssessmentRepositoryService` | Persist versioned selection evidence; persist validation/rejection feedback only if separately approved. | Existing; versioned identity is required by the agreed target; feedback remains optional. | Save coordinate, `BehaviourId` and catalogue version with assessment evidence; retain the referenced catalogue content and mapping. Feedback would require staleness/version rules. |

The earlier proposal was a separate Maturity pattern engine invoked during result refresh, leaving numeric scoring unchanged. This is one option, not the agreed target: extending the existing registered `BehaviourRuleEngine` may be simpler. The full redesign replaces static confidence/priority with dynamic evaluation; sequencing and protection of Backlog Health behaviour require a separately approved migration plan. Do not force semantic patterns into `DerivedSignal` or reuse current numeric ranking as pattern confidence.

## 6. Test strategy and provisional acceptance matrix

### Per-rule positive fixtures

The workbook's `Test Scenario` descriptions are used to seed one candidate fixture per rule. The tuples below use the current application mapping IDs; new selections persist their resolved ID and catalogue version. Each fixture respects one level per lens/theme. The workbook's reference lists include multiple levels at some coordinates, so the selected level in each fixture is a proposed alternative—not a claim that the other listed levels are conjunctive requirements.

**Expected result once the rule is approved:** the named rule should report a match for its approved source combination and return the listed evidence. Pattern aggregation must then independently decide whether the pattern is sufficiently supported. Until the exact required/optional logic is approved, these are seed fixtures and their final pass/fail expectations cannot be frozen.

| Rule ID / Pattern | Proposed selected coordinates (current mapping ID in brackets) | Expected result |
|---|---|---|
| BF01-R01 / BF01 | `ProductOwner/Value=L3` [`ProductOwner_Value_L3`]; `Squad/Value=L1` [`Squad_Value_L1`] | Candidate value-translation rule match; support remains a separate decision. |
| BF02-R01 / BF02 | `Squad/Ownership=L4` [`Squad_Ownership_L4`]; `Leader/Ownership=L1` [`Leader_Ownership_L1`]; `Leader/Trust=L1` [`Leader_Trust_L1`] | Candidate accountability/authority rule match; do not infer unsupported accountability from this pair alone. |
| BF03-R01 / BF03 | `Leader/Ownership=L2` [`Leader_Ownership_L2`]; `Leader/Trust=L2` [`Leader_Trust_L2`]; `Squad/Ownership=L4` [`Squad_Ownership_L4`] | Candidate delegation/confidence rule match; do not infer motivation or confidence as fact. |
| BF04-R01 / BF04 | `ProductOwner/System=L4` [`ProductOwner_System_L4`]; `ProductOwner/Trust=L1` [`ProductOwner_Trust_L1`]; `ProductOwner/Value=L4` [`ProductOwner_Value_L4`]; `ProductOwner/Ownership=L4` [`ProductOwner_Ownership_L4`] | Candidate product-authority rule match; stakeholder confidence itself is not observed by this assessment. |
| BF05-R01 / BF05 | `ScrumMaster/Trust=L3` [`ScrumMaster_Trust_L3`]; `ScrumMaster/Ownership=L3` [`ScrumMaster_Ownership_L3`]; `Squad/Ownership=L1` [`Squad_Ownership_L1`]; `Squad/Trust=L1` [`Squad_Trust_L1`]; `Squad/Learning=L2` [`Squad_Learning_L2`] | Candidate coaching/team-behaviour rule match; support must follow approved sufficiency guidance. |
| BF06-R01 / BF06 | `ScrumMaster/Learning=L3` [`ScrumMaster_Learning_L3`]; `Leader/Learning=L1` [`Leader_Learning_L1`]; `Leader/Trust=L1` [`Leader_Trust_L1`]; `Squad/Learning=L4` [`Squad_Learning_L4`] | Candidate improvement/permission rule match; preserve leadership support as evidence, not an inferred cause. |
| BF07-R01 / BF07 | `ProductOwner/Value=L1` [`ProductOwner_Value_L1`]; `ProductOwner/Learning=L1` [`ProductOwner_Learning_L1`]; `Squad/Value=L5` [`Squad_Value_L5`]; `Squad/Learning=L3` [`Squad_Learning_L3`] | Candidate customer-value disconnect match; product prioritisation linkage needs approved semantics. |
| BF08-R01 / BF08 | `Leader/System=L1` [`Leader_System_L1`]; `ScrumMaster/System=L4` [`ScrumMaster_System_L4`]; `Squad/System=L4` [`Squad_System_L4`] | Candidate collaboration/boundary rule match; do not assume organisational boundaries without validation. |
| BF09-R01 / BF09 | `ProductOwner/Value=L5` [`ProductOwner_Value_L5`]; `ProductOwner/System=L4` [`ProductOwner_System_L4`]; `Squad/Value=L1` [`Squad_Value_L1`]; `Squad/System=L1` [`Squad_System_L1`] | Candidate strategy/delivery rule match; review the PO Value label mismatch before approval. |
| EM01-R01 / EM01 | `ProductOwner/Ownership=L4` [`ProductOwner_Ownership_L4`]; `Squad/Ownership=L1` [`Squad_Ownership_L1`] | Candidate directional expectation-projection match; direction and required evidence need approval. |
| EM01-R02 / EM01 | `Leader/Ownership=L4` [`Leader_Ownership_L4`]; `Squad/Ownership=L1` [`Squad_Ownership_L1`] | Candidate directional expectation-projection match; do not treat as equivalent to EM01-R01 without approval. |
| EM02-R01 / EM02 | `Leader/Ownership=L1` [`Leader_Ownership_L1`]; `Leader/Trust=L1` [`Leader_Trust_L1`]; `Squad/Ownership=L4` [`Squad_Ownership_L4`] | Candidate direction/autonomy mismatch match; a reversed relation is a separate test. |
| EM02-R02 / EM02 | `ScrumMaster/Ownership=L3` [`ScrumMaster_Ownership_L3`]; `ScrumMaster/Trust=L3` [`ScrumMaster_Trust_L3`]; `Squad/Ownership=L1` [`Squad_Ownership_L1`]; `Squad/Trust=L3` [`Squad_Trust_L3`] | Candidate direction/autonomy mismatch match; support depends on approved role relationship. |
| CC01-R01 / CC01 | `Squad/Ownership=L4` [`Squad_Ownership_L4`]; `Squad/Trust=L4` [`Squad_Trust_L4`]; `Leader/Ownership=L1` [`Leader_Ownership_L1`]; `Leader/Trust=L2` [`Leader_Trust_L2`] | Candidate capability/enablement match; do not infer capability from selected level alone. |
| CC02-R01 / CC02 | `ProductOwner/Value=L5` [`ProductOwner_Value_L5`]; `Leader/Value=L1` [`Leader_Value_L1`]; `Leader/System=L2` [`Leader_System_L2`] | Candidate product/organisational alignment match; review the PO Value label mismatch. |
| CC03-R01 / CC03 | `Squad/Learning=L4` [`Squad_Learning_L4`]; `ScrumMaster/Learning=L4` [`ScrumMaster_Learning_L4`]; `Leader/Learning=L2` [`Leader_Learning_L2`] | Candidate learning-bottleneck match; the selected levels do not establish why wider learning differs. |
| CC04-R01 / CC04 | `Leader/System=L2` [`Leader_System_L2`]; `ScrumMaster/System=L4` [`ScrumMaster_System_L4`]; `Squad/System=L4` [`Squad_System_L4`] | Candidate cross-team/system-enablement match; validate the interpretation. |
| SC01-R01 / SC01 | `Leader/Value=L4` [`Leader_Value_L4`]; `Leader/Ownership=L1` [`Leader_Ownership_L1`]; `Leader/Trust=L1` [`Leader_Trust_L1`]; `Squad/Value=L3` [`Squad_Value_L3`] | Candidate outcome/output-control match; do not describe output focus as inherently poor practice. |
| SC02-R01 / SC02 | `Leader/Ownership=L4` [`Leader_Ownership_L4`]; `Squad/Ownership=L4` [`Squad_Ownership_L4`]; `Leader/Trust=L1` [`Leader_Trust_L1`]; `Squad/Trust=L4` [`Squad_Trust_L4`] | Candidate autonomy/control match; the rule must explain the relationship and avoid treating role differences as contradiction by default. |
| SC03-R01 / SC03 | `Leader/Learning=L5` [`Leader_Learning_L5`]; `Leader/Trust=L1` [`Leader_Trust_L1`]; `Squad/Learning=L4` [`Squad_Learning_L4`] | Candidate learning/compliance match; whether these are actually contradictory is an approval question. |
| SC04-R01 / SC04 | `ProductOwner/Value=L5` [`ProductOwner_Value_L5`]; `ProductOwner/Trust=L1` [`ProductOwner_Trust_L1`]; `ProductOwner/Ownership=L5` [`ProductOwner_Ownership_L5`]; `ProductOwner/System=L4` [`ProductOwner_System_L4`] | Candidate product/stakeholder-control match; actual stakeholder control is not directly observable. |
| SC05-R01 / SC05 | `Leader/System=L2` [`Leader_System_L2`]; `ScrumMaster/System=L4` [`ScrumMaster_System_L4`]; `Squad/System=L4` [`Squad_System_L4`] | Candidate integrated-delivery/fragmented-leadership match; validate rather than diagnose fragmentation. |
| PR01-R01 / PR01 | `Leader/Ownership=L4` [`Leader_Ownership_L4`]; `Leader/Trust=L4` [`Leader_Trust_L4`]; `ScrumMaster/Ownership=L3` [`ScrumMaster_Ownership_L3`]; `Squad/Ownership=L4` [`Squad_Ownership_L4`] | Candidate positive ownership pattern; use the same support standard as friction patterns. |
| PR02-R01 / PR02 | `ProductOwner/Value=L5` [`ProductOwner_Value_L5`]; `Squad/Value=L5` [`Squad_Value_L5`]; `Leader/Value=L5` [`Leader_Value_L5`] | Candidate shared-value pattern; selected level similarity alone must not prove shared meaning. |
| PR03-R01 / PR03 | `ScrumMaster/Learning=L4` [`ScrumMaster_Learning_L4`]; `Leader/Learning=L5` [`Leader_Learning_L5`]; `Squad/Learning=L4` [`Squad_Learning_L4`] | Candidate continuous-learning pattern; preserve evidence for each role and any system-level qualification. |
| PR04-R01 / PR04 | `ScrumMaster/Trust=L4` [`ScrumMaster_Trust_L4`]; `ProductOwner/Trust=L3` [`ProductOwner_Trust_L3`]; `Leader/Trust=L4` [`Leader_Trust_L4`]; `Squad/Trust=L4` [`Squad_Trust_L4`] | Candidate trust-across-system pattern; do not require identical levels unless the approved rule explicitly says so. |
| PR05-R01 / PR05 | `ProductOwner/System=L4` [`ProductOwner_System_L4`]; `ScrumMaster/System=L4` [`ScrumMaster_System_L4`]; `Leader/System=L4` [`Leader_System_L4`]; `Squad/System=L4` [`Squad_System_L4`] | Candidate integrated-value-chain pattern; selected levels are evidence, not proof of end-to-end outcomes. |
| PR06-R01 / PR06 | `ScrumMaster/Learning=L3` [`ScrumMaster_Learning_L3`]; `ScrumMaster/Ownership=L3` [`ScrumMaster_Ownership_L3`]; `Squad/Learning=L4` [`Squad_Learning_L4`]; `Squad/Ownership=L4` [`Squad_Ownership_L4`]; `Leader/Learning=L4` [`Leader_Learning_L4`]; `Leader/Ownership=L4` [`Leader_Ownership_L4`] | Candidate distributed-improvement-leadership pattern; support must be distinct from the rule match. |

### Cross-cutting tests

These are required test categories. Where workbook logic is still ambiguous, the expected behavior below is the implementation invariant; rule-specific pass/fail fixtures must be finalized at approval.

| Test | Explicit input | Expected result |
|---|---|---|
| Negative: no source evidence | All coordinates relevant to a rule are level 0 or absent. | No rule match and no insight. Missing selections remain unknown, not contradictory evidence. |
| Negative: required counterpart absent | For BF01, `ProductOwner/Value=L3` [`ProductOwner_Value_L3`], while `Squad/Value=0`. | No BF01 match if the approved rule requires both roles. The test locks the required set only after workbook owners approve it. |
| Supporting evidence alone | `Squad/Ownership=L4` [`Squad_Ownership_L4`] without the approved BF02 source counterpart. | Supporting evidence is recorded, but does not independently trigger BF02. |
| Contradictory/qualifying evidence | BF01 assessment A: `ProductOwner/Value=L3` [`ProductOwner_Value_L3`], `Squad/Value=L1` [`Squad_Value_L1`]. Assessment B: `ProductOwner/Value=L3`, `Squad/Value=L3` [`Squad_Value_L3`]. | Run as two separate assessments: the same Squad/Value coordinate cannot hold L1 and L3 simultaneously. The evaluator must not combine the two fixtures or report the alternative level as simultaneous contradictory evidence. The workbook does not yet specify a distinct-coordinate contradiction pair and its expected support outcome; that is an approval gap. |
| Workbook exclusion / alternative | `Squad/Ownership=L4` [`Squad_Ownership_L4`], `Leader/Ownership=L1` [`Leader_Ownership_L1`], with no independent evidence about decision authority. | BF02 may be a rule candidate if its approved source logic matches, but the “do not assume” caveat means the pair alone must not become a supported diagnosis. No unobserved authority context is fabricated. |
| Observable exclusion | For every exclusion approved as a selectable behavior, run one fixture with and without that exact selected tuple. | The exclusion changes rule/support state only as its approved semantics specify. Prose-only context exclusions cannot be tested as behavior selections and remain a documented gap. |
| Insufficient evidence | Select only one side of a proposed relationship, such as `Leader/Ownership=L1` [`Leader_Ownership_L1`] with `Squad/Ownership=0`. | A rule may report partial evidence, but the pattern is insufficient and produces no insight. |
| Same-coordinate alternatives | Attempt to define `Leader/Ownership=L1 AND Leader/Ownership=L4` as one rule's required evidence. | Rule configuration validation rejects the impossible conjunction. A valid assessment can contain at most one selected value at that coordinate. |
| Reversed directional relationship | EM02-R01 candidate: `Leader/Ownership=L1` [`Leader_Ownership_L1`], `Leader/Trust=L1` [`Leader_Trust_L1`], `Squad/Ownership=L4` [`Squad_Ownership_L4`]. Reverse fixture: `Leader/Ownership=L4` [`Leader_Ownership_L4`], `Leader/Trust=L4` [`Leader_Trust_L4`], `Squad/Ownership=L1` [`Squad_Ownership_L1`]. | A directional rule matches only its approved direction. The reverse must not match that rule unless the rule is explicitly bidirectional; it may match a separately approved rule. |
| Overlapping patterns | Use BF02 fixture plus `Squad/Trust=L4` [`Squad_Trust_L4`] to exercise the shared evidence area with CC01. | Retain separate BF02 and CC01 evaluations and evidence. Aggregate duplicate rules only within the same Pattern ID; do not merge or suppress distinct patterns without an approved policy. |
| Positive reinforcing patterns | PR01 fixture in the positive matrix. | Trigger and support are reported using the same evidence-provenance and sufficiency rules as friction patterns. |
| Incomplete assessment | `Squad/Ownership=L4` [`Squad_Ownership_L4`], `Leader/Ownership=0`, with other themes incomplete. | Missing Leader ownership is unknown. It neither satisfies nor contradicts a rule; normal results UI remains subject to existing completion behavior. |
| Unsupported external condition | BF04 fixture with no stakeholder-response input. | Rule may state a conditional hypothesis, but it cannot claim stakeholder confidence or control as observed. |
| Narrative grounding | Any supported fixture; compare every evidence sentence to the selected coordinates and catalogue entry. | Every observation maps to selected evidence. No unselected behavior, motivation, or delivery impact is added as fact. |
| Duplicate insight | Multiple approved Rule IDs for one Pattern ID match the same assessment. | One pattern card with merged provenance and all contributing Rule IDs. |

### Whole-assessment interaction scenarios

These scenarios exercise interactions beyond single rules. Their exact triggered/support states depend on approval of each rule's required/alternative sets.

1. **Value friction with ownership context:** `ProductOwner/Value=L3` [`ProductOwner_Value_L3`], `Squad/Value=L1` [`Squad_Value_L1`], `ProductOwner/Ownership=L3` [`ProductOwner_Ownership_L3`], `Squad/Ownership=L3` [`Squad_Ownership_L3`]. The BF01 draft records a Direction A candidate; Product Owner and Squad Ownership are outside its pilot scope and must not be assigned invented contributions. Never also select `Squad/Value=L3` in this same assessment.
2. **Accountability, leadership, and trust overlap:** `Squad/Ownership=L4` [`Squad_Ownership_L4`], `Squad/Trust=L4` [`Squad_Trust_L4`], `Leader/Ownership=L1` [`Leader_Ownership_L1`], `Leader/Trust=L2` [`Leader_Trust_L2`]. Expected: evaluate BF02 and CC01 independently, preserve shared evidence, and do not let either pattern imply the other.
3. **Learning across roles with an organisational constraint:** `Squad/Learning=L4` [`Squad_Learning_L4`], `ScrumMaster/Learning=L4` [`ScrumMaster_Learning_L4`], `Leader/Learning=L2` [`Leader_Learning_L2`], `Leader/Trust=L1` [`Leader_Trust_L1`]. Expected: test the relevant friction/capability rules together; do not add a positive learning pattern that requires a different Leader/Learning level in the same assessment. A missing or contrary piece remains explicit rather than being silently averaged away.

A later workbook's Behaviour Test Inputs sheet includes one populated input row without an expected result. The matrix above is plan-authored from the rule narratives and reference crosswalk. Rule owners must approve the input sets and expected outcomes before they become executable acceptance tests.

## 7. User experience proposal

- Keep the existing Maturity result grid, radar profile, and maturity progression view as the primary assessment results. Pattern cards should add interpretation, not repeat or replace the maturity visuals or roadmap.
- Display only patterns that pass the separate support decision; keep triggered-but-insufficient rules available in diagnostics, not as user-facing insight.
- Group cards into friction/areas to explore and reinforcing patterns. Do not hide positive patterns behind improvement content, and do not use positive patterns to diminish unresolved friction.
- For each card, visually distinguish **What we observed** from **What we suspect** and **What it might mean**. Show the selected role, theme, level, catalogue headline, and relationship direction inline or as expandable evidence details.
- Show contradictory or qualifying evidence and alternative explanations clearly. Mark hypotheses with conditional language and provide a validation question.
- Present an action only after the user can indicate that the hypothesis fits. “Recognise this” and “Not in my context” are possible labels; wording and whether feedback persists require approval.
- Anchor a pattern to the relevant theme/role cells when its evidence supports precise placement. Put genuinely cross-theme patterns in a system-level group; do not use numeric signal attribution to guess anchors.
- The workbook supplies no approved priority ordering or quantitative confidence. The current top-five numeric ranking is not a justified pattern ranking. For the first release, aggregate duplicates by Pattern ID and group results without a numeric score; agree ordering and any display cap before one is added.

## 8. Implementation sequence, risks, and decisions

| Order | Work item | Dependencies | Likely files/components |
|---|---|---|---|
| 0 | Review the proposed BF01 contract, v1 compatibility, contextual effects and first test cases. | Product/content approval. | BF01 draft and this plan. |
| 1 | Implement and validate direct-evidence projection and reusable semantic evaluation, with BF01 and trace in focused tests. | Step 0; existing versioned evidence. | Maturity-specific models, services and tests; no UI or shared numeric changes. |
| 2 | Agree support eligibility and authored narrative; integrate diagnostic findings and trace into the existing results experience. | Step 1 and product decisions. | Maturity results and diagnostics. |
| 3 | Agree the diagnostic-to-platform contract and transition any existing consumers without fabricating numeric values. | Step 1; shared architecture decisions. | Adapter and platform integration, separately scoped. |
| 4 | Progressively review, define and test additional patterns without blocking BF01. | Relevant pattern decisions. | Pattern definitions and tests. |
| 5 | Design and implement Role Profiles as a separately approved future enhancement and user experience. | Profile interpretation and versioning decisions; original evidence retained. | Future profile builder, evidence and UI. |

### Risks and assumptions

- **Approval status:** the 28 rules are approved as a living catalogue despite the workbook's Proposed labels; executable semantics, copy and unresolved mappings still need review.
- **Unresolved pattern count:** 26 IDs are listed while an earlier summary says 27. Do not invent a missing pattern.
- **Catalogue semantics:** all 249 references resolve to tuples, but some workbook labels differ from current catalogue headlines. Coordinate resolution does not resolve content meaning.
- **Historical identity:** new selections persist a behaviour ID, catalogue version and content snapshot. Retained versions must remain available, and legacy sessions without provenance cannot be interpreted safely.
- **Alternative collisions:** 92 same-coordinate collisions make a literal all-references-required interpretation invalid. Treating them as alternatives is a constraint-based interpretation that must be approved per rule.
- **Unstructured logic:** free-text workbook columns do not provide executable AND/OR groups or a complete decision table.
- **Unobservable context:** authority, stakeholder behavior, capacity, motivation, and actual delivery outcomes are not assessment fields. These remain user-validation prompts.
- **Sufficiency ambiguity:** qualitative terms such as “moderate” and “stronger” do not justify scores or confidence percentages.
- **Architecture mismatch:** existing numeric rule and insight models discard semantic detail and apply a top-five ranked selection. Reusing them directly could conflate a trigger with a supported pattern.
- **Feedback staleness:** if validation feedback is persisted, edits to selections, catalogue content, or rule versions may make it stale. Define invalidation before storing it.
- **Rule overlap and clutter:** no approved priority, ordering, cap, or cross-pattern merge policy exists.
- **Flow scope:** the workbook includes a Flow Rule Register with review comments that differ from current runtime documentation. Keep those candidate changes separate and obtain Flow-specific approval; do not import them into Maturity pattern rules.

### Decisions requiring approval

1. Is an ID missing from the stated total of 27, given the 28 approved rows currently list 26 patterns?
2. Are all workbook rule references correctly mapped to the current catalogue despite label differences such as `SQ-OWN-L2` and `PO-VAL-L5`?
3. For each same-coordinate collision, which levels are alternatives, required, support, or contradiction?
4. What exact Boolean logic and direction apply to each rule? Can any relationship be bidirectional?
5. Which exclusions are observable assessment evidence, and which remain non-computable context questions?
6. What qualitative evidence sufficiency makes a matched rule eligible for an insight, without introducing unsupported scores?
7. What approved text supplies the missing “What to do” field, and when should the action be shown?
8. Should “Recognise this” / “Not in my context” feedback persist in localStorage? If so, when does it expire?
9. How should distinct overlapping patterns be grouped and ordered? Is there a display cap?
10. Is the current Flow Rule Register intended to propose changes to the runtime, or is it a diagnostic review snapshot? Which Flow-specific findings should be approved separately?

### Broader catalogue definition of done

The following is a later catalogue-wide target, not a prerequisite for the isolated BF01-first implementation.

- All approved rule references resolve to existing canonical tuples and catalogue entries; unknown or semantically unresolved references fail validation visibly.
- A selection saved against catalogue version A remains interpretable against A after its headline or definition changes in version B; reopening and re-evaluation do not silently use B. Missing or inconsistent version A is reported explicitly.
- Every approved rule has an explicit, tested positive fixture and applicable negative, contradiction, alternative/exclusion, missing-data, and directionality fixtures.
- Impossible same-coordinate conjunctions are rejected; one selected level remains the only evidence for a coordinate.
- Rule-trigger status and pattern-support status are distinct in types, diagnostics, and tests.
- Multiple rules for one Pattern ID produce one evidence-grounded result; distinct overlapping patterns are not silently merged or suppressed.
- Positive and friction patterns use the same approved sufficiency process.
- Every observation is traceable to selected assessment evidence; hypotheses, implications, and actions are authored, conditional, and visibly distinguishable.
- Incomplete data is treated as unknown; the existing result grid and radar remain useful, and any changes to numeric and Flow insight behaviour follow an approved migration and regression plan.
- The Pattern Detection diagnostics section explains matches, missing evidence, contradictions, support decisions, and suppression.
- User validation/rejection behavior and persistence policy are approved and covered by tests.
