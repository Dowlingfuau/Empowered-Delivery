---
target_identity: "url:http://localhost:5217/bake-off"
timestamp: 2026-09-03T01-52-23Z
slug: localhost-bake-off
---
# Round 2 Bake-off Critique

## Design Health Score

| # | Heuristic | Score | Key issue |
|---|---|---:|---|
| 1 | Visibility of system status | 2/4 | Active round is clear; concept selection/progress and reveal state are not surfaced globally. |
| 2 | Match system / real world | 3/4 | Terrain, signal, and relationship metaphors fit coaching practice, though some copy is abstract. |
| 3 | User control and freedom | 2/4 | Clear All confirms, but there is no undo, compare mode, or meaningful CTA destination. |
| 4 | Consistency and standards | 3/4 | Gallery framing is consistent, but internal controls and CTA treatments diverge sharply. |
| 5 | Error prevention | 2/4 | Destructive action is confirmed; inert primary actions create uncertainty. |
| 6 | Recognition rather than recall | 3/4 | Labels help, but long-scroll comparison forces users to remember differences. |
| 7 | Flexibility and efficiency | 2/4 | Favourites and notes help, but there is no shortlist or comparison view. |
| 8 | Aesthetic and minimalist design | 3/4 | Strong art direction; metaphor density and gallery length increase effort. |
| 9 | Error recovery | 1/4 | No undo for Clear All; primary actions have no visible outcome. |
| 10 | Help and documentation | 1/4 | No evaluation rubric, Round 2 explanation, or next-step guidance. |
| **Total** | | **22/40** | Strong visual concepts, weak decision support and interaction closure. |

## Design Specificity Verdict

The three concepts are strongly authored for Operational Intelligence Hub rather than category-interchangeable SaaS. They express system thinking through terrain, hidden signals, and relationships, and all connect insight to a Play. The main missed opportunity is the bake-off shell: it still says “six live concepts” while Round 2 contains three, and it gives no explicit comparison framework.

Assessment A found Option 1 the strongest overall direction, Option 2 the strongest campaign/narrative direction but highest UX risk, and Option 3 the clearest explanatory direction but least interactive. Assessment B found 3 deterministic findings: Inter is overused in the three Round 2 component files (P3); browser evidence found unlabeled notes inputs, an unnamed terrain SVG, and confirmation-modal semantics/focus issues (P1); incomplete tabs and unannounced reveal state (P2); and 320px clipping on the terrain Play CTA (P1). No user-visible overlay was available because the URL detector dependency (puppeteer) was unavailable.

## Overall Impression

This is a meaningful step beyond Round 1: the options are genuinely different product narratives, not palette swaps. Option 1 is the best lead. The single biggest opportunity is to make the gallery itself help the evaluator decide, not just admire three long-form posters.

## What's Working

- **Distinctive product voice:** “Healthy measures can still describe an unhealthy route” and “The pattern is not in the metrics” make the system-thinking position memorable.
- **Narrative consistency:** Each direction moves from surface signal to interpretation, hypothesis, and a concrete Play.
- **Real visual differentiation:** Terrain, retro-futurist darkness, and editorial string art have different composition grammars while sharing a credible operational tone.

## Priority Issues

### [P1] Primary concept actions are non-functional
**Why it matters:** “Map my system,” “Open Play,” “Trace a relationship,” and “Try this Play” promise product behaviour but appear inert. That prevents evaluators from testing the proposed experiences.
**Fix:** Wire each CTA to a real destination, detail panel, or explicit prototype state. If an action is illustrative, label it as such.
**Suggested command:** `$impeccable harden`

### [P1] The gallery provides no decision framework or synthesis
**Why it matters:** Users can favourite and note, but cannot compare clarity, trust, distinctiveness, and actionability or see what happens after choosing.
**Fix:** Add a compact synopsis per option, a persistent shortlist, and a “continue with favourites” summary.
**Suggested command:** `$impeccable distill`

### [P1] Round framing is internally inconsistent
**Why it matters:** “Six live concepts” and “six directions” imply missing content when Round 2 has three options.
**Fix:** Make the header round-aware: “3 Round 2 directions,” explain the round purpose, and show evaluation progress.
**Suggested command:** `$impeccable clarify`

### [P2] Long-form concepts are hard to compare
**Why it matters:** Three mini-sites require users to retain differences from memory.
**Fix:** Add an at-a-glance synopsis above each concept: promise, emotional tone, interaction model, risk, and best-fit audience.
**Suggested command:** `$impeccable layout`

### [P2] Metaphors sometimes outrun comprehension
**Why it matters:** “Side door,” “leverage,” and “operating system underneath” are evocative but not always operationally concrete.
**Fix:** Pair each metaphor with plain-language interpretation and one example of what the user would actually do.
**Suggested command:** `$impeccable clarify`

## Persona Red Flags

- **Agile/delivery coach:** Can recognise system-thinking intent, but cannot tell which option best supports facilitation because diagrams are static and Play CTAs do not open an intervention.
- **Engineering/delivery leader:** May appreciate the challenge to green metrics, but Option 2 can feel accusatory and illustrative values such as `+38%` and `11d` have no evidence trail.
- **First-time evaluator:** Lacks instructions for how to judge options, what Round 2 means, or what favouriting changes; repeated long-scroll layouts increase abandonment risk.

## Minor Observations

- “Clear All” could more clearly say it clears all four rounds; it has no recovery path.
- Option 2’s subdued pre-activation reveal zone may be missed.
- Option 3’s static map has no directional arrows or trace affordance.
- Mobile tabs wrap awkwardly and the gallery becomes extremely vertical.
- Notes save on `change`, which can be missed if a user types and navigates without blurring.
- Reusing “Make the trade-off visible” in all three directions reduces the perceived difference at the decisive moment.

## Option Verdicts

- **Option 1 — Operational Terrain:** Recommended lead. Best balance of specificity, explanatory clarity, visual identity, and actionability. Make the map interactive or soften “Map my system.”
- **Option 2 — Pulling the System Out of the Dark:** Best campaign narrative and reveal mechanic, but dramatic framing, low-contrast surfaces, and hidden payoff create the highest UX risk.
- **Option 3 — Connecting the Dots:** Clearest explanatory direction and strongest editorial layer, but static string art and a flat ending make it feel more like thought leadership than an operational tool.

## Questions to Consider

- What exactly is the evaluator deciding: visual preference, product promise, interaction model, or business direction?
- If every option ends at the same Play, what meaningful difference remains?
- Should “Map my system” and “Trace a relationship” be real interactions before these directions are judged?
- What should the user feel and know immediately after favouriting one option?
