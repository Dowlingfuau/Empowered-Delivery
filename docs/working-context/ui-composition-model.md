🧱 DRAFT — WORKING CONTEXT (VERSION 0.1)

You can paste this into your md.

Operational UI Composition Model (Working Draft)
1. Core Philosophy

The system is built on layered composition, not components.

Each visual element is constructed from independent layers with single responsibilities.

No layer should perform more than one responsibility.

2. Core Layers

Every composed UI element follows this structure:

Section
  Container (optional)
    Stage
      Environment (Light, Effects)
      Frame (Object boundary)
        Material (Surface)
          Content
3. Layer Responsibilities
Section
Semantic grouping
Vertical rhythm
No visual styling responsibility
Container
Horizontal constraint (max-width)
Horizontal padding
Must not contain visual styling (no backgrounds, no effects)
Stage
Hosts environment and objects
Defines spatial context
Does NOT define shape or styling
Environment (Light, Effects)
Creates atmosphere
Sits behind all objects
Must not define boundaries or structure
Frame (Object Boundary)
Defines object shape (radius)
Handles edge interaction with light
Responsible for glow and boundary definition
Does NOT contain content logic
Material (Surface)
Defines surface appearance (clear, solid, frosted)
Hosts content
Must inherit shape from Frame
Must not define external glow or boundary
Content
Typography, layout, interaction
No visual layering responsibilities
4. Ownership Rules
Radius
Owned by Frame
Material must use border-radius: inherit
Edge / Glow
Owned by Frame
Must not be applied to Material
Shadow (Current State)
Temporarily on Material
Will be migrated to Frame later
Light
Must exist within Stage
Must not be nested inside Material
5. Composition Rules
Each layer must be explicit in markup
No layer should be skipped or merged without justification
Avoid introducing new classes unless they represent a new responsibility
6. Anti-Patterns (DO NOT DO)

❌ Apply glow to Material
❌ Use borders instead of edge system
❌ Introduce container variants for visual styling
❌ Mix layout and visual responsibilities in one class
❌ Add new classes without defined responsibility

7. Current Known Gaps
Frame not yet handling shadow
Light system not yet directional or layered correctly
No noise / texture layer
Edge system currently uniform (needs refinement)
8. Implementation Strategy
Fix structure (current step)
Align ownership (radius, edge, shadow)
Fix light system
Add noise / micro effects
Refine edge behavior