# Current review

See [live comparison, current checks and retained evidence](details-panel-visual-discrepancies.md). The Inter validation below records the committed c4ab9ae checkpoint; it is not a fresh full-suite run.

# Details #81 review checkpoint

## Inter delivery follow-up

This bounded follow-up starts from reviewed checkpoint
`ae2782ba38d7de3d3374defdb477ce9058ab8d3a` on the existing Details branch.
**The Inter delivery gap is resolved.** The earlier report and screenshots below remain historical
records of the fallback-font checkpoint; the new evidence in this section supersedes their font
appearance and text metrics. The supplied Part/Attribute/State reference mapping remains unchanged.

### Cause and delivery

Before the change, the published app had no `FontFace` entries and made no font requests.
Chromium rendered `SegoeUI-Semibold`; a `FontFace` probe using only `local("Inter")` failed.
The existing `--font-sans` and semantic typography tokens already requested Inter. No usable face
was supplied by the application, its component stylesheets or this machine.

The application now self-hosts the unchanged normal and italic variable WOFF2 files from the
[official Inter 4.1 release](https://github.com/rsms/inter/releases/tag/v4.1).
[Source/version, archive and file hashes](../Mycelium.Bloom/wwwroot/fonts/inter/SOURCE.md) and the
[complete upstream SIL OFL 1.1 license](../Mycelium.Bloom/wwwroot/fonts/inter/LICENSE.txt) accompany
the assets. `NOTICE` also identifies the font and license location.

`App.razor` loads `fonts/inter/inter.css` through the existing `Assets` resolver. Its two
`@font-face` rules register CSS family `Inter`, normal/italic styles, **100–900** weight ranges
and `font-display: swap`. Relative URLs are served by `MapStaticAssets`; no CDN or local-font
source is used. Both fonts, stylesheet, license and provenance are present in the Release publish.
The upstream variable axes support `wght` 100–900 (default 400) and `opsz` 14–32 (default 14);
no font binaries or optical-sizing settings were modified.

All semantic tokens, font sizes, weights, component styles and Browser/Details ViewModels are
unchanged. Tailwind regeneration adds only `.italic{font-style:italic}` after discovering the new
font/provenance content; removing that additional utility reproduces the previous `app.css`
byte-for-byte. No existing layout or typography rule changes.

### Published application and actual rendered fonts

[Font acceptance measurements](evidence/details-panel-81-inter/font-acceptance.json) record a fresh
Chromium session against the Release publish in Production mode. Inter was not installed locally:
the local-only probe failed, while every sampled Inter glyph reports `isCustomFont: true` through
CDP `CSS.getPlatformFontsForNode`. Every measurement and capture waits for `document.fonts.ready`.

| Delivered file | HTTP / MIME | Bytes | Verification |
| --- | --- | --- | --- |
| `InterVariable.woff2` | 200 / `font/woff2` | 352,240 | Response SHA-256 matches the official asset |
| `InterVariable-Italic.woff2` | 200 / `font/woff2` | 387,976 | Response SHA-256 matches the official asset |

The upright face loads for the UI; the italic face loads on demand during the isolated style probe.
There is no font CDN request. Both faces report loaded with weight range `100 900`. Eighteen
temporary DOM probes verify normal and italic at each weight 100, 200, …, 900 using custom Inter
glyphs. These probes are removed before screenshots and never change model data.

| Representative UI text | Unchanged CSS size / weight / line height | Actual Chromium PostScript face |
| --- | --- | --- |
| Identity value | 12px / 500 / 15px | `InterVariable-Text-Medium` |
| Inactive tab | 12px / 500 / 16px | `InterVariable-Text-Medium` |
| Active tab | 12px / 600 / 16px | `InterVariable-Text-SemiBold` |
| Panel heading | 14px / 600 / 17px | `InterVariable-Text-SemiBold` |
| Selected-element name | 18px / 600 / 22px | `InterVariable-Text-SemiBold` |
| Attribute badge | 10px / 600 / 12px | `InterVariable-Text-SemiBold` |
| Redefinition badge | 9px / 500 / 11px | `InterVariable-Text-Medium` |
| Selected Browser label | 12px / 500 / 18px | `InterVariable-Text-Medium` |
| Workspace tab label | 12px / 600 / 15px | `InterVariable-Text-SemiBold` |

All **58 UI samples** in both themes use custom Inter faces, not the previous Segoe fallback.
Theme selection uses the application's button, with class, colour scheme and panel background
asserted after tab rendering. This is actual-font evidence in addition to computed CSS.

### Refreshed visual and interaction evidence

The real `dimensions` element passes **24 tab/width/theme checks**, six long-identifier fallback
checks, two Browser/workspace smoke groups, two short-viewport focus/scroll groups and one canonical
expression-navigation check (**35 checks**). No browser exceptions or failed assets occurred.

At 320/380/540px the qualified name occupies three/two/one namespace-aligned lines respectively;
`dimensions` remains unbroken, the full SDK string is preserved, and the label column stays 112px.
There is no horizontal overflow. Header/tabs/footer remain 46/38/56px. The footer stays at the panel
bottom and does not move when content scrolls in the short viewport. Home/End/ArrowRight activate
and focus the expected tabs with a visible outline. Browser rows remain 32px tall and retain
canonical keyboard selection and Show in Browser behavior. Project Browser, Editor 2 and Editor 3
tabs activate successfully; the workspace captures show their text and controls with Inter.

| Capture | Light | Dark |
| --- | --- | --- |
| Properties 320px | [PNG](evidence/details-panel-81-inter/dimensions-properties-light-320.png) | [PNG](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81-inter/dimensions-properties-dark-320.png) |
| Properties 380px | [PNG](evidence/details-panel-81-inter/dimensions-properties-light-380.png) | [PNG](evidence/details-panel-81-inter/dimensions-properties-dark-380.png) |
| Properties 540px | [PNG](evidence/details-panel-81-inter/dimensions-properties-light-540.png) | [PNG](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81-inter/dimensions-properties-dark-540.png) |
| Relations 380px | [PNG](evidence/details-panel-81-inter/dimensions-relations-light-380.png) | [PNG](evidence/details-panel-81-inter/dimensions-relations-dark-380.png) |
| Workspace / Browser / editor tabs | [PNG](evidence/details-panel-81-inter/workspace-light.png) | [PNG](evidence/details-panel-81-inter/workspace-dark.png) |
| Keyboard focus, 380px short panel | [PNG](evidence/details-panel-81-inter/keyboard-light-380.png) | [PNG](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81-inter/keyboard-dark-380.png) |
| Scrolled content / fixed footer | [PNG](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/evidence/details-panel-81-inter/scrolled-light-380.png) | [PNG](evidence/details-panel-81-inter/scrolled-dark-380.png) |

The [evidence directory](evidence/details-panel-81-inter) also includes Relations at 320/540px and
Comments/Attachments at 380px. Its 22 PNGs supersede fallback-font captures for current font
acceptance. Previous fixture screenshots remain useful for the unchanged semantic/variant mapping,
but are not evidence of the newly delivered font and were not silently overwritten.

### Validation and remaining limits

- **878 .NET tests passed**, zero failures: previous 876 plus two hosting cases for the normal and
  italic font. No tests removed. The existing stylesheet-order contract now includes the font CSS.
  Hosting tests follow the rendered stylesheet URL, check the same-origin asset responses, MIME,
  WOFF2 signature, official SHA-256 hashes and distributed license. The unchanged explicit child-only
  SDK test remains excluded from the outer count and is exercised by its parent traversal test.
- CI-style Release build (`--no-incremental`, `ContinuousIntegrationBuild=true`): zero warnings/errors.
  Release publish passed. Published font/license/provenance bytes match source.
- `VerifyMyceliumStyles`, **37 token tests**, and `git diff --check` passed. Initial stale-stylesheet
  failures were resolved through `RegenerateMyceliumStyles`; accepted token/render checks use that
  regenerated output. No generated semantic token artifact changed.
- Existing JavaScript interop/converter evidence remains applicable: no interop, converter or package
  inputs changed. The affected token/build checks were rerun; the unchanged full-suite functional
  evidence was retained after the additive generated CSS utility.
- Docker's Linux engine pipe is still unavailable. Container/security, hosted SonarCloud and CodeQL
  gates are not claimed as completed locally.

The font-delivery gap is closed. Exact visual parity remains limited by downsampled reference crops;
no pixel-perfect dark-mode parity is claimed from light-only references. State remains mapped to
its supplied reference. Unsupported workflows/data providers and Browser #80's existing acceptance
gaps remain unchanged. No model behavior, local model data, selection or navigation contract changed.


The earlier ae2782b narrative and test reconciliation remain in [the immutable report](https://github.com/mycelium-cmbse/mycelium-bloom/blob/c4ab9ae06de9695edd984dbec780f9e071b2c068/docs/details-panel-checkpoint.md#reviewed-checkpoint-ae2782b-historical).
