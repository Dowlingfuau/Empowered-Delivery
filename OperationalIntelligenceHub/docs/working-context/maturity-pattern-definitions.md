# Maturity Assessment — Pattern Definitions (Working Draft)

## How to read these definitions

The definitions below condense the intent, hypothesis, example, behavioural evidence, alternative explanations, possible delivery impacts, and validation prompts in the Rule Register Review. They describe what a pattern might mean if its evidence mapping is later confirmed. They do not assert that the pattern is present, nor define executable behaviour combinations.

“Possible impact” is deliberately conditional. Delivery outcomes, intent, capability, and context cannot be inferred from selected behaviours alone.

The updated workbook supplies candidate composite behaviour references for 28 rule rows. These have been cross-checked against current assessment tuples and mapping IDs in the [pattern detection rules](./maturity-pattern-detection-rules.md). In this document, “Evidence to map” means that the intended semantic relationship, required/alternative logic, direction, and sufficiency still need confirmation; it does not mean that the workbook references have no current tuple mapping.

## Expectation mismatch

### EM01 — Expectation Projection

**Intent and hypothesis:** Explore whether one role's selected behaviours imply expectations of another role that may not match the latter's current way of working. The mismatch could leave responsibility or next steps implicit.

**Illustrative case:** A Product Owner may expect a Squad to turn broad direction into delivery goals independently, while the Squad's selections suggest it relies on more explicit direction.

**Evidence to map:** Two or more of Squad, Product Owner, Scrum Master, and Leader; any of Ownership, Trust, Value, Learning, or System. Establish the implied assumption, the expected behaviour, the relationship, and its direction (A to B, B to A, or both).

**Alternative explanations and validation:** Roles may have legitimately different responsibilities; delegation may be intentional; capability, experience, decision rights, or environmental constraints may explain the difference. Ask: *Does one role expect another to work in a way that feels natural to the first, but may not be natural or appropriate to the second?*

**Possible impact:** Unspoken responsibility assumptions, frustration, delayed decisions, stalled work, or attempts to correct behaviour without addressing expectations.

### EM02 — Direction and Autonomy Mismatch

**Intent and hypothesis:** Explore whether roles differ in how much direction, structure, or autonomy they expect for work to progress. One role may believe enough direction has been provided while another is still waiting for guidance.

**Illustrative case:** A Product Owner may expect the Squad to derive delivery goals from product direction, while the Squad's selections suggest it needs more explicit guidance.

**Evidence to map:** Product Owner, Squad, Scrum Master, and/or Leader behaviours across relevant themes. Candidate references are listed in the pattern detection rules; direction, required/alternative logic, relationship, and interaction still need approval.

**Alternative explanations and validation:** Different roles may appropriately need different levels of direction. Delegation may be understood; capability, experience, unclear decision rights, or organisational constraints may explain the apparent mismatch. Ask whether the roles agree on who shapes the next steps and how explicitly.

**Possible impact:** Waiting, duplicated effort, rework, delayed decisions, or uncertainty about who should shape the work.

## Behavioural friction

### BF01 — Value Translation Gap

**Intent and hypothesis:** Explore whether product or organisational value intent is not translating into how delivery work is understood, prioritised, or organised.

**Illustrative case:** Product Owner selections may emphasise customer or product outcomes while Squad selections suggest work is organised mainly around backlog completion or delivery measures.

**Evidence to map:** Primarily Product Owner and Squad; Value, with possible Ownership and System. Map the relationship between selected behaviours; do not infer a gap from different levels alone.

**Alternative explanations and validation:** Delivery measures may complement outcome thinking; value intent may be understood but not visible in the assessment. Ask whether the Squad can explain how current work contributes to its intended outcome.

**Possible impact:** Work completed without shared understanding of its value, difficulty adapting priorities, or weak links between product decisions and delivery goals.

### BF02 — Accountability Without Authority

**Intent and hypothesis:** Explore whether a role or team may be responsible for delivery or outcomes without the decision authority or control needed to influence them.

**Illustrative case:** Squad selections may show collective delivery responsibility while Leader selections suggest important decisions remain centralised.

**Evidence to map:** Primarily Squad and Leader; Ownership and Trust, with possible System implications. Map Squad Ownership against Leader Ownership and Trust behaviours.

**Alternative explanations and validation:** Central control may be appropriate for risk, regulation, finance, or strategy; authority may exist through channels not captured; accountability may be shared. Ask which decisions the Squad can make, which need support, and how to escalate the rest.

**Possible impact:** Blocked work, slow impediment resolution, excess escalation, frustration, or responsibility experienced as unfairly distributed.

### BF03 — Delegation Without Confidence

**Intent and hypothesis:** Explore whether responsibility is transferred while trust or confidence in independent decision-making remains constrained.

**Illustrative case:** Leaders may encourage Squad ownership while other selected behaviours suggest decisions are checked, redirected, or retained by leaders.

**Evidence to map:** Primarily Leader and Squad; Ownership and Trust. Map Leader Ownership and Trust against Squad Ownership behaviours.

**Alternative explanations and validation:** Oversight may be required by governance or risk; leaders may be coaching rather than controlling; informal delegation agreements may not appear in the assessment. Ask whether decision boundaries and escalation conditions are explicit and consistently honoured.

**Possible impact:** Repeated approvals, slower decisions, reduced initiative, or escalation of decisions teams could make.

### BF04 — Product Authority Without Stakeholder Confidence

**Intent and hypothesis:** Explore whether Product Owner decision responsibility is assigned while stakeholder influence or approval expectations continue to shape product decisions.

**Illustrative case:** Product Owner behaviours may indicate ownership of priorities while stakeholder behaviours suggest priorities remain driven by external requests or approval.

**Evidence to map:** Primarily Product Owner and Leader/stakeholders; Ownership, Trust, and Value. Map Product Owner Ownership and Value with relevant Product Owner Trust and Leader behaviours; stakeholder pathways need explicit mapping.

**Alternative explanations and validation:** Consultation does not necessarily undermine product authority; decision rights may be clear outside the assessment; selected behaviours may reflect different products. Ask who owns each product decision and how stakeholder input and priority changes are handled.

**Possible impact:** Changing priorities, conflicting direction, delayed decisions, or difficulty maintaining product focus.

### BF05 — Coaching and Team Behaviour Misalignment

**Intent and hypothesis:** Explore whether Scrum Master support or facilitation is mismatched with the Squad's needs or preferred way of operating.

**Illustrative case:** Scrum Master selections may suggest structure and guidance while Squad selections indicate a different readiness or desire for independent ownership.

**Evidence to map:** Primarily Scrum Master and Squad; Ownership, Trust, and Learning. Map Scrum Master Ownership and Trust against Squad Ownership and Learning.

**Alternative explanations and validation:** The Scrum Master may be responding to organisational requirements; the Squad may value structure; team members may need different support. Ask what the Squad wants to own and where facilitation helps.

**Possible impact:** Dependence on facilitation, disengagement, frustration, or missed opportunities to build team ownership.

### BF06 — Improvement Without Permission

**Intent and hypothesis:** Explore whether improvement is encouraged or initiated but the environment does not provide sufficient permission, time, trust, or support to act on it.

**Illustrative case:** Squad selections may show a desire to improve while leadership selections suggest limited room to experiment or change established practices.

**Evidence to map:** Primarily Squad, Scrum Master, and Leader; Learning and Trust, with Ownership. Map team Learning against Leader Learning, Trust, and Ownership.

**Alternative explanations and validation:** Safety, compliance, or operational constraints may limit experiments; teams may lack time or capability; improvement mechanisms may not be represented. Ask which changes teams can trial and where leadership support is needed.

**Possible impact:** Recurring problems, untested improvement ideas, or learning that does not change the wider system.

### BF07 — Customer Value Disconnect

**Intent and hypothesis:** Explore whether customer or user learning in delivery is not influencing product direction or prioritisation.

**Illustrative case:** Squad behaviours may indicate feedback and learning from delivered work while Product Owner behaviours suggest direction is shaped by other inputs.

**Evidence to map:** Primarily Squad and Product Owner; Value and Learning, with possible Trust. Map Squad Value/Learning against Product Owner Value/Learning and the feedback-to-decision pathway.

**Alternative explanations and validation:** Feedback may reach the Product Owner through other channels; decisions may balance customer insight with strategy, regulation, cost, or other evidence. Ask how customer evidence influences what happens next.

**Possible impact:** Missed learning, work that does not respond to customer needs, or weak feedback loops between delivery and product direction.

### BF08 — System Collaboration Versus Organisational Boundaries

**Intent and hypothesis:** Explore whether delivery roles seek cross-system collaboration while organisational structures or leadership practices reinforce boundaries.

**Illustrative case:** Squad or Scrum Master behaviours may show work across dependencies while Leader behaviours suggest separate responsibilities and decision paths.

**Evidence to map:** Squad, Scrum Master, and Leader; System, Ownership, and Trust. Map delivery-role System behaviours against Leader System and Ownership behaviours.

**Alternative explanations and validation:** Boundaries may support accountability, security, or specialist expertise; coordination mechanisms may not be captured; roles may have different scopes. Ask who can coordinate and resolve cross-boundary dependencies.

**Possible impact:** Unresolved dependencies, hand-offs, duplicated effort, or local optimisation at the expense of end-to-end flow.

### BF09 — Strategic Product Direction Versus Delivery Reality

**Intent and hypothesis:** Explore whether broad product direction or value-chain intent is difficult to translate into delivery behaviours and near-term action.

**Illustrative case:** Product Owner selections may indicate a broader view of outcomes and dependencies while Squad selections focus on its immediate backlog and commitments.

**Evidence to map:** Primarily Product Owner and Squad; System and Value, with possible Ownership and Learning. Cross-level and cross-theme relationships require validation.

**Alternative explanations and validation:** The Squad may have a deliberately narrow remit; strategic context may be communicated elsewhere; delivery focus may suit its responsibilities. Ask whether near-term goals, dependencies, and the Squad's contribution are clear.

**Possible impact:** Unclear priorities, difficulty sequencing dependencies, or delivery not visibly connected to broader outcomes.

## Capability constraints

### CC01 — Team Capability Constrained by Leadership

**Intent and hypothesis:** Explore whether Squad ownership or capability may not be fully enabled by leadership practices.

**Illustrative case:** Squad behaviours may suggest readiness for more ownership while Leader behaviours indicate constrained decision authority or trust.

**Evidence to map:** Primarily Squad and Leader; Ownership and Trust, with possible System. Map Squad Ownership against Leader Ownership and Trust. This pattern does not directly establish capability.

**Alternative explanations and validation:** The team may not want or be ready for autonomy; controls may be necessary; skills, capacity, role clarity, or dependencies may explain conditions. Ask which constraints limit the Squad and what leadership can change.

**Possible impact:** Underused capability, avoidable escalation, slower decisions, or limited ownership of outcomes.

### CC02 — Product Leadership Without Organisational Alignment

**Intent and hypothesis:** Explore whether Product Owner leadership is not consistently supported by organisational direction, decision-making, or leadership behaviours.

**Illustrative case:** Product Owner behaviours may indicate value-led product direction while Leader behaviours suggest competing priorities or limited alignment.

**Evidence to map:** Primarily Product Owner and Leader; Ownership, Value, System, and Trust. Compare Product Owner behaviours with Leader behaviours in corresponding themes.

**Alternative explanations and validation:** Strategy may legitimately change; leaders may balance portfolio or regulatory needs; alignment forums may not be represented. Ask how product priorities are sponsored and protected.

**Possible impact:** Competing priorities, repeated reprioritisation, delayed decisions, or difficulty maintaining coherent direction.

### CC03 — Organisational Learning Bottleneck

**Intent and hypothesis:** Explore whether team learning and improvement are not reinforced or acted upon at organisational level.

**Illustrative case:** Squad and Scrum Master behaviours may indicate reflection and improvement while Leader behaviours show limited support for learning beyond the team.

**Evidence to map:** Squad, Scrum Master, and Leader; Learning, with possible System and Trust. Map team Learning against Leader Learning and relevant System behaviours.

**Alternative explanations and validation:** Learning may be shared through channels not reflected; leadership may face other urgent constraints; local improvements may be sufficient. Ask which recurring issues need organisational action and how team learning can influence it.

**Possible impact:** Recurring cross-team problems, limited reuse of learning, or improvement dependent on individual effort.

### CC04 — Cross-Team Capability Without System Enablement

**Intent and hypothesis:** Explore whether teams recognise or attempt cross-team collaboration but lack organisational mechanisms or support to act across boundaries.

**Illustrative case:** Team behaviours may show awareness of dependencies while Leader behaviours suggest separate decision paths and limited coordination.

**Evidence to map:** Squad, Scrum Master, and Leader; System, Ownership, and Trust. Map delivery-role System behaviours against Leader System, Ownership, and Trust behaviours.

**Alternative explanations and validation:** Forums may already exist; dependencies may be outside the organisation's control; teams may lack mandate or capacity. Ask which cross-team issues cannot be resolved locally and who can act.

**Possible impact:** Persistent dependencies, slow cross-team decisions, hand-off delays, or systemic impediments left unresolved.

## Strategic contradictions

### SC01 — Outcome Ambition, Output Control

**Intent and hypothesis:** Explore whether outcome-oriented intent coexists with practices that control delivery activity, scope, or execution.

**Illustrative case:** Leader behaviours may encourage outcome focus while other Leader behaviours suggest close control over what is delivered and how.

**Evidence to map:** Primarily Leader; Value and Ownership/Trust, with possible Squad implications. Map Leader Value against Leader Ownership and Trust; define contradictory combinations and interpretation boundaries.

**Alternative explanations and validation:** Regulation, contracts, safety, or funding may require controls; scope discipline can coexist with outcome focus; adaptive mechanisms may be unobserved. Ask where teams can adapt while remaining accountable for outcomes.

**Possible impact:** Optimising for completion over outcomes, difficulty responding to learning, or tension between stated goals and delivery controls.

### SC02 — Autonomy Rhetoric, Centralised Decisions

**Intent and hypothesis:** Explore whether autonomy is encouraged while decision authority remains concentrated.

**Illustrative case:** Teams may be encouraged to take ownership while Leader behaviours suggest significant decisions remain centralised.

**Evidence to map:** Primarily Leader and Squad; Ownership and Trust. Map Leader Ownership against Leader Trust and Squad Ownership; specific contradictory combinations are outstanding.

**Alternative explanations and validation:** Central decisions may be appropriate for risk or strategy; delegation may be clear but not reflected in selected behaviours. Ask which decisions can move safely closer to teams.

**Possible impact:** Approval delays, unnecessary escalation, hesitation, or a gap between expected ownership and actual decision-making.

### SC03 — Improvement Culture, Compliance Behaviour

**Intent and hypothesis:** Explore whether improvement intent coexists with behaviours or expectations that reinforce compliance with prescribed practices.

**Illustrative case:** Leaders may encourage improvement while selected behaviours also emphasise following established practices with little room to adapt.

**Evidence to map:** Primarily Leader, with Squad and Scrum Master; Learning, Trust, and Ownership. Map Leader Learning against Leader Ownership/Trust and team Learning; validate the contradictions.

**Alternative explanations and validation:** Compliance may be essential for safety, regulation, or quality; improvement may be possible within defined constraints. Ask what is mandatory and where safe experimentation is possible.

**Possible impact:** Superficial improvement, reluctance to challenge ineffective practices, or lessons that do not change ways of working.

### SC04 — Product Strategy, Stakeholder Control

**Intent and hypothesis:** Explore whether strategic product leadership coexists with stakeholder influence or control over priorities and decisions.

**Illustrative case:** Product Owner behaviours may indicate broad product direction while stakeholder-related behaviours suggest individual requests or approvals dominate.

**Evidence to map:** Primarily Product Owner; Ownership, Value, and Trust, with Leader context. Map Product Owner Ownership/Value against Product Owner Trust and relevant Leader behaviours; precisely map stakeholder control.

**Alternative explanations and validation:** Stakeholder input may be appropriate; the Product Owner may balance portfolio, customer, or regulatory needs; formal governance may be unobserved. Ask how input is weighed and who makes final prioritisation decisions.

**Possible impact:** Competing priorities, fragmented product direction, repeated reprioritisation, or unclear trade-offs.

### SC05 — Integrated Delivery, Fragmented Leadership

**Intent and hypothesis:** Explore whether delivery roles collaborate across boundaries while leadership behaviours reinforce organisational separation.

**Illustrative case:** Teams may coordinate around dependencies and shared outcomes while leadership decisions and priorities remain fragmented.

**Evidence to map:** Squad, Scrum Master, and Leader; System, Ownership, and Trust. Map delivery-role System behaviours against Leader System, Ownership, and Trust behaviours.

**Alternative explanations and validation:** Specialised structures may be intentional; governance or product structures may coordinate effectively; separate accountability need not prevent integration. Ask how leaders coordinate cross-boundary priorities and decisions.

**Possible impact:** Unresolved dependencies, conflicting priorities, slow decisions, or integration relying on informal relationships.

## Positive reinforcement

### PR01 — Empowered Ownership

**Intent and hypothesis:** Explore whether Squad ownership is supported by leadership trust and Scrum Master facilitation.

**Illustrative case:** The Squad takes shared responsibility while leaders support team decisions and the Scrum Master facilitates rather than directs.

**Evidence to map:** Squad, Leader, and Scrum Master; Ownership and Trust, with possible Learning. Map Squad Ownership with Leader Ownership/Trust and Scrum Master Ownership.

**Alternative explanations and validation:** Ownership may be uneven; autonomy may coexist with constraints; selected behaviours do not prove effectiveness or absence of friction. Ask which practices enable ownership and how to preserve them.

**Possible impact:** Clearer accountability, timely local decisions, and reduced reliance on external direction, if the conditions are present.

### PR02 — Shared Value Orientation

**Intent and hypothesis:** Explore whether Product Owner, Squad, and Leader behaviours align around value and outcomes.

**Illustrative case:** Product priorities, leadership direction, and Squad delivery may all connect to meaningful outcomes.

**Evidence to map:** Product Owner, Squad, and Leader; Value, with possible System and Learning. Map Value behaviours across these perspectives and define what demonstrates shared orientation.

**Alternative explanations and validation:** Roles may use the same value language but mean different things; shared language may conceal trade-offs or disagreement. Ask whether the roles agree on intended outcomes and how work contributes.

**Possible impact:** Coherent prioritisation, clearer delivery goals, and a stronger connection between work and intended outcomes.

### PR03 — Continuous Learning System

**Intent and hypothesis:** Explore whether learning and improvement across Squad, Scrum Master, and Leader are mutually supportive and extend beyond the team.

**Illustrative case:** The Squad adapts, the Scrum Master helps turn learning into improvement, and leadership supports changes beyond the team.

**Evidence to map:** Squad, Scrum Master, and Leader; Learning, with System and Trust. Map Learning behaviours across roles, including System behaviours where learning spreads.

**Alternative explanations and validation:** Learning activities may not change practice; leadership support may be episodic; sustained results may be unobserved. Ask how useful learning is shared and acted on outside the team.

**Possible impact:** Recurring improvement, shared learning, and earlier resolution of issues affecting multiple teams.

### PR04 — Trust Across the Delivery System

**Intent and hypothesis:** Explore whether trust-related behaviours across roles support openness, collaboration, and appropriate autonomy.

**Illustrative case:** Roles can raise concerns, challenge assumptions, discuss trade-offs, and make decisions within agreed boundaries.

**Evidence to map:** All four perspectives; Trust, with Ownership and System. Define reinforcing relationships without requiring every role to show the same behaviour or level.

**Alternative explanations and validation:** Trust may vary by topic or relationship; selected behaviours may not reflect everyone; openness does not establish authority or follow-through. Ask which practices make it safe and useful to raise concerns and act.

**Possible impact:** Earlier issue visibility, constructive challenge, faster resolution, and more effective collaboration.

### PR05 — Integrated Value Chain

**Intent and hypothesis:** Explore whether System behaviours across roles show shared awareness of dependencies, end-to-end flow, and broader outcomes.

**Illustrative case:** Roles consider how work moves across teams and how local decisions affect the wider value chain.

**Evidence to map:** All four perspectives; System, with Value and Ownership where relevant. Define combinations that show shared awareness and coordination.

**Alternative explanations and validation:** System awareness may be conceptual rather than actionable; roles may understand dependencies but have conflicting priorities; assessment evidence does not establish actual flow performance. Ask how shared awareness informs decisions and sequencing.

**Possible impact:** Earlier dependency management, coherent sequencing, and attention to end-to-end outcomes.

### PR06 — Distributed Improvement Leadership

**Intent and hypothesis:** Explore whether team-led improvement is supported by Scrum Master facilitation and leadership enablement.

**Illustrative case:** The Squad identifies improvements, the Scrum Master helps develop its approach, and leadership enables changes requiring broader support.

**Evidence to map:** Squad, Scrum Master, and Leader; Ownership and Learning, with System and Trust. Map Squad Ownership/Learning with Scrum Master facilitation and Leader Learning/Trust/System behaviours.

**Alternative explanations and validation:** Improvement may depend on a few individuals; support may not extend to all teams; changes may not be sustained or shared. Ask how to preserve the balance between team-led improvement and leadership enablement.

**Possible impact:** More frequent local improvements, stronger team ownership of change, and broader adoption of useful practices where appropriate.
