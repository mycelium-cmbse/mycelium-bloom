# Project Browser placement and Properties handoff

Browser work: [#80](https://github.com/mycelium-cmbse/mycelium-bloom/issues/80).
Properties work: [#81](https://github.com/mycelium-cmbse/mycelium-bloom/issues/81).
Both refine [#75](https://github.com/mycelium-cmbse/mycelium-bloom/issues/75) and
the implementation in [#76](https://github.com/mycelium-cmbse/mycelium-bloom/pull/76).

## Authority and interpretation

- Figma [Browser 3250:2](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=3250-2) governs general Browser composition. [Unified Browser / Filter Bar 8563:22](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=8563-22) governs the single search/filter surface. The separate funnel/drawer is removed; Contains, multiple Types, removable criteria, keyboard suggestions and Clear all share the field and popover.
- [Part Tree 3250:411](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=3250-411) is supporting visual language only. No VALUE column, budgets, publication workflow or second tree mode is implemented.
- [Browser Column 54:490](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=54-490) supports column layout and exported icons. [Tree Node 22:179](https://www.figma.com/design/xsoA6uSGmYKNFxgtLm67iG/Bloom?node-id=22-179) supports component states; the earlier instruction making it the row-anatomy authority is superseded.
- [Antoine's review](https://github.com/mycelium-cmbse/mycelium-bloom/pull/76#pullrequestreview-5652824034) controls semantic placement and the six collapsed kind groups. The column sample does not establish all six groups.
- The [project SSS](https://github.com/mycelium-cmbse/requirements/blob/b9c2f3daf2a30eb6c3a2543fa17d5fea4da32661/Software-System-Specification%20-%20Project.md) and full SSS were checked. Their authoring requirements do not add editing, drag-and-drop, live updates or tabular-browser workflows to this correction.

The name-first composition is reconciled with Antoine's required visible kinds by retaining compact inline guillemets **after name, type, multiplicity and value**. Visibility uses labelled + / − / # markers; model-supported indicators use labelled icons. His illustrated prefix ordering and the composed Figma sample cannot both be reproduced literally. This explicit interpretation retains the information and name hierarchy, without claiming identical ordering or exact parity.

Live design-context reads of all three newly supplied frames, and read-only Plugin API inspection, were blocked by the Figma View-seat MCP quota. The public viewer returned CloudFront HTTP 403. The locally available Bloom.pdf export (233 pages; PDF fingerprint f4cc280b2fc1506c98a909c743dc75f0), including page 47, supports the composed name-first Browser, distinct icons, NAME/OWN, selection and focus. It exposes neither node IDs nor the Filter Bar specification, so exact correspondence to the new nodes cannot be verified. Earlier live contexts for 54:490 and 22:179 remain supporting evidence only. **Exact parity with 3250:2 and 8563:22 remains unverified until their current context/export is available.**

Node creation/modification metadata and Figma version history were unavailable through the quota-blocked tools; the PDF has no creation/modification metadata. No chronological relationship is asserted or inferred from node IDs.

Rows retain 32px interaction targets with 12px names, 16px indentation, compact secondary notation, specific exported/Lucide symbols, a continuous selected background and visible focus outlines. The 40px OWN cells are reserved and aligned with their heading, without invented domain values. The NAME column now stays within the available width and ellipsizes long notation, consistent with the supporting exported Browser composition (page 47, ReactionWheelAs…); the full notation remains in the treeitem accessible name and native hover tooltip. Filtering, local expansion and independent instances remain. This is supporting evidence for the bounded layout, not verification of the unavailable primary frame. No inert Add control is introduced.

## Placement matrix

Membership role is evaluated before broad SDK interfaces. Exclusion suppresses an entire owned subtree; transparent membership traversal alone may promote its member.

| Element kind / owning role | Browser placement | Applicable row data | Canonical owner and intended Properties destination |
| --- | --- | --- | --- |
| Package, LibraryPackage, classifier, ordinary namespace | Structural | Kind/name, available library and standard-library indicators | Package/namespace: Imports, Memberships, Type hierarchy |
| Part, Item, Port, Attribute definitions/usages | Structural | Kind/name; available feature types, multiplicity, scalar attribute value; conjugation indicator on ports | Element: Classification, Feature properties, Values |
| EnumerationDefinition | Structural | «enum def» and name; intrinsic enumeration variation is not a variation-point badge | Definition: Enumeration values |
| Ordinary enum-typed EnumerationUsage | Structural attribute row | Name/type/multiplicity/value where available | Usage: Classification/Values |
| Action/Perform, State/Exhibit, Requirement, Constraint, Calculation, Analysis/Verification Case, Metadata definitions/usages | Structural, except detail roles below | Specific kind/name, available feature type/multiplicity; parallel-state and variation indicators | Element-specific Properties variant |
| ConnectionDefinition/Usage, BindingConnector | Connections group | Canonical model rows | Relationship: Ends |
| InterfaceDefinition/Usage | Interfaces group | Canonical model rows | Interface: Ends |
| FlowDefinition/Usage, SuccessionFlowUsage | Flows group | Canonical model rows | Flow: Ends/Item flows |
| AllocationDefinition/Usage | Allocations group | Canonical model rows | Allocation: Relationships |
| SatisfyRequirementUsage | Satisfies group | Canonical model rows | Requirement: Relationships |
| Usage through VariantMembership, except enumeration values | Variants group | Canonical usage row; variation point retains its own structural row | Variation point: Variants |
| Ordinary Membership, including non-owning alias | Wrapper elided | Member's kind; occurrence's alias name/visibility when supplied | Namespace/type: Memberships |
| Resolved imported membership | Member rendered by its own classification | Imported indicator from SDK importedMembership; occurrence name/visibility | Importing namespace: Imports |
| EndFeatureMembership or feature with IsEnd | Excluded subtree | No independent row | Owning connector/association/connection/interface/flow: Ends |
| StateSubactionMembership, TransitionFeatureMembership | Excluded subtree, including nested domain usages | No independent row | State/transition owner: State behaviour (entry/do/exit, trigger/guard/effect) |
| RequirementConstraintMembership | Excluded subtree | No independent row | Requirement: Assumed/required constraints |
| FeatureValue, ResultExpressionMembership | Excluded subtree | Supported scalar attribute literal inline on owner | Owning feature/calculation: Values/Expressions |
| Expression/literal not a domain usage | Excluded subtree | No independent row | Owning feature/constraint/calculation: Values/Expressions |
| Multiplicity/MultiplicityRange and bound expressions | Excluded subtree | Supported multiplicity inline on owner | Owning type/feature: Multiplicity |
| Succession, SuccessionAsUsage, kernel SuccessionFlow (except domain SuccessionFlowUsage) | Excluded subtree | Initial-state marker remains derived from the canonical succession on its target state | Owning state/action/feature: Control sequencing/Relationships |
| TransitionUsage | Excluded subtree | No independent row | Owning state: State behaviour/Transitions |
| EnumerationUsage owned by EnumerationDefinition (SDK representation of an enum literal) | Excluded subtree, including variant membership | No independent row | EnumerationDefinition: Enumeration values, ordinal, documentation |
| Comment, Documentation, TextualRepresentation | Excluded subtree | Documentation indicator on annotated/owning element | Annotated/owning element: Comments/documentation/text |
| Applied MetadataUsage | Excluded subtree | Metadata indicator on annotated/owning element | Annotated/owning element: Metadata |
| Imports | Excluded subtree; resolved imported members are separate occurrences | No import declaration row | Importing namespace: Imports (kind, target, recursion, visibility) |
| Typing, specialization, subsetting, redefinition, conjugation | Excluded subtree | Available feature type inline | Owning type/feature: Specializations/Type hierarchy |
| Dependency and other non-structural relationships | Excluded subtree | No independent row | Owning element: Relationships |
| Unknown kind | Generic structural fallback | SDK metaclass kind/name, neutral outlined symbol | Canonical element's Properties |

Constraint and calculation usages can also implement IExpression; this alone never excludes a domain usage. An EnumerationUsage is an enum-typed attribute in SDK 0.23.0; only values owned by an EnumerationDefinition are treated as enum literals.

The pinned SDK makes ISuccessionAsUsage an IUsage, IConnector and ISuccession; kernel SuccessionFlow is also ISuccession. A broad connector/usage fallback previously exposed these relationships. Antoine explicitly groups **SuccessionFlowUsage** in Flows, but does not explicitly list plain Succession in Tier A/B. Treating generic sequencing as Tier C is an interpretation of his non-structural relationship rule, corroborated by SSS-PA-BEH-Q4N (Succession relates features in execution order). The projection now checks IFlowUsage and ISuccession before broad fallbacks. Tests assert exact state children, unchanged initial target/source references and reveal of both hidden succession and its end to the visible owner without replacing shared canonical selection.

## Navigation and state contract

Groups are presentation-only nodes: SourceElement, ElementId and ElementType are null. They are collapsed by default and toggled without publishing application selection. Filtering shows a group only when it contains a visible model row; temporary filtered expansion does not overwrite stored expansion.

Every model occurrence retains the canonical SDK element. Aliases/imports can repeat that identity with unique Browser occurrence IDs. Alias/import reference cycles end at a repeated ancestor occurrence. Hidden elements and their owned relationships remain indexed against their nearest visible owner. Focus expands the path and selects that local owner without changing shared selection or clearing filter criteria; a nonmatching active filter can still hide the focused row. No model elements are deleted or reparented.

The [W3C Tree View pattern](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/) governs local keyboard behavior: one roving treeitem tab stop; Up/Down through rendered rows; Right expands or enters the first child; Left collapses or reaches the parent; Home/End reach the first/last visible row; Enter/Space activate the established component handler. Type-ahead searches visible names. Focus movement does not publish model selection. Presentation-only group activation toggles expansion only. Active filters continue to force the matching ancestor/group path open; Left on such a row reaches its parent instead of overwriting stored expansion. A removed focused descendant falls back to its nearest surviving ancestor. Listeners are local to each instance and disposed with the component. Pointer handlers are retained; internal buttons no longer add duplicate Tab stops.

The two Browser inline comments are addressed by a named NodeState record and the single-line ProjectBrowserElementPresentation record declaration. Component handlers retain responsibility for dispatching callbacks.

## Acceptance pass: resolved facets and precise dependencies

The bundled Quantities ownership graph contains 114 elements, with **zero MetadataUsages, StateUsages, VerificationCaseUsages and unit-bracket expressions**. Absence in that demonstration does not establish absence of SDK capability. Controlled fixtures use actual POCO containment, references and memberships; the SDK's internal Id values are unique, as required by its writer cursor cache.

| Facet | Investigation and implementation | Verification | Status |
| --- | --- | --- | --- |
| Ownership | SDK IElement.ownedAnnotation -> IAnnotation.annotatingElement -> IMetadataUsage and canonical feature-value references can represent it. Row presentation currently displays only a generic metadata indicator. No Owner-to-Ownership badge resolver exists. RD-01 identifies Owner MetadataDefinition and Ownership ItemDefinition/ItemUsage, but supplies neither an unambiguous library identity/reference-feature path nor a colour source/mapping. IModelLoaderService currently loads the Quantities payload, not a Concurrent Design library or project metadata provider. | CONTRIBUTING, pinned 0.23.0 XML/API, application sources and resources, SSS-PT-NAV-7U5, SSS-PA-MGMT-YC1, RD-01 Ownership table, and repository search for the domain library/definition were inspected. No fabricated Owner fixture is claimed as implementation evidence. | **Category 3 dependency; Browser criterion incomplete.** SDK metadata APIs exist; the domain binding contract/data source and colour decision are missing. Supply the actual Concurrent Design library/schema and Owner reference path, label policy and colour mapping, then implement and test the resolver and populated OWN badges. This remains Browser work. |
| Default/initial state | Implemented in ProjectBrowserRowPresentation.IsInitialState. SysML Part 1 §7.18.2 defines an initial substate by a succession from the parent's entry action. SDK IStateDefinition/IStateUsage.entryAction, ISuccession.sourceFeature and targetFeature provide this directly. A unique unconditional target receives Initial, including an unguarded transition exposing its succession target; name/order are never used. Parallel parents, ambiguous starts and guarded/triggered entry transitions do not get an unconditional marker. | CreateStates places the initial state second; real EndFeatureMembership/ReferenceSubsetting ends resolve to entry and target. Acceptance facet tests and a real node-component test verify the marker. | **Category 2 fixed; now Category 1 for the bundled model.** Conditional execution is not evaluated or misrepresented as a known default. |
| Verification verdict | Implemented for definitions and usages through their result feature, a single explicit FeatureValue and a canonical FeatureReferenceExpression to a value of standard-library VerificationCases::VerdictKind. No invocation is executed. The referenced standard enum supplies pass/fail/inconclusive/error; arbitrary same-named user enums and absent/ambiguous results are rejected. | Real return membership, enum definition, variant literal, typing and reference fixtures; four value cases; definition case; nonstandard/renamed library and absent/ambiguous negatives; accessible outlined BbBadge rendering. | **Category 2 fixed; now Category 1 for the bundled model.** A calculated result with no recorded value cannot be displayed as an execution verdict. |
| Unit-bearing attribute value | ProjectBrowserRowPresentation uses ElementPresentation.GetExpressionText. The bounded SDK writer serializes the stored bracket expression, including its canonical SI unit reference. No unit is inferred from a label or assigned by the UI. | Controlled 3[SI::kg] follows SI.sysml's attribute <kg> kilogram : MassUnit declaration. Real bracket/parameter/feature-value/reference structure reaches the real row component and screenshot. | **Category 2 fixed; now Category 1 for the bundled model.** This presents stored notation, not a converted or evaluated quantity. |
| Expression multiplicity | GetMultiplicity now formats nonnegative integers, unbounded upper values, supported reference/arithmetic/conditional/operator notation, ranges and singleton/equal-bound notation. GetExpressionText reuses SdkExpressionText with bounded complete-operand/reference checks. Conditional right operands use the SDK's owned-expression-reference form; incomplete forms are omitted rather than producing truncated text. | Exact [capacity], [0..Assembly::count + 1], [Assembly::count + 1], [Assembly::count + 1..*] and coalescing cases; literal singleton/range/infinity cases; malformed reference/operator/deferred-operand negatives; owner-row rendering. The existing canonical inspection trail remains tested. | **Category 2 fixed for supported forms; full SSS-PA-VIS-U7M remains visibly incomplete for forms outside the bounded writer.** |

The exact remaining expression dependency is safe, complete serialization for generic/body or otherwise unsupported expression shapes. The pinned SDK exposes imperative BuildOwnedExpression rather than a public capability/completeness result; its grammar validation helpers are internal and its generic fallback can recurse or produce incomplete notation for incompatible structures. The existing bounded SdkExpressionText adapter explicitly rejects generic unsupported forms. Supporting additional shapes needs a bounded adapter/SDK fix with canonical fixtures; omitting those forms is not full expression-text acceptance and is not reassigned to Properties.

The ownership wording also needs care: the review asks for badges on every node, while Tree Node explicitly has an ownership=none variant. Missing annotations remain that unassigned state, following the instruction not to invent ownership. This does not satisfy rendering for elements that actually carry Owner metadata.

## Exact repeated-occurrence assertions

The real-model repeat is **an alias, not an import**. QuantityValue is a non-owning alias of TensorQuantityValue. VerifyQuantitiesKeepsConstraintsAndTheirCanonicalExpressionInformation asserts, for each of dimensions, orderSum and boundMatch:

- Exactly two occurrences, in this order: Model/Quantities/TensorQuantityValue/{name} and Model/Quantities/QuantityValue/{name}.
- Both rows have the same canonical SourceElement and ElementId, but different Browser occurrence Id values.
- Both descendant rows retain the same owning FeatureMembership, visibility public, and no Imported indicator.
- Their canonical owner is TensorQuantityValue. The two container rows share that canonical element but have different occurrence memberships; the alias membership is non-owning and has MemberName QuantityValue.
- Selecting the alias occurrence followed by FocusElement deterministically reaches the first canonical occurrence. Revealing the excluded dimensions expression reaches that owner and retains the active dimensions filter.

The separate VerifyAliasAndImportedOccurrencesKeepCanonicalIdentityAndMembershipData uses real Namespace, Membership and MembershipImport objects. It asserts exact labels [Alias, Imported alias], visibility [private, protected], one shared SourceElement, distinct row IDs, exact occurrence membership references, Imported only on the imported row, and unchanged public visibility on the source membership. Effective imported visibility comes from importingNamespace.VisibilityOf, fixing the previous source-visibility error. Selecting the imported occurrence and then focusing the canonical element deterministically reaches the first alias row without replacing shared canonical selection.

Group fixtures cover all six reviewed groups, header identity without a model element/type/ID, header interaction with both empty and populated shared selection, ancestor/group filter paths, hidden-owner reveal and independent Browser instances. Known exclusions precede broad usage/classifier fallbacks; standalone constraint/calculation usages and genuine unknown-kind fallback remain intact.

## Inspectable visual evidence

Current screenshots and measured checks are committed in [evidence/project-browser-80](evidence/project-browser-80/visual-checks.json). They use the **real interactive ProjectBrowser component and ViewModel**, hosted by an external local Blazor fixture. Its controlled model reuses BrowserAcceptanceModelTestFactory and real POCO memberships, feature typing, references and stored values. It is not the bundled Quantities model. No ownership data is fabricated.

| Screenshot | Viewport | Evidence |
| --- | --- | --- |
| [collapsed light](evidence/project-browser-80/collapsed-light-300.png) / [dark](evidence/project-browser-80/collapsed-dark-300.png) | 300 × 900 | NAME/OWN, name-first rows, kind symbols, collapsed Connections |
| [selected light](evidence/project-browser-80/expanded-selected-light-1100.png) / [dark](evidence/project-browser-80/expanded-selected-dark-1100.png) | 1100 × 900 | Full long row, expression bounds, 3[SI::kg], initial-state and verdict markers, protected imported occurrence |
| [selected light](evidence/project-browser-80/expanded-selected-light-300.png) / [dark](evidence/project-browser-80/expanded-selected-dark-300.png) | 300 × 900 | Narrow-sidebar density, ellipsis and persistent NAME/OWN alignment |
| Historical metadata-scrolled light/dark files | 300 × 900 | Retained previous-checkpoint evidence only; superseded by constrained rows and complete accessible/hover notation |
| [unified filters light](evidence/project-browser-80/unified-filters-light-300.png) / [dark](evidence/project-browser-80/unified-filters-dark-300.png) | 300 × 900 | Single field, model Type options and Clear all |
| [keyboard focus light](evidence/project-browser-80/keyboard-focus-light-300.png) / [dark](evidence/project-browser-80/keyboard-focus-dark-300.png) | 300 × 900 | Roving treeitem keyboard target and visible focus ring |
| [hover light](evidence/project-browser-80/hover-light-300.png) / [dark](evidence/project-browser-80/hover-dark-300.png) | 300 × 900 | Hovered model row alongside the selected row |
| [filtered light](evidence/project-browser-80/filtered-light-300.png) / [dark](evidence/project-browser-80/filtered-dark-300.png) | 300 × 900 | Exactly Model → Connections → powerLink |

The JSON records viewport/row/header/OWN geometry, full accessible notation, interactions, console errors and failed asset requests. The Chromium stress run passed **101 immediate Type-accept / reopen / Clear-all cycles**, waiting only for the committed criterion token before reopening, without waiting for portal detachment or introducing delays. Pointer/ArrowDown opening, Escape, group activation without model selection, arrows, Home/End, row selection, visible keyboard focus, filter paths and restored expansion/selection were exercised. Both 300px and 1100px viewports have scrollWidth equal to clientWidth. The final visual rerun passed another 21 cycles after the focus-outline CSS correction. Fourteen current light/dark screenshots were inspected; the two historical metadata-scrolled screenshots are not current acceptance evidence. Component and DOM tests retain canonical selection, repeated occurrences, independent instances, focus recovery and disposal coverage. This is keyboard interaction evidence, not a screen-reader conformance audit. Native title supplies pointer hover text; the complete treeitem accessible name supplies keyboard/assistive-technology metadata.

Earlier static-render screenshots and Figma exports remain outside the repository at %TEMP%/bloom-80-evidence/ as historical supporting artifacts. The bundled Quantities model cannot demonstrate state, verdict, unit or ownership facets.

Measured selected name contrast is 4.96:1 light / 14.03:1 dark; symbol contrast is 4.96:1 / 4.85:1. Secondary kind/type text measures 4.31:1 light / 6.33:1 dark. OWN heading and row slots share the same right edge (292px at 300px, 1092px at 1100px).

The supporting Tree Node's exact light selected mono pair remains #64748b on #e8f7f2, **4.31:1**, below 4.5:1 for small text. Selected names and symbols use contrasting existing light/dark tokens. The mono contrast remains an explicit design decision, not a completed acceptance claim. Populated OWN cells and exact comparison with the unavailable primary frames remain incomplete.

## Resolved unified-filter lifecycle race

[rapid-reopen-check.json](evidence/project-browser-80/rapid-reopen-check.json) retains the reproduced failing baseline and records the corrected scenario. Inspection of BlazorBlueprint 3.17.0 found that BbFloatingPortal.CleanupAsync awaits positioning/exit teardown before unregistering its portal ID. Reusing a popover context ID allowed the previous cleanup to unregister newly opened content. DOM evidence showed aria-expanded=true while the portal disappeared; this was not simply an absent Type or a legitimate close callback.

Each opening now receives a distinct keyed popover/context/portal identity, with generation-bound callbacks rejecting stale close notifications. The SearchInput stays mounted and enabled; asynchronous owner callbacks cannot close the user's next opening. ProjectBrowserSearchAnchor registers the persistent element for positioning and synchronizes its DOM ID with the current Blueprint TriggerId so the field is also recognized by outside-click exclusion. A preliminary stress run exposed an intermittent outside-click callback exception before that trigger identity was connected; the final 101-cycle run passed with no browser exceptions or failed assets. No delay, click throttling, input disabling or dependency upgrade was introduced.

VerifyRapidReopenSurvivesPreviousPortalCleanup deterministically holds positioning disposal open, reopens before release, confirms a distinct portal ID and matching trigger ID, releases old cleanup, verifies the new surface survives, invokes a stale close callback and verifies Clear all still works. VerifyUnifiedFieldAcceptsInputWhileCriterionOwnerCompletes keeps an owner callback pending and verifies the next draft and the exact native input component instance survive. The real-browser script exercises the previously failing sequence without waiting for old content to detach.

## Validation and separate Properties handoff

Follow-up validation used the existing ignored bin/browser-80-validation artifact directory, preserving the running application and all earlier semantic changes. Docker was not retried while its daemon remained unavailable. No dependencies changed. Styles were generated/verified through the repository MSBuild targets.

- 298 focused Browser/formatter/consumer tests passed; the final complete .NET suite passed 883 tests under dotnet-coverage.
- CI-style Release build (--no-incremental /p:ContinuousIntegrationBuild=true): zero warnings/errors; local publish passed.
- VerifyMyceliumStyles passed; 37 token-consumer tests passed. JavaScript coverage pipeline passed: 10 converter tests, 62 interop/token tests, 96.56% line coverage overall and 100% for the new Browser module.
- Fourteen current real-component screenshots and 101 immediate-reopen cycles passed in Chromium; no console errors or failed assets. Keyboard and narrow-layout assertions passed as described above.
- The interrupted session left a malformed partial test edit; it was repaired before validation. The first new deferred-cleanup test registered services after its portal host had initialized; it now uses an isolated context with services registered before rendering. These intermediate failures are not claimed as successful checks.

Commands used from the repository root (MSBuild style and npm commands from Mycelium.Bloom):

~~~powershell
dotnet test Mycelium.Bloom.Tests/Mycelium.Bloom.Tests.csproj --artifacts-path "$PWD/Mycelium.Bloom.Tests/bin/browser-80-validation" --no-restore --filter 'FullyQualifiedName~ProjectBrowser|FullyQualifiedName~ElementPresentation|FullyQualifiedName~ElementSemantics' --verbosity minimal
dotnet build Mycelium.Bloom.sln --artifacts-path "$PWD/Mycelium.Bloom.Tests/bin/browser-80-validation" --configuration Release --no-restore --no-incremental /p:ContinuousIntegrationBuild=true
dotnet-coverage collect "dotnet test Mycelium.Bloom.sln --artifacts-path $PWD/Mycelium.Bloom.Tests/bin/browser-80-validation --configuration Release --no-restore --no-build --verbosity minimal" -f xml -o "$env:TEMP/bloom-80-results/followup-coverage.xml"
dotnet publish Mycelium.Bloom/Mycelium.Bloom.csproj --artifacts-path "$PWD/Mycelium.Bloom.Tests/bin/browser-80-validation" --configuration Release --no-restore --no-build --output "$env:TEMP/bloom-80-final-publish"
dotnet msbuild Mycelium.Bloom.csproj -t:RegenerateMyceliumStyles
dotnet msbuild Mycelium.Bloom.csproj -t:VerifyMyceliumStyles
npm run test:tokens
npm run test:coverage:sonar
node "$env:TEMP/bloom-80-interactive/followup-stress.cjs"
node "$env:TEMP/bloom-80-interactive/followup-check.cjs"
~~~

The external fixture host and Chromium drivers are local validation tools; the canonical fixture factory, screenshots, measured results and the baseline/corrected scenario are in this checkpoint. The SDK fixture deliberately exercises explicit stored data, without domain ownership or evaluated results.

Properties implementation remains unchanged. #81 still receives ends, state behaviour, enum values, imports, annotations, expression detail and relationship sections from the placement matrix. Ownership badges, Browser initial/verdict markers, inline units and expression multiplicity remain requirements of **#80**, not substitutes in #81.

**Browser #80 is not fully accepted:** ownership presentation still requires its domain contract/provider, unsupported expression shapes remain outside full SSS-PA-VIS-U7M coverage, the selected mono contrast needs an explicit design decision, exact parity with the new primary frames is blocked by access. Passing automated tests does not close these criteria.
