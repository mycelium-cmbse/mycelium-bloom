# Details / Properties presentation (#81)

This local implementation is stacked on Browser checkpoint `cc737cb361783d207e4d448d7d1da85d23542302`.
Integrate #80 before #81 into the original #76 feature branch. Browser source and its unresolved
ownership, unsupported-expression and primary-Figma dependencies remain outside this change.

## Current visual acceptance

Live Figma MCP now supplies the primary references. See [the live comparison](details-panel-visual-discrepancies.md) for exact nodes, remaining decisions and current validation. The following implementation history predates restored access; its quota, fallback-font and missing-State statements are historical only.

## Review and design mapping

Authority: [issue #81](https://github.com/mycelium-cmbse/mycelium-bloom/issues/81),
[original issue #75](https://github.com/mycelium-cmbse/mycelium-bloom/issues/75),
[Antoine's complete review](https://github.com/mycelium-cmbse/mycelium-bloom/pull/76#pullrequestreview-5652824034),
[project SSS](https://github.com/mycelium-cmbse/requirements/blob/b9c2f3daf2a30eb6c3a2543fa17d5fea4da32661/Software-System-Specification%20-%20Project.md),
and [Browser placement handoff](project-browser-placement.md).

| Review / design requirement | Existing location | Correction |
| --- | --- | --- |
| Direct `VM.Presentation`, UI-owned click handlers, decomposition (4378982056, 4379000133, 4379002248, 4379035346) | DetailsPanel.razor | Shell composes PropertiesSummary and PropertiesSections; handlers dispatch to the VM or composition callback. |
| Mock service and VM (4377818708, 4377870626, 4377886126) | DetailsPanelTestFixture, ExpressionPanelTestFixture | Component tests receive mocked IDetailsPanelViewModel; real graph/selection checks belong to named semantic/integration fixtures. |
| Back/history concern, backing-field convention (4386083128, 4386095992) | DetailsPanelViewModel | Remove history API and Subject/OAPH machinery; cache a notified Presentation snapshot using existing selection observation. |
| Formatting, XML documentation (4386022873, 4386090726, 4386160664, 4386181713) | Details presentation and link/expression helpers | Block statements, property ordering, documentation, and method-backed expression path. |
| Generic property suggestion (4386056670) | DetailsPanelPresentation | Reusable named section structure with explicitly interpreted SDK interfaces; no reflective property dump. |
| No diagnostic notation panel (4386203131) | DetailsPanel / SdkExpressionText | Remove the diagnostic block. Retain bounded SDK formatting for actual Values, Expressions and multiplicity; Browser #80 depends on this adapter. |
| Workspace lifetime and return docs (4379193813, 4385993247, 4385993644, 4385994769) | WorkspaceShell | Checkpoint already implements IAsyncDisposable and documents the three return values; retain it. |
| [Part Properties 7259:3682](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=7259-3682) | Generic Identity/Attributes sections | Header, four tabs, selected summary; Identity → Attributes → Documentation → Classification. |
| [Attribute Properties 7242:3798](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=7242-3798) | Generic Value / Expression sections | Identity → Documentation → Value → Classification; supported stored quantity notation and canonical references. |
| [State Properties 7516:4428](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=7516-4428) | No specialized state content | Supplied `state/01-properties` PNG/CSS supports Identity, state machine, state/parallel-region cards and transitions; preserve initial state, entry/do/exit and canonical role links. |

## Initial design evidence and limits (superseded)

On 2026-10-29 all three live design-context reads failed with the Figma Professional View-seat
MCP quota error. The available `Bloom.pdf` is the same 233-page export used by #80 (PDF fingerprint
`f4cc280b2fc1506c98a909c743dc75f0`). It has no node IDs or creation/modification metadata, so it cannot
prove correspondence to the requested node IDs or their current revisions.

Inspected supporting variants: Part on pages 18, 19, 20 and 30; Attribute on pages 26 and 33;
attachments on 28 and 31; requirement constraints on 49; variation presentation on 55 and 63.
Pages 26 and 30 were rendered and visually inspected at their native 1920×1080 page dimensions
(render scale 1.5); their right Properties panel is 380px wide. No State Properties frame was
identified in the export. State semantics follow the SSS/review; no exact State visual parity is claimed.

The supported shell uses the exported header, underline tabs, summary, divided labelled sections,
scrolling body and footer action placement. Existing shared theme tokens provide light/dark colours.
No author/date, owner colour, committed/pending value, quantity-kind binding, subscription,
configuration or attachment data is fabricated. Editing, uploads, persistence and collaboration
actions remain separate. Attachments states that the model has no attachment integration.
The pinned SDK has `IsConstant`/`IsVariable`, not `IsReadOnly`; these are labelled by their actual
meaning rather than pretending to implement the design's editing permission field.

## Semantic destinations

| Browser-excluded information | Properties destination |
| --- | --- |
| Feature values and supported expressions | Value / Expressions; references navigate to canonical targets. |
| Multiplicity and bound expressions | Classification plus canonical lower/upper bound links in Multiplicity. |
| Type references and feature flags | Classification; canonical Typed by links and explicit direction/composition/ordering/uniqueness. |
| Entry/do/exit and transitions | State behaviour / Transitions, preserving every trigger, guard and effect. |
| Initial state and nested states | State structure; reuse #80's canonical entry-succession interpretation, not names or order. |
| Succession | Relations → Control sequencing. |
| Connector/association/connection/interface/flow ends | Ends: canonical end, referent, multiplicity and direction. |
| Documentation, comments, text representations | Documentation in Properties; Comments tab retains plain text and language/locale. |
| Applied metadata | Metadata: canonical definition and stored feature values. |
| Import declarations | Relations → Imports: kind, target, recursion, import-all, visibility. |
| Typing, subsetting, redefinition, specialization, conjugation | Relations → Specializations / Type hierarchy. |
| Requirement constraint memberships | Constraints, labelled by the SDK's assumed/required role. |
| Enumeration values | Enumeration values with canonical names, explicit stored values and documentation; no invented ordinals. |
| Variants, aliases, dependencies and generic relationships | Variants / Memberships / Relationships with canonical targets. |

Inspection changes shared canonical selection without manipulating Browser state. Show in Browser is
an explicit composition-owned action using the existing deterministic focused/active/first Browser
selection policy. It does not clear filters or replace a hidden element with its visible owner in
shared selection. Component disposal releases only factory-owned ViewModels; caller-owned state survives.

Close hides the workspace Properties region without clearing selection; the existing workspace toggle
can reopen it. At compact widths Show in Browser also reveals the main workspace pane. Its callback is
available only while the routed page has a Browser and is released when that page is disposed.
Inverse type relationships are limited to the loaded ownership graph (4,096 visited elements), with a
visible notice when the scan reaches that limit; this does not query unloaded projects or libraries.

## Initial validation and rendered evidence (historical)

Final local acceptance on 2026-10-29:

- Release build and complete .NET suite: **870 passed, 0 failed**. Build emitted no warnings or errors.
  The SDK safety fixture also launches its isolated canonical-library traversal; its child-only test
  is explicitly excluded from normal in-process execution.
- Coverlet: **91.71% line / 81.57% branch** overall; semantic presentation **95.05% / 89.68%**;
  DetailsPanelViewModel **100% / 100%**. These are local collection results, not a SonarCloud gate result.
- `VerifyMyceliumStyles` passed and all **37 token-consumer tests** passed against the current sources.
- Retained #80's converter and JavaScript interop coverage results: their scripts, JavaScript modules,
  package dependencies and generated token inputs are unchanged. The current token-consumer run above
  was repeated because it also checks Razor/CSS consumers. No Docker or hosted SonarCloud run was made.
- `git diff --check` passed. The branch remains at the starting checkpoint with an empty Git index;
  all implementation and evidence files are local working-tree changes.

The test changes separate mocked component tests from SDK semantic and selection integration tests.
They cover Part/Attribute section order and quantity values; canonical unit/type navigation; initial,
parallel and nested states; every transition role (including multiple effects and incomplete data);
relationship ends; annotations and metadata; imports and direct/inverse type relationships; reactive
refresh/disposal/remount; two-Browser selection isolation; footer reveal and close/reopen behavior.

Actual interactive Blazor components were hosted locally with the production published styles,
Blazor Blueprint and the pinned SysML2.NET POCOs. The fixtures use the existing Browser acceptance
factory plus a harness Part, quantity Attribute, state machine, connection and imported namespace.
This is rendered component evidence; routed workspace behavior is covered by bUnit integration tests.
The temporary host and Playwright scripts reside outside the repository in `%TEMP%/bloom-81-visual`.

At **380 × 900px**, Part/Attribute section order, header/tab/summary grouping, field alignment,
dividers and footer placement were compared with PDF pages 30/26. The header measures 45px including
its border and the tab strip 41px. Content differs where the export shows unavailable author/date,
ownership, configuration, committed/pending or editing information. Dark colours use the existing
shared theme; a corresponding dark Properties export was not verified. No pixel-perfect comparison
or exact node parity is claimed.

Saved evidence in [evidence/details-panel-81](evidence/details-panel-81):

| Render | Light | Dark |
| --- | --- | --- |
| Part, 380px | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/part-light-380.png) | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/part-dark-380.png) |
| Attribute, 380px | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/attribute-light-380.png) | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/attribute-dark-380.png) |
| State, 380px (semantic evidence only) | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/state-light-380.png) | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/state-dark-380.png) |
| Relationship ends, 380px | [image](evidence/details-panel-81/connection-light-380.png) | [image](evidence/details-panel-81/connection-dark-380.png) |
| Imports, 380px | [image](evidence/details-panel-81/imports-light-380.png) | [image](evidence/details-panel-81/imports-dark-380.png) |
| Comments, 380px | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/comments-light-380.png) | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/comments-dark-380.png) |
| Scrolled metadata, 380px | [image](evidence/details-panel-81/part-metadata-light-380.png) | [image](evidence/details-panel-81/part-metadata-dark-380.png) |
| Part, 320px | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/part-light-320.png) | [image](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/part-dark-320.png) |

[Measured browser checks](evidence/details-panel-81/visual-checks.json) record ten 380px renders and
twelve additional Part/Attribute/State checks at 320/540px across both themes, with no horizontal
overflow. ArrowRight/Home/End activate and focus the expected tabs, `aria-selected` uses explicit
true/false values, canonical unit links navigate, the footer dispatches the selected element and
Close preserves selection. There were no browser exceptions or failed asset requests. This is
interaction evidence, not a screen-reader conformance audit.
The [keyboard-focus render](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81/keyboard-focus-light-380.png) verifies a visible
tab outline. After the final scoped focus CSS adjustment, the app was republished, browser captures
and interaction checks were refreshed, and all 37 token checks passed again. The earlier complete
.NET results remain applicable because no C# or Razor logic changed afterward.

## Changed files

Paths below are relative to the repository; evidence consists of this report, 17 PNGs and the measured
browser-check JSON. No Browser production source, styles, generated tokens or SDK adapter changed.

| Directory | Files |
| --- | --- |
| `Mycelium.Bloom/Components/UI/Organisms/DetailsPanel/` | `DetailsPanel.razor`, `.razor.cs`, `.razor.css`; new `PropertiesSummary.razor`, `.razor.css`, `PropertiesSections.razor`, `.razor.css` |
| `Mycelium.Bloom/ViewModel/DetailsPanel/` | `DetailsPanelPresentation.cs`, new `DetailsPanelPresentation.Semantics.cs`, new `PropertySectionPresentation.cs`, `DetailsPanelViewModel.cs`, `IDetailsPanelViewModel.cs`, `ElementLinkPresentation.cs`, `ExpressionPresentation.cs` |
| `Mycelium.Bloom/Components/Layout/` | `WorkspaceLayout.razor`, `WorkspaceLayout.razor.cs` |
| `Mycelium.Bloom/Components/Pages/Workspace/` | `Modelling.razor.cs` |
| `Mycelium.Bloom/Components/UI/Organisms/WorkspaceShell/` | `WorkspaceShell.razor.cs` |
| `Mycelium.Bloom.Tests/Components/UI/Organisms/DetailsPanel/` | `DetailsPanelTestFixture.cs`, `ExpressionPanelTestFixture.cs`, new `DetailsPanelIntegrationTestFixture.cs` |
| `Mycelium.Bloom.Tests/ViewModel/` | `DetailsPanelViewModelTestFixture.cs`, `ElementSemanticsTestFixture.cs`, new `DetailsPanelSemanticPresentationTestFixture.cs` |
| `Mycelium.Bloom.Tests/ViewModel/ProjectBrowser/` | `ProjectBrowserElementPresentationTestFixture.cs` (replace removed Back API with explicit canonical inspection) |
| `Mycelium.Bloom.Tests/Components/Layout/` | `WorkspaceLayoutTestFixture.cs` |
| `Mycelium.Bloom.Tests/Components/Pages/Workspace/` | `ModellingTestFixture.cs` |

## Integration into #76

Apply #80 first, then this working-tree patch; do not base it on `development`. The only Browser test
adjustment migrates the removed Details history call; it preserves the Browser's existing canonical
identity assertions. Existing Browser unresolved items remain as documented in its placement report.

The history interface removal must travel with the Details and workspace/test updates. The footer
registration and WorkspaceShell main-pane reveal must travel together to preserve narrow-layout
navigation. Retain `SdkExpressionText` and Browser classification/initial-state helpers used by both
features. There are no new dependencies, persistence changes, remote subscriptions or editing flows.

The supplied reference pack now supports Attribute, Part and State layout review; see the current
visual matrix for supported corrections and remaining scope/data limitations.
The initial implementation pass performed no remote writes. The subsequent authorized review
checkpoint includes this implementation; publishing it does not update PR #76, issues or reviews.
