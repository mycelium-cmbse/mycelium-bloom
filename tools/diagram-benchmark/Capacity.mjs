// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { createHash } from 'node:crypto';
import { createScene } from './Scene.mjs';
import { summarizeTraffic } from './Protocol.mjs';

const execute = promisify(execFile);
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
const canvasModule = '/Components/UI/Organisms/InterconnectionCanvas/InterconnectionCanvas.razor.js';

export async function measureCapacity(h) {
    const { browser, context, page, cdp, root, canvas, engine, size, directory, scenesDirectory, results } = h;
    const started = performance.now(), browserSession = await browser.newBrowserCDPSession();
    const processInfo = await browserSession.send('SystemInfo.getProcessInfo');
    const browserPid = processInfo.processInfo.find(process => process.type === 'browser')?.id;
    let stopped = false, sampling = false, stage = 'navigation', limitFailure;
    const observations = [], operations = [], deadlines = new Set();
    results.capacity = { renderDeadlineMs: 180000, caseDeadlineMs: 600000, browserProcessLimitBytes: 6 * 1024 ** 3,
        occurrencesAreRepeatedCanonicalFixture: true, uniqueCanonicalParts: 7, observations, operations };
    const checkpoint = () => writeFile(join(directory, 'capacity-checkpoint.json'), JSON.stringify({ ...results, stage }, null, 2));
    const killOwnedBrowser = async () => {
        if (browserPid) await execute('taskkill', ['/PID', String(browserPid), '/T', '/F'], { windowsHide: true }).catch(() => {});
    };
    const bounded = async (name, operation, timeout = 30000) => {
        stage = name;
        await checkpoint();
        let timer;
        try {
            return await Promise.race([operation(), new Promise((_, reject) => {
                timer = setTimeout(() => reject(new Error(`${name}: deadline exceeded (${timeout} ms)`)), timeout);
                deadlines.add(timer);
            })]);
        } finally { clearTimeout(timer); deadlines.delete(timer); }
    };
    const ceiling = setTimeout(() => { limitFailure = 'case deadline (600000 ms)'; void killOwnedBrowser(); }, 600000);
    const sample = async () => {
        if (sampling || stopped) return;
        sampling = true;
        try {
            const processes = (await browserSession.send('SystemInfo.getProcessInfo')).processInfo;
            const ids = processes.map(process => process.id).filter(Number.isInteger);
            const memory = await execute('pwsh', ['-NoProfile', '-Command',
                `Get-Process -Id ${ids.join(',')} -ErrorAction SilentlyContinue | Select-Object Id,WorkingSet64,PrivateMemorySize64,CPU | ConvertTo-Json -Compress`],
                { windowsHide: true, timeout: 5000 });
            const values = JSON.parse(memory.stdout || '[]');
            const items = Array.isArray(values) ? values : [values];
            const workingSetBytes = items.reduce((sum, value) => sum + value.WorkingSet64, 0);
            observations.push({ elapsedMs: performance.now() - started, stage, workingSetBytes,
                privateBytes: items.reduce((sum, value) => sum + value.PrivateMemorySize64, 0),
                cpuSeconds: processes.reduce((sum, value) => sum + value.cpuTime, 0), processes: processes.length });
            if (workingSetBytes > results.capacity.browserProcessLimitBytes) {
                limitFailure = 'browser process working-set ceiling (6 GiB)';
                await killOwnedBrowser();
            }
            await checkpoint();
        } catch {} finally { sampling = false; }
    };
    const monitor = setInterval(sample, 3000);
    const counts = () => root.evaluate((element, engine) => ({
        nodes: element.querySelectorAll(engine === 'x6' ? '.x6-node' : '.part-svg').length,
        ports: element.querySelectorAll(engine === 'x6' ? '.x6-port' : '.part-svg__port').length,
        edges: element.querySelectorAll(engine === 'x6' ? '.x6-edge' : '.connection-svg__line').length,
        edgesWithGeometry: [...element.querySelectorAll(engine === 'x6' ? '.x6-edge' : '.connection-svg__line')].filter(edge =>
            engine === 'x6' ? [...edge.querySelectorAll('path')].some(path => path.getAttribute('d')?.length > 3) : edge.getAttribute('d')?.length > 3).length,
        pathsWithGeometry: [...element.querySelectorAll(engine === 'x6' ? '.x6-edge path' : '.connection-svg__line')].filter(path => path.getAttribute('d')?.length > 3).length,
        domElements: element.querySelectorAll('*').length
    }), engine);
    const paintOpportunity = () => page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
    const waitViewport = viewport => page.waitForFunction(({ engine, x, y, zoom }) => {
        const element = document.querySelector(engine === 'x6' ? '.x6-graph-svg-viewport' : '.diagram-svg-layer');
        if (!element) return false;
        const matrix = new DOMMatrixReadOnly(getComputedStyle(element).transform);
        return Math.abs(matrix.a - zoom) < 0.00001 && Math.abs(matrix.d - zoom) < 0.00001
            && Math.abs(matrix.e - x) < 0.01 && Math.abs(matrix.f - y) < 0.01;
    }, { engine, ...viewport }, { timeout: 40000 });
    const operation = async (name, action) => {
        const start = performance.now();
        try {
            const value = await bounded(name, action, 45000);
            operations.push({ name, milliseconds: performance.now() - start, succeeded: true, value });
            await checkpoint();
            return value;
        } catch (error) {
            operations.push({ name, milliseconds: performance.now() - start, succeeded: false, error: String(error) });
            await checkpoint();
            throw error;
        }
    };
    try {
        await page.addInitScript(() => {
            window.capacityLongTasks = [];
            new PerformanceObserver(list => {
                for (const entry of list.getEntries()) window.capacityLongTasks.push({ start: entry.startTime, duration: entry.duration });
            }).observe({ type: 'longtask', buffered: true });
        });
        await bounded('navigation', async () => {
            const start = performance.now();
            await page.goto(`${h.url}/workspace/modeling`);
            await page.waitForFunction(() => document.documentElement.style.getPropertyValue('--radius') !== '');
            await page.locator('[role="treeitem"]').first().waitFor();
            results.pageReadyMs = performance.now() - start;
        });
        await bounded('factory readiness', async () => {
            const start = performance.now();
            await h.open(3, `Stress Test — ${engine === 'x6' ? 'AntV X6' : 'Z.Blazor.Diagrams'}`);
            await h.ready(126);
            await h.bindProbe();
            results.coldFactoryMount = { count: 126, milliseconds: performance.now() - start };
        });
        results.runtimeBefore = await h.resources();
        results.browserBefore = await h.browserMetrics();
        assert.ok(h.packets.length > 0, 'actual SignalR WebSocket messages must be observed');
        results.transport = 'observed _blazor WebSocket proxy messages in both directions';
        const seed = await h.exportJson('factory.presentation.json');
        const portCounts = await root.evaluate((element, { engine, seed }) => Object.fromEntries(seed.occurrences.slice(0, 7).map(node => {
            const shape = engine === 'x6' ? element.querySelector(`.x6-node[data-cell-id="${node.id}"]`)
                : [...element.querySelectorAll('.part-svg')].find(part => part.getAttribute('data-element-id') === node.elementId);
            return [node.elementId, shape?.querySelectorAll(engine === 'x6' ? '.x6-port' : '.part-svg__port').length ?? 0];
        })), { engine, seed });
        const prepareStart = performance.now();
        await mkdir(scenesDirectory, { recursive: true });
        const scenePath = join(scenesDirectory, `${size}.presentation.json`);
        let scene;
        try { scene = JSON.parse(await readFile(scenePath, 'utf8')); }
        catch { scene = createScene(seed, size, true); await writeFile(scenePath, JSON.stringify(scene)); }
        const payload = Buffer.from(JSON.stringify(scene));
        results.scene = { nodes: scene.occurrences.length, edges: scene.routes.length, ports: scene.occurrences.reduce((sum, node) => sum + portCounts[node.elementId], 0), payloadBytes: payload.length,
            nestedNodes: scene.occurrences.filter(node => node.parentId).length,
            sha256: createHash('sha256').update(payload).digest('hex'), preparationMs: performance.now() - prepareStart };
        await page.screenshot({ path: join(directory, 'before-load.png') });
        const loadStarted = performance.now(), loadEpoch = performance.timeOrigin + loadStarted;
        await page.evaluate(({ engine, id }) => {
            window.capacityLoad = { start: performance.now(), firstImportedDom: null, lastMutation: null, samples: [] };
            const canvas = document.querySelector('.interconnection__canvas');
            const selector = engine === 'x6' ? `.x6-node[data-cell-id="${id}"]` : `[data-node-id="${id}"]`;
            window.capacityObserver = new MutationObserver(() => {
                const trace = window.capacityLoad;
                trace.lastMutation = performance.now();
                if (trace.firstImportedDom === null && canvas.querySelector(selector)) trace.firstImportedDom = performance.now();
            });
            window.capacityObserver.observe(canvas, { childList: true, subtree: true, attributes: true, attributeFilter: ['d', 'transform'] });
        }, { engine, id: scene.occurrences.find(node => !node.parentId).id });
        await bounded('complete scene load', async () => {
            const transferStart = performance.now();
            await root.locator('input[type=file]').setInputFiles({ name: 'large.presentation.json', mimeType: 'application/json', buffer: payload });
            results.fileInputDispatchMs = performance.now() - transferStart;
            await h.ready(size);
            await root.getByText('Prototype presentation restored.', { exact: true }).waitFor();
            await page.waitForFunction(({ engine, edges }) => document.querySelectorAll(engine === 'x6' ? '.x6-edge' : '.connection-svg__line').length === edges,
                { engine, edges: scene.routes.length }, { timeout: 180000 });
            results.completeDomMs = performance.now() - loadStarted;
            await paintOpportunity();
            results.completeRenderOpportunityMs = performance.now() - loadStarted;
            results.sceneCounts = await counts();
            assert.equal(results.sceneCounts.nodes, size);
            assert.equal(results.sceneCounts.edges, scene.routes.length);
            assert.equal(results.sceneCounts.ports, results.scene.ports);
            assert.equal(results.sceneCounts.edgesWithGeometry, scene.routes.length);
            assert.ok(results.sceneCounts.pathsWithGeometry >= scene.routes.length);
            const initial = scene.occurrences[0];
            results.acceptedAfterLoad = await h.readback(initial.id);
            assert.equal(results.acceptedAfterLoad.nodes, size);
            assert.equal(results.acceptedAfterLoad.edges, scene.routes.length);
        }, 180000);
        results.loadTraffic = summarizeTraffic(h.packets, loadEpoch, loadEpoch, performance.timeOrigin + performance.now());
        results.loadTrace = await page.evaluate(() => { window.capacityObserver.disconnect(); return window.capacityLoad; });
        results.runtimeLoaded = await h.resources();
        results.browserLoaded = await h.browserMetrics();
        results.completeSceneRendered = true;
        results.geometryVerification = await root.evaluate((element, { scene, engine }) => {
            const positions = new Map();
            for (const node of element.querySelectorAll(engine === 'x6' ? '.x6-node' : '[data-node-id], [data-group-id]')) {
                const matrix = node.transform?.baseVal.consolidate()?.matrix;
                if (!matrix) continue;
                const id = node.getAttribute(engine === 'x6' ? 'data-cell-id' : 'data-node-id');
                const semantic = node.querySelector('.part-svg')?.getAttribute('data-element-id');
                positions.set(id ?? `${semantic}:${matrix.e}:${matrix.f}`, { x: matrix.e, y: matrix.f });
            }
            const mismatches = scene.occurrences.filter(node => {
                const position = positions.get(node.id) ?? positions.get(`${node.elementId}:${node.geometry.x}:${node.geometry.y}`);
                return !position || Math.abs(position.x - node.geometry.x) > 0.01 || Math.abs(position.y - node.geometry.y) > 0.01;
            });
            return { checked: scene.occurrences.length, mismatches: mismatches.length };
        }, { scene, engine });
        assert.equal(results.geometryVerification.mismatches, 0);
        await bounded('normal zoom capture', () => page.screenshot({ path: join(directory, 'normal-zoom.png') }));
        await operation('fit to view', async () => {
            await root.getByRole('button', { name: 'Fit to view', exact: true }).click();
            await delay(2000);
            await paintOpportunity();
            const state = await h.readback(scene.occurrences[0].id);
            await waitViewport(state.viewport);
            await page.screenshot({ path: join(directory, 'fit-overview.png') });
            return { viewport: state.viewport, counts: await counts() };
        });
        results.firstActionProcessedMs = performance.now() - loadStarted;
        const groupElements = engine === 'z' ? await root.locator('[data-group-id]').evaluateAll(elements => elements.map(element => ({ id: element.getAttribute('data-group-id'),
            x: element.transform.baseVal.consolidate()?.matrix.e, y: element.transform.baseVal.consolidate()?.matrix.f }))) : [];
        for (const node of scene.occurrences.filter(node => scene.occurrences.some(child => child.parentId === node.id))) {
            const group = groupElements.find(item => Math.abs(item.x - node.geometry.x) < 0.01 && Math.abs(item.y - node.geometry.y) < 0.01);
            if (group) h.groupIds.set(node.id, group.id);
        }
        const restoreViewport = async (x, y, zoom) => {
            const next = { ...scene, viewport: { x, y, zoom } };
            await root.locator('input[type=file]').setInputFiles({ name: 'viewport.presentation.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(next)) });
            await page.waitForFunction(async ({ path, id, x, y, zoom }) => {
                const probe = await import(path), root = document.querySelector('[data-testid="diagram-benchmark-probe"]');
                const state = await probe.read(root, id);
                return state?.viewport.x === x && state.viewport.y === y && state.viewport.zoom === zoom;
            }, { path: '/Components/UI/Organisms/DiagramBenchmarkProbe/DiagramBenchmarkProbe.razor.js', id: scene.occurrences[0].id, x, y, zoom }, { timeout: 40000 });
            await waitViewport({ x, y, zoom });
            await delay(1000);
            await paintOpportunity();
        };
        await operation('distant region at normal zoom', async () => {
            const node = scene.occurrences.at(-1);
            await restoreViewport(64 - node.geometry.x, 66 - node.geometry.y, 1);
            await page.screenshot({ path: join(directory, 'distant-normal-zoom.png') });
            return { occurrenceId: node.id, geometry: node.geometry, counts: await counts() };
        });
        await operation('restore origin at normal zoom', () => restoreViewport(0, 0, 1));
        const connected = scene.occurrences.find(node => node.geometry.x === 64 && node.geometry.y === 66);
        const isolated = scene.occurrences.find(node => !node.parentId && !scene.routes.some(route => route.sourceId === node.id || route.targetId === node.id));
        const group = scene.occurrences.find(node => scene.occurrences.some(child => child.parentId === node.id));
        const recordMove = async (name, node, dx, dy) => operation(name, () => h.move(node, dx, dy, true));
        results.coldFirstDrag = await recordMove('first connected drag', connected, 30, 10);
        await operation('canonical Details selection', async () => {
            await page.locator('.mb-details-panel').getByText(/aocs/).first().waitFor();
            return true;
        });
        await recordMove('return first drag', connected, -30, -10);
        for (let index = 0; index < h.warmups; index++) await recordMove(`warm-up ${index + 1}`, connected, index % 2 ? -30 : 30, 0);
        for (const rtt of h.rtts) {
            h.setRtt(rtt);
            await delay(500 + rtt * 2);
            const profile = { addedRttMs: rtt, effectiveRtt: [], samples: [], serverBefore: await h.resources(), browserBefore: await h.browserMetrics() };
            for (let index = 0; index < 3; index++) profile.effectiveRtt.push(await h.ping());
            results.profiles.push(profile);
            for (let index = 0; index < h.samples; index++) {
                profile.samples.push(await recordMove(`measured ${rtt} ms ${index + 1}`, connected, index % 2 ? -30 : 30, 0));
                await checkpoint();
            }
            profile.serverAfter = await h.resources();
            profile.browserAfter = await h.browserMetrics();
        }
        h.setRtt(0);
        if (h.diagnosticProfile) {
            const probePath = '/Components/UI/Organisms/DiagramBenchmarkProbe/DiagramBenchmarkProbe.razor.js';
            const invokeProbe = name => page.evaluate(async ({ path, name }) => (await import(path))[name](document.querySelector('[data-testid="diagram-benchmark-probe"]')), { path: probePath, name });
            const diagnostic = { serverBefore: await h.resources(), browserBefore: await h.browserMetrics(), packetStart: h.packets.length };
            await cdp.send('Profiler.enable');
            await cdp.send('Profiler.start');
            await cdp.send('Tracing.start', { categories: 'devtools.timeline,disabled-by-default-devtools.timeline,blink.user_timing,v8,cc,input,latencyInfo', transferMode: 'ReturnAsStream' });
            await invokeProbe('startTrace');
            try {
                diagnostic.gesture = await recordMove('instrumented connected drag', connected, 30, 0);
            } finally {
                diagnostic.serverPhases = await invokeProbe('stopTrace');
                const cpu = await cdp.send('Profiler.stop');
                await writeFile(join(directory, 'browser.cpuprofile'), JSON.stringify(cpu.profile));
                const finished = new Promise(resolve => cdp.once('Tracing.tracingComplete', resolve));
                await cdp.send('Tracing.end');
                const { stream } = await finished;
                const chunks = [];
                for (;;) {
                    const chunk = await cdp.send('IO.read', { handle: stream });
                    chunks.push(chunk.base64Encoded ? Buffer.from(chunk.data, 'base64') : Buffer.from(chunk.data));
                    if (chunk.eof) break;
                }
                await cdp.send('IO.close', { handle: stream });
                await writeFile(join(directory, 'browser-trace.json'), Buffer.concat(chunks));
                diagnostic.packets = h.packets.slice(diagnostic.packetStart);
                diagnostic.serverAfter = await h.resources();
                diagnostic.browserAfter = await h.browserMetrics();
                await writeFile(join(directory, 'diagnostic.json'), JSON.stringify(diagnostic, null, 2));
            }
        }
        if (isolated) {
            await recordMove('isolated obstacle drag', isolated, 30, 0);
            await recordMove('isolated obstacle return', isolated, -30, 0);
        }
        if (group) {
            await recordMove('nested container drag', group, 30, 0);
            await recordMove('nested container return', group, -30, 0);
        }
        await operation('pan and zoom', async () => {
            const box = await canvas.boundingBox();
            await page.mouse.move(box.x + box.width - 35, box.y + box.height - 35);
            await page.mouse.down();
            await page.mouse.move(box.x + box.width - 85, box.y + box.height - 65, { steps: 15 });
            await page.mouse.up();
            await page.keyboard.down('Control');
            await page.mouse.wheel(0, -100);
            await page.keyboard.up('Control');
            await delay(2000);
            const state = await h.readback(connected.id);
            await waitViewport(state.viewport);
            return state;
        });
        results.runtimeAfter = await h.resources();
        results.browserAfter = await h.browserMetrics();
        if (engine === 'x6') results.routing = await canvas.evaluate(async (element, path) => {
            const value = (await import(path)).inspect(element);
            return { fallbacks: value.fallbacks.length, pending: value.pending, blocked: value.blocked, metrics: value.metrics.routes };
        }, canvasModule);
        await page.screenshot({ path: join(directory, 'after-interaction.png') });
        await operation('close and reopen', async () => {
            const title = `Stress Test — ${engine === 'x6' ? 'AntV X6' : 'Z.Blazor.Diagrams'}`;
            const before = { browser: await h.browserMetrics(), server: await h.resources() };
            await page.getByRole('button', { name: `Close ${title}`, exact: true }).click();
            await root.waitFor({ state: 'detached' });
            await delay(3000);
            const browserAfterClose = await h.browserMetrics();
            await h.open(3, title);
            await h.ready(126);
            await h.bindProbe();
            await delay(3000);
            return { before, browserAfterClose, browserAfterReopen: await h.browserMetrics(), serverAfterReopen: await h.resources(), counts: await counts() };
        });
        results.completed = true;
    } catch (error) {
        results.completed = false;
        results.failure = { stage, error: limitFailure ?? String(error), elapsedMs: performance.now() - started };
        try {
            results.failureCounts = await bounded('failure observation', counts, 5000);
            await bounded('failure capture', () => page.screenshot({ path: join(directory, 'failure.png'), timeout: 5000 }), 5000);
        } catch {}
    } finally {
        stopped = true;
        clearInterval(monitor);
        clearTimeout(ceiling);
        for (const timer of deadlines) clearTimeout(timer);
        try { results.longTasks = await bounded('final observations', () => page.evaluate(() => window.capacityLongTasks), 5000); } catch {}
        delete results.url;
        results.elapsedMs = performance.now() - started;
        await checkpoint();
        await writeFile(join(directory, 'results.json'), JSON.stringify(results, null, 2));
        await Promise.race([context.close().catch(() => {}), delay(5000)]);
        await Promise.race([browser.close().catch(() => {}), delay(5000)]);
        if (browser.isConnected()) await killOwnedBrowser();
    }
    return results;
}
