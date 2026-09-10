# Shared theme consumption

The DTCG designer export and approved shared contract decisions are the source of
truth. Bloom temporarily checks in two finished outputs from the validated
external token pipeline:

| File | Responsibility |
| --- | --- |
| `wwwroot/css/tokens.css` | Canonical runtime values and combined light/dark selectors |
| `Styles/Generated/MyceliumTokens/theme.css` | Value-free Tailwind `@theme inline` mappings, imported before compilation |

Both files carry generated-file headers and retain LF line endings. Never edit
their values manually. Bloom contains no DTCG parser, token transformer, package
resolver or external artifact staging step. The generated CSS is a delivery
artifact, not an independent design authority.

## Build a normal checkout

Install the .NET 10 SDK, Node 24 and the pnpm version declared in package.json.
Node must be on the environment PATH inherited by Rider/MSBuild; restart an IDE
opened before installing Node. From the application directory run
`pnpm install --frozen-lockfile`, then `pnpm run css:build`. This frontend build
does not need .NET restore or an external token directory.

Normal `dotnet build`, Rider builds and `dotnet publish` invoke the same frontend
entry point before static asset discovery. Restore .NET dependencies from the
repository's Nuget.Config. No unpublished token package or local feed is needed.

Docker compiles the frontend from the checked-in artifacts in its Node stage.
The SDK stage restores ordinary application packages, receives compiled app.css,
and publishes with `MyceliumCssPrebuilt=true`. This switch is only for frontend
output already compiled in the same build. No external context or restore secret
is required. The running application serves its published static assets and has
no dependency on Node, NuGet caches or the token pipeline.

## Update the temporary static delivery

1. Obtain the latest corrected DTCG export from design, preserving the original.
2. Use the validated external pipeline and its approved contract configuration.
   For the current v2 contract, run these commands in that pipeline workspace:

   ```sh
   node src/cli.mjs validate --zip <designer-export.zip> --config config/designer-export-v2.json
   node --test tests/*.test.mjs
   node src/cli.mjs build --zip <designer-export.zip> --config config/designer-export-v2.json --out dist
   ```

   A configurable token directory can be supplied with `--source` instead of
   `--zip`. Continue only with zero validation blockers and passing tests.
3. Repeat generation and verify deterministic outputs. Preserve approved units,
   semantics, typography publication, SysML contrast and border/motion decisions;
   resolve new ambiguity with the owners instead of inventing defaults.
4. Replace Bloom's two files with `dist/bloom/tokens.css` and
   `dist/bloom/theme.css` byte-for-byte, including their generated headers. Verify
   both SHA-256 hashes against those upstream outputs. Do not patch generated CSS.
5. Run `pnpm run css:build`, `pnpm run test:tokens`, the normal .NET/JavaScript,
   build/analyzer/coverage/publish/Docker gates and light/dark browser checks.
   Review the generated diff and visual comparison together.

When the official design-token repository and `Mycelium.DesignTokens` package are
available, package consumption will replace this static-artifact update step.
The runtime/theme responsibilities and canonical component contract remain the
same. Bloom currently has no token-package reference or preview-path override.

## Theme and component contract

Runtime styles load as Blueprint primitives, Blueprint components, shared tokens,
application CSS and scoped component CSS. Generated canonical values own the
theme. Blueprint still owns theme switching. Nested `.dark` scopes consume shared
overrides; portals outside a nested preview must carry that preview's dark scope.

Secondary controls use card/border at rest, muted on hover/press and foreground
for labels/icons; accent remains the selection tint. The Blueprint radius bridge
is value-free. The radius scale remains 4/6/8/12px, with full radius exactly 999px.
All 20 curated typography styles are included; 129 raw migration styles remain
excluded. Untriaged primitives are preserved without semantic/utility promotion.
Inter and JetBrains Mono use system fallbacks; font delivery remains an owner
decision and no external font dependency is added here.

Use canonical semantic/domain tokens and generated Tailwind namespaces for
shared values. Shared borders and transitions use border-width-default,
motion-duration-fast and motion-easing-standard. Component properties carry only
scoped instance state, configuration, geometry and derived presentation; they do
not recreate a parallel theme. Extend the upstream contract when a shared concept
is missing. The consumer regression rejects the retired custom-property
namespace across first-party source/tests and these shared artifacts, excluding
compiled app.css and third-party/build output. variables.css contains only
color-scheme declarations.
