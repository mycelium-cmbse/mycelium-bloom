// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import { spawn, execFileSync } from 'node:child_process';
import { createHash, randomBytes } from 'node:crypto';
import { mkdir, readFile, writeFile, open, readdir } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import os from 'node:os';
import { measureCase } from './Browser.mjs';
import { verifyCase } from './Verification.mjs';
import { measureViewport } from './Viewport.mjs';
import { readResumeCases } from './Resume.mjs';

const argumentsMap = new Map(process.argv.slice(2).map(argument => argument.replace(/^--/, '').split('=')));
const output = argumentsMap.get('output') ?? '.local/issue-79/benchmark/results';
const environments = (argumentsMap.get('environments') ?? 'publish,docker,release,debug').split(',');
const sizes = (argumentsMap.get('sizes') ?? '7,126,350').split(',').map(Number);
const samples = Number(argumentsMap.get('samples') ?? 24), warmups = Number(argumentsMap.get('warmups') ?? 32);
const rtts = (argumentsMap.get('rtts') ?? '0,80,160').split(',').map(Number);
const artifacts = '.local/issue-79/benchmark/artifacts';
const publish = '.local/issue-79/benchmark/publish';
const loading = argumentsMap.has('loading');
const viewport = argumentsMap.has('viewport') || loading;
if (loading && sizes.some(size => size % 7 !== 0 || size > 9996)) throw new Error('On-demand fixture sizes must be multiples of seven, at most 9996.');
const capacity = argumentsMap.has('capacity') || viewport;
const asyncRendering = argumentsMap.has('async-rendering');
const simpleRouting = argumentsMap.has('simple-routing'), diagnosticProfile = argumentsMap.has('diagnostic-profile');
const image = argumentsMap.get('image') ?? (capacity ? `mycelium-bloom:diagram-large-scene${asyncRendering ? '-async' : ''}` : 'mycelium-bloom:diagram-benchmark');
const port = Number(argumentsMap.get('port') ?? 5317);
const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
const execute = (command, args, options = {}) => execFileSync(command, args, { encoding: 'utf8', windowsHide: true, maxBuffer: 16 * 1024 * 1024, ...options });

await mkdir(output, { recursive: true });
const files = execute('git', ['ls-files', '-co', '--exclude-standard']).trim().split(/\r?\n/).filter(Boolean);
const source = {};
for (const file of [...new Set(files)].sort()) source[file] = createHash('sha256').update(await readFile(file)).digest('hex');
const sceneHashes = {};
if (argumentsMap.has('scenes')) {
    for (const file of (await readdir(argumentsMap.get('scenes'))).filter(file => file.endsWith('.json')).sort()) {
        sceneHashes[file] = createHash('sha256').update(await readFile(join(argumentsMap.get('scenes'), file))).digest('hex');
    }
}
const metadata = { started: new Date().toISOString(), base: execute('git', ['rev-parse', 'HEAD']).trim(), branch: execute('git', ['branch', '--show-current']).trim(),
    source, sourceHash: createHash('sha256').update(JSON.stringify(source)).digest('hex'),
    machine: { platform: os.platform(), architecture: os.arch(), cpuModel: os.cpus()[0].model, logicalProcessors: os.cpus().length, totalMemoryBytes: os.totalmem() },
    sdk: execute('dotnet', ['--version']).trim(), runtimes: execute('dotnet', ['--list-runtimes']).replaceAll(process.env.ProgramFiles ?? 'C:/Program Files', '<runtime-root>'),
    node: process.version, samples, warmups, rtts, sizes, environments, capacity, viewport, loading, eagerLoading: argumentsMap.has('eager-loading'), virtualization: !argumentsMap.has('without-virtualization'), asyncRendering, simpleRouting, diagnosticProfile, cases: [] };
Object.assign(metadata, { verification: argumentsMap.has('verification'), interactionsOnly: argumentsMap.has('interactions-only'),
    recovery: argumentsMap.has('recovery'), sceneHashes, image });
if (argumentsMap.has('resume') && environments.includes('docker')) {
    metadata.docker = { image: execute('docker', ['image', 'inspect', image, '--format', '{{.Id}}']).trim() };
}
const reusable = argumentsMap.has('resume') ? await readResumeCases(output, metadata) : new Set();
if (!argumentsMap.has('resume')) await writeFile(join(output, 'manifest.json'), JSON.stringify(metadata, null, 2));

async function command(command, args, name, env = process.env) {
    const log = await open(join(output, `${name}.log`), 'w');
    try {
        const child = spawn(command, args, { stdio: ['ignore', log.fd, log.fd], env, windowsHide: true });
        const code = await new Promise((resolve, reject) => { child.once('error', reject); child.once('exit', resolve); });
        if (code !== 0) throw new Error(`${name} exited ${code}; see ${name}.log`);
    } finally { await log.close(); }
}

if (!argumentsMap.has('skip-build')) {
    for (const configuration of ['Debug', 'Release'].filter(value => environments.includes(value.toLowerCase()))) {
        await command('dotnet', ['build', 'Mycelium.Bloom/Mycelium.Bloom.csproj', '-c', configuration, '--artifacts-path', artifacts, '-warnaserror'], `build-${configuration.toLowerCase()}`);
    }
    if (environments.includes('publish')) await command('dotnet', ['publish', 'Mycelium.Bloom/Mycelium.Bloom.csproj', '-c', 'Release', '--artifacts-path', artifacts, '-o', publish, '-warnaserror'], 'publish');
    if (environments.includes('docker')) await command('docker', ['build', '--pull', '--target', 'final', '--tag', image, '.'], 'docker-build');
}
if (environments.includes('docker')) {
    metadata.docker = { image: execute('docker', ['image', 'inspect', image, '--format', '{{.Id}}']).trim(),
        version: JSON.parse(execute('docker', ['version', '--format', '{{json .Server}}'])),
        runtime: execute('docker', ['run', '--rm', '--entrypoint', 'dotnet', image, '--info']).trim(), cpuLimit: 4, memoryBytes: 4 * 1024 ** 3 };
}

let caseIndex = 0;
for (const environment of environments) {
    if (!['publish', 'docker', 'release', 'debug'].includes(environment)) throw new Error(`Unsupported environment: ${environment}`);
    for (const size of sizes) for (const engine of (argumentsMap.get('engines')?.split(',') ?? (caseIndex++ % 2 ? ['x6', 'z'] : ['z', 'x6']))) {
        const name = `${environment}-${size}-${engine}`, directory = join(output, name);
        await mkdir(directory, { recursive: true });
        if (reusable.has(name)) {
            metadata.cases.push({ environment, engine, size, directory: name, completed: true, reused: true });
            console.log(`Reusing completed ${name}`);
            continue;
        }
        const key = randomBytes(32).toString('hex'), url = `http://localhost:${port}`;
        const runtimeEnvironment = environment === 'debug' || environment === 'release' ? 'Development' : 'Production';
        const env = { ...process.env, ASPNETCORE_ENVIRONMENT: runtimeEnvironment, ASPNETCORE_URLS: url,
            DiagramBenchmark__OnDemandTiles: String(loading ? size / 7 : 0), DiagramBenchmark__PreloadOnDemandFixture: String(argumentsMap.has('eager-loading')),
            DiagramViewport__Enabled: String(!argumentsMap.has('without-virtualization')), DiagramBenchmark__Enabled: 'true', DiagramBenchmark__AccessKey: key, DiagramBenchmark__LargeSceneEnabled: String(capacity), DiagramBenchmark__AsyncRendering: String(asyncRendering), DiagramBenchmark__SimpleRouting: String(simpleRouting) };
        const log = await open(join(directory, 'server.log'), 'w');
        let child, container, stats, statsLog;
        const started = performance.now();
        try {
            if (environment === 'docker') {
                container = `bloom-diagram-benchmark-${process.pid}`;
                execute('docker', ['run', '--detach', '--name', container, '--cpus=4', '--memory=4g', '-p', `127.0.0.1:${port}:8080`,
                    '--env', 'DiagramBenchmark__OnDemandTiles', '--env', 'DiagramBenchmark__PreloadOnDemandFixture', '--env', 'DiagramViewport__Enabled', '--env', 'ASPNETCORE_ENVIRONMENT', '--env', 'DiagramBenchmark__Enabled', '--env', 'DiagramBenchmark__AccessKey', '--env', 'DiagramBenchmark__LargeSceneEnabled', '--env', 'DiagramBenchmark__AsyncRendering', '--env', 'DiagramBenchmark__SimpleRouting', image], { env });
                statsLog = await open(join(directory, 'docker-stats.jsonl'), 'w');
                stats = spawn('docker', ['stats', '--format', '{{json .}}', container], { stdio: ['ignore', statsLog.fd, 'ignore'], windowsHide: true });
            } else {
                const assembly = environment === 'publish' ? join(publish, 'Mycelium.Bloom.dll') : join(artifacts, 'bin/Mycelium.Bloom', environment, 'Mycelium.Bloom.dll');
                child = spawn('dotnet', [resolve(assembly)], { cwd: environment === 'publish' ? resolve(publish) : resolve('Mycelium.Bloom'), env,
                    stdio: ['ignore', log.fd, log.fd], windowsHide: true });
            }
            let ready = false;
            for (let attempt = 0; attempt < 300; attempt++) {
                try { if ((await fetch(`${url}/ready`)).ok) { ready = true; break; } } catch {}
                if (child?.exitCode !== null && child?.exitCode !== undefined) throw new Error(`Application exited ${child.exitCode}`);
                await delay(100);
            }
            if (!ready) throw new Error('Application readiness timeout');
            const startupToReadyMs = performance.now() - started;
            if ((await fetch(`${url}/workspace/modeling`)).status !== 403) throw new Error('Diagnostic deployment was accessible without its capability');
            console.log(`Measuring ${name}; ready in ${startupToReadyMs.toFixed(1)} ms`);
            const measure = viewport ? measureViewport : argumentsMap.has('verification') ? verifyCase : measureCase;
            const result = await measure({ url, key, engine, size, samples, warmups, rtts, directory, capacity, diagnosticProfile, scenesDirectory: argumentsMap.get('scenes') ?? join(output, 'scenes'),
                loading,
                interactionsOnly: argumentsMap.has('interactions-only'),
                verifyRecovery: argumentsMap.has('recovery') && size === 7 && ['publish', 'docker'].includes(environment) });
            result.environment = environment;
            result.runtimeEnvironment = runtimeEnvironment;
            result.startupToReadyMs = startupToReadyMs;
            result.sourceHash = metadata.sourceHash;
            await writeFile(join(directory, 'results.json'), JSON.stringify(result, null, 2));
            metadata.cases.push({ environment, engine, size, directory: name, completed: result.completed });
        } catch (error) {
            metadata.cases.push({ environment, engine, size, directory: name, completed: false, error: String(error) });
            if (!capacity) throw error;
        } finally {
            if (child) { child.kill(); await new Promise(resolve => child.exitCode !== null ? resolve() : child.once('exit', resolve)); }
            if (container) {
                try { await writeFile(join(directory, 'server.log'), execute('docker', ['logs', container], { stdio: ['ignore', 'pipe', 'pipe'] })); } catch {}
                execute('docker', ['stop', '--time=5', container]);
                execute('docker', ['rm', container]);
            }
            stats?.kill();
            await statsLog?.close();
            await log.close();
            await writeFile(join(output, 'manifest.json'), JSON.stringify(metadata, null, 2));
        }
    }
}
metadata.finished = new Date().toISOString();
await writeFile(join(output, 'manifest.json'), JSON.stringify(metadata, null, 2));
