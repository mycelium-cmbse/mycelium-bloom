# Shared theme consumption

Bloom consumes `Mycelium.Design.Bloom` 1.1.0 from the configured NuGet feed.
[mycelium-design](https://github.com/mycelium-cmbse/mycelium-design) owns DTCG
validation, transformation and publication. Bloom owns component CSS, Blueprint
bridges and application Tailwind compilation. No token workspace or Node/npm is
needed to build or publish; Node 24 remains required for JavaScript tests only.

## Build and upgrade

Update the explicit package version in `Mycelium.Bloom.csproj`, then restore and
build with the .NET 10 SDK:

```sh
dotnet restore Mycelium.Bloom.sln --configfile Nuget.Config
dotnet build Mycelium.Bloom.sln
```

MSBuild validates `MyceliumDesignBloomTokensCssPath` and
`MyceliumDesignBloomThemeCssPath`, supplied by the package through
`MyceliumDesignBloomCssDir`. It stages the runtime file at
`wwwroot/css/tokens.css` and the build-time mapping at
`Styles/AutoGen/MyceliumDesign/theme-mycelium.css`. These ignored files are
replaced from the package on each build; never edit or commit them. The package
is content-only and does not provide an RCL `_content` URL.

The theme is explicitly included as a `None` item with `AutoGen=true`, even before
the first build stages it. SonarScanner for .NET excludes this generated item from
its analysis file list; the directory name alone does not provide that metadata.
SonarQube Cloud centrally excludes `**/AutoGen*/**/*` and `**/wwwroot/css/*.css`
from source and duplication analysis. The latter covers every CSS file directly
in `wwwroot/css`, including authored files, but not nested directories or other
file types. Authored `Styles/tailwind.css` remains eligible for analysis; the
workflow does not override the central exclusions.

The package's `css/tokens-bloom.css` is served as `css/tokens.css`, preserving
the existing application URL and ASP.NET asset fingerprinting. The former
checked-in `tokens.css` is removed; only the ignored package copy occupies that
path. There is no second runtime token file.

Normal builds now run the pinned, checksum-verified standalone Tailwind 4.3.3
CLI before static-web-asset discovery. This intentionally replaces the previous
committed-CSS-only build contract. Recompiling on every build handles package
upgrades, downgrades, older package timestamps and changed Razor classes without
an incremental cache that could silently retain stale mappings. Missing package
assets fail before stale staged files can be used. IDE design-time builds skip
this work. First builds and publishes explicitly include both generated assets.

`app.css` remains committed for review. After building, review its diff with the
package or source change. The compiler scans application Components, ViewModel
and Model directories, excluding test fixtures and local evidence.

From the application directory:

```sh
dotnet msbuild -t:RegenerateMyceliumStyles
dotnet msbuild -t:VerifyMyceliumStyles
```

Verification compiles separately under `obj/tailwind/verify/` and fails if the
committed output differs; it does not repair `app.css`. Restore before running
these standalone targets. Explicit regeneration supports `-p:TailwindWatch=true`.
Do not enable watch mode for a normal build or publish.

CI restores before CSS verification, and CodeQL builds use the same targets.
First-build/publish fixtures cover asset discovery without pre-existing staged
files. A clean SDK/container build needs network access to the pinned GitHub CLI
release (or its checksum-identical mirror); offline builds need the cached CLI.
Docker validation still requires a running daemon.

The CLI is downloaded to `obj/tailwind/4.3.3/` when absent and checked before each
execution. A verified cached CLI works offline. `TailwindFeedUrl` may point to a
mirror with the same release layout; checksums remain mandatory. Docker's SDK
stage now downloads and runs this executable; the runtime image receives only
published assets and needs neither Tailwind nor Node.

## Consumer contract

Styles load in this order: Blueprint primitives, Blueprint components, packaged
runtime tokens, application CSS, Blazor scoped CSS. Blueprint remains the theme
switching authority. Tokens define light values on `:root` and dark values on
`.dark`; portalled previews must retain their originating dark scope.

Use composite typography variables with `font: var(--typography-typography-body-md), sans-serif`
(or `monospace` for mono styles). Runtime spacing names are pixel dimensions;
Tailwind spacing names are quarter-scale steps (`p-4` maps to `--spacing-16`).
Do not interchange these namespaces by matching suffixes. Domain color names for
ownership, collaborators, lifecycle and SysML remain semantic runtime aliases.
The small Blueprint radius bridge references official numeric radius variables.

## Typography and application styling

Version 1.1.0 preserves typography tracking separately from composite fonts.
Use `--typography-typography-display-lg-letter-spacing` for display headings,
`--typography-typography-label-sm-letter-spacing` for compact labels and
`--typography-typography-label-xs-letter-spacing` for uppercase section labels.
Tailwind exposes the corresponding `tracking-display-lg`, `tracking-label-sm`
and `tracking-label-xs` utilities. A CSS `font` shorthand does not set tracking.

Use a complete composite font only when all its metrics are intended. The
NavigationRail selected menu state uses the official weight mapping alone;
ProjectBrowser counts retain label-xs metrics with label-sm weight. Named rail
markers derive their height from the official font using `1lh` plus padding.
Organization page headers combine the official 28px size, heading-md weight and
display-xl line height. Organization navigation uses the body-md-medium weight
mapping without resetting its 13px size. Focus outlines retain 1px geometry and
use the published 4px radius.

Bloom-specific borders and motion remain component-owned. They are not part of
the package's DTCG contract and do not require a local token system.

## Validation

Run `dotnet test Mycelium.Bloom.sln`, then in the application directory run
`npm ci --ignore-scripts` and `npm run test:coverage:sonar`. Consumer tests exercise
real package restore, CLI integrity, missing assets, changed mappings with older
timestamps, freshness verification and first-build/publish discovery in isolated
fixtures. Review light/dark controls, portals, workspace and scoped styles after
package upgrades. Keep screenshots and diagnostic logs in ignored `artifacts/`.
