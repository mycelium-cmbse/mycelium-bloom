// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import assert from 'node:assert/strict';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { chromium } from 'playwright';
import { messages, summarizeTraffic } from './Protocol.mjs';

const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
const execute = promisify(execFile);
const probePath = '/Components/UI/Organisms/DiagramBenchmarkProbe/DiagramBenchmarkProbe.razor.js';

export async function measureViewport({ url, key, engine, size, samples, warmups, rtts, directory, scenesDirectory, interactionsOnly = false, loading = false }) {
    assert.equal(engine, 'z');
    await mkdir(directory, { recursive: true });
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1, recordVideo: { dir: join(directory, 'video'), size: { width: 1920, height: 1080 } } });
    await context.addCookies([{ name: 'Bloom.DiagramBenchmark', value: key, url, httpOnly: true, sameSite: 'Strict' }]);
    const page = await context.newPage(), results = { scenario: 'viewport rendering; distinct from all-at-once capacity', engine, size, completed: false, errors: [], resources: [], regions: [], drags: [] }, packets = [];
    let rtt = 0, sampling = false, stage = 'startup';
    const browserCdp = await browser.newBrowserCDPSession();
    const browserPid = (await browserCdp.send('SystemInfo.getProcessInfo')).processInfo.find(p => p.type === 'browser').id;
    const ceiling = setTimeout(() => execute('taskkill', ['/PID', String(browserPid), '/T', '/F'], { windowsHide: true }).catch(() => {}), 600000);
    const checkpoint = () => writeFile(join(directory, 'checkpoint.json'), JSON.stringify({ ...results, stage }, null, 2));
    const sample = async () => {
        if (sampling) return;
        sampling = true;
        try {
            const processes = (await browserCdp.send('SystemInfo.getProcessInfo')).processInfo;
            const output = await execute('pwsh', ['-NoProfile', '-Command', `Get-Process -Id ${processes.map(p => p.id).join(',')} -ErrorAction SilentlyContinue | Select-Object WorkingSet64,PrivateMemorySize64 | ConvertTo-Json -Compress`], { windowsHide: true, timeout: 5000 });
            const values = JSON.parse(output.stdout || '[]'), items = Array.isArray(values) ? values : [values];
            const workingSetBytes = items.reduce((sum, item) => sum + item.WorkingSet64, 0);
            results.resources.push({ time: Date.now(), stage, workingSetBytes, privateBytes: items.reduce((sum, item) => sum + item.PrivateMemorySize64, 0), cpuSeconds: processes.reduce((sum, item) => sum + item.cpuTime, 0) });
            if (workingSetBytes > 6 * 1024 ** 3) await execute('taskkill', ['/PID', String(browserPid), '/T', '/F'], { windowsHide: true });
        } catch {} finally { sampling = false; }
    };
    const monitor = setInterval(sample, 3000);
    page.on('pageerror', error => results.errors.push(error.message));
    await page.routeWebSocket(/_blazor/, socket => {
        results.webSocketObserved = true;
        const server = socket.connectToServer();
        socket.onMessage(data => { packets.push({ time: Date.now(), direction: 'sent', bytes: Buffer.byteLength(data), messages: messages(data) }); setTimeout(() => { try { server.send(data); } catch {} }, rtt / 2); });
        server.onMessage(data => setTimeout(() => { packets.push({ time: Date.now(), direction: 'received', bytes: Buffer.byteLength(data), messages: messages(data) }); try { socket.send(data); } catch {} }, rtt / 2));
    });
    const cdp = await context.newCDPSession(page);
    await cdp.send('Performance.enable');
    const root = page.locator('[data-testid="interconnection"]');
    const counts = () => root.evaluate(element => ({ nodes: element.querySelectorAll('.part-svg').length, ports: element.querySelectorAll('.part-svg__port').length, edges: element.querySelectorAll('.connection-svg__line').length, dom: element.querySelectorAll('*').length }));
    const read = id => page.evaluate(id => window.viewportProbe.read(window.viewportProbeRoot, id), id ?? '00000000-0000-0000-0000-000000000000');
    const resources = () => page.evaluate(() => window.viewportProbe.resources(window.viewportProbeRoot));
    const metrics = async () => Object.fromEntries((await cdp.send('Performance.getMetrics')).metrics.map(item => [item.name, item.value]));
    const frames = () => page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve))));
    const open = async () => {
        for (let attempt = 0; attempt < 3; attempt++) {
            await page.getByRole('button', { name: 'Add tab to Editor group 3', exact: true }).click();
            try { await page.getByRole('menuitem', { name: 'Stress Test — Z.Blazor.Diagrams', exact: true }).click({ timeout: 2000 }); break; }
            catch (error) { if (attempt === 2) throw error; await page.keyboard.press('Escape'); }
        }
        await root.locator('.part-svg').first().waitFor({ timeout: loading ? 30000 : 180000 });
        await page.evaluate(async path => { window.viewportProbe = await import(path); window.viewportProbeRoot = document.querySelector('[data-testid="diagram-benchmark-probe"]'); }, probePath);
    };
    let detailsReady = async () => {};
    const restore = async state => {
        const node = state.occurrences[0], previous = await read(node.id);
        const start = performance.now();
        results.pendingRestore = { viewport: state.viewport, step: "import" };
        await root.locator('input[type=file]').setInputFiles({ name: 'viewport.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(state)) });
        for (let attempt = 0; attempt < 1800; attempt++) {
            const accepted = await read(node.id);
            if (accepted?.nodes === state.occurrences.length && accepted.revision > previous.revision
                && JSON.stringify(accepted.geometry) === JSON.stringify(node.geometry)) break;
            if (attempt === 1799) throw new Error('Presentation import readiness deadline');
            await delay(100);
        }
        results.pendingRestore.step = "viewport transform";
        await page.waitForFunction(viewport => {
            const layer = document.querySelector('.diagram-svg-layer'), matrix = layer?.style.transform;
            if (!matrix) return false;
            const actual = new DOMMatrix(matrix);
            return Math.abs(actual.e - viewport.x) < 1 && Math.abs(actual.f - viewport.y) < 1 && Math.abs(actual.a - viewport.zoom) < 0.01;
        }, state.viewport, { timeout: loading ? 30000 : 180000 });
        results.pendingRestore.step = "details";
        if (loading) await detailsReady();
        const containerHandle = await page.waitForFunction(() => {
            const canvas = document.querySelector('[data-testid="interconnection"] .diagram-canvas');
            if (!canvas?.getClientRects().length) return false;
            const box = canvas.getBoundingClientRect();
            return { width: box.width, height: box.height };
        });
        const container = await containerHandle.jsonValue();
        await containerHandle.dispose();
        const parents = new Set(state.occurrences.map(node => node.parentId));
        const visible = state.occurrences.find(node => !node.parentId && !parents.has(node.id)
            && node.geometry.x * state.viewport.zoom + state.viewport.x >= 0
            && node.geometry.x * state.viewport.zoom + state.viewport.x < container.width
            && node.geometry.y * state.viewport.zoom + state.viewport.y >= 0
            && node.geometry.y * state.viewport.zoom + state.viewport.y < container.height);
        assert.ok(visible, 'The restored viewport must contain a known non-container occurrence.');
        results.pendingRestore.step = "node transform";
        await page.waitForFunction(node => {
            const element = document.querySelector(`[data-node-id="${node.id}"]`);
            const matrix = element?.transform.baseVal.consolidate()?.matrix;
            return matrix && Math.abs(matrix.e - node.geometry.x) < 0.01 && Math.abs(matrix.f - node.geometry.y) < 0.01;
        }, visible, { timeout: loading ? 30000 : 180000 });
        const geometry = new Map(state.occurrences.map(node => [node.id, node.geometry]));
        const inside = box => box && (box.x + box.width / 2) * state.viewport.zoom + state.viewport.x >= 0
            && (box.x + box.width / 2) * state.viewport.zoom + state.viewport.x < container.width
            && (box.y + box.height / 2) * state.viewport.zoom + state.viewport.y >= 0
            && (box.y + box.height / 2) * state.viewport.zoom + state.viewport.y < container.height;
        const visibleRoutes = state.routes.filter(route => inside(geometry.get(route.sourceId)) && inside(geometry.get(route.targetId))).map(route => route.id);
        results.pendingRestore.step = "visible routes";
        await page.waitForFunction(ids => ids.every(id => document.querySelector(`[data-link-id="${id}"] .connection-svg__line`)), visibleRoutes, { timeout: loading ? 30000 : 180000 });
        await frames();
        return performance.now() - start;
    };
    const download = async (button, filename) => {
        const pending = page.waitForEvent('download', { timeout: loading ? 30000 : 180000 });
        await root.getByRole('button', { name: button, exact: true }).click();
        const file = join(directory, filename); await (await pending).saveAs(file); return file;
    };
    const gesture = async (start, dx, dy) => {
        await page.mouse.move(start.x, start.y);
        const pendingInput = [];
        const dispatch = (type, x, y) => pendingInput.push(cdp.send('Input.dispatchMouseEvent', {
            type, x, y, button: type === 'mouseMoved' ? 'none' : 'left',
            buttons: type === 'mouseReleased' ? 0 : 1, clickCount: type === 'mouseMoved' ? 0 : 1,
            timestamp: (performance.timeOrigin + performance.now()) / 1000, pointerType: 'mouse'
        }));
        dispatch('mousePressed', start.x, start.y);
        const began = performance.now();
        for (let step = 1; step <= 30; step++) {
            await delay(Math.max(0, began + step * 20 - performance.now()));
            dispatch('mouseMoved', start.x + dx * step / 30, start.y + dy * step / 30);
        }
        dispatch('mouseReleased', start.x + dx, start.y + dy);
        await Promise.all(pendingInput);
    };
    try {
        results.browser = browser.version();
        await page.goto(url + '/workspace/modeling');
        await page.waitForFunction(() => document.documentElement.style.getPropertyValue('--radius') !== '');
        await page.locator('[role=treeitem]').first().waitFor();
        const requestStart = performance.now();
        await open();
        detailsReady = async () => {
            if (!loading) return;
            await page.waitForFunction(() => {
                const names = [...document.querySelectorAll('.part-svg__name')];
                return names.length > 0 && names.every(node => !['Loading…', 'Unavailable'].includes(node.textContent.trim()));
            }, null, { timeout: loading ? 30000 : 180000 });
            await frames();
        };
        const dataStats = () => page.locator('[data-testid="diagram-data-stats"]').evaluate(element => Object.fromEntries(Object.entries(element.dataset).filter(([key]) => key !== 'testid').map(([key, value]) => [key, Number(value)])));
        await detailsReady();
        results.requestToUsableViewportMs = performance.now() - requestStart;
        if (loading) results.initialData = await dataStats();
        const scene = JSON.parse(await readFile(loading ? await download('Export presentation', 'source.presentation.json') : join(scenesDirectory, `${size}.presentation.json`), 'utf8'));
        results.beforeLoad = { server: await resources(), browser: await metrics() };
        await page.evaluate(firstId => {
            window.viewportLoad = { start: performance.now(), firstContent: null, maximumMounted: 0, longTasks: [] };
            new PerformanceObserver(list => window.viewportLoad.longTasks.push(...list.getEntries().map(entry => ({ start: entry.startTime, duration: entry.duration })))).observe({ type: 'longtask' });
            const observer = new MutationObserver(() => {
                const nodes = document.querySelectorAll('[data-testid="interconnection"] .part-svg').length;
                window.viewportLoad.maximumMounted = Math.max(window.viewportLoad.maximumMounted, nodes);
                if (document.querySelector(`[data-node-id="${firstId}"]`) && window.viewportLoad.firstContent === null) window.viewportLoad.firstContent = performance.now();
            });
            observer.observe(document.querySelector('[data-testid="interconnection"]'), { childList: true, subtree: true });
        }, scene.occurrences.find(node => !node.parentId).id);
        stage = 'large import';
        results.importAndRenderedViewportMs = await restore(scene);
        results.initial = { counts: await counts(), accepted: await read(scene.occurrences[0].id), server: await resources(), browser: await metrics(), load: await page.evaluate(() => window.viewportLoad) };
        await page.screenshot({ path: join(directory, 'initial-viewport.png') });
        await checkpoint();
        const connected = scene.occurrences.find(node => node.geometry.x === 64 && node.geometry.y === 66);
        for (const latency of rtts) {
            rtt = 0; await restore(scene);
            rtt = latency; stage = `drag +${rtt} ms RTT`;
            const ping = [];
            for (let i = 0; i < 5; i++) { const start = performance.now(); await page.evaluate(() => window.viewportProbe.ping(window.viewportProbeRoot)); ping.push(performance.now() - start); }
            results.regions.push({ stage, effectivePingMs: ping, server: await resources(), browser: await metrics() });
            for (let i = -warmups; i < samples; i++) {
                const element = root.locator(`[data-node-id="${connected.id}"]`), box = await element.boundingBox(), accepted = await read(connected.id);
                assert.ok(box && accepted.geometry);
                const dx = i % 2 ? -45 : 45, dy = 12;
                await element.evaluate(element => {
                    window.dragTrace = { release: null, nodes: [], edges: [] };
                    window.nodeObserver = new MutationObserver(() => window.dragTrace.nodes.push(performance.now()));
                    window.nodeObserver.observe(element, { attributes: true, attributeFilter: ['transform'] });
                    window.edgeObserver = new MutationObserver(records => { if (records.some(record => record.target.closest('.connection-svg'))) window.dragTrace.edges.push(performance.now()); });
                    window.edgeObserver.observe(element.closest('.interconnection__canvas'), { attributes: true, subtree: true, attributeFilter: ['d', 'transform'] });
                    document.addEventListener('pointerup', event => { window.dragTrace.release = event.timeStamp; }, { once: true, capture: true });
                });
                const packetStart = packets.length;
                await gesture({ x: box.x + 80, y: box.y + 25 }, dx, dy);
                const afterRelease = performance.now(), releaseEpoch = Date.now();
                let final;
                for (let attempt = 0; attempt < 300; attempt++) {
                    final = await read(connected.id);
                    if (Math.abs(final.geometry.x - accepted.geometry.x - dx / accepted.viewport.zoom) < 0.01 && Math.abs(final.geometry.y - accepted.geometry.y - dy / accepted.viewport.zoom) < 0.01) break;
                    if (attempt === 299) {
                        results.failedGesture = { before: accepted, after: final, dx, dy, dom: await element.getAttribute('transform') };
                        throw new Error('Authoritative drag position did not converge');
                    }
                    await delay(50);
                }
                const acceptedMs = performance.now() - afterRelease;
                results.lastGesture = { rtt, sample: i, expected: final.geometry, acceptedMs };
                await checkpoint();
                await page.waitForFunction(({ id, geometry }) => {
                    const node = document.querySelector(`[data-node-id="${id}"]`);
                    const matrix = node?.transform.baseVal.consolidate()?.matrix;
                    return matrix && Math.abs(matrix.e - geometry.x) < 0.01 && Math.abs(matrix.f - geometry.y) < 0.01;
                }, { id: connected.id, geometry: final.geometry }, { timeout: 45000 });
                await page.waitForFunction(id => {
                    const trace = window.dragTrace, last = Math.max(trace.release ?? 0, ...trace.nodes, ...trace.edges);
                    return document.querySelector(`[data-node-id="${id}"]`) && performance.now() - last > 350;
                }, connected.id, { timeout: 45000 });
                await frames();
                const trace = await page.evaluate(() => { window.nodeObserver.disconnect(); window.edgeObserver.disconnect(); return window.dragTrace; });
                const transform = await element.evaluate(node => ({ x: node.transform.baseVal.consolidate().matrix.e, y: node.transform.baseVal.consolidate().matrix.f }));
                results.lastGesture.rendered = transform;
                results.lastGesture.acceptedAfterQuiet = await read(connected.id);
                assert.deepEqual(results.lastGesture.acceptedAfterQuiet.geometry, final.geometry);
                assert.ok(Math.abs(transform.x - final.geometry.x) < 0.01 && Math.abs(transform.y - final.geometry.y) < 0.01);
                const measurement = { rtt, sample: i, sceneSettlingMs: Math.max(0, Math.max(...trace.nodes, ...trace.edges) - trace.release), acceptedReadbackMs: acceptedMs, finalCorrect: true, traffic: summarizeTraffic(packets.slice(packetStart), 0, releaseEpoch, Date.now()), counts: await counts() };
                if (i >= 0) results.drags.push(measurement);
                else (results.excludedWarmups ??= []).push(measurement);
                await checkpoint();
            }
        }
        results.afterDrags = { server: await resources(), browser: await metrics() };
        if (loading) {
            rtt = 0; stage = 'bounded model navigation'; results.modelNavigation = [];
            for (const x of [0, -5000, 0, -10000, -15000, -20000, 0, -5000, 0, -20000, 0]) {
                const before = await dataStats(), began = performance.now();
                await restore({ ...scene, viewport: { x, y: 0, zoom: 1 } });
                await detailsReady();
                results.modelNavigation.push({ x, restoreAndDetailsMs: performance.now() - began, before, after: await dataStats(), counts: await counts(), server: await resources(), browser: await metrics() });
                await checkpoint();
            }
            await page.screenshot({ path: join(directory, 'loaded-region.png') });

            await restore(scene);
        await page.evaluate(() => { window.loadingPointers = []; for (const type of ['pointerdown', 'pointerup', 'pointercancel', 'lostpointercapture', 'gotpointercapture']) window.addEventListener(type, e => window.loadingPointers.push({ type, target: e.target.className?.baseVal ?? e.target.className, x: e.clientX, y: e.clientY, time: performance.now() }), true); });
        stage = 'fast pointer pan';
        for (let i = 0; i < 3; i++) {
            const point = await root.locator('.diagram-canvas').evaluate(canvas => {
                const box = canvas.getBoundingClientRect();
                for (let y = box.y + 20; y < box.bottom - 20; y += 30) for (let x = box.x + 20; x < box.right - 20; x += 30) {
                    const hit = document.elementFromPoint(x, y);
                    if (hit?.closest('.interconnection__canvas') && !hit.closest('[data-node-id], .diagram-group, .diagram-link')) return { x, y };
                }
                throw new Error('No blank canvas point');
            });
            const before = await read(connected.id), began = performance.now();
            await gesture(point, -250, 0);
            await page.waitForFunction(async before => {
                const next = await window.viewportProbe.read(window.viewportProbeRoot, '00000000-0000-0000-0000-000000000000');
                return Math.abs(next.viewport.x - before.x + 250) < 0.1 && Math.abs(next.viewport.y - before.y) < 0.1;
            }, before.viewport, { timeout: 45000 });
            const accepted = await read(connected.id);
            await page.waitForFunction(viewport => {
                const matrix = new DOMMatrix(document.querySelector('.diagram-svg-layer').style.transform);
                return Math.abs(matrix.e - viewport.x) < 0.1 && Math.abs(matrix.f - viewport.y) < 0.1;
            }, accepted.viewport);
            const bounds = await root.locator('.diagram-canvas').boundingBox();
            const expected = scene.occurrences.find(node => !node.parentId && node.geometry.x + accepted.viewport.x >= 0
                && node.geometry.x + accepted.viewport.x < bounds.width && node.geometry.y + accepted.viewport.y >= 0
                && node.geometry.y + accepted.viewport.y < bounds.height);
            if (expected) await root.locator(loading ? `.part-svg[data-element-id="${expected.elementId}"]` : `[data-node-id="${expected.id}"], .diagram-group[data-group-id="${expected.id}"]`).waitFor();
            await frames();
            results.regions.push({ method: 'pointer pan', sample: i, gestureAndReadbackMs: performance.now() - began, counts: await counts() });
        }

            await restore(scene);
            stage = 'loading selection and read-only';
            await page.getByRole('button', { name: 'Add tab to Editor group 1', exact: true }).click();
            await page.getByRole('menuitem', { name: 'Demonstration Project Browser', exact: true }).click();
            const browserNames = page.locator('[data-testid="workspace-project-browser"] .mb-project-browser-node__title');
            await browserNames.filter({ hasText: /^controller$/ }).click();
            await browserNames.filter({ hasText: /^aocs$/ }).click();
            await page.locator('.mb-details-panel').getByText(/aocs/).first().waitFor();
            await root.locator('[data-node-id="' + connected.id + '"]').waitFor();
            await restore({ ...scene, viewport: { x: -20000, y: 0, zoom: 1 } });
            await root.getByRole('button', { name: 'Reveal selection', exact: true }).click();
            await page.waitForFunction(id => document.activeElement?.getAttribute('data-node-id') === id, connected.id);
            await detailsReady();
            results.browserReveal = { focus: await page.evaluate(() => document.activeElement?.getAttribute('data-node-id')), accepted: await read(connected.id) };
            assert.equal(results.browserReveal.focus, connected.id);
            await page.screenshot({ path: join(directory, 'loading-reveal.png') });
            await restore(scene);
            await root.getByRole('checkbox', { name: 'Read-only', exact: true }).check();
            await page.waitForFunction(() => [...document.querySelectorAll('[data-testid="interconnection"] button')].find(button => button.textContent.trim() === 'Manual bend')?.disabled);
            const locked = await read(connected.id), lockedBox = await root.locator('[data-node-id="' + connected.id + '"]').boundingBox();
            await gesture({ x: lockedBox.x + 80, y: lockedBox.y + 25 }, 30, 10);
            assert.deepEqual((await read(connected.id)).geometry, locked.geometry);
            results.readOnly = true;
            await root.getByRole('checkbox', { name: 'Read-only', exact: true }).uncheck();
            await page.setViewportSize({ width: 1440, height: 900 });
            await detailsReady(); await restore(scene);
            results.resized = { counts: await counts(), data: await dataStats() };
            await page.screenshot({ path: join(directory, 'loading-resize.png') });
            const beforeExport = { data: await dataStats(), server: await resources(), browser: await metrics() };
            if (size <= 560) {
                stage = 'detached complete export'; const began = performance.now();
                const file = await download('Export SVG', 'complete.svg');
                const independent = await browser.newPage();
                await independent.goto(pathToFileURL(resolve(file)).href);
                results.svg = { nodes: await independent.locator('.part-svg').count(), ports: await independent.locator('.part-svg__port').count(), edges: await independent.locator('.connection-svg__line').count(), errors: await independent.locator('parsererror').count(), elapsedMs: performance.now() - began };
                assert.equal(results.svg.nodes, size); assert.equal(results.svg.ports, size / 7 * 10); assert.equal(results.svg.edges, size / 7 * 5); assert.equal(results.svg.errors, 0);
                await independent.close();
                results.exportResources = { before: beforeExport, after: { data: await dataStats(), server: await resources(), browser: await metrics() } };
            }
            const tab = page.getByRole('tab', { name: /Stress Test.*Z.Blazor.Diagrams/ });
            const tabId = await tab.getAttribute('data-tab-id');
            await page.locator(`[data-testid="editor-workspace-tab-close"][data-tab-id="${tabId}"]`).click();
            await open(); await detailsReady();
            results.reopened = { counts: await counts(), data: await dataStats(), server: await resources(), browser: await metrics() };
            results.completed = true; return results;
        }
        if (interactionsOnly) { results.completed = true; return results; }
        rtt = 0; stage = 'navigation and resize';
        for (const tile of [400, 1100, 600, 0, 1100, 0]) {
            const node = scene.occurrences[tile * 7], viewport = { x: 40 - node.geometry.x, y: 80 - node.geometry.y, zoom: 1 };
            const elapsed = await restore({ ...scene, viewport });
            results.regions.push({ tile, method: 'validated presentation viewport restoration', elapsedMs: elapsed, counts: await counts(), server: await resources(), browser: await metrics() });
            await page.screenshot({ path: join(directory, `region-${tile}.png`) });
        }
        stage = 'fast pointer pan';
        for (let i = 0; i < 8; i++) {
            const point = await root.locator('.diagram-canvas').evaluate(canvas => {
                const box = canvas.getBoundingClientRect();
                for (let y = box.y + 20; y < box.bottom - 20; y += 30) for (let x = box.x + 20; x < box.right - 20; x += 30) {
                    const hit = document.elementFromPoint(x, y);
                    if (hit?.closest('.interconnection__canvas') && !hit.closest('[data-node-id], .diagram-group, .diagram-link')) return { x, y };
                }
                throw new Error('No blank canvas point');
            });
            const before = await read(connected.id), began = performance.now();
            await gesture(point, -400, -200);
            await page.waitForFunction(async before => {
                const next = await window.viewportProbe.read(window.viewportProbeRoot, '00000000-0000-0000-0000-000000000000');
                return Math.abs(next.viewport.x - before.x + 400) < 0.1 && Math.abs(next.viewport.y - before.y + 200) < 0.1;
            }, before.viewport, { timeout: 45000 });
            const accepted = await read(connected.id);
            await page.waitForFunction(viewport => {
                const matrix = new DOMMatrix(document.querySelector('.diagram-svg-layer').style.transform);
                return Math.abs(matrix.e - viewport.x) < 0.1 && Math.abs(matrix.f - viewport.y) < 0.1;
            }, accepted.viewport);
            const bounds = await root.locator('.diagram-canvas').boundingBox();
            const expected = scene.occurrences.find(node => !node.parentId && node.geometry.x + accepted.viewport.x >= 0
                && node.geometry.x + accepted.viewport.x < bounds.width && node.geometry.y + accepted.viewport.y >= 0
                && node.geometry.y + accepted.viewport.y < bounds.height);
            if (expected) await root.locator(loading ? `.part-svg[data-element-id="${expected.elementId}"]` : `[data-node-id="${expected.id}"], .diagram-group[data-group-id="${expected.id}"]`).waitFor();
            await frames();
            results.regions.push({ method: 'pointer pan', sample: i, gestureAndReadbackMs: performance.now() - began, counts: await counts() });
        }
        const distant = scene.occurrences[7700];
        await restore({ ...scene, viewport: { x: 40 - distant.geometry.x, y: 80 - distant.geometry.y, zoom: 1 } });
        await page.getByRole('button', { name: 'Add tab to Editor group 1', exact: true }).click();
        await page.getByRole('menuitem', { name: 'Demonstration Project Browser', exact: true }).click();
        await page.locator('[data-testid="workspace-project-browser"] .mb-project-browser-node__title').filter({ hasText: /^controller$/ }).click();
        await page.locator('[data-testid="workspace-project-browser"] .mb-project-browser-node__title').filter({ hasText: /^aocs$/ }).click();
        await root.locator(`[data-node-id="${connected.id}"]`).waitFor();
        await page.locator('.mb-details-panel').getByText(/aocs/).first().waitFor();
        results.browserReveal = { accepted: await read(connected.id), focus: await page.evaluate(() => document.activeElement?.getAttribute('data-node-id')), counts: await counts() };
        assert.equal(results.browserReveal.focus, connected.id);
        await page.screenshot({ path: join(directory, 'browser-reveal.png') });
        await restore(scene);
        const parent = scene.occurrences.find(node => scene.occurrences.some(child => child.parentId === node.id));
        const group = root.locator('[data-group-id]').filter({ has: page.locator('.part-svg__name').filter({ hasText: /^power/ }) }).first();
        const groupBox = await group.boundingBox();
        assert.ok(groupBox);
        await gesture({ x: groupBox.x + 60, y: groupBox.y + 25 }, 20, 10);
        for (let attempt = 0; attempt < 100; attempt++) {
            if (Math.abs((await read(parent.id)).geometry.x - parent.geometry.x - 20) < 0.01) break;
            await delay(50);
        }
        results.nestedMove = await read(parent.id);
        assert.ok(Math.abs(results.nestedMove.geometry.x - parent.geometry.x - 20) < 0.01);
        await restore(scene);
        stage = 'read-only and fit';
        await root.getByRole('checkbox', { name: 'Read-only', exact: true }).check();
        await page.waitForFunction(() => [...document.querySelectorAll('[data-testid="interconnection"] button')].find(button => button.textContent.trim() === 'Manual bend')?.disabled);
        assert.equal(await root.getByRole('button', { name: 'Manual bend', exact: true }).isDisabled(), true);
        const locked = await read(connected.id), lockedBox = await root.locator(`[data-node-id="${connected.id}"]`).boundingBox();
        await gesture({ x: lockedBox.x + 80, y: lockedBox.y + 25 }, 30, 10);
        assert.deepEqual((await read(connected.id)).geometry, locked.geometry);
        results.readOnly = true;
        await root.getByRole('checkbox', { name: 'Read-only', exact: true }).uncheck();
        await page.waitForFunction(() => ![...document.querySelectorAll('[data-testid="interconnection"] button')].find(button => button.textContent.trim() === 'Manual bend')?.disabled);
        await page.setViewportSize({ width: 1440, height: 900 }); await delay(300); await frames();
        results.resized = await counts();
        const canvasBox = await root.locator('.diagram-canvas').boundingBox();
        await page.mouse.click(canvasBox.x + 15, canvasBox.y + 15);
        await root.getByRole('button', { name: 'Fit to view', exact: true }).click(); await delay(300);
        results.fit = { accepted: await read(connected.id), counts: await counts(), status: await root.locator('[role=status]').innerText() };
        assert.equal(results.fit.accepted.viewport.zoom, 0.5);
        assert.match(results.fit.status, /does not fit/);
        assert.equal(await root.getByLabel('Current zoom', { exact: true }).innerText(), '50%');
        assert.equal(await root.getByRole('button', { name: 'Zoom out', exact: true }).isDisabled(), true);
        await page.screenshot({ path: join(directory, 'minimum-zoom.png') });
        stage = 'dense viewport';
        const controller = scene.occurrences.find(node => node.geometry.x === 334 && node.geometry.y === 366);
        const dense = { ...scene, occurrences: scene.occurrences.filter(node => node.elementId === controller.elementId).slice(0, 700).map(node => ({ ...node, geometry: { ...node.geometry, x: 64, y: 66 } })), routes: [], viewport: { x: 0, y: 0, zoom: 0.5 } };
        await restore(dense);
        await page.waitForFunction(() => document.querySelectorAll('[data-testid="interconnection"] .part-svg').length === 700);
        results.dense = { counts: await counts(), status: await root.locator('[role=status]').innerText() };
        assert.match(results.dense.status, /Dense viewport: 700/);
        await page.screenshot({ path: join(directory, 'dense-viewport.png') });
        await restore(scene);
        const json = await download('Export presentation', 'complete.presentation.json');
        const exported = JSON.parse(await readFile(json));
        assert.equal(exported.occurrences.length, size); assert.equal(exported.routes.length, scene.routes.length);
        stage = 'complete SVG export';
        const beforeExport = await counts(), svgFile = await download('Export SVG', 'complete.svg');
        const independent = await browser.newPage();
        await independent.goto(pathToFileURL(resolve(svgFile)).href, { timeout: loading ? 30000 : 180000 });
        results.svg = { nodes: await independent.locator('.part-svg').count(), ports: await independent.locator('.part-svg__port').count(), edges: await independent.locator('.connection-svg__line').count(), errors: await independent.locator('parsererror').count() };
        assert.deepEqual(results.svg, { nodes: 10000, ports: 14287, edges: 7142, errors: 0 });
        await independent.close();
        assert.deepEqual(await counts(), beforeExport);
        results.after = { server: await resources(), browser: await metrics(), load: await page.evaluate(() => window.viewportLoad) };
        stage = 'close and reopen';
        const tab = page.getByRole('tab', { name: /Stress Test.*Z.Blazor.Diagrams/ });
        const tabId = await tab.getAttribute('data-tab-id');
        assert.ok(tabId);
        await page.locator(`[data-testid="editor-workspace-tab-close"][data-tab-id="${tabId}"]`).click();
        await open();
        results.reopened = { accepted: await read(null), counts: await counts(), server: await resources(), browser: await metrics() };
        assert.equal(results.reopened.accepted.nodes, 126);
        results.completed = true;
    } catch (error) { results.failure = String(error); results.pointerEvents = await page.evaluate(() => window.loadingPointers ?? []); results.failureState = await read(null).catch(() => null); results.failureData = await root.locator('[data-testid="diagram-data-stats"]').evaluate(node => ({...node.dataset})).catch(() => null); results.failureTransform = await root.locator('.diagram-svg-layer').evaluate(node => node.style.transform).catch(() => null); await page.screenshot({ path: join(directory, 'failure.png'), timeout: 5000 }).catch(() => {}); }
    finally { clearTimeout(ceiling); clearInterval(monitor); await checkpoint(); await context.close().catch(() => {}); await browser.close().catch(() => {}); }
    return results;
}
