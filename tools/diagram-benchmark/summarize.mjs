// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import { readdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { distribution } from './Protocol.mjs';

const directory = process.argv[2] ?? '.local/issue-79/benchmark/matrix';
const rows = [], cold = [], resources = [];
for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    let result;
    try { result = JSON.parse(await readFile(join(directory, entry.name, 'results.json'), 'utf8')); } catch { continue; }
    if (!result.completed) continue;
    cold.push({ environment: result.environment, size: result.size, engine: result.engine, startupMs: result.startupToReadyMs,
        pageMs: result.pageReadyMs, factoryCount: result.coldFactoryMount.count, factoryMountMs: result.coldFactoryMount.elapsedMs,
        requestedSceneImportMs: result.firstRequestedSceneImportMs, firstNodeSettlingMs: result.coldFirstDrag.settlingMs,
        firstSceneSettlingMs: result.coldFirstDrag.sceneSettlingMs, firstStateAcknowledgmentMs: result.coldFirstDrag.stateAcknowledgmentMs,
        remount: distribution(result.supplementary?.remount ?? []), scene: result.sceneCounts, assets: result.assets,
        runtime: result.runtime, svg: result.svg, jsonRoundtrip: result.jsonRoundtrip });
    for (const profile of result.profiles) {
        const application = sample => sample.traffic.application;
        const summarize = getter => distribution(profile.samples.map(getter));
        const first = profile.samples.slice(0, Math.floor(profile.samples.length / 2)), last = profile.samples.slice(Math.floor(profile.samples.length / 2));
        const intervals = values => values.slice(1).map((value, index) => value - values[index]);
        rows.push({ environment: result.environment, size: result.size, engine: result.engine, addedRttMs: profile.simulatedRttMs, samples: profile.samples.length,
            ...profile.summary, inputDispatchDurationMs: summarize(sample => sample.inputDispatchDurationMs),
            releaseHandlerDelayMs: summarize(sample => sample.releaseHandlerDelayMs),
            rafFrameIntervalsMs: distribution(profile.samples.flatMap(sample => intervals(sample.trace.raf.filter(time => time >= sample.trace.down && time <= sample.trace.released)))),
            nodeTransformIntervalsMs: distribution(profile.samples.flatMap(sample => intervals(sample.trace.frames.map(frame => frame.time)))),
            applicationFrames: summarize(sample => (application(sample).sentframes ?? 0) + (application(sample).receivedframes ?? 0)),
            applicationBytes: summarize(sample => (application(sample).sentbytes ?? 0) + (application(sample).receivedbytes ?? 0)),
            continuousApplicationBytes: summarize(sample => (application(sample).duringsentbytes ?? 0) + (application(sample).duringreceivedbytes ?? 0)),
            rawFrames: summarize(sample => (sample.traffic.raw.sentframes ?? 0) + (sample.traffic.raw.receivedframes ?? 0)),
            rawBytes: summarize(sample => (sample.traffic.raw.sentbytes ?? 0) + (sample.traffic.raw.receivedbytes ?? 0)),
            finalPositionErrorPixels: Math.max(...profile.samples.map(sample => sample.finalErrorPixels)),
            earlySceneSettling: distribution(first.map(sample => sample.sceneSettlingMs)), lateSceneSettling: distribution(last.map(sample => sample.sceneSettlingMs)),
            resourceFile: `${entry.name}/results.json` });
        const before = profile.postWarmResources, after = profile.postMeasureResources;
        const elapsedSeconds = (after.timestamp - before.timestamp) / after.frequency;
        resources.push({ environment: result.environment, size: result.size, engine: result.engine, addedRttMs: profile.simulatedRttMs,
            elapsedSeconds, serverCpuMilliseconds: after.cpuMilliseconds - before.cpuMilliseconds,
            serverCpuPercentOneCore: (after.cpuMilliseconds - before.cpuMilliseconds) / (elapsedSeconds * 10),
            serverCpuMillisecondsPerGesture: (after.cpuMilliseconds - before.cpuMilliseconds) / profile.samples.length,
            allocatedBytesPerGesture: (after.allocatedBytes - before.allocatedBytes) / profile.samples.length,
            allocationBytesPerSecond: (after.allocatedBytes - before.allocatedBytes) / elapsedSeconds,
            workingSetBytes: distribution([before.workingSetBytes, ...profile.resources.map(value => value.server.workingSetBytes), after.workingSetBytes]),
            managedBytes: distribution([before.managedBytes, ...profile.resources.map(value => value.server.managedBytes), after.managedBytes]),
            gcCollections: after.collections.map((value, index) => value - before.collections[index]),
            browserHeapBytes: distribution([profile.browserBefore.JSHeapUsedSize, ...profile.resources.map(value => value.browser.JSHeapUsedSize), profile.browserAfter.JSHeapUsedSize]),
            browserCpuTaskMilliseconds: (profile.browserAfter.TaskDuration - profile.browserBefore.TaskDuration) * 1000,
            browserCpuTaskMillisecondsPerGesture: (profile.browserAfter.TaskDuration - profile.browserBefore.TaskDuration) * 1000 / profile.samples.length,
            browserCpuTaskPercentOneCore: (profile.browserAfter.TaskDuration - profile.browserBefore.TaskDuration) / elapsedSeconds * 100,
            scriptMilliseconds: (profile.browserAfter.ScriptDuration - profile.browserBefore.ScriptDuration) * 1000,
            layoutMilliseconds: (profile.browserAfter.LayoutDuration - profile.browserBefore.LayoutDuration) * 1000,
            jitMethods: { beforeWarmup: profile.preWarmResources.jitMethods, afterWarmup: before.jitMethods, afterMeasurement: after.jitMethods },
            jitMillisecondsDuringMeasurement: after.jitMilliseconds - before.jitMilliseconds });
    }
}
const summary = { cases: cold.length, measuredGestures: rows.reduce((sum, row) => sum + row.samples, 0), rows, cold, resources };
await writeFile(join(directory, 'summary.json'), JSON.stringify(summary, null, 2));
const columns = ['environment', 'size', 'engine', 'addedRttMs', 'samples', 'effectiveRttMs', 'dragStartMs', 'pointerLagMs', 'settlingMs', 'connectorSettlingMs',
    'sceneSettlingMs', 'stateAcknowledgmentMs', 'inputDispatchDurationMs', 'releaseHandlerDelayMs', 'applicationFrames', 'applicationBytes', 'continuousApplicationBytes'];
const csv = [columns.join(','), ...rows.map(row => columns.map(column => typeof row[column] === 'object' ? JSON.stringify(`${row[column].p50} / ${row[column].p95}`) : row[column]).join(','))].join('\n');
await writeFile(join(directory, 'summary.csv'), `${csv}\n`);
console.log(JSON.stringify({ cases: summary.cases, measuredGestures: summary.measuredGestures }));
