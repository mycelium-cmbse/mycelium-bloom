# Details #81: supplied-reference alignment

The [review checkpoint report](details-panel-checkpoint.md) adds the final identity/source trace,
typography measurements, expression-placement decision, 876-test inventory reconciliation and
current real-application screenshots. Its evidence supersedes the earlier totals and workspace
dark-theme claims below; the supplied-reference mapping remains applicable.

The supplied `Figma_Details_Complete_Reference_Pack.zip` supersedes the earlier PDF approximations.
SHA-256: `111ce2314b5c1e9a8ece3a31cb12fe1da8ff076b88ca1dd0557f76fb8f2abedb`.
All 14 pairs were verified against its README, DESIGN_METRICS, CODEX_HANDOFF and
[reference index](evidence/details-panel-81-reference/reference-index.csv). Paths in that index are
relative to the supplied archive. PNGs are visual authority; copied CSS supplies measurements only.
The available PNGs are 243px-wide montage crops, not native 380px exports. No separate larger PNGs
were found in Downloads. All supplied references are light theme.

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

### Rendered evidence

New evidence is in [details-panel-81-reference](evidence/details-panel-81-reference); the previous
17 screenshots remain intact as historical evidence. The native reference panel is 380×1000px.

| Variant | Light, native width | Dark, native width |
| --- | --- | --- |
| Attribute Properties | [PNG](evidence/details-panel-81-reference/attribute-properties-light-380.png) | [PNG](evidence/details-panel-81-reference/attribute-properties-dark-380.png) |
| Part Properties | [PNG](evidence/details-panel-81-reference/part-properties-light-380.png) | [PNG](evidence/details-panel-81-reference/part-properties-dark-380.png) |
| State Properties | [PNG](evidence/details-panel-81-reference/state-properties-light-380.png) | [PNG](evidence/details-panel-81-reference/state-properties-dark-380.png) |
| Attribute Relations | [PNG](evidence/details-panel-81-reference/attribute-relations-light-380.png) | [PNG](evidence/details-panel-81-reference/attribute-relations-dark-380.png) |
| Part Relations | [PNG](evidence/details-panel-81-reference/part-relations-light-380.png) | [PNG](evidence/details-panel-81-reference/part-relations-dark-380.png) |
| Attribute Comments | [PNG](evidence/details-panel-81-reference/attribute-comments-light-380.png) | [PNG](evidence/details-panel-81-reference/attribute-comments-dark-380.png) |
| Part Comments | [PNG](evidence/details-panel-81-reference/part-comments-light-380.png) | [PNG](evidence/details-panel-81-reference/part-comments-dark-380.png) |
| Attribute Attachments | [PNG](evidence/details-panel-81-reference/attribute-attachments-light-380.png) | [PNG](evidence/details-panel-81-reference/attribute-attachments-dark-380.png) |
| Part Attachments | [PNG](evidence/details-panel-81-reference/part-attachments-light-380.png) | [PNG](evidence/details-panel-81-reference/part-attachments-dark-380.png) |
| Running workspace: baseQuantities | [PNG](evidence/details-panel-81-reference/workspace-basequantities-light-380.png) | [PNG](evidence/details-panel-81-reference/workspace-basequantities-dark-380.png) |

The folder also contains 320/540px Properties captures for all three element kinds and the running
workspace, plus State Relations/Comments/Attachments at 380px. Those State tabs use the shared shell;
the pack supplies no separate State tab references. State Relations show available references, and
the fixture's empty Comments/Attachments are explicit empty/unavailable states.

- [Render checks](evidence/details-panel-81-reference/render-checks.json): **72 combinations**
  (three elements × four tabs × three widths × two themes), asserting chrome dimensions, column
  sizing, selected-tab state and no horizontal overflow. There were no page exceptions or failed assets.
- [Running application checks](evidence/details-panel-81-reference/workspace-checks.json): **24 tab/width/theme
  checks** against the published application and its real Quantities model, using the existing
  Properties splitter to resize. Close/reopen retains selection, Show in Browser reveals the selected
  canonical node, and type navigation changes Properties without changing the Browser's local selection.
- [Scroll/focus checks](evidence/details-panel-81-reference/interaction-checks.json): a short 380×400px
  panel scrolls its summary/content while retaining header/footer positions. ArrowRight/Home/End move
  focus and activate the correct tab in both themes, with a visible focus outline. Corresponding PNGs
  are named `part-scrolled-*-380x400.png` and `keyboard-*-380x400.png`.

Part/Attribute/State example graphs use the temporary acceptance host with the actual production
components, pinned SDK POCOs and published styles. Fixtures exist outside the repository; none are
production data. Running-application evidence is separately labelled above. Playwright scripts and
the host remain in `%TEMP%/bloom-81-visual` for reproduction. These checks do not constitute a
screen-reader conformance audit.

### Previous reference-pass validation and changed files

Release tests: **874 passed, zero failures**. Coverlet reports **91.71% line / 81.28% branch** overall;
the new visual grouping has **100% line coverage**, and DetailsPanelViewModel remains 100% line/branch.
The complete suite includes the isolated SDK library safety traversal. Header-format failures found
in the first run were corrected before the accepted run. Release publish, `VerifyMyceliumStyles`,
all **37 token tests**, and `git diff --check` passed. The final tab-inset CSS adjustment was republished
and the render/token checks repeated; C#/Razor functional test inputs did not change afterward.

The #80 checkpoint had 883 tests. The initial #81 implementation replaced obsolete history and
overlapping component tests with mocked-component and named semantic/integration coverage, producing
870 tests. This continuation adds four regression tests (state cards/parallel actions, infinity
labels, canonical relation rows and real baseQuantities) for 874. Existing canonical navigation tests
were also extended. No Browser production tests or implementation were removed by this continuation.
Unchanged JavaScript interop/converter checks retain #80's recorded results; no Docker or hosted
SonarCloud run was made.

Continuation source changes:

- Details UI: `DetailsPanel.razor` / `.razor.css`, `PropertiesSummary.razor` / `.razor.css`,
  `PropertiesSections.razor` / `.razor.css`, new `PropertyReference.razor`.
- Details presentation: new `DetailsPanelPresentation.Layout.cs`, additive section/card properties
  in `PropertySectionPresentation.cs`, constructor wiring in `DetailsPanelPresentation.cs`,
  target identity in `ElementLinkPresentation.cs`, and the localized infinity-label correction
  in `DetailsPanelPresentation.Semantics.cs`.
- Tests: `DetailsPanelSemanticPresentationTestFixture.cs`, `DetailsPanelIntegrationTestFixture.cs`,
  and `DetailsPanelTestFixture.cs`. The earlier #81 changes remain in place.
- Documentation: this matrix, the updated implementation report, reference index and new evidence.

### Deliberate differences and remaining limitations

The State Properties reference is now available and used; the previous missing-State-layout limitation
is superseded. Exact pixel parity is still not claimed: the supplied images are downsampled montage
crops, all in light mode. Existing font fallbacks remain in use; no font binaries were introduced.
The kind badge uses the existing purple token family blended with the card surface rather than
adding the reference's literal pink/purple palette to the design system. Dark colours use Bloom's
existing theme tokens because no dark reference was supplied.

Data-supported sections remain visible even where the example does not show them: explicit feature
flags, canonical multiplicity bounds, metadata, state-owner actions, incomplete-data notices and
multiple transition roles. State transitions retain named canonical role links instead of flattening
multiple triggers/effects into one example expression. The "initial" badge reflects the existing
unconditional-entry semantics; it is not a new default-state rule. The machine's state-usage count
includes parallel state usages; the ordinary-card and parallel-region groups show their own counts.

No author/date, avatar, threaded reply, file, ownership assignment, committed/pending value or
configuration-specific value is fabricated. Attachments remain unavailable. The existing functional
Show in Browser action remains in the fixed footer across tabs; unsupported Add/Edit/Subscribe/Upload
actions are omitted, so tab footers intentionally differ from the examples. Copy-link, overflow,
traceability Graph/Tree and configuration workflows were not added. Varied-Part and traceability
frames are mapped references for separate scope decisions.

This remains stacked on #80 at `cc737cb361783d207e4d448d7d1da85d23542302` on
`81-align-details-panel-figma-semantic-presentation`. Preserve #80 before integrating #81 into #76.
The reference-alignment pass performed no staging or remote writes. The subsequent authorized
review checkpoint includes these changes and evidence; see the current checkpoint report.
