# Project Browser placement and navigation

The Browser corrections in [#80](https://github.com/mycelium-cmbse/mycelium-bloom/issues/80) follow [the classification review of #76](https://github.com/mycelium-cmbse/mycelium-bloom/pull/76#pullrequestreview-5652824034).
The composed [Browser](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=3250-2) and [unified filter bar](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=8563-22) guide presentation.

## Semantic placement

Membership role takes precedence over broad SDK interfaces. Exclusion suppresses the whole owned subtree; eliding a membership traverses to its member.

| Elements or occurrence roles | Placement |
| --- | --- |
| Packages, ordinary definitions/usages, unknown kinds | Structural rows; unknown kinds use a generic metaclass label |
| Connection definitions/usages, binding connectors | Connections group |
| Interface definitions/usages | Interfaces group |
| Flow definitions/usages, including SuccessionFlowUsage | Flows group |
| Allocation definitions/usages | Allocations group |
| SatisfyRequirementUsage | Satisfies group |
| VariantMembership usages, except enumeration values | Variants group |
| Ordinary memberships and aliases | Elided wrapper; preserve occurrence name and visibility |
| Resolved imported memberships | Classify the member; preserve imported status and effective importing-namespace visibility |
| End, state-subaction, transition-feature and requirement-constraint memberships | Excluded subtree; information belongs to the visible owner |
| Values, expressions, multiplicities, transitions, generic successions, annotations, import declarations and non-structural relationships | Owner information, not independent rows |
| EnumerationUsage owned by EnumerationDefinition | Enumeration value; ordinary enum-typed usages remain structural rows |

Constraint/calculation usages may implement IExpression; that interface alone never excludes them. SuccessionFlowUsage stays grouped, while plain Succession, SuccessionAsUsage and kernel SuccessionFlow are owner information. Group once at the visible owner: structural children first, then groups in first-occurrence order.

## Identity, navigation and lifecycle

- Rows retain exact SDK elements. Aliases/imports may repeat canonical identity with distinct occurrence IDs. Ancestor reference cycles stop at the repeated occurrence.
- Imported visibility comes from the importing namespace without mutating source membership. Incomplete imports invent no targets.
- Excluded elements and relationships map to the nearest visible owner. Reveal changes local focus/selection without replacing shared canonical selection or clearing filters; a nonmatching filter can still hide the row.
- Groups have no model element, element ID or runtime type. They start collapsed; activation toggles expansion without publishing selection.
- Each initialization stages identifiers, types and nodes; cancellation or failure publishes no partial tree. Browser instances retain independent expansion/filter state.
- One roving Tab stop enters/exits each tree. Arrows and Home/End move focus; Enter/Space activate the component handler. Focus alone does not publish selection. Removed rows recover to a surviving ancestor; scrolling follows the focused row within the Browser.
- Filtering exposes matching ancestor/group paths without overwriting stored expansion.

Each search-popover opening has a distinct identity and generation-bound callbacks: delayed Blueprint cleanup must not unregister or close the next opening. The search input stays mounted. The workspace Add tab fallback handles only unhandled End and immediate Enter/Space during Blueprint startup, yielding to Blueprint when its handler processes the key.

## Presentation boundaries

Name/type/multiplicity/value precede compact kind notation to retain the composed design's name hierarchy; this differs from the review's prefix order. Selected secondary text uses primary-strong for ordinary-text contrast; dark colours are unchanged.

OWN cells require a domain metadata binding and colour mapping. The bounded SDK writer omits unsupported/incomplete expression forms. Initial-state markers derive from entry successions, not names/order; verdicts require explicit canonical standard-library values rather than executing calculations.

Editing, drag-and-drop, live updates and additional Part Tree/tabular workflows remain outside scope. Sample model data and notation decisions prevent an exact Figma-parity claim.
