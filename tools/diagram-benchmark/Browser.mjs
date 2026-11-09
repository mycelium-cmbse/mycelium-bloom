// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import assert from 'node:assert/strict';
import { writeFile, readFile, mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { chromium } from 'playwright';
import { distribution, messages, summarizeTraffic } from './Protocol.mjs';
import { createScene } from './Scene.mjs';
import { measureCapacity } from './Capacity.mjs';

const probeUrl = '/Components/UI/Organisms/DiagramBenchmarkProbe/DiagramBenchmarkProbe.razor.js';
const canvasUrl = '/Components/UI/Organisms/InterconnectionCanvas/InterconnectionCanvas.razor.js';
const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

export async function measureCase({ url, key, engine, size, samples, warmups, rtts, directory, duration = 600, steps = 30, scenesDirectory, verifyRecovery = false, capacity = false, diagnosticProfile = false }) {
    await mkdir(directory, { recursive: true });
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1, colorScheme: 'light', reducedMotion: 'reduce',
        ...(capacity ? { recordVideo: { dir: join(directory, 'video'), size: { width: 1920, height: 1080 } } } : {}) });
    await context.addCookies([{ name: 'Bloom.DiagramBenchmark', value: key, url, httpOnly: true, sameSite: 'Strict' }]);
    const page = await context.newPage(), errors = [], packets = [];
    let rtt = 0, closed = false, blocked = false;
    const sockets = [];
    page.on('pageerror', error => errors.push({ time: Date.now(), message: error.message }));
    await page.routeWebSocket(/_blazor/, socket => {
        const server = socket.connectToServer();
        sockets.push(socket);
        socket.onMessage(data => {
            packets.push({ time: performance.timeOrigin + performance.now(), direction: 'sent', bytes: Buffer.byteLength(data), messages: messages(data) });
            setTimeout(() => { if (!closed && !blocked) { try { server.send(data); } catch {} } }, rtt / 2);
        });
        server.onMessage(data => setTimeout(() => {
            if (!closed && !blocked) {
                packets.push({ time: performance.timeOrigin + performance.now(), direction: 'received', bytes: Buffer.byteLength(data), messages: messages(data) });
                try { socket.send(data); } catch {}
            }
        }, rtt / 2));
    });
    const cdp = await context.newCDPSession(page);
    await cdp.send('Performance.enable');
    const root = page.locator(`[data-testid="${engine === 'x6' ? 'interconnection-x6' : 'interconnection'}"]`);
    const canvas = root.locator('.interconnection__canvas');
    const groupIds = new Map();
    const nodeSelector = id => engine === 'x6' ? `.x6-node[data-cell-id="${id}"]` : groupIds.has(id) ? `[data-group-id="${groupIds.get(id)}"]` : `[data-node-id="${id}"]`;
    const resolveGroups = async scene => {
        if (engine !== 'z') return;
        const groups = await root.locator('[data-group-id]').evaluateAll(elements => elements.map(element => ({ id: element.getAttribute('data-group-id'),
            semanticId: element.querySelector('.part-svg')?.getAttribute('data-element-id'),
            x: element.transform.baseVal.consolidate()?.matrix.e, y: element.transform.baseVal.consolidate()?.matrix.f })));
        groupIds.clear();
        for (const parent of scene.occurrences.filter(node => scene.occurrences.some(child => child.parentId === node.id))) {
            const group = groups.find(group => group.semanticId === parent.elementId && Math.abs(group.x - parent.geometry.x) < 0.01 && Math.abs(group.y - parent.geometry.y) < 0.01);
            assert.ok(group, 'library-generated container identity must resolve by canonical identity and occurrence geometry');
            groupIds.set(parent.id, group.id);
        }
    };
    const ready = async count => {
        await page.waitForFunction(({ engine, count }) => document.querySelectorAll(engine === 'x6' ? '.x6-node' : '.part-svg').length === count, { engine, count }, { timeout: capacity ? 180000 : 30000 });
        await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
    };
    const open = async (group, name) => {
        for (let attempt = 0; attempt < 3; attempt++) {
            await page.getByRole('button', { name: `Add tab to Editor group ${group}`, exact: true }).click();
            try {
                const item = page.getByRole('menuitem', { name, exact: true });
                await item.waitFor({ timeout: 2000 });
                await item.click();
                return;
            } catch (error) {
                if (attempt === 2) throw error;
                await page.keyboard.press('Escape');
            }
        }
    };
    const bindProbe = async () => {
        await page.waitForSelector('[data-testid="diagram-benchmark-probe"]', { state: 'attached' });
        for (let attempt = 0; attempt < 50; attempt++) {
            const result = await page.evaluate(async path => {
                window.bloomProbe = await import(path);
                window.bloomProbeRoot = document.querySelector('[data-testid="diagram-benchmark-probe"]');
                return window.bloomProbe.ping(window.bloomProbeRoot);
            }, probeUrl);
            if (result) return;
            await delay(50);
        }
        throw new Error('Diagnostic probe did not initialize.');
    };
    const resources = () => page.evaluate(() => window.bloomProbe.resources(window.bloomProbeRoot));
    const readback = id => page.evaluate(id => window.bloomProbe.read(window.bloomProbeRoot, id), id);
    const ping = async () => page.evaluate(async () => { const start = performance.now(); await window.bloomProbe.ping(window.bloomProbeRoot); return performance.now() - start; });
    const exportJson = async name => {
        const pending = page.waitForEvent('download');
        await root.getByRole('button', { name: 'Export presentation', exact: true }).click();
        const file = join(directory, name);
        await (await pending).saveAs(file);
        return JSON.parse(await readFile(file, 'utf8'));
    };
    const importJson = async scene => {
        await root.locator('input[type=file]').setInputFiles({ name: 'benchmark.presentation.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(scene)) });
        await ready(scene.occurrences.length);
        await root.getByText('Prototype presentation restored.', { exact: true }).waitFor();
        const initial = scene.occurrences[0];
        for (let attempt = 0; attempt < 50; attempt++) {
            const value = await readback(initial.id);
            if (value?.nodes === scene.occurrences.length && value.geometry?.x === initial.geometry.x) {
                await page.waitForFunction(({ scene, engine }) => {
                    const root = document.querySelector(`[data-testid="${engine === 'x6' ? 'interconnection-x6' : 'interconnection'}"]`);
                    return scene.occurrences.every(node => {
                    const selector = engine === 'x6' ? `.x6-node[data-cell-id="${node.id}"]` : `[data-node-id="${node.id}"]`;
                    const element = root.querySelector(selector);
                    const position = element?.transform.baseVal.consolidate()?.matrix;
                    if (position) return Math.abs(position.e - node.geometry.x) < 0.01 && Math.abs(position.f - node.geometry.y) < 0.01;
                    return engine === 'z' && [...root.querySelectorAll('g[data-group-id]')].some(group => {
                        const position = group.transform.baseVal.consolidate()?.matrix;
                        return group.querySelector('.part-svg')?.getAttribute('data-element-id') === node.elementId && position
                            && Math.abs(position.e - node.geometry.x) < 0.01 && Math.abs(position.f - node.geometry.y) < 0.01;
                    });
                    });
                }, { scene, engine }, { timeout: 10000 });
                await resolveGroups(scene);
                return;
            }
            await delay(50);
        }
        throw new Error('Imported scene was not reflected by the authoritative snapshot.');
    };
    const browserMetrics = async () => {
        const values = (await cdp.send('Performance.getMetrics')).metrics;
        return Object.fromEntries(values.filter(value => ['TaskDuration', 'ScriptDuration', 'LayoutDuration', 'RecalcStyleDuration', 'JSHeapUsedSize', 'JSHeapTotalSize', 'Nodes', 'Documents', 'JSEventListeners'].includes(value.name)).map(value => [value.name, value.value]));
    };
    const move = async (node, dx, dy, measured = true) => {
        const before = await readback(node.id);
        const expected = { x: before.geometry.x + dx, y: before.geometry.y + dy };
        await page.evaluate(({ id, expected }) => {
            window.bloomWatch = null;
            window.bloomProbe.watch(window.bloomProbeRoot, id, expected.x, expected.y)
                .then(snapshot => { window.bloomWatch = { time: performance.now(), snapshot }; })
                .catch(error => { window.bloomWatch = { error: String(error) }; });
        }, { id: node.id, expected });
        await ping();
        const element = root.locator(nodeSelector(node.id)), box = await element.boundingBox();
        await element.evaluate(element => {
            window.bloomTrace?.observer?.disconnect();
            window.bloomTrace?.routeObserver?.disconnect();
            const trace = window.bloomTrace = { frames: [], routes: [], moves: [], raf: [], down: 0, released: 0, active: true, timeOrigin: performance.timeOrigin };
            const position = () => {
                const values = element.getAttribute('transform')?.match(/[-+]?\d*\.?\d+(?:e[-+]?\d+)?/gi)?.map(Number) ?? [];
                return values.length === 6 ? { x: values[4], y: values[5] } : { x: values[0], y: values[1] };
            };
            trace.initial = position();
            trace.observer = new MutationObserver(() => trace.frames.push({ time: performance.now(), ...position() }));
            trace.observer.observe(element, { attributes: true, attributeFilter: ['transform'] });
            trace.routeObserver = new MutationObserver(records => {
                if (records.some(record => record.target.closest('.x6-edge, .diagram-link, .connection-svg'))) trace.routes.push(performance.now());
            });
            trace.routeObserver.observe(element.closest('.interconnection__canvas'), { subtree: true, attributes: true, attributeFilter: ['d', 'transform'] });
            const down = event => { trace.down = event.timeStamp; trace.downHandled = performance.now(); trace.start = { x: event.clientX, y: event.clientY }; };
            const movement = event => { if (trace.down && !trace.released) trace.moves.push({ time: event.timeStamp, handled: performance.now(), x: trace.initial.x + event.clientX - trace.start.x, y: trace.initial.y + event.clientY - trace.start.y }); };
            const up = event => { trace.released = event.timeStamp; trace.releaseHandled = performance.now(); document.removeEventListener('pointermove', movement, true); };
            document.addEventListener('pointerdown', down, { once: true, capture: true });
            document.addEventListener('pointermove', movement, true);
            document.addEventListener('pointerup', up, { once: true, capture: true });
            const frame = time => { if (trace.active) { trace.raf.push(time); requestAnimationFrame(frame); } };
            requestAnimationFrame(frame);
        });
        const startX = box.x + 60, startY = box.y + 25;
        await page.mouse.move(startX, startY);
        const start = performance.now();
        const dispatches = [], pendingInput = [];
        const dispatch = (type, x, y) => {
            const timestamp = performance.timeOrigin + performance.now();
            dispatches.push({ type, elapsedMs: performance.now() - start, x, y });
            pendingInput.push(cdp.send('Input.dispatchMouseEvent', { type, x, y, button: type === 'mouseMoved' ? 'none' : 'left',
                buttons: type === 'mouseReleased' ? 0 : 1, clickCount: type === 'mouseMoved' ? 0 : 1, timestamp: timestamp / 1000, pointerType: 'mouse' }));
        };
        dispatch('mousePressed', startX, startY);
        for (let step = 1; step <= steps; step++) {
            const remaining = start + duration * step / steps - performance.now();
            if (remaining > 0) await delay(remaining);
            dispatch('mouseMoved', startX + dx * step / steps, startY + dy * step / steps);
        }
        dispatch('mouseReleased', startX + dx, startY + dy);
        await Promise.all(pendingInput);
        await page.waitForFunction(() => window.bloomWatch !== null, null, { timeout: 20000 });
        await page.waitForFunction(({ selector, expected }) => {
            const element = document.querySelector(selector), values = element?.getAttribute('transform')?.match(/[-+]?\d*\.?\d+(?:e[-+]?\d+)?/gi)?.map(Number) ?? [];
            const x = values.length === 6 ? values[4] : values[0], y = values.length === 6 ? values[5] : values[1];
            return Math.abs(x - expected.x) < 0.01 && Math.abs(y - expected.y) < 0.01;
        }, { selector: nodeSelector(node.id), expected });
        if (engine === 'x6') {
            for (let attempt = 0; attempt < 100; attempt++) {
                const pending = await canvas.evaluate(async (element, path) => (await import(path)).inspect(element).pending, canvasUrl);
                if (!pending) break;
                await delay(20);
            }
        }
        await delay(80);
        const observation = await page.evaluate(() => {
            window.bloomTrace.active = false;
            window.bloomTrace.observer.disconnect();
            window.bloomTrace.routeObserver.disconnect();
            const { observer, routeObserver, ...trace } = window.bloomTrace;
            return { trace, watch: window.bloomWatch, end: performance.timeOrigin + performance.now() };
        });
        assert.ok(observation.watch.snapshot, 'accepted-state watcher must complete');
        assert.equal(observation.watch.snapshot.geometry.x, expected.x);
        assert.equal(observation.watch.snapshot.geometry.y, expected.y);
        const native = engine === 'x6' ? await canvas.evaluate(async (element, path) => (await import(path)).inspect(element).metrics, canvasUrl) : null;
        const trace = observation.trace;
        const visible = trace.frames.filter(frame => Math.abs(frame.x - trace.initial.x) > 0.01 || Math.abs(frame.y - trace.initial.y) > 0.01);
        const lags = visible.map(frame => {
            const candidates = trace.moves.filter(point => Math.abs(point.x - frame.x) <= 0.6 && Math.abs(point.y - frame.y) <= 0.6 && point.time <= frame.time + 1);
            return candidates.length ? frame.time - candidates.at(-1).time : null;
        }).filter(value => value !== null);
        const during = trace.raf.filter(time => time >= trace.down && time <= trace.released);
        const frameGaps = during.slice(1).map((time, index) => time - during[index]);
        const result = { expected, finalErrorPixels: 0, dragStartMs: visible[0] ? visible[0].time - trace.down : null,
            actualGestureMs: trace.released - trace.down, settlingMs: Math.max(0, ...trace.frames.map(frame => frame.time - trace.released)),
            inputDispatchDurationMs: dispatches.at(-1).elapsedMs - dispatches[0].elapsedMs,
            releaseHandlerDelayMs: trace.releaseHandled - trace.released,
            connectorSettlingMs: Math.max(0, ...trace.routes.map(time => time - trace.released)),
            sceneSettlingMs: Math.max(0, ...trace.frames.map(frame => frame.time - trace.released), ...trace.routes.map(time => time - trace.released)),
            stateAcknowledgmentMs: observation.watch.time - trace.released, nativeAcknowledgmentMs: native?.acknowledgments.at(-1) ?? null,
            pointerLagMs: distribution(lags), dragFrameIntervalsMs: distribution(frameGaps),
            estimatedMissedRafFrames: frameGaps.reduce((sum, gap) => sum + Math.max(0, Math.round(gap / (1000 / 60)) - 1), 0),
            updatesAfterRelease: trace.frames.filter(frame => frame.time > trace.released).length,
            transformUpdates: trace.frames.length, inputMoves: trace.moves.length,
            routing: native?.routes.at(-1) ?? null, acceptedSnapshot: observation.watch.snapshot,
            traffic: summarizeTraffic(packets, trace.timeOrigin + trace.down, trace.timeOrigin + trace.released, observation.end),
            trace: measured ? trace : undefined, dispatches: measured ? dispatches : undefined };
        return result;
    };
    const results = { engine, size, browser: await browser.version(), url: new URL(url).origin, viewport: { width: 1920, height: 1080, deviceScaleFactor: 1 }, profiles: [], errors };
    if (capacity) {
        try {
            return await measureCapacity({ browser, context, page, cdp, root, canvas, engine, size, directory, scenesDirectory, samples, warmups, rtts,
                results, packets, open, bindProbe, resources, readback, ping, exportJson, browserMetrics, move, ready, diagnosticProfile,
                setRtt: value => { rtt = value; }, url, groupIds });
        } finally {
            closed = true;
            await context.close().catch(() => {});
            await browser.close().catch(() => {});
        }
    }
    try {
        const beforeLoad = await browserMetrics(), navigationStart = performance.now();
        await page.goto(`${url}/workspace/modeling`);
        await page.waitForFunction(() => document.documentElement.style.getPropertyValue('--radius') !== '');
        await page.locator('[role="treeitem"]').first().waitFor();
        results.pageReadyMs = performance.now() - navigationStart;
        const factoryCount = size === 7 ? 7 : 126;
        const title = `${size === 7 ? 'Interconnection' : 'Stress Test'} — ${engine === 'x6' ? 'AntV X6' : 'Z.Blazor.Diagrams'}`;
        const coldStart = performance.now();
        await open(3, title);
        await ready(factoryCount);
        results.coldFactoryMount = { count: factoryCount, elapsedMs: performance.now() - coldStart };
        await bindProbe();
        let seed = await exportJson('factory.presentation.json');
        await mkdir(scenesDirectory, { recursive: true });
        const scenePath = join(scenesDirectory, `${size}.presentation.json`);
        let scene;
        try { scene = JSON.parse(await readFile(scenePath, 'utf8')); }
        catch { scene = createScene(seed, size); await writeFile(scenePath, JSON.stringify(scene, null, 2)); }
        const fullStart = performance.now();
        await importJson(scene);
        results.firstRequestedSceneImportMs = performance.now() - fullStart;
        results.afterColdBrowser = await browserMetrics();
        results.coldBrowserDelta = Object.fromEntries(Object.keys(beforeLoad).map(name => [name, results.afterColdBrowser[name] - beforeLoad[name]]));
        results.runtime = await resources();
        results.assets = await page.evaluate(() => performance.getEntriesByType('resource').filter(entry => /\/vendor\/x6\/|Z\.Blazor\.Diagrams.*script|min\.js|blazor\.web\.js/.test(entry.name)).map(entry => ({ path: new URL(entry.name).pathname, duration: entry.duration, transferSize: entry.transferSize, encodedBodySize: entry.encodedBodySize, decodedBodySize: entry.decodedBodySize })));
        results.sceneCounts = await root.evaluate((element, engine) => ({ nodes: element.querySelectorAll(engine === 'x6' ? '.x6-node' : '.part-svg').length,
            ports: element.querySelectorAll(engine === 'x6' ? '.x6-port' : '.part-svg__port').length,
            edges: element.querySelectorAll(engine === 'x6' ? '.x6-edge' : '.connection-svg__line').length,
            domElements: element.querySelectorAll('*').length, svgElements: [...element.querySelectorAll('*')].filter(item => item.namespaceURI === 'http://www.w3.org/2000/svg').length }), engine);
        assert.equal(results.sceneCounts.nodes, size);
        assert.equal(results.sceneCounts.edges, size / 7 * 5);
        const connected = scene.occurrences.find(node => node.geometry.x === 64 && node.geometry.y === 66);
        results.coldFirstDrag = await move(connected, 60, 20);
        await importJson(scene);
        for (let profileIndex = 0; profileIndex < rtts.length; profileIndex++) {
            rtt = rtts[profileIndex];
            await delay(300 + rtt * 2);
            await importJson(scene);
            const profile = { simulatedRttMs: rtt, effectiveRttBeforeMs: [], effectiveRttAfterMs: [], warmup: [], samples: [], resources: [] };
            for (let index = 0; index < 7; index++) profile.effectiveRttBeforeMs.push(await ping());
            profile.preWarmResources = await resources();
            const warmCount = profileIndex === 0 ? warmups : 6;
            for (let index = 0; index < warmCount; index++) profile.warmup.push(await move(connected, index % 2 === 0 ? 60 : -60, index % 2 === 0 ? 20 : -20, false));
            if (warmCount % 2) await importJson(scene);
            await delay(2000);
            profile.postWarmResources = await resources();
            profile.browserBefore = await browserMetrics();
            profile.measurementStarted = Date.now();
            for (let index = 0; index < samples; index++) {
                profile.samples.push(await move(connected, index % 2 === 0 ? 60 : -60, index % 2 === 0 ? 20 : -20));
                if (index % 4 === 0) profile.resources.push({ sample: index, server: await resources(), browser: await browserMetrics() });
            }
            profile.measurementEnded = Date.now();
            profile.browserAfter = await browserMetrics();
            profile.postMeasureResources = await resources();
            for (let index = 0; index < 7; index++) profile.effectiveRttAfterMs.push(await ping());
            profile.summary = Object.fromEntries(['settlingMs', 'connectorSettlingMs', 'sceneSettlingMs', 'stateAcknowledgmentMs', 'nativeAcknowledgmentMs', 'dragStartMs', 'actualGestureMs', 'estimatedMissedRafFrames'].map(name => [name, distribution(profile.samples.map(item => item[name]).filter(value => value !== null))]));
            profile.summary.pointerLagMs = distribution(profile.samples.flatMap(item => item.pointerLagMs.count ? [item.pointerLagMs.p50] : []));
            profile.summary.effectiveRttMs = distribution([...profile.effectiveRttBeforeMs, ...profile.effectiveRttAfterMs]);
            results.profiles.push(profile);
            await writeFile(join(directory, 'results.json'), JSON.stringify(results, null, 2));
            console.log(JSON.stringify({ engine, size, rtt, samples, settling: profile.summary.settlingMs, acknowledgment: profile.summary.stateAcknowledgmentMs }));
        }
        rtt = 0;
        await delay(400);
        results.supplementary = {};
        const obstacle = scene.occurrences.find(node => node.geometry.x === 334 && node.geometry.y === 366);
        const child = scene.occurrences.find(node => node.parentId), parent = scene.occurrences.find(node => node.id === child.parentId);
        for (const [name, node] of [['isolatedObstacle', obstacle], ['nestedContainer', parent]]) {
            await importJson(scene);
            const samples = [];
            for (let index = 0; index < 3; index++) {
                await importJson(scene);
                samples.push(await move(node, 20, 10));
            }
            results.supplementary[name] = samples;
            if (name === 'nestedContainer') {
                const movedChild = await readback(child.id);
                assert.equal(movedChild.geometry.x, child.geometry.x + 20);
                assert.equal(movedChild.geometry.y, child.geometry.y + 10);
            }
        }
        await importJson(scene);
        const lockedState = await exportJson('before-readonly.presentation.json');
        await root.getByRole('checkbox', { name: 'Read-only', exact: true }).check();
        const lockedBox = await root.locator(nodeSelector(connected.id)).boundingBox();
        await page.mouse.move(lockedBox.x + 60, lockedBox.y + 25);
        await page.mouse.down();
        await page.mouse.move(lockedBox.x + 90, lockedBox.y + 45, { steps: 20 });
        await page.mouse.up();
        assert.deepEqual((await exportJson('readonly.presentation.json')).occurrences, lockedState.occurrences);
        await root.getByRole('button', { name: 'Zoom in', exact: true }).click();
        const navigated = await exportJson('readonly-zoom.presentation.json');
        assert.ok(navigated.viewport.zoom > lockedState.viewport.zoom);
        await root.getByRole('checkbox', { name: 'Read-only', exact: true }).uncheck();
        results.supplementary.readOnlyNavigation = true;
        await importJson(scene);
        results.supplementary.remount = [];
        for (let index = 0; index < 3; index++) {
            const before = await exportJson('before-remount.presentation.json');
            await page.getByRole('tab', { name: 'Editor 3', exact: true }).click();
            await root.waitFor({ state: 'detached' });
            const start = performance.now();
            await page.getByRole('tab', { name: title, exact: true }).click();
            await ready(size);
            results.supplementary.remount.push(performance.now() - start);
            await bindProbe();
            await resolveGroups(before);
            assert.deepEqual(await exportJson('after-remount.presentation.json'), before);
        }
        const separator = page.getByRole('separator', { name: 'Resize Editor group 2 and Editor group 3', exact: true });
        const split = await separator.boundingBox(), pane = await canvas.boundingBox();
        await page.mouse.move(split.x + split.width / 2, split.y + 30);
        await page.mouse.down();
        await page.mouse.move(split.x + split.width / 2 - 40, split.y + 30, { steps: 20 });
        await page.mouse.up();
        await page.waitForFunction(({ engine, width }) => document.querySelector(`[data-testid="${engine === 'x6' ? 'interconnection-x6' : 'interconnection'}"] .interconnection__canvas`).getBoundingClientRect().width !== width, { engine, width: pane.width });
        results.supplementary.resize = { before: pane, after: await canvas.boundingBox() };
        assert.deepEqual((await exportJson('after-resize.presentation.json')).occurrences, scene.occurrences);
        await root.getByRole('button', { name: 'Fit to view', exact: true }).click();
        const fitted = await exportJson('fitted.presentation.json');
        assert.ok(fitted.viewport.zoom >= 0.1 && fitted.viewport.zoom <= 4);
        results.supplementary.fit = fitted.viewport;
        await importJson(scene);
        if (verifyRecovery) {
            const saved = await exportJson('before-reconnect.presentation.json');
            if (engine === 'x6') {
                const inspect = () => canvas.evaluate(async (element, path) => (await import(path)).inspect(element), canvasUrl);
                const waitForCanvas = async predicate => {
                    for (let attempt = 0; attempt < 200; attempt++) {
                        const value = await inspect();
                        if (predicate(value)) return value;
                        await delay(100);
                    }
                    throw new Error('Canvas did not recover authoritative agreement.');
                };
                blocked = true;
                const box = await root.locator(nodeSelector(connected.id)).boundingBox();
                await page.mouse.move(box.x + 60, box.y + 25);
                await page.mouse.down();
                await page.mouse.move(box.x + 120, box.y + 45, { steps: 20 });
                await page.mouse.up();
                const timedOut = await waitForCanvas(value => value.blocked);
                assert.equal(timedOut.geometry.find(node => node.id === connected.id).geometry.x, connected.geometry.x);
                blocked = false;
                const recovered = await waitForCanvas(value => !value.blocked);
                assert.equal(recovered.snapshot.state.occurrences.find(node => node.id === connected.id).geometry.x, connected.geometry.x);
                assert.deepEqual((await exportJson('recovered.presentation.json')).occurrences, saved.occurrences);
                results.supplementary.interruption = { rollback: true, authoritativeRecovery: true, timedOutMetrics: timedOut.metrics };
                await move(connected, 20, 10);
                await importJson(scene);
            }
            const start = performance.now();
            await sockets.at(-1).close({ code: 1012, reason: 'Diagnostic reconnect' });
            let reconnected = false;
            for (let attempt = 0; attempt < 60; attempt++) {
                try {
                    const responding = await page.evaluate(() => Promise.race([window.bloomProbe.ping(window.bloomProbeRoot), new Promise(resolve => setTimeout(() => resolve(0), 500))]));
                    if (responding) { reconnected = true; break; }
                } catch {}
                await delay(100);
            }
            assert.ok(reconnected, 'Blazor circuit must reconnect');
            results.supplementary.socketReconnect = { elapsedMs: performance.now() - start, connections: sockets.length };
            assert.deepEqual((await exportJson('after-reconnect.presentation.json')).occurrences, saved.occurrences);
            await move(connected, 20, 10);
            await importJson(scene);
        }
        await page.screenshot({ path: join(directory, 'scene.png') });
        const exported = await exportJson('roundtrip.presentation.json');
        await importJson(exported);
        assert.deepEqual(await exportJson('restored.presentation.json'), exported);
        results.jsonRoundtrip = true;
        const svgDownload = page.waitForEvent('download');
        await root.getByRole('button', { name: 'Export SVG', exact: true }).click();
        const svgFile = join(directory, 'diagram.svg');
        await (await svgDownload).saveAs(svgFile);
        const svgText = await readFile(svgFile, 'utf8');
        assert.ok(!svgText.includes('foreignObject'));
        const counts = await page.evaluate(({ text, engine }) => {
            const document = new DOMParser().parseFromString(text, 'image/svg+xml');
            return { errors: document.querySelectorAll('parsererror').length, nodes: document.querySelectorAll(engine === 'x6' ? '.x6-node' : '.part-svg').length,
                edges: document.querySelectorAll(engine === 'x6' ? '.x6-edge' : '.connection-svg__line').length, markers: document.querySelectorAll('marker').length,
                ports: document.querySelectorAll(engine === 'x6' ? '.x6-port' : '.part-svg__port').length, viewBox: document.documentElement.getAttribute('viewBox') };
        }, { text: svgText, engine });
        assert.equal(counts.errors, 0);
        assert.equal(counts.nodes, size);
        assert.equal(counts.edges, size / 7 * 5);
        results.svg = counts;
        const independent = await browser.newPage({ viewport: { width: 1200, height: 900 } });
        try {
            await independent.goto(pathToFileURL(resolve(svgFile)).href);
            assert.equal(await independent.locator('parsererror').count(), 0);
            assert.equal(await independent.locator(engine === 'x6' ? '.x6-node' : '.part-svg').count(), size);
            results.svg.independentDocument = true;
            const imagePreview = async (file, name) => {
                const html = join(directory, `${name}.html`);
                await writeFile(html, `<html><body style="margin:0"><img src="${file}"></body></html>`);
                await independent.goto(pathToFileURL(resolve(html)).href);
                await independent.locator('img').evaluate(element => element.decode());
                if (size === 7) await independent.screenshot({ path: join(directory, `${name}.png`), fullPage: true });
            };
            await imagePreview('diagram.svg', 'standalone-light');
            await page.getByRole('button', { name: 'Switch to dark mode', exact: true }).click();
            await page.waitForFunction(() => document.documentElement.classList.contains('dark'));
            await delay(100);
            const darkDownload = page.waitForEvent('download');
            await root.getByRole('button', { name: 'Export SVG', exact: true }).click();
            const darkFile = join(directory, 'diagram-dark.svg');
            await (await darkDownload).saveAs(darkFile);
            await independent.goto(pathToFileURL(resolve(darkFile)).href);
            assert.equal(await independent.locator('parsererror').count(), 0);
            assert.equal(await independent.locator(engine === 'x6' ? '.x6-node' : '.part-svg').count(), size);
            assert.notEqual(await readFile(darkFile, 'utf8'), svgText);
            results.svg.darkIndependentDocument = true;
            await imagePreview('diagram-dark.svg', 'standalone-dark');
        } finally { await independent.close(); }
        results.errors = errors;
        assert.deepEqual(errors.filter(item => !item.message.includes('JsOnClickOutside')), []);
        results.completed = true;
        await writeFile(join(directory, 'results.json'), JSON.stringify(results, null, 2));
        return results;
    } catch (error) {
        results.failure = String(error);
        await writeFile(join(directory, 'results.json'), JSON.stringify(results, null, 2));
        await page.screenshot({ path: join(directory, 'failure.png') }).catch(() => {});
        throw error;
    } finally {
        closed = true;
        await context.close();
        await browser.close();
    }
}
