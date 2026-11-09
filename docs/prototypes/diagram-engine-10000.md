# Large-model Interconnection assessment

## Current direction

Proceed with Z for the accepted viewport-based workflow. The normal application
opens real workspace models through the diagram-local session opener, and each
independent diagram has its own Browser and presentation state. See
[the current workflow](interconnection.md). X6 remains preserved internally.

Rendering virtualization is implemented. Production backend partial fetching is
not: the current file source eagerly deserializes and assembles the selected
model. Regional projection and the bounded fetched-payload cache do not change
that fact or bound total retained canonical SDK memory.

## Evidence boundaries

The historical all-at-once fixture repeats seven canonical parts into **10,000
occurrences, 14,287 ports and 7,142 connections**. Z completed it; both original
X6 configurations and the simple-routing control missed the 180-second deadline.
The original **21.05-second checkpoint** is not general time-to-interactive.
The original warm Z scene-settling median was **6.45 seconds, N=10**.

The indexed port calculation reduced measured server work. Separate clean
three-gesture windows measured **2.060 s → 0.206 s** server CPU, while settling
medians were **4.94 s → 7.24 s** with substantial variation. These measurements
show no demonstrated visual speedup. Browser tracing identified expensive native
rendering phases, but not the precise causal SVG/compositor structure. Traced,
original and corrected clean timings must remain separate. X6’s remaining
initialization bottleneck is unresolved. These are integration results, not
intrinsic limits of C#, JavaScript or either library.

The matched viewport comparison used Docker Release/Production, .NET 10.0.11,
Chromium 153.0.8010.12, 1920×1080, 100% diagram zoom and four CPUs/four GiB.
Each profile excluded two warm-ups and measured eight drags; these support
medians/ranges, not reliable tail estimates.

| Same usable viewport | Virtualized | Unculled |
| --- | ---: | ---: |
| Baseline scene settling median [range], ms | 18.2 [11.6–32.7] | 673.2 [78.5–1,495.8] |
| +160 ms simulated RTT settling median [range], ms | 188.4 [171.8–206.4] | 1,008.6 [938.7–1,546.6] |
| Mounted parts / ports / connections | 9 / 14 / 6 | 10,000 / 14,287 / 7,142 |
| Diagram DOM elements | 270 | 255,748 |

These historical figures retain their original source manifests. SVG mutation
settling is not a GPU paint measurement; accepted C# state is not Fabric
persistence. No concurrent-user capacity or 10,000-unique-SDK-object claim follows
from the repeated-occurrence scene. Later normal-file tests cover 10,000 distinct
Part Usages separately; no performance matrix was repeated for standards fixes.

## Production-source dependency

The prior read-only inspection of Fabric revision
`6677aaba7ca4643ab46b02e8bf71cd666d68e3fa` found project/commit-scoped read contracts
in `Mycelium.Fabric.ConcurrentServer/Services/AutoGenServices/IElementNavigationService.cs`
and route bindings in `Mycelium.Fabric.ConcurrentServer/Modules/ElementApi.cs`.
The inspected startup and
`Mycelium.Fabric.ConcurrentServer/Serialization/ServiceCollectionExtensions.cs`
(`AddRequiredServices`) did not register an implementation. Endpoint tests injected
mocks; `ApiBase.NotYetImplemented` returns HTTP 500 when reached. This concerns the
inspected configuration, not every backend branch or deployment.

Production partial fetching requires an operational implementation of these
contracts, selected project/commit context, and a supported metadata/dependency
policy preserving complete SDK update semantics. No endpoint or service was
invented. Live semantic updates remain dependent on the supported change pipeline.

The diagnostic provider verifies bounded batches, deduplication, cancellation,
revision/access isolation and cache eviction. The dedicated 1 MiB/120-second cache
settings are prototype configuration, not approved production budgets. Canonical
objects remain retained for their model context; read-cache eviction cannot remove
pending edits, another tab’s objects, or presentation state.

## Release gates and evidence

The integrated candidate uses development commit
`92deb55474a1b3ab2e593c86691352cc2973a82a`. Its production image digest is
`sha256:5275f4719d94d72ac1f6e92c85ef05074f9b8b6bd54a800b74538768ed8a7ee5`.
Trivy 0.76.2 completed on 2026-11-09 with that day's database: zero HIGH/CRITICAL
OS or application/runtime-library findings, exit 0. The image uses the merged
ASP.NET 10.0.14/Noble APT fixes. The earlier image's failed scan remains historical
evidence; it does not describe this rebuilt image.

Hosted Sonar and PR checks remain pending. Font delivery and known Blueprint
callback limitations remain separate. The unchanged development-only jsdom
lockfile has npm advisories for source-map-js 1.2.1 and undici 7.29.0; these tools
are not published in the runtime image.

Original reports, provenance, diagnostic analysis and successive validation records
are preserved in `.local/issue-79/audit-corrections-20261109/history/`. Their raw
artifacts remain in the original `.local/issue-79/` directories referenced there.
Fresh correction logs and the final source manifest belong to
`.local/issue-79/audit-corrections-20261109/`; earlier test counts are not evidence
for a subsequently changed source tree.
## Correction validation

The bounded standards correction passes 869 .NET tests (including normal-file
large-model coverage), 78 application/token JavaScript checks, 10 coverage-converter
checks and nine benchmark-harness checks. Debug/Release warning-as-error builds,
publish and CSS/X6 asset verification pass. Targeted full `dotnet format`
verification passes on the 32 affected C# files; it is not a claim that the
whole-solution baseline has been reformatted. Rider inspected the affected C# and
interop files; convention and intentional test-lifetime advisories remain, with
no errors. This does not substitute for the unavailable Sonar gate.

Published Production and Docker Production pass five bounded workflow checks:
default tab uniqueness through closure/reconnection; independent diagram/Browser
pairs and reuse; retained layout/search/scroll with targeted reveal; cross-group
movement and surviving consumers after closure; and independent uploads with
overlapping IDs and correct Details. Diagnostics and authentication were disabled,
and WebSockets were observed. An initial local harness attempt disconnected before
close acceptance; the corrected check waits for tab removal before disconnecting.
Both attempts are preserved. No renderer performance matrix was rerun.

Subsequent integration validation passed 935 .NET tests, 78 application/token
JavaScript checks, 10 converter checks and nine harness checks; Debug/Release
warning-as-error builds, publish, CSS/X6 assets and scoped formatting also passed.
The same bounded published/Docker workflows passed anonymously with diagnostics
disabled, plus account/organization navigation and return to workspace. Sonar
remains NOT VERIFIED locally and must be evaluated on the pull request.
