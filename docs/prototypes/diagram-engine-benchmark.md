# Diagram engine benchmark provenance

The historical integrated comparison covered Debug, Release, published Production
and Docker Production at 7, 126 and 350 occurrences, with baseline, +80 ms and
+160 ms delayed-WebSocket profiles. Warm-up, cold execution and measured gestures
were recorded separately. The complete methodology, raw-result references and
original tables are preserved in
`.local/issue-79/audit-corrections-20261109/history/diagram-engine-benchmark.md`.
Raw evidence remains at `.local/issue-79/benchmark/scheduled-matrix/` with its own
source manifests; it has not been rerun or relabelled as current-source evidence.

Representative Docker Release, 126 occurrences/90 connections, +160 ms simulated
RTT, N=24; latency values are p50/p95 milliseconds:

| Metric | Z.Blazor.Diagrams | AntV X6 |
| --- | ---: | ---: |
| Complete-scene settling | 187.8 / 191.9 | 36.1 / 41.6 |
| Accepted C# state readback | 188.5 / 192.5 | 232.1 / 276.9 |
| Median application frames/drag | 102 | 7 |
| Median encoded application bytes/drag | 53,743 | 115,743 |

X6’s local gestures improved visual settling under simulated latency; its current
full-snapshot synchronization produced larger payloads and slower accepted-state
readback. That overhead is an adapter choice, not an inherent X6 requirement.
Neither result establishes durable persistence or concurrent-user capacity.

The later all-at-once and viewport results changed the implementation direction.
Use [the large-model assessment](diagram-engine-10000.md) for the current decision,
not the historical small-scene recommendation. Reproducible developer commands
remain in [the harness README](../../tools/diagram-benchmark/README.md).
