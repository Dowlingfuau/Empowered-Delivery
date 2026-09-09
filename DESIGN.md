---
name: Empowered Delivery — Final Homepage
description: Retro-futurist industrial editorial system for the Empowered Delivery narrative homepage
colors:
  ink: "#111315"
  charcoal: "#17191a"
  cream: "#f2eadb"
  muted: "#a9aaa2"
  line: "#4a4d4a"
  vermillion: "#d5523d"
  petrol-blue: "#2f789d"
  vermillion-deep: "#a23f31"
  cutaway-paper: "#d6d0c0"
  lenses-petrol-dark: "#1b272c"
  hypothesis-mineral: "#c8c3b5"
typography:
  display:
    fontFamily: "Georgia, serif"
    fontSize: "clamp(50px, 8vw, 120px)"
    fontWeight: 400
    lineHeight: 0.91
    letterSpacing: "-0.06em"
  headline:
    fontFamily: "Georgia, serif"
    fontSize: "clamp(44px, 7vw, 108px)"
    fontWeight: 400
    lineHeight: 0.88
    letterSpacing: "-0.07em"
  body:
    fontFamily: "Inter, Segoe UI, sans-serif"
    fontSize: "17px"
    fontWeight: 400
    lineHeight: 1.4
  label:
    fontFamily: "Inter, Segoe UI, sans-serif"
    fontSize: "10px"
    fontWeight: 700
    lineHeight: 1.15
    letterSpacing: "0.14em"
rounded:
  sm: "2px"
  circle: "50%"
spacing:
  section-y-desktop: "120px"
  section-y-compact: "100px"
  section-x-desktop: "clamp(24px, 9vw, 140px)"
components:
  button-primary:
    backgroundColor: "transparent"
    textColor: "{colors.cream}"
    rounded: "{rounded.sm}"
    padding: "14px 18px"
  button-primary-hover:
    textColor: "{colors.vermillion}"
  nav-link:
    textColor: "{colors.cream}"
    typography: "{typography.label}"
---

## Overview

The Empowered Delivery final homepage (`BakeOffFinalHomepage.razor`) tells one continuous story across fourteen full-bleed scenes: reports show the surface, the system is underneath. It is a retro-futurist industrial editorial world — dark machine-shop scenes alternating with warm cream "paper" scenes — where oversized Georgia display type and treated photographic machinery carry the narrative instead of cards, icons, or dashboard chrome. Vermillion marks tension, action, and diagnostic emphasis; petrol/steel blue marks systemic relationships. The page is meant to be read scene by scene, not scanned as a grid of features.

## Colors

Two alternating fields — near-black ink/charcoal machine scenes and warm cream paper scenes — carry a single vermillion accent throughout, with petrol blue reserved for relationship/systems moments.

### Primary
- **Vermillion** (`#d5523d`): the one accent used for emphasis text (`em`/`strong` inside headlines), section index labels, CTA underlines and arrows, active states, and diagnostic markers. Used sparingly against both dark and light fields.
- **Vermillion Deep** (`#a23f31`): the same accent role recolored for legibility when it sits on the cream field (e.g. section index labels, dashboard captions on `.final-surface`, `.final-cutaway`, `.final-hypothesis`).

### Secondary
- **Petrol/Steel Blue** (`#2f789d` base; realized as `#78929a` rings, `#91b4bc` / `#5b7379` accents in the lenses scene): reserved for the "system relationships" motif — the concentric lens-orbit diagram and its labels. Never used for CTAs or action.

### Neutral
- **Ink** (`#111315`): primary dark background for hero, contradiction, tools, and momentum scenes.
- **Charcoal** (`#17191a`): secondary dark surface (tools console, play scene background).
- **Cream** (`#f2eadb`): primary light "paper" background (surface, clarity, ecosystem scenes) and the default text color on dark fields.
- **Muted** (`#a9aaa2`): secondary/supporting body copy on dark fields.
- **Line** (`#4a4d4a`): hairline dividers and borders on dark fields (tool console rules, contradiction underlines use `#3d403d`).
- **Cutaway Paper** (`#d6d0c0`): a distinct warm grey-cream used only for the cutaway scene, so photographic multiply-blend artwork reads correctly.
- **Hypothesis Mineral** (`#c8c3b5`): a third neutral field for the hypothesis-equation scene, distinguishing it from both cream and cutaway paper.

### Named Rules
**The One Accent Rule.** Vermillion is the only accent used for action, tension, and emphasis anywhere in the page. When a scene needs a second color relationship (the lenses diagram), it borrows petrol blue instead of introducing a second warm accent — the two never compete on the same scene.

## Typography

**Display Font:** Georgia (serif), used exclusively for every `h1`/`h2` headline across all fourteen scenes.
**Body Font:** Inter (with Segoe UI, sans-serif fallback), used for all body copy, labels, navigation, and UI controls.

**Character:** A newsroom/industrial-manual pairing — a heavy, tightly-tracked serif display face for editorial statements, set against a clean grotesque for everything functional (nav, labels, captions, controls). The contrast signals "field guide," not "software product."

### Hierarchy
- **Display** (400, `clamp(50px,8vw,120px)`, line-height 0.91, letter-spacing -0.06em): hero headline only (`.final-hero h1`).
- **Headline** (400, ranges from `clamp(44px,6vw,108px)` down to `clamp(50px,6vw,90px)` depending on scene, letter-spacing -0.06em to -0.07em): every other scene's `h2` (surface, contradiction, cutaway, lenses, clarity, tools, play, learning, momentum, ecosystem, services, close). All share the same Georgia/400/negative-tracking treatment; only the clamp range varies per scene's visual weight.
- **Body** (400, 17px, line-height 1.4–1.55): scene descriptions and supporting paragraphs; capped to a narrow measure per scene (270–590px) rather than a fixed ch value, since the layout is scene-composed, not column-flowed.
- **Label** (700, 10–11px, letter-spacing 0.1em–0.16em, uppercase): section index numbers (`01 / THE MACHINE`), nav links, CTA text, art notes, tool console rail. This is the connective tissue that makes fourteen very different scenes read as one system.

### Named Rules
**The Georgia-for-Meaning Rule.** Only headline text and the equation/theory statements use Georgia. Everything structural, navigational, or numeric (labels, nav, buttons, readouts) stays in Inter. Mixing the two inside a single line (e.g. plain word + `em` in Georgia) is the page's signature emphasis technique — never swap fonts to add emphasis.

## Layout

Full-viewport, full-bleed scene composition: each `<section>` is its own visual "page" (min-heights 620px–960px) with no shared container width or card grid. Horizontal padding uses `clamp(24px, 8–10vw, 120–160px)` so the same scene compresses gracefully without ever centering into a fixed max-width column. Two-column scenes (clarity, lenses, play, learning, services) use CSS grid with asymmetric fractions (e.g. `1.2fr .8fr`, `.8fr 1.2fr`) rather than even halves, so image and copy never look like a templated split. Photographic art frequently bleeds past the section edge (`right:-4%`, `right:-6%`) or is absolutely positioned with caption tags pinned directly onto it, reinforcing that imagery is story machinery, not a decorative card.

At ≤760px, navigation collapses to brand + primary action, two-column grids stack to one, the tool-console rail becomes stacked tabs, and image bleed becomes a controlled crop. No horizontal scroll or overflow is permitted at any width.

## Elevation & Depth

No box-shadow-driven card elevation anywhere in the system. Depth is conveyed through: (1) field alternation between ink/charcoal and cream/paper backgrounds, (2) `mix-blend-mode: multiply` on cutaway artwork so photography sits into the paper tone rather than floating above it, (3) a `backdrop-filter: blur(10px)` translucent nav bar over the hero image, and (4) one soft inset shadow (`box-shadow: inset 0 -1px 0 rgba(255,255,255,0.04)`) closing the hero scene. Interactive focus uses a visible ring (`box-shadow: 0 0 0 3px var(--red)`), not elevation.

### Named Rules
**The Flat Field Rule.** Surfaces are flat. The only permitted "shadow" is the hero's closing hairline and text-shadow on the hero headline (`0 12px 32px rgba(0,0,0,0.18)`) for legibility over photography — never a card or button drop shadow.

## Shapes

Almost entirely rectangular, edge-to-edge scenes with sharp corners; the only rounding in the system is functional, not decorative: the `ED` brand mark (2px radius, effectively a squared badge) and two fully circular elements — the concentric lens-orbit diagram and the circular signal-reveal button. Borders are 1px hairlines in `line`/muted tones, used for dividers, tags, and the tool console grid rather than to frame cards.

## Components

### Buttons / CTAs
- **Shape:** No fill, no radius — a bordered or underlined text button (`final-action-button`, `final-hypothesis-toggle` use `border:1px solid currentColor`; `final-nav-action`/`final-hero-link`/`final-close-action` use a single `border-bottom` underline instead).
- **Primary:** Text in `cream` (on dark) or `ink` (on light), with the trailing arrow glyph (`↗ ↓ →`) always rendered in vermillion via a nested `<span>`.
- **Hover / Focus:** Links fade to `opacity: 0.9` on hover; the one circular button (`final-signal`) uses a visible vermillion focus ring instead of a hover color change.

### Section Index Label
- **Style:** `01 / SCENE NAME` in vermillion (or vermillion-deep on cream), 10px, uppercase, 0.16em tracking. Appears at the top of every scene and is the primary wayfinding device across the fourteen-scene sequence — treat it as a required element of any new scene, not optional chrome.

### Diagnostic Console (Tools scene)
- **Style:** A single two-column instrument (300px selector rail + flexible readout panel) divided by hairlines, not a tab-and-card pattern. Active tool state is a color change only (`muted` → `cream`), no background highlight or pill.
- **Readout bar:** A 4px flat track with a vermillion fill representing signal strength — the system's one data-visualization motif; do not introduce a second bar/gauge style elsewhere.

### Signal Reveal Button
- **Style:** 320px circle, `#22282a` fill, concentric ring decoration, centered vermillion dot. Reveals a `<small>` caption in place on click. This is the system's one "hidden content" interaction pattern — used exactly once, for the emotional pivot from reporting to interpretation.

### Lens Orbit Diagram
- **Style:** Concentric circles (petrol-toned borders) with three orbiting uppercase labels and a centered vermillion-accented result. Purely structural (no image), used only in the "Fuller Picture" scene to visualize the system+behaviour+culture relationship.

### Navigation
- Fixed/absolute translucent bar over the hero only (`rgba(17,19,21,0.34)` + blur), brand mark + wordmark left, five uppercase nav links center, single underlined CTA right. Nav links and the close-scene CTA both point back into the same anchor set (`#final-tools`, `#final-intelligence`, `#final-play`, `#final-ecosystem`, `#final-services`), reinforcing the "one system, four ways in" ecosystem message.

## Do's and Don'ts

### Do:
- **Do** keep every scene full-bleed with its own background field; never introduce a shared max-width container or repeating card grid.
- **Do** reserve vermillion as the single accent for tension, action, and emphasis; use petrol blue only for the systems/relationships motif.
- **Do** pair Georgia display type with Inter functional type; keep section index labels and nav in Inter uppercase.
- **Do** treat photography as narrative machinery — bleed it, blend it, or pin captions directly onto it rather than boxing it in a card.
- **Do** use the `01 / SCENE NAME` index label on every new scene added to this page.

### Don't:
- **Don't** add drop-shadow card elevation, gradients-as-decoration, or rounded corner cards; the system is flat and rectangular except for the two circular instruments and the brand mark.
- **Don't** introduce a second color accent alongside vermillion; if a scene needs a systemic/relationship color, use petrol blue, never a third hue.
- **Don't** repeat the signal-reveal interaction pattern elsewhere; it is a one-time emotional pivot, not a generic disclosure component.
- **Don't** center content into a fixed-width column; padding is fluid (`clamp()`) against the full viewport at every breakpoint.
