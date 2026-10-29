# Details #81 review checkpoint

This checkpoint preserves Browser #80 at `cc737cb361783d207e4d448d7d1da85d23542302`
and the existing Details implementation. It completes the focused identity, typography,
wrapping and expression-placement checks. The [14-reference discrepancy matrix](details-panel-visual-discrepancies.md)
remains the design index; this report supersedes its earlier validation totals and real-workspace
dark-theme evidence. No new editing, publication, subscription or traceability workflow is introduced.

## Final discrepancy matrix

| Concern | Evidence / previous discrepancy | Checkpoint result |
| --- | --- | --- |
| Identity `name` | Browser and summary said `dimensions`; Identity said `Not declared` because it projected `DeclaredName` | `name` now projects SDK `.name`; the separate `declaredName` row retains `Not declared` when the declaration differs or is absent. No model mutation or generated name. |
| Ordinary values and inactive tabs | Attribute Properties CSS specifies Inter 12px/500; computed size/weight already matched | Retained 12px/500, with 15px value and 16px tab line heights. Actual font remains a documented dependency below. |
| Header | Reference 14px/600/17px; local line height was 18px | Uses existing `--spacing-17`; other header dimensions unchanged. |
| Relation chips | Blueprint inherited 600 weight; every category used teal | Explicit 9px/500/11px. Direct specialization/redefinition/subsetting uses the info palette; typing keeps the accent/primary palette; other roles use neutral tokens. |
| Qualified name | Final `dimensions` identifier split mid-word | Break opportunities at namespace boundaries; complete text preserved; unusually long identifiers can still wrap within their segment. Label column stays 112px. |
| Feature-chain expression | Attribute Relations listed it as an owned feature because SDK expressions implement `IFeature` | Non-usage expressions move to Properties → Value references, with a canonical root-expression link and existing target/referent links. Domain usages remain owned features; other element kinds keep their existing enumeration. |
| State | Supplied `state/01-properties` PNG/CSS supports the State-specific layout | Implemented Identity, state machine, state/parallel-region cards, actions and transitions remain mapped to that reference. No missing-State-reference claim. |
| Empty model data / unsupported actions | Figma examples contain people, dates, files and editing actions | No invented content or inert controls. These are data/scope differences, not styling defects. |

## Naming provenance: real Quantities model

Selected `AttributeUsage`: `334779bf-6357-5adf-a7c5-ffb32c2ffd17`.
The values below were read from the pinned SDK objects loaded by the application and independently
asserted using the bundled model in `ElementSemanticsTestFixture`.

| Surface / property | Actual source | Actual value |
| --- | --- | --- |
| Browser row | `ElementPresentation.GetDisplayName`: declared name, declared short name, then SDK effective name/short name | `dimensions`, from `.name` because both declarations are null |
| Properties summary | `DetailsPanelPresentation.DisplayName` → shared display-name formatter for this Attribute | `dimensions`, from the same SDK `.name` |
| Identity `name` | Cached `Effective name` fact → SDK `IElement.name` | `dimensions` |
| Identity `declaredName` | `IElement.DeclaredName` | null, displayed as `Not declared` |
| Identity `qualifiedName` | SDK `IElement.qualifiedName`, not a string assembled in Razor | `Quantities::TensorQuantityValue::dimensions` |
| SDK `NamingFeature()` | Redefined `Feature`, `b81e8170-64bd-590e-869f-6de1ad059600` | Declared/effective name `dimensions`; its own qualified name is null |

The owned `Redefinition` (`ab82b7af-2f73-5f64-b9a6-75976f5f8b9b`) points to that same naming feature.
The old `Not declared` correctly described the absent declaration, but its label incorrectly implied
an absent effective name. Genuinely unnamed elements now show `No effective name`; a semantic UI
fallback such as an anonymous kind label is never written into the Identity name field. Incomplete
SDK reads retain their existing unresolved explanation. Differing effective/declared short names
also remain distinguishable. No declaration, model object or canonical selection is changed.

Regression coverage: extended `VerifyShortAndEffectiveNameProvenance` and
`VerifyQuantitiesRedefinitionsAndValueNotation`; new
`VerifyAnonymousIdentityDoesNotUseADisplayFallbackAsAnSdkName` and
`VerifyDimensionsIdentityWrappingAndValueExpressionNavigation`.

## Typography and relation colours

The sources are `figma-copy-css/attribute/01-properties.txt` and `02-relations.txt`, paired with
their PNGs in the supplied archive. Final Chromium computed styles, collected after transitions
settled in both actual application themes, are:

| Representative element | Size | Weight | Line height |
| --- | --- | --- | --- |
| Inactive tab | 12px | 500 | 16px |
| Ordinary Identity / relation value | 12px | 500 | 15px |
| Panel heading | 14px | 600 | 17px |
| Selected-element name | 18px | 600 | 22px |
| Section heading | 10px | 600 | 12px |
| Attribute kind chip | 10px | 600 | 12px |
| Redefinition chip | 9px | 500 | 11px |

All samples compute `Inter, ui-sans-serif, system-ui, sans-serif`. CDP
`CSS.getPlatformFontsForNode` reports **Segoe UI Semibold**, PostScript `SegoeUI-Semibold`,
`isCustomFont: false`, for their rendered glyphs. This is an unresolved **font-delivery dependency**:
`--font-sans` resolves through `--typography-typography-body-md-font-family` to Inter, but the
application supplies no Inter font files or `@font-face` source. This environment has no usable
Inter face. Matching computed 500 does not establish Inter's visual weight or metrics. No weight
was lowered to compensate for the fallback, and no global typography or dependency was changed.

Light specialization foreground/background resolve to the reference's `#2B66C9` / `#E8F2FF`
through `--info` / `--info-subtle`. Those upstream tokens do not provide a distinct dark pair.
The scoped dark treatment therefore blends existing `--info` with `--foreground` (65% info) and
`--card` (20% info). Measured dark sRGB values are approximately `#6F96D8` / `#1A2842`.
This uses the existing colour scheme and tokens; it does not copy light-only colours into dark mode
or claim a supplied dark Figma reference. Full computed colours and actual fonts are in
[rendered acceptance](evidence/details-panel-81-checkpoint/rendered-acceptance.json).

## Expression placement and navigation

`FeatureValue` `edf6fbf8-7c23-54fd-9430-b3867f9a7095` owns the value
`FeatureChainExpression` `b65cf530-f98d-59f7-862b-e3f0813ea566`. Its supported SDK notation is
`mRef.dimensions`. The broad owned-member traversal reaches that expression through the value
relationship, and `OfType<IFeature>()` previously admitted it to Relations → Owned features.
That was an SDK inheritance detail, not an additional domain feature declaration.

The [Browser placement handoff](project-browser-placement.md) puts FeatureValue and non-usage
expressions in Values / Expressions. Properties → Value references now includes the original
root object as `Value expression: mRef.dimensions`, alongside `Target: dimensions` and
`Reference: mRef`. The latter remains the canonical Attribute
`fe3c4ab9-061f-5390-b049-98192b2990a6` (`Quantities::TensorQuantityValue::mRef`).
The Value field and summary keep the notation. Opening the root displays its existing
`Feature chain` presentation; Browser local selection remains on `dimensions`. The integration
test asserts reference identity, not merely equal names. No Back/history workflow is restored.

## Final rendered acceptance

The real published application, using its bundled Quantities model, passed **24 tab/width/theme
checks**, **6 long-identifier fallback checks** and **1 expression-navigation check** with no
horizontal overflow, browser exceptions or failed assets. **46 typography samples** include both
computed styles and actual platform font usage. The application theme button was used, and the
theme class, colour scheme and panel background were checked after every tab render.

The full qualified name remains accessible as unchanged DOM text. At 320px it occupies three
namespace-aligned lines; at 380px, two; at 540px, one. `dimensions` remains one 15px-high line at
all widths. Each 112px label slot is preserved. A temporary DOM-only 345-character identifier
also wrapped without overflow in all six cases; it was restored immediately, never model data.

| Width | Properties light / dark | Relations light / dark |
| --- | --- | --- |
| 320px | [Light](evidence/details-panel-81-checkpoint/dimensions-properties-light-320.png) / [Dark](evidence/details-panel-81-checkpoint/dimensions-properties-dark-320.png) | [Light](evidence/details-panel-81-checkpoint/dimensions-relations-light-320.png) / [Dark](evidence/details-panel-81-checkpoint/dimensions-relations-dark-320.png) |
| 380px | [Light](evidence/details-panel-81-checkpoint/dimensions-properties-light-380.png) / [Dark](evidence/details-panel-81-checkpoint/dimensions-properties-dark-380.png) | [Light](evidence/details-panel-81-checkpoint/dimensions-relations-light-380.png) / [Dark](evidence/details-panel-81-checkpoint/dimensions-relations-dark-380.png) |
| 540px | [Light](evidence/details-panel-81-checkpoint/dimensions-properties-light-540.png) / [Dark](evidence/details-panel-81-checkpoint/dimensions-properties-dark-540.png) | [Light](evidence/details-panel-81-checkpoint/dimensions-relations-light-540.png) / [Dark](evidence/details-panel-81-checkpoint/dimensions-relations-dark-540.png) |

These captures use the real workspace height (1020px panel in an 1800×1100 viewport). The retained
fixture matrix uses 380×1000px panels for the native reference comparison, plus 320/540px variants.
Its Part/Attribute/State layout and keyboard/scroll checks remain useful; the header line-height
and relation-category/identity corrections above supersede the corresponding old pixels.
The earlier real-workspace script manually toggled the root theme class; Blazor can overwrite it
on rerender, so its images labelled dark are **not final dark-theme acceptance evidence**.
They are preserved as history. The new real-application evidence above supersedes those claims.

## Test inventory reconciliation

The Browser checkpoint was archived outside the working tree and its tests discovered without
switching branches or altering local changes. Comparing discovered test-case names as multisets
gives **884 baseline cases → 877 current cases**. Both include the same explicitly child-only
`VerifyCanonicalLibraryInChildProcess`, excluded from the normal run; the parent safety test
continues to invoke the isolated SDK traversal. Runnable counts are therefore:

**883 − 31 removed cases + 24 added cases = 876 passing cases.**

The [complete removed/added inventory](evidence/details-panel-81-checkpoint/test-inventory.json)
lists each parameterized case separately. The preceding 874-test checkpoint plus the two focused
identity/wrapping regressions produces the same 876 total.

| Removed / reorganized coverage | Current coverage / intentional retirement |
| --- | --- |
| Heading name precedence, unnamed fallback, SDK properties, documentation, field order | Retained and extended `ElementSemanticsTestFixture`, Browser formatter tests; `VerifyPartAndAttributeSectionOrderAndUnitValue`, annotation/metadata semantic tests, and shell rendering with mocked VM |
| Null/clear selection, replacement, accessible root, subscriptions and disposal | `VerifyEmptyStateAndAccessibleRoot`, `VerifyReactiveReplacementAndNullSelection`, owned/caller VM lifetime and exactly-once unmount tests; retained VM observer/disposal tests |
| Repeated rendering, refresh, remount and background selection | `VerifyCacheRefreshDisposalAndRemount` plus retained `VerifyPresentationCachesSelectionAndSupportsExplicitRefresh` |
| Expression notation/input navigation and independent Browsers | Three `VerifySupportedExpressionsAndCanonicalReferences` cases; real-model `VerifyExpressionNavigationPreservesIndependentBrowsersAndCanonicalSelection`; existing formatter tests and new dimensions root-expression integration |
| Advanced multiplicity inspection and return | Retained canonical multiplicity semantic tests; `VerifyUnboundedReferenceMatchesMultiplicityWithoutChangingCanonicalTarget` and real `baseQuantities` integration. Back action intentionally removed. |
| History trail reset, bounded history, repeated Back, and disclosure/diagnostic layout assertions | Obsolete UI contracts retired with those controls. No equivalent Back/disclosure behavior is claimed or reintroduced to preserve a test count. Canonical inspection, selection replacement, cache and disposal still have dedicated coverage. |
| New specialized presentation | State structure/parallel actions, every transition role, incomplete siblings, relationship ends, imports/type links, metadata/comments, canonical relation rows, infinity and footer integration |

Browser production implementation, generated tokens and SDK formatter are unchanged. The single
Browser test adaptation replaces the removed Details Back call with explicit canonical inspection;
its identity and independent Browser assertions remain.

## Validation and integration limits

- Final Release suite with `ContinuousIntegrationBuild=true`: **876 passed, zero failed**; unchanged
  explicit child-only case is listed as not executed by the outer runner. Coverlet: **91.75% line /
  81.36% branch** overall; DetailsPanelViewModel remains 100% line/branch.
- CI-style Release build (`--no-restore --no-incremental`, `ContinuousIntegrationBuild=true`):
  zero warnings/errors. Release publish and running-application validation passed.
- `VerifyMyceliumStyles` and **37 token tests** passed. No generated token or Tailwind artifact changed.
- Unchanged JavaScript interop/converter inputs retain the Browser checkpoint's recorded
  **62 interop/token cases, 10 converter cases, 96.56% line coverage**; the affected token checks
  were rerun. No redundant JavaScript coverage run was needed.
- Prior 72 fixture combinations and short-panel keyboard/scroll evidence remain linked in the
  visual matrix, with the superseded pixels/theme caveat above. They are not represented as a
  fresh run of the final source.
- Hosted SonarCloud/CodeQL and container build/security scanning were not run locally. The
  previously unavailable Docker daemon remains an external gate; no claim of remote CI acceptance.

Only source, tests, reports and portable screenshots/measurements belong to this checkpoint.
Temporary hosts, scripts containing local paths, build products, raw TRX/coverage files and
credentials are excluded. Integrate #80 before #81 into #76; keep the Details API removal,
workspace footer callback and narrow-layout reveal changes together. The current branch is
`81-align-details-panel-figma-semantic-presentation`; no PR, review thread or issue mutation is
part of publishing this checkpoint.

Remaining acceptance gaps are Inter delivery, exact pixel comparison from downsampled light-only
reference crops, unsupported model providers/workflows, and independent review/hosted gates.
The supplied State reference is available and mapped. Browser #80's ownership, unsupported
expressions, selected mono contrast and primary-design limitations remain untouched.
