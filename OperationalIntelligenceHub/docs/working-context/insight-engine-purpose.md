# Insight Engine — Purpose and High-Level Model

## Purpose

The Insight Engine interprets diagnostic signals and accumulated evidence to generate relevant, actionable insights that help users understand how their delivery system behaves and what they might do next.

## How it works

### 1. Diagnostic-specific insights
Each diagnostic produces signals in its own way and may produce different types of signals. The Insight Engine interprets these signals to generate insights relevant to that diagnostic.

### 2. Cross-diagnostic and historical insights
As users complete more diagnostics and build a history of evidence, the engine analyses information across diagnostics, teams and time to identify relationships, patterns and potential underlying causes.

### 3. Prioritisation
Generated insights are prioritised so the experience can focus attention on what matters most.

### 4. Contextual rendering
Insights are surfaced when relevant to the current screen, activity and available evidence. The existence of an insight does not mean it should be displayed everywhere.

For example, a Backlog Ageing Risk insight should appear in a Maturity Assessment only when backlog evidence meaningfully helps interpret the maturity evidence in that context.

### 5. Guidance and next steps
Insights can connect users to relevant Plays, Perspectives, articles or other resources that help them understand possible next steps.

### 6. Available evidence
The engine can use currently entered, unsaved diagnostic data as well as saved and historical evidence. Unsaved inputs must remain distinguishable from completed assessments and retained history.

### 7. Rule-driven triggering
Backlog Health insights come from numeric rules in `flow_rules_v3.json`; system-level insights come from `system_rules.json`. A rule can carry different interpretation text per condition, and the engine prefers that text over the rule-level text. Each insight condition should have one rule, so duplicate representations (such as the removed `FlowDisruption`) are avoided. See [backlog-health-flow-rules.md](./backlog-health-flow-rules.md).

## Core principle

**The Insight Engine determines what the evidence means. Prioritisation determines what matters most. Contextual rendering determines what the user sees, where and when.**

The experience connects that understanding to useful next steps.