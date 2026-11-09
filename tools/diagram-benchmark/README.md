# Diagram engine benchmark

## Z viewport rendering

This mode tests Antoine's clarified visible-region requirement, separately from
the historical all-at-once measurements below. Reuse the saved deterministic scene
and choose fresh output directories:

```sh
docker build --target final --tag mycelium-bloom:diagram-viewport-review .
node tools/diagram-benchmark/run.mjs --viewport --environments=docker --engines=z --sizes=10000 --samples=8 --warmups=2 --rtts=0,160 --skip-build --image=mycelium-bloom:diagram-viewport-review --scenes=.local/issue-79/benchmark/large-scene/baseline/scenes --output=.local/diagram-viewport/on
node tools/diagram-benchmark/run.mjs --viewport --interactions-only --without-virtualization --environments=docker --engines=z --sizes=10000 --samples=8 --warmups=2 --rtts=0,160 --skip-build --image=mycelium-bloom:diagram-viewport-review --scenes=.local/issue-79/benchmark/large-scene/baseline/scenes --output=.local/diagram-viewport/off
```

`--without-virtualization` sets `DiagramViewport:Enabled=false`, honored only by
isolated benchmark tabs. Ordinary editors still virtualize. `--interactions-only`
omits the functional/export sequence after measured dragging. Normal .NET and
Docker builds remain Node-free; Node/Playwright are explicit diagnostic tooling.
The existing ephemeral capability and large-scene gates are retained. Limits are
ten minutes per case, 180 seconds per import/render readiness check, six GiB observed
Chromium working set, and four CPUs/four GiB for the localhost-only container.

Import readiness requires a newer accepted revision, expected geometry, the actual
viewport transform and visible node/connection graphics. Gesture checks also compare
accepted state after visual settling. The full run records distant viewport
restoration, pointer panning, browser selection/reveal, nested movement, read-only,
minimum zoom, a 700-part overlap case, complete JSON/SVG export and close/reopen.
See the viewport section in `docs/prototypes/diagram-engine-10000.md` for metrics,
provenance and limitations. Historical all-at-once reproduction must use its
archived image: rebuilding current source applies the new detailed-zoom policy.

## One complete 10,000-occurrence scene

The bounded capacity mode reuses the browser driver, timed gestures, WebSocket
proxy, accepted-state probe and Docker runner below. It does not rerun the old
matrix. Use a fresh output directory for each experiment:

```sh
docker build --target final --tag mycelium-bloom:diagram-large-scene .
node tools/diagram-benchmark/run.mjs --environments=docker --sizes=350 --samples=2 --warmups=2 --rtts=0 --capacity --skip-build --output=.local/diagram-capacity/readiness
node tools/diagram-benchmark/run.mjs --environments=docker --sizes=10000 --samples=10 --warmups=4 --rtts=0 --capacity --skip-build --output=.local/diagram-capacity/baseline
```

Capacity mode adds `DiagramBenchmark:LargeSceneEnabled` to the existing ephemeral
capability gate. Only stress tabs receive 10,000-node / 15,000-route / 16 MiB
limits and a 0.005 minimum scale. Ordinary tabs retain their existing limits;
identity, containment, coordinate and read-only validation still apply. The scene
repeats seven canonical parts: 10,000 occurrences, 14,287 ports, 7,142 connections
and 1,428 nested children. A final partial tile contains only valid endpoints.

Each case has a 180-second complete-render deadline, ten-minute overall deadline,
six-GiB observed Chromium working-set ceiling and the ordinary four-CPU / four-GiB
container bounds. Failure checkpoints, recordings, screenshots and process samples
survive a timeout. Chromium resources belong to the runner; unrelated applications
and containers are not stopped. Resource samples are observations, not exact peaks.
Inspect each result's `completed` and `failure` fields: an observed capacity
failure is retained as benchmark data rather than treated as a runner crash.

The baseline preserves synchronous X6 rendering. To assess its supported
asynchronous scheduler separately, without changing normal renderer behavior:

```sh
docker build --target final --tag mycelium-bloom:diagram-large-scene-async .
node tools/diagram-benchmark/run.mjs --environments=docker --sizes=10000 --engines=x6 --samples=6 --warmups=2 --rtts=0,160 --capacity --async-rendering --skip-build --scenes=.local/diagram-capacity/baseline/scenes --output=.local/diagram-capacity/async-scheduler
```

`--async-rendering` enables `DiagramBenchmark:AsyncRendering` only in isolated
large-scene mode; culling stays disabled. Latency profiles run only after complete
rendering, navigation and the first accepted drag. All expected connectors must
exist; graph model initialization or the first screenful is insufficient.
The common driver records SVG mutations and animation-frame opportunities, not
GPU presentation timestamps. Videos demonstrate painting; operation durations
include deliberate waits, probes and screenshot capture and are not pure input
latencies. The observed 10,000-scene results and limitations are in
`docs/prototypes/diagram-engine-10000.md`.

### Bounded diagnostics

Use `--image=<local-tag>` to preserve earlier executable images. `--diagnostic-profile`
adds one separately labelled drag after the clean samples, collecting Chromium
timeline/CPU traces and an explicitly activated, mount-owned C# activity listener.
Inclusive activity durations overlap; do not sum them or pool the traced gesture
with clean timing samples. The listener is inactive during ordinary execution.

```sh
docker build --target final --tag mycelium-bloom:diagram-diagnostic .
node tools/diagram-benchmark/run.mjs --environments=docker --capacity --sizes=10000 --engines=z --samples=3 --warmups=2 --rtts=0 --skip-build --image=mycelium-bloom:diagram-diagnostic --diagnostic-profile --scenes=.local/diagram-capacity/baseline/scenes --output=.local/diagram-capacity/z-diagnostic
node tools/diagram-benchmark/run.mjs --environments=docker --capacity --async-rendering --simple-routing --sizes=10000 --engines=x6 --samples=0 --warmups=0 --rtts=0 --skip-build --image=mycelium-bloom:diagram-diagnostic --scenes=.local/diagram-capacity/baseline/scenes --output=.local/diagram-capacity/x6-routing-control
```

`--simple-routing` enables endpoint-only X6 `orth` routing exclusively in
capability-protected large-scene benchmark tabs. Shapes, ports, labels, connections,
manual bends, culling settings and deadlines remain unchanged. This diagnoses
routing cost; it does not demonstrate Manhattan obstacle-avoidance parity. The
normal renderer still uses Manhattan routing. Readiness verifies each connector
has path geometry; viewport captures also wait for the rendered SVG matrix to
match accepted C# state. Neither check proves compositor presentation completion.

## Earlier size and deployment matrix

Run from the repository root with .NET 10, Docker Desktop, Node 24 and the pinned
Playwright Chromium installed. Node is required only by this explicit diagnostic
workflow; the application build, publish and Dockerfile do not invoke it.

```sh
npm ci --ignore-scripts --prefix tools/diagram-benchmark
node tools/diagram-benchmark/node_modules/playwright/cli.js install chromium
node --test tools/diagram-benchmark/Protocol.test.mjs tools/diagram-benchmark/Scene.test.mjs
node tools/diagram-benchmark/run.mjs --environments=publish,docker,release,debug --sizes=126,7,350 --samples=24 --warmups=24 --rtts=0,80,160 --output=.local/diagram-benchmark
node tools/diagram-benchmark/summarize.mjs .local/diagram-benchmark
```

The runner builds Debug and Release into isolated artifacts, publishes actual
Release output and builds the repository's `final` Docker target. It starts a
fresh application and Chromium context per engine/size, alternates engine order,
and runs one engine at a time. Local build executions use Development; published
and Docker executions explicitly use Production. No debugger or hot reload is
attached. Docker has a four-CPU quota and a 4 GiB memory limit, identical for both
engines, with only a localhost port binding. Choose another `--port` if needed.

`--skip-build` reuses existing executable artifacts and image; use it only after
verifying their source revision. `--environments=docker,publish --sizes=7 --samples=0
--warmups=0 --rtts=0 --verification --scenes=.local/diagram-benchmark/scenes
--output=.local/diagram-verification --skip-build` runs command rejection,
pending acknowledgments, timeout recovery, socket reconnection and tab lifecycle
checks without collecting a warm performance distribution. Use sizes `7,126,350`
for routing and lifecycle coverage at every size. Every result retains
its source manifest and whether the case completed. Failed/pilot outputs must
remain separate from a final measurement directory.

Benchmark mode is disabled by default. The runner creates an ephemeral access
capability, sets `DiagramBenchmark__Enabled` and `DiagramBenchmark__AccessKey`
only for its own process/container, and supplies an HttpOnly capability cookie
to its browser context. The key is not printed or saved. Enabling the flag
without a sufficiently long key fails startup; diagnostic deployments reject
unauthorized application, asset and SignalR requests. Health probes remain
available. Do not enable these settings in a shared deployment or reuse a key.

The ordinary editor menu exposes Interconnection for the tab’s canonical workspace model. Model opening stays inside that diagram; the example is an explicit dialog choice. Explicit benchmark
mode exposes both engines and two independent 126-occurrence stress entries. Larger scenes import
the same deterministic version-1 presentation into an isolated tab, within the
existing validation limits. They repeat canonical element identities and never
construct new SysML elements. Both engines use the same cached scene JSON.

The harness sends 30 mouse moves on a 600 ms CDP schedule without awaiting each
move. It saves actual dispatch timestamps, pointer timestamps, SVG mutations,
animation-frame intervals, exact accepted coordinates, C# readback observations,
runtime/JIT/GC counters, browser task time and heap observations. It observes
node and connector settling separately. Cold process/mount/first-drag samples
are separate from excluded warm-up gestures and measured distributions.

The WebSocket proxy adds half the requested delay in each direction to actual
SignalR traffic; HTTP/static downloads are not delayed, and there is no bandwidth
cap or packet loss. Probe pings verify effective RTT. Binary envelope accounting
retains raw transport totals and separates identifiable measurement probes,
heartbeats and shared outside-click errors from application traffic. It measures
encoded payload bytes, not TLS/TCP wire overhead. A frame can contain messages
from multiple categories, so category frame counts are not additive. Browser
animation-frame observations are proxies for visible updates, not GPU capture.

CPU/heap counters and observers add overhead. Resource windows include fixed
measurement readbacks and accepted-state watchers. Browser task time represents
the renderer main thread, not all Chromium processes; Docker stats and .NET
process counters cover their own scopes. Do not interpret this single-user test
as a collaboration capacity test or a geographically deployed benchmark.

Each case preserves raw results, screenshots, exact JSON round trips, light/dark
SVGs and independent file-document/image previews. The summary generator emits
JSON and CSV without replacing raw samples. See
`docs/prototypes/diagram-engine-benchmark.md` for the reviewed results and limits.

## On-demand model-data fixture

`--loading` selects the explicitly labelled, generated SDK provider in the Z
stress tab. Use a multiple of seven, up to 9,996 distinct part IDs. Metadata is
created up front; complete dependency payloads are generated only when requested.
This is not a Fabric endpoint or a network-transfer benchmark. Normal file loading
and X6 remain unchanged. `--eager-loading` deliberately preloads the same provider
and retains all renderer models as a separate matched control.

```sh
docker build -t mycelium-bloom:diagram-loading-review .
node tools/diagram-benchmark/run.mjs --loading --environments=docker --engines=z --sizes=560 --samples=4 --warmups=1 --rtts=0,160 --skip-build --image=mycelium-bloom:diagram-loading-review --output=.local/issue-79/loading-review/new-demand
node tools/diagram-benchmark/run.mjs --loading --eager-loading --environments=docker --engines=z --sizes=560 --samples=4 --warmups=1 --rtts=0,160 --skip-build --image=mycelium-bloom:diagram-loading-review --output=.local/issue-79/loading-review/new-eager
```

For a bounded larger-source check, use `--sizes=9996 --samples=2 --warmups=1
--rtts=0` and another output directory. Both runs retain complete JSON metadata;
the full SVG check is bounded to scenes of at most 560 parts. The runner records
actual WebSockets, image identity, source hashes, viewport restoration, cache
reuse/eviction, renderer/DOM counts, sampled resources and real captures. Do not
combine failed runs or changed harness versions with the matched measurements.

The isolated configuration uses `DiagramBenchmark:OnDemandTiles` and requires the
existing enabled/capability/large-scene gate. The read cache's `DiagramData` options
default to 1,048,576 UTF-8 payload bytes, 120 seconds sliding expiration, four
bundles per request and two concurrent source requests. These bounds exclude
metadata, in-flight payloads and the SDK identity cache. The eager control uses
`DiagramBenchmark:PreloadOnDemandFixture`; it cannot be enabled without the
fixture. No Node execution is added to ordinary build, publish or Docker targets.

## Resume provenance

`--resume` requires a matching source manifest, SDK/runtime, machine and run settings, including supplied scene hashes. Every reused completed result must carry matching source and case identities. Missing historical provenance is rejected; use a new output directory instead of relabelling old evidence. No benchmark is run by the unit tests (`npm test --prefix tools/diagram-benchmark`).
