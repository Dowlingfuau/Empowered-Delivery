# Backlog Health Diagnostic

## Working Specification — v1
**Status:** Working agreement
**Scope:** Diagnostic inputs, calculations, signal outputs, visual presentation, and overall health assessment.
**Out of scope:** Derived insights, cross-diagnostic interpretation, and Insight Engine rules. These will be addressed separately.

---

## 1. Purpose
The Backlog Health Diagnostic provides an evidence-based snapshot of how work is being managed within a delivery system.

It examines five dimensions:

1. Backlog Size
2. Backlog Age
3. Backlog Volatility
4. Backlog Prioritisation
5. Delivery Predictability
The diagnostic should help someone answer:

- What is happening in this backlog?
- Should I be concerned about it right now?
- If so, what is contributing to that condition?
The diagnostic describes observable evidence. It must not make unsupported claims about the team, its behaviours, or its longer-term operating model.

### Core communication principle
Interpretations must be bounded by the period and evidence being assessed.

For example:

"Looking at your backlog over the last six weeks, we can see..."

Not:

"Your team consistently struggles to manage its backlog."

The first describes the available evidence. The second makes a broader claim that the diagnostic may not be able to support.

---

## 2. Diagnostic inputs
The initial implementation uses manual data entry.

Users should be able to obtain the information from their normal work management system, such as Jira or Azure DevOps.

### 2.1 Backlog Size
**Inputs**

- Total backlog items.
- Throughput observations, measured as items completed over delivery periods and labelled "Under 2 weeks", "2–4 weeks", and "Over 4 weeks".
**Calculation**

Weeks of Work = Total Backlog Items ÷ Average Weekly Throughput

The diagnostic uses week-based wording rather than sprint labels. The observation duration behind each throughput value still needs to be confirmed: the current implementation averages the entered counts, so the resulting weeks-of-work estimate is reliable only when those values represent comparable, known periods.

A future enhancement may allow users to select a measurement unit, such as weeks, sprints, or another delivery period.

**Minimum data required**

- Total backlog items.
- At least one throughput observation.
Until sufficient data is entered, the signal remains in its no-data state.

### 2.2 Backlog Age
**Inputs**

Users provide the number of backlog items in three age ranges:

- Less than 2 weeks.
- 2–4 weeks.
- More than 4 weeks.
The assessment describes the distribution over the observed six-week period.

**Important limitation**

The diagnostic does not currently collect item-level update history or average interaction recency.

That information would help distinguish old work that is actively maintained from old work that has been neglected. However, collecting it manually would impose too much effort.

A future API integration may make this information available with little additional user effort.

**Minimum data required**

A complete age distribution for the items being assessed.

If no age data is entered, display "No age data". Do not interpret missing data as zero aged items.

### 2.3 Backlog Volatility
**Inputs**

- Items added during the observation period.
- Items completed during the same period.
- Current backlog item count.
The observation period must be consistent across the added and completed counts.

Starting Backlog = Current Backlog − Items Added + Items Completed

**Purpose**

The signal communicates both:

- Direction: whether the backlog is shrinking or growing.
- Magnitude: how far and how quickly it is changing.
These are separate pieces of information and must remain separately available to the Insight Engine.

### 2.4 Backlog Prioritisation
**Inputs**

- Total backlog items.
- High-priority items.
- Medium-priority items.
- Low-priority items.
Users are not asked to enter unprioritised items separately.

**Derived input**

Unprioritised Items = Total Items − High Priority − Medium Priority − Low Priority

This is intentional. It avoids asking users to provide information that can be derived from the other inputs.

The calculation evaluates both the coverage of prioritisation and the distribution of priority levels. It measures how clearly priorities are expressed, not whether the decisions themselves are correct.

### 2.5 Delivery Predictability
**Inputs**

Historical throughput observations, labelled "Under 2 weeks", "2–4 weeks", and "Over 4 weeks".

The diagnostic uses these observations to assess variation in delivery output.

The number of observations matters. A small sample should not be presented as if it establishes a reliable pattern.

---

## 3. Signal calculations and interpretation

### 3.1 Backlog Size
**What it measures**

The amount of work currently held in the backlog relative to the team's delivery capacity.

**Calculation**

Weeks of Work = Total Backlog Items ÷ Average Weekly Throughput

**Interpretation**

The result indicates the approximate amount of delivery capacity represented by the current backlog.

The signal's continuum communicates the two extremes:

- Left: Starvation Risk — insufficient work to sustain flow.
- Centre: Balanced — a manageable planning horizon.
- Right: Overload Risk — a backlog that represents a large amount of future work.
The marker position is based on the normalised result, not a judgement about whether a particular backlog size is universally correct.

**Edge case**

If the calculated value exceeds the continuum's upper bound, the marker remains within the visual boundary. The underlying calculated value must not be discarded or artificially reduced.

### 3.2 Backlog Age
**What it measures**

The distribution of work by age, indicating how much of the backlog is relatively new, moderately aged, or older.

**Calculation**

Total Items = Fresh Items + Mid-Age Items + Old Items

Fresh % = Fresh Items ÷ Total Items

Mid % = Mid-Age Items ÷ Total Items

Old % = Old Items ÷ Total Items

Distribution Distance = |Fresh % − 0.2| + |Mid % − 0.6| + |Old % − 0.2|

Age Score = 1 − (Distribution Distance ÷ 1.6)

The score is highest for a distribution close to 20% fresh, 60% mid-age, and 20% old. This is the implemented reference distribution; it is not a universal target for every backlog.

**Continuum mapping**

Bias = Fresh % − Old %

- Fresh % greater than Old %: direction is left (under-ripe).
- Old % greater than Fresh %: direction is right (over-ripe).
- Equal Fresh % and Old %: direction is centred.

The marker uses both this direction and a magnitude derived from the age score and the absolute bias. It is a visual mapping of the implemented score and direction, not a second age score.

Continuum Magnitude = Age Score × (1 − |Bias|)

The shared gauge uses this magnitude as distance-from-centre and `Direction` to choose the side. Equal fresh and old percentages therefore place the marker at the centre.

**Shape**

The implementation classifies the distribution as `Symmetric`, `SkewLeft`, `SkewRight`, or `Bimodal`. The classification uses the fresh, mid-age, and old percentages. The diagnostic should describe these observed proportions without inferring why the distribution occurred.

**Data limitation**

The current manual-input model does not apply a recency-of-interaction adjustment. A distribution is available only when all three bucket counts are supplied and their total is greater than zero. An incomplete distribution is displayed as "Incomplete age data" and receives a neutral continuum position rather than being scored as if missing buckets were zero.

**Interpretation**

The continuum represents the balance between:

- Under-ripe: work may be too new to have been sufficiently understood or refined.
- Over-ripe: work may have remained in the backlog long enough to become stale or lose relevance.
The interpretation must refer to the observed age distribution, not assume why items have aged.

**Future consideration**

Average interaction recency may be introduced when data can be retrieved directly from a connected work management system.

### 3.3 Backlog Volatility
**What it measures**

The direction and magnitude of change in the backlog over the observation period.

**Calculation**

Net Change = Items Added − Items Completed

Percentage Change = Net Change ÷ Items at the Start of the Period × 100

The implementation derives the starting count from the current count and net change. If the derived starting backlog is zero, percentage change is undefined; the signal still reports direction and places non-zero movement at the corresponding continuum endpoint.

**Interpretation**

- Negative net change: backlog is shrinking.
- Positive net change: backlog is growing.
- Zero net change: backlog is unchanged.
The continuum communicates direction and magnitude:

- Left: shrinking.
- Centre: little or no net change.
- Right: growing.
The marker's distance from the centre reflects the magnitude of change.

Example:

"Backlog is shrinking rapidly.

Net -30 items (-300% change) over the observed period."

Direction and magnitude must remain separate outputs. A shrinking backlog is not automatically healthy, and a growing backlog is not automatically unhealthy.

### 3.4 Backlog Prioritisation
**What it measures**

How clearly and meaningfully priority is expressed across the backlog.

The diagnostic is deliberately agnostic about the prioritisation method. It can work with priority categories, ordering, or equivalent structures, provided the inputs can be represented consistently.

**Derived percentages**

High % = High Priority Items ÷ Total Items

Medium % = Medium Priority Items ÷ Total Items

Low % = Low Priority Items ÷ Total Items

Unprioritised % = Unprioritised Items ÷ Total Items

Unprioritised Items are derived by elimination, as defined in the input model.

**Calculation**

The preferred calculation is the implemented approach rather than the original design-document formula.

The implementation's precise weighting and calculation must be confirmed and documented against the current code before this section is treated as final.

**Interpretation**

The continuum represents two different forms of unclear prioritisation:

- Left: No Priorities — work is not visibly prioritised.
- Right: No Distinction — too much work is treated as high priority.
The signal measures visibility and differentiation, not the correctness of priority decisions.

### 3.5 Delivery Predictability
**What it measures**

The consistency of delivery output across observed delivery periods.

The current calculation uses throughput variation, including the coefficient of variation:

Average Throughput = Mean of Throughput History

Standard Deviation = Standard Deviation of Throughput History

Coefficient of Variation = Standard Deviation ÷ Average Throughput

Predictability Score = 1 − Coefficient of Variation

The score is bounded below at zero.

**Pattern output**

Pattern presentation must account for the number of observations.

- Fewer than 3 observations: do not render the delivery-pattern label or classification.
- 3 or more observations: determine the supported pattern.

At least two positive-mean observations are required to calculate a meaningful variation score. With fewer than two observations, report that variation is not yet available rather than implying the delivery is predictable. With two observations, the score and observed variation may be shown, but the pattern classification remains hidden.

The observed variation can still be communicated.

For example:

"Delivery varied between 10 and 30 items across the observed periods. There are not yet enough observations to identify a delivery pattern."

**Interpretation**

Low predictability should contribute negatively to the overall health assessment. It is not simply a confidence reduction.

The diagnostic must distinguish the calculated predictability score from the pattern classification.

---

## 4. Continuum and gauge behaviour
Each signal should communicate its result visually and in plain language.

The gauge is not a substitute for the calculation. It is a representation of the result.

The presentation should include:

- Signal name and explanatory context.
- Gauge or continuum.
- Calculation method.
- Actual calculated result.
- Plain-language interpretation.
The interpretation should explain what the result means without repeating the formula.

The continuum must represent the specific dimension being measured. Different signals have different meanings, directions, and desirable ranges. They should not be treated as interchangeable scores.

---

## 5. Overall Backlog Health

### Purpose
The overall result should answer one question:

**Should I worry about this right now?**

If the answer is yes, the diagnostic should explain why.

The overall result is a snapshot based on the available evidence. It should not imply a permanent or universal judgement about the team.

### Calculation principles

- The overall result is derived from the five diagnostic signals.
- Unequal weighting is intentional.
- Delivery Predictability and balanced Volatility should have more influence than ideal Backlog Size.
- The calculation must account for the fact that the signals have different meanings and directions.
- The individual signal results must remain visible so users can understand what contributes to the overall result.

### Insight-based adjustments
A second adjustment based on insight categories (such as risk, positive, or constraint) should not be applied unless an independent reason for doing so is established.

The underlying calculations already contribute to the overall result and can also trigger insights. Applying another adjustment risks double-counting the same evidence.

### Presentation
The existing overall continuum is retained for now.

However, the primary communication should focus on the immediate health question and the reasons behind the result, rather than asking users to interpret another abstract continuum.

---

## 6. Missing and incomplete data
Missing data must never be silently interpreted as zero.

The diagnostic must distinguish between:

**No data entered**

- Inputs remain empty.
- Display `—` rather than zero.
- Provide context explaining what information is required.
**Limited data entered**

- Display the available result only where it can be calculated meaningfully.
- Explain what information is missing.
- Avoid presenting incomplete evidence as a complete assessment.
**Sufficient data entered**

- Display the normal calculation and interpretation.

### Initial continuum behaviour
Until enough data is available to produce a meaningful result, the continuum marker remains centred.

The centre position is a neutral visual placeholder. It must not be presented as a calculated balanced or healthy result.

For Backlog Size, sufficient data requires total backlog items and at least one throughput input.

For Backlog Age, no age data should produce the explicit message "No age data".

For Delivery Predictability, do not render a pattern label or classification with fewer than three observations. When at least two observations are available, continue to show the measured variation.

---

## 7. Signal outputs and Insight Engine boundary
The diagnostic produces structured signals. It does not independently determine broader system-level meaning.

The five signals are:

- `BacklogSize`
- `BacklogAge`
- `BacklogVolatility`
- `BacklogPrioritisation`
- `DeliveryPredictability`
`DeliveryPredictability` is the canonical name and must be used consistently in rules, traces, tests, and outputs.

The diagnostic should preserve the underlying evidence needed for further interpretation.

In particular:

- Backlog Volatility must expose direction and magnitude separately.
- Delivery Predictability must expose observed variation, observation count, and pattern output.
- Backlog Age must preserve its age distribution.
- Backlog Prioritisation must preserve the priority distribution.
- Backlog Size must preserve its calculated weeks of work and throughput basis.
The Insight Engine can then use these signals in combination with other diagnostic evidence.

---

## 8. Deferred capabilities
The following are deliberately not part of the current manual-input scope:

- API integration with Jira, Azure DevOps, or other work management systems.
- Item-level age and interaction history.
- Average interaction recency.
- Automated detection of cyclical work creation patterns from richer historical data.
- Alternative Backlog Size units such as sprints or story points.
- Broader cross-diagnostic insight interpretation.
These should not be introduced as requirements for the current diagnostic implementation.

---

## 9. Approved implementation alignment

The alignment plan was approved. The current implementation follows these confirmed decisions:

- The original three throughput ranges are retained for now: under 2 weeks, 2–4 weeks, and over 4 weeks.
- The implemented Backlog Age formula and mapping are the source of truth. Incomplete or empty age distributions are unavailable and use a neutral continuum.
- Overall health weights are Backlog Size 10%, Backlog Age 20%, Backlog Volatility 25%, Backlog Prioritisation 15%, and Delivery Predictability 30%.
- Overall health position increases from unhealthy to healthy. Scores below 0.4 are "Unhealthy", scores from 0.4 to below 0.7 are "Needs Attention", and scores of 0.7 or above are "Healthy".
- Unavailable signals are excluded from the weighted calculation; the remaining weights are renormalised. If no signal is available, the state is "Insufficient Data".
- Insight-category scores do not modify overall health, avoiding an extra adjustment on top of the underlying signals.
- `WorkHorizon`, `FlowBalance`, and `OldWorkPercentage` remain outside this alignment scope.

| Area | Alignment | Current implementation |
|---|---|---|
| Backlog Size | Partial | Missing or non-positive throughput and missing backlog counts are unavailable. The continuum is centred on five weeks and bounded for health aggregation. The current calculation averages entered throughput counts; until their observation duration is confirmed, "weeks of work" remains an estimate rather than a guaranteed time unit. |
| Backlog Age | Aligned | Uses the implemented score, direction, continuum mapping, and shape classification. Incomplete or empty distributions are unavailable and neutral. |
| Backlog Volatility | Aligned | Added and completed counts are compared only for matching week ranges. Starting backlog is derived from current backlog minus net change; percentage change uses that starting backlog. Direction and magnitude remain separate. No matched period is unavailable and neutral. |
| Backlog Prioritisation | Aligned for input availability | Requires a total backlog count and all three priority counts before the signal contributes to overall health. The existing coverage/distribution scoring remains unchanged. |
| Delivery Predictability | Aligned for evidence gates | A positive mean and at least two observations are required for predictability to contribute to health. Fewer than three observations hide the rhythm classification while retaining available variation evidence. Unobserved period positions remain visible in their correct slots. |
| Overall health | Aligned | Uses the approved five weights and thresholds, higher-is-healthier direction, available-signal renormalisation, and no insight-category modifier. |

### Remaining verification

- Confirm the duration represented by throughput observations so the weeks-of-work estimate has a defensible time basis.
- Keep the three original range labels until the observation-period wording is revisited.
- Continue to leave the auxiliary derived signals unchanged in this phase.