# Insight Engine — Purpose and High-Level Model

**Status:** This document specifies the agreed target architecture, not a claim that the current application implements it. Current runtime constraints are noted below; detailed design, evaluation policies and persistence remain unresolved.

## Purpose

The intelligence architecture helps users understand their delivery system and consider practical next steps. Diagnostic intelligence interprets each diagnostic in its own context; Operational MRI organises and evaluates intelligence across scopes and operational dimensions. A central Insight Engine may provide shared evaluation capabilities without owning every diagnostic's interpretation.

## How it works

### 1. Diagnostic-specific insights
Diagnostic intelligence interprets evidence within an individual diagnostic, understands its measures, behaviours and limitations, and produces diagnostic-specific findings and insights with their underlying evidence and provenance. Shared capabilities may help evaluate or compose these results, but the central engine should not need to understand each diagnostic's internal input structure or own its interpretation.

### 2. Cross-diagnostic and historical insights
Operational MRI consumes diagnostic insights and their underlying evidence, measurements, signals and derived signals. It connects relevant information across diagnostics, teams and time, evaluates relationships between operational conditions, and can corroborate, challenge or contextualise diagnostic interpretations or propose broader operating-system hypotheses. These remain conditional rather than established causes. Historical and cross-diagnostic capabilities are target architecture, not a claim that they exist today.

### Intelligence scope and operational dimensions

**Foundational target principle:** Operational MRI interprets delivery environments across multiple intelligence scopes and five common operational dimensions. Scope describes the breadth of information and relationships considered; dimensions describe the subject of an insight. They are independent axes.

| Intelligence scope | Evidence context |
|---|---|
| Single diagnostic | One diagnostic and its immediate evidence and measurements; no cross-diagnostic, historical or cross-team context. The diagnostic owns its interpretation. |
| Team system | Relationships and patterns within one team's operating environment, using multiple diagnostics and historical information. |
| Multi-team system | Patterns, constraints and relationships across teams within a business unit or wider organisational area, using diagnostics and history. |

Scope is not an evidence-strength or priority rating. A diagnostic insight does not become a team-system insight merely because another diagnostic exists; actual relevant relationships and available evidence must be evaluated. Missing history or other inputs must be explicit, not invented.

Single diagnostic is a scope describing breadth, not ownership: the diagnostic generates and interprets its own insight. MRI may classify, organise, contextualise and present that insight without regenerating it or taking over its diagnostic interpretation. An MRI system insight requires separate evaluation of relevant relationships across diagnostic insights and underlying evidence.

The operational dimensions are **Clarity, Performance, Leadership, Culture and Foundation**. Operational MRI owns this shared classification taxonomy; individual diagnostics do not have to understand or assign dimensions. MRI can associate a diagnostic insight or system insight with one or more dimensions and optionally identify a primary one. Classification does not change diagnostic ownership, determine scope or create new evidence. These operational dimensions are not Maturity Assessment lenses, which denote the role perspectives Scrum Master, Product Owner, Squad and Leader. See the [Operational Dimension Model](./lens-model-spec.md). For example, a single-diagnostic backlog prioritisation insight might be associated with Foundation, while a team-system hypothesis about stakeholder intervention and ownership might relate to Leadership. Such examples are illustrative, not assertions about actual conditions.

Dimensions may contain a flexible hierarchy of categories and elements at varying depths, not a fixed three-level schema. Examples include Performance → Metrics → Flow Time and Leadership → Product Leadership → Value Alignment; these are illustrative taxonomy entries, not implemented definitions. MRI presentation may group and visualise insight associations across this hierarchy. They are classification and navigation, not five separate intelligence engines or five silos; intelligence can reason across dimensions.

An **organisational domain** is an organisational unit such as a business unit or tribe, not a named intelligence layer. An **intelligence scope** describes breadth; an **operational dimension**, **dimension category** and **dimension element** identify a location in the subject taxonomy; an **insight** is an interpretation of evidence at a particular scope. “Domain Intelligence” is retired as the name of a processing layer. The taxonomy model, assignment policy, scope boundaries in partial-data cases and organisational access are implementation decisions.

### 3. Prioritisation
Generated insights are prioritised so the experience can focus attention on what matters most.

### 4. Contextual rendering
Insights are surfaced when relevant to the current screen, activity and available evidence. The existence of an insight does not mean it should be displayed everywhere.

For example, a Backlog Ageing Risk insight should appear in a Maturity Assessment only when backlog evidence meaningfully helps interpret the maturity evidence in that context.

### 5. Guidance and next steps
Insights can connect users to relevant Plays, Perspectives, articles or other resources that help them understand possible next steps.

### 6. Available evidence
The engine can use currently entered, unsaved diagnostic data as well as saved and historical evidence. Unsaved inputs must remain distinguishable from completed assessments and retained history.

### 7. Current rule-driven triggering
Backlog Health insights come from numeric rules in `flow_rules_v3.json`; system-level insights come from `system_rules.json`. A rule can carry different interpretation text per condition, and the engine prefers that text over the rule-level text. Each insight condition should have one rule, so duplicate representations (such as the removed `FlowDisruption`) are avoided. See [backlog-health-flow-rules.md](./backlog-health-flow-rules.md).

The Maturity Assessment semantic engine is a diagnostic-owned path separate from the current numeric pipeline. Its configurable definitions can evolve without rewriting the evaluator; BF01 is the first independently testable pilot, not yet integrated into the application results flow. The 28 workbook rules, covering 26 pattern IDs, remain a living catalogue. New selections persist their behaviour ID, catalogue version and content snapshot; legacy selections without provenance are not automatically migrated. Some workbook labels differ from the catalogue. See the [pattern catalogue](./maturity-pattern-catalogue.md), [definitions](./maturity-pattern-definitions.md), [detection rules](./maturity-pattern-detection-rules.md), and [implementation plan](./maturity-pattern-implementation-plan.md).

Initially the Maturity engine acquires and projects original selected evidence, evaluates configurable contributions and relationships, and produces candidates, trace and, subject to eligibility, diagnostic findings. A future deterministic Role Profile Builder will synthesise each role's five selected behaviours into its current state, characteristics and growth focus. Role Profiles are planned as both a dedicated user experience and an additional interpreted evidence source; they are not required for the initial engine. Original evidence and its provenance remain authoritative and accessible. The engine may later evaluate relationships between roles using both profiles and original evidence; a profile does not create a tension within its own role. No Role Profile models, UI or rules are part of the initial build.

## Agreed target: evidence-led intelligence

Preserve original inputs, selected behavioural evidence, measurements, distributions and supporting data, signals, optional normalised values, derived signals, diagnostic insights, evidence relationships, provenance and evaluation history. Consumers may use appropriate projections, but must be able to access the underlying information and its origin. Retention and access policies remain to be decided.

Evidence is the underlying observation; a signal is a standardised observable dimension. Either can be evaluated directly or together. Semantic behavioural selections need not become numeric signals. Normalisation is optional and serves a particular consumer; retain raw and calculated values alongside it. A common numeric range does not make different concepts comparable.

Diagnostics retain ownership of their diagnostic-specific interpretations and insights. Operational MRI consumes those insights **and** their underlying evidence, measurements, signals and derived signals. It may corroborate, challenge or contextualise a diagnostic insight, relate operational conditions, or propose a new system hypothesis. An additional diagnostic does not automatically confirm anything. Relationship-aware intelligence considers role responsibilities and structural significance; it is not merely an average of role scores. These are responsibility boundaries, not a requirement for separate services; how they are implemented is open.

### Definitions and evaluation

Definitions describe identity, relevant evidence and signals, required and supporting evidence, roles, domains, relationships, dependencies, conditions and possible implications. The intended Maturity JSON also owns editable narrative and action text. Definitions must not prescribe a fixed confidence or priority. Evaluation determines whether to generate or suppress an insight, what supports or challenges it, evidence strength, confidence, significance, priority and whether it is provisional or sufficiently supported. A rule trigger alone does not establish sufficient support.

Different rule problems use distinct schemas and evaluation strategies: Backlog Health's numeric flow rules evaluate signals and thresholds, while Maturity patterns evaluate semantic combinations of selected behaviours, relationships and alternatives. Evidence and provenance, evaluation outcomes, subsequent intelligence processing and traceability can be shared without forcing those strategies into one rule model or requiring separate services. For an eligible candidate, evaluate support and relationships, reliability and evidence strength, then confidence and priority before composing and deciding presentation; this is a logical ordering, not a mandated deployment topology.

Evidence relationships must distinguish **supporting**, **challenging**, **contextual**, **missing/unavailable**, **not evaluated**, and **related but insufficiently relevant**. Trace relationships to their sources. Derived outputs sharing a measurement or dependency are not independent corroboration merely because there are several outputs. Re-evaluation may change, retain or challenge an interpretation, or create a new hypothesis.

### Reliability, evidence strength and confidence

These are distinct:

1. **Measurement reliability:** validity, diagnostic-specific completeness, sample size, observation period, consistency and whether the value is measured or estimated.
2. **Evidence strength:** relevance and completeness of support, challenge and context, including independence and missing evidence.
3. **Insight confidence:** how well the interpretation is supported, considering both of the above and the quality of the relationships.

A reliable measurement can support an uncertain interpretation; new evidence may strengthen an interpretation without changing the original measurement's reliability. Diagnostics should assess quality according to required versus optional inputs and their evidential importance, not a generic count of fields; the central engine need not understand each diagnostic's input structure. For example, complete relevant Backlog Age inputs can provide stronger evidence than partial inputs. Confidence is re-evaluated as relevant evidence changes and may rise, fall or remain unchanged. Represent insufficient evidence explicitly rather than inventing certainty. No confidence formula, percentage or threshold is approved.

### Significance and priority

Priority asks how important it is to investigate or respond; confidence asks how well an interpretation is supported. Consider potential severity and consequences, delivery impact, systemic influence, dependencies, persistence or worsening over time, reach, urgency, confidence and the value of investigation. Confidence informs priority but is not a simple multiplier: low confidence may justify lower priority, or high potential consequences or urgency may justify priority despite low confidence. No weights, formulas or thresholds are approved.

For example, a potentially oversized backlog may have low confidence because throughput and age evidence are insufficient. A Product Owner Ownership Level 1 selection might change the context or urgency of investigating whether the issue can be addressed, without validating the backlog interpretation or establishing a lack of ownership as fact. The relevance and direction of that relationship require evaluation and user validation. Evidence can affect priority without changing confidence, and either can rise, fall or remain unchanged independently.

### Presentation and trace

Users receive evidence-grounded observations, conditional hypotheses, possible implications, validation questions and practical next steps. Internal confidence and priority values or labels are for development and validation in the Diagnostics Console, not automatically user-facing scores. Do not duplicate maturity progression or roadmap views.

The target shared **Intelligence Trace** contract covers the whole diagnostic and intelligence lifecycle, not only signals. Diagnostic and intelligence pipeline stages produce diagnostic-agnostic records with diagnostic-specific detail as needed; the console renders recorded data rather than interpreting it. The existing `SignalTraceService` name and limited implementation may remain unchanged; no rename is implied. Distinguish no evidence, not evaluated, evaluated with no match, support, challenge, insufficient evidence and failure; absence is not a negative result. See the contract and lifecycle in [debug-diagnostic-console.md](./debug-diagnostic-console.md).

### Current implementation and open decisions

The active `OperationalInsightEngine` currently ranks using intensity × configured priority × confidence weight and selects at most five; those static configuration values are current behaviour, **not** the target. An independently testable Maturity semantic evaluator loads and executes the separate BF01 draft, but is not connected to the page or platform pipeline; the registered `BehaviourRuleEngine` is not called by the main pipeline. Team Health signal building and insight generation are not implemented. Existing shared traces are currently session-only and omit free-text notes; the semantic evaluator returns its own structured trace, and the complete shared lifecycle above is future work.

Open decisions include evidence and relationship contracts, diagnostic-specific quality assessment, qualitative or other justified evaluation methods, provenance independence and history retention, confidence/priority revision and explanation, cross-diagnostic integration, user feedback, ordering and suppression. These require representative examples, ownership and approval before implementation; none should be filled with invented numerical rules.

## Core principle

**Diagnostic intelligence interprets diagnostic evidence; Operational MRI evaluates broader relationships. Shared evaluation can inform what matters most, while contextual rendering determines what the user sees, where and when.**

The experience connects that understanding to useful next steps.