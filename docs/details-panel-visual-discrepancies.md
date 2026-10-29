# Details #81 visual comparison

## Live review — 2026-10-29

Live MCP context, hierarchy, styles, variables and screenshots were read for Properties
7242:3798 (Attribute), 7259:3682 (Part), 7516:4428 (State), and sibling tabs
7246:3683/3857/4205 and 7259:3850/3985/4223 (Relations/Comments/Attachments).
These references supersede the export-only limitations below. No separate State tab or dark
component variants were established; dark screenshots validate the application theme, not exact Figma parity.

| Reference | Actionable difference | Decision / impact |
| --- | --- | --- |
| 7246:3857, 7246:4205, 7259:3985, 7259:4223 | Rechecked exact hierarchies: each contains visible `props-body > el-hdr`, including names and badges | Original summary behavior retained across all four tabs; State-specific suppression unchanged. The earlier omission claim was incorrect. |
| 7242:3798, 7259:3682 | Badge uses existing variation colour; Figma binds mode-cd/ownership-payload-bg | Low; retain pending design-token review, rather than change shared domain colours here. |
| 7259:3850 | Owned-feature badges remain neutral rather than example-specific blue/teal categories | Low; retain pending agreement on category mapping; canonical links remain functional. |
| 7516:4428 | Initial-state wording, role links and counts differ from sample default-state cards | Intentional SDK semantics; do not infer default-state rules or flatten multiple roles. |
| All panels | Missing author/audit, configuration, subscribe, editing and upload controls | Intentional unsupported workflows/data; not fabricated. |

Live geometry confirms 46/38/56px shell, 112px labels + 10px gap, 12/15px values,
18/22px summary names, divided sections and compact relation rows. Current Inter fixture
captures cover Part, Attribute, State, Relations, Comments, Attachments and narrow/dark rendering:
[measurements and capture names](evidence/details-panel-81-inter/review-validation.json).
The fixture uses real SDK POCOs with different values from Figma; existing Quantities captures remain separate.

Correction validation: 37 Details/Expression tests passed; Release publish and fixture build passed
without warnings. Seventeen Chromium checks verify summary identity across tabs, State-specific
suppression, both themes, no horizontal overflow and actual custom Inter glyphs. No page exceptions.
The previous style verification and 37 token tests remain applicable; no styles changed in this correction.
Fresh original-resolution screenshots and explicit node-visibility reads confirm the summaries in
all four requested Figma frames. The earlier omission conclusion was a review error, not a design change.
No hosted CI or human approval is claimed.

Evidence cleanup removes 76 redundant PNGs from the original 105 files, retains all eight
JSON/CSV verification/provenance files and 21 representative originals, and adds seven current
captures plus their measurements: 37 files remain. Unique connection/import/metadata and
baseQuantities evidence is retained. Historical JSON still describes its original runs;
removed capture names resolve in [the immutable checkpoint](https://github.com/mycelium-cmbse/mycelium-bloom/tree/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence).

## Export mapping (historical provenance)

## Reference mapping and discrepancy matrix

Each path stem below maps `screenshots/<stem>.png` to `figma-copy-css/<stem>.txt` in the archive.
The initial column describes the implementation at the start of this continuation, before corrections.

| PNG / CSS stem | Target component | Initial discrepancy | Implemented correction / data boundary |
| --- | --- | --- | --- |
| `attribute/01-properties` | DetailsPanel, PropertiesSummary, PropertiesSections | 45/41/61px chrome; weak value emphasis; neutral kind badge; fluid label slot; oversized body labels | 46/38/56px chrome, compact badges, 18/22 title, stronger stored value, 112px labels with 10px gap; no committed/pending or audit data invented |
| `part/01-properties` | Same; attribute rows | Attributes lack count and row separators; all links teal; summary outside scrolling body | Counted attribute section, divided rows, medium-weight values; summary inside scroll area; actual SDK values only |
| `state/01-properties` | PropertiesSections and state presentation | Generic classification/role list and redundant summary precede state structure; no state cards | Identity then State machine, state usages/parallel-region cards and transition groups; preserve initial-state and action semantics |
| `attribute/02-relations` | PropertiesSections / relation rows | Generic role/value table, no grouping/counts or secondary target identity | Counted relation lists with role badges, canonical targets and navigation affordance; only existing resolved relationships |
| `part/02-relations` | Same | Same; owned features absent from Relations | Reuse existing direct owned feature and type references; no reverse-dependency inference |
| `attribute/03-comments` | PropertiesSections / annotation content | Generic property table and undifferentiated text | Plain comment entries with dividers; stored textual representations in a language-labelled code card; no composer, authors, timestamps or replies without data |
| `part/03-comments` | Same | Same | Same treatment with actual Part annotations |
| `attribute/04-attachments` | PropertiesSections empty state | Plain unstructured unavailable message | Labelled, padded empty state; no fabricated files or upload control |
| `part/04-attachments` | Same | Same | Same; attachment service remains unavailable |
| `attribute/05-traceability-graph` | No implementation target | Graph tab absent | Reference inspected; new traceability workflow outside #81 |
| `attribute/06-traceability-tree` | No implementation target | Tree tab absent | Reference inspected; no new graph/tree switch |
| `part/05-traceability-graph` | No implementation target | Graph tab absent | Reference inspected; preserve existing four tabs |
| `part/06-traceability-tree` | No implementation target | Tree tab absent | Reference inspected; no inert controls |
| `part-varied/01-properties` | Summary, attributes, existing Variants section | Configuration-specific values absent | Reuse shared typography/rows; do not synthesize configuration values or selectors |

Shared footer placement is supported; editing, Add relationship/comment, Subscribe, Upload and
publication actions are not. Existing functional Show in Browser navigation remains available.
The copy-link and overflow examples do not establish an implemented action contract and are omitted.

## Acceptance

The scoped component styles now measure **46px header / 38px tabs / 56px footer**, with **112px
property labels, 10px column gaps, 11/13px label text and 12/15px value text**. Tab labels use 12/16px
type, 8px horizontal inset and distinct active weight/underline. Summary names use 18/22px type;
stored Attribute values use a prominent 16px treatment. The selection summary now scrolls with the
body, as in the supplied frames. Header, tabs and footer remain fixed. At 320px the summary wraps and
the tabs use tighter outer padding without overflowing the panel. At 540px the label slot stays 112px.

`DetailsPanelPresentation.Layout.cs` adds visual grouping alongside the existing `ContentSections`.
It retains canonical elements and the existing selection/cache/lifetime contracts. State cards use
the existing nested-state and initial-state interpretation; entry/do/exit references are projected
in the ViewModel. Parallel regions show their actual contained states. Transitions retain every
source, target, trigger, guard and effect. Relations show counted typing, specialization and direct
owned-feature lists with role badges, available target identity and canonical navigation arrows.
Comments retain plain model text; stored `TextualRepresentation` bodies use a language-labelled code
card. They are not generated diagnostic notation blocks. Attachments use a labelled unavailable state.

The handoff's `baseQuantities` question was checked against the real bundled Quantities model:
element `adacf513-a227-5c98-8cf0-666c25aa6b2f` is typed by `ScalarQuantityValue`, has no owned
`FeatureValue`, and has an unbounded multiplicity. Its Value therefore explicitly says
"No local value; inherited/default values not resolved". No computed value is added to its summary.
The Details bound reference now labels its actual `LiteralInfinity` as `*`, consistent with `[*]`;
the canonical bound object and the shared Browser formatter are unchanged.
