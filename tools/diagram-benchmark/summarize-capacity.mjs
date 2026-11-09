// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import { readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

const directory = process.argv[2] ?? '.local/issue-79/benchmark/large-scene';
const median = values => {
    const sorted = values.filter(Number.isFinite).sort((a, b) => a - b);
    const middle = Math.floor(sorted.length / 2);
    return sorted.length ? sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2 : null;
};
const range = values => ({ count: values.length, median: median(values), min: Math.min(...values), max: Math.max(...values) });
const memoryBytes = text => {
    const match = text.match(/^([\d.]+)([KMGT]?i?B)/);
    const units = { B: 1, kB: 1000, KB: 1000, MB: 1000000, GB: 1000000000, KiB: 1024, MiB: 1024 ** 2, GiB: 1024 ** 3 };
    return match ? Number(match[1]) * units[match[2]] : null;
};
const cases = [];
for (const [run, engine] of [['baseline', 'z'], ['baseline', 'x6'], ['async-scheduler', 'x6'], ['delayed-z', 'z']]) {
    const path = join(directory, run, `docker-10000-${engine}`);
    let result;
    try { result = JSON.parse(await readFile(join(path, 'results.json'), 'utf8')); } catch { continue; }
    const manifest = JSON.parse(await readFile(join(directory, run, 'manifest.json'), 'utf8'));
    const statsText = await readFile(join(path, 'docker-stats.jsonl'), 'utf8');
    const stats = [...statsText.matchAll(/\{[^\r\n]*\}/g)].map(match => JSON.parse(match[0]));
    const before = result.runtimeBefore, loaded = result.runtimeLoaded;
    cases.push({ run, engine, sourceHash: manifest.sourceHash, image: manifest.docker.image, started: manifest.started,
        completed: result.completed, failure: result.failure, completeSceneRendered: result.completeSceneRendered ?? false,
        counts: result.sceneCounts ?? result.failureCounts, expected: result.scene,
        completeRenderOpportunityMs: result.completeRenderOpportunityMs ?? null,
        firstImportedDomMs: result.loadTrace ? result.loadTrace.firstImportedDom - result.loadTrace.start : null,
        initialRuntime: result.coldFactoryMount, startupMs: result.startupToReadyMs, pageReadyMs: result.pageReadyMs,
        browserPeakObservedWorkingSetBytes: Math.max(...result.capacity.observations.map(value => value.workingSetBytes)),
        browserPeakObservedPrivateBytes: Math.max(...result.capacity.observations.map(value => value.privateBytes)),
        postLoadJsHeapBytes: result.browserLoaded?.JSHeapUsedSize ?? null,
        serverPostLoadWorkingSetBytes: loaded?.workingSetBytes ?? null,
        serverPostLoadManagedBytes: loaded?.managedBytes ?? null,
        serverPeakObservedWorkingSetBytes: Math.max(...[before, loaded, result.runtimeAfter, ...result.profiles.flatMap(profile => [profile.serverBefore, profile.serverAfter])].filter(Boolean).map(value => value.workingSetBytes)),
        serverLoadWindowCpuMs: loaded ? loaded.cpuMilliseconds - before.cpuMilliseconds : null,
        serverLoadWindowAllocatedBytes: loaded ? loaded.allocatedBytes - before.allocatedBytes : null,
        serverLoadWindowCollections: loaded ? loaded.collections.map((value, index) => value - before.collections[index]) : null,
        browserLoadWindowTaskMs: result.browserLoaded ? (result.browserLoaded.TaskDuration - result.browserBefore.TaskDuration) * 1000 : null,
        containerPeakObservedBytes: Math.max(...stats.map(value => memoryBytes(value.MemUsage))),
        containerPeakObservedCpuPercent: Math.max(...stats.map(value => Number.parseFloat(value.CPUPerc)).filter(Number.isFinite)),
        longTasks: result.longTasks ? { count: result.longTasks.length, maximumMs: Math.max(...result.longTasks.map(task => task.duration)) } : null,
        profiles: result.profiles.map(profile => ({ addedRttMs: profile.addedRttMs, effectiveRttMs: range(profile.effectiveRtt),
            sceneSettlingMs: range(profile.samples.map(sample => sample.sceneSettlingMs)),
            acceptedReadbackMs: range(profile.samples.map(sample => sample.stateAcknowledgmentMs)),
            inputHandlerDelayMs: range(profile.samples.map(sample => sample.releaseHandlerDelayMs)),
            deliveredMoves: range(profile.samples.map(sample => sample.inputMoves)),
            updatesAfterRelease: range(profile.samples.map(sample => sample.updatesAfterRelease)),
            applicationFrames: range(profile.samples.map(sample => (sample.traffic.application.sentframes ?? 0) + (sample.traffic.application.receivedframes ?? 0))),
            applicationBytes: range(profile.samples.map(sample => (sample.traffic.application.sentbytes ?? 0) + (sample.traffic.application.receivedbytes ?? 0))),
            serverCpuMs: profile.serverAfter ? profile.serverAfter.cpuMilliseconds - profile.serverBefore.cpuMilliseconds : null,
            serverAllocatedBytes: profile.serverAfter ? profile.serverAfter.allocatedBytes - profile.serverBefore.allocatedBytes : null,
            serverCollections: profile.serverAfter ? profile.serverAfter.collections.map((value, index) => value - profile.serverBefore.collections[index]) : null,
            browserTaskMs: profile.browserAfter ? (profile.browserAfter.TaskDuration - profile.browserBefore.TaskDuration) * 1000 : null,
            allFinalPositionsCorrect: profile.samples.every(sample => sample.finalErrorPixels === 0) })),
        operations: result.capacity.operations.map(operation => ({ name: operation.name, succeeded: operation.succeeded, milliseconds: operation.milliseconds,
            sceneSettlingMs: operation.value?.sceneSettlingMs, acceptedReadbackMs: operation.value?.stateAcknowledgmentMs })) });
}
await writeFile(join(directory, 'capacity-summary.json'), JSON.stringify({ cases }, null, 2));
console.log(`Summarized ${cases.length} capacity cases.`);
