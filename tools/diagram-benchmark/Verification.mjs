// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import assert from 'node:assert/strict';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { chromium } from 'playwright';

const canvasModule = '/Components/UI/Organisms/InterconnectionCanvas/InterconnectionCanvas.razor.js';
const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

export async function verifyCase({ url, key, engine, size, directory, scenesDirectory }) {
    await mkdir(directory, { recursive: true });
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 1 });
    await context.addCookies([{ name: 'Bloom.DiagramBenchmark', value: key, url, httpOnly: true, sameSite: 'Strict' }]);
    const page = await context.newPage(), checks = [], errors = [], held = [];
    await page.addInitScript(() => performance.setResourceTimingBufferSize(10000));
    let interrupted = false, holdCommands = false, suppressReplies = false;
    const sockets = [];
    await page.routeWebSocket(/_blazor/, socket => {
        const server = socket.connectToServer();
        sockets.push({ socket, server });
        socket.onMessage(message => {
            if (interrupted) return;
            if (holdCommands && Buffer.from(message).toString('utf8').includes('PresentationChanged')) held.push({ server, message });
            else server.send(message);
        });
        server.onMessage(message => { if (!interrupted && !suppressReplies) socket.send(message); });
    });
    page.on('pageerror', error => errors.push(error.message));
    const root = page.locator(`[data-testid="${engine === 'x6' ? 'interconnection-x6' : 'interconnection'}"]`);
    const canvas = root.locator('.interconnection__canvas');
    const title = `${size === 7 ? 'Interconnection' : 'Stress Test'} — ${engine === 'x6' ? 'AntV X6' : 'Z.Blazor.Diagrams'}`;
    const open = async (group, name) => {
        await page.getByRole('button', { name: `Add tab to Editor group ${group}`, exact: true }).click();
        await page.getByRole('menuitem', { name, exact: true }).click();
    };
    const exportState = async (element = root, name = 'verification.presentation.json') => {
        const pending = page.waitForEvent('download');
        await element.getByRole('button', { name: 'Export presentation', exact: true }).click();
        const file = join(directory, name);
        await (await pending).saveAs(file);
        return JSON.parse(await readFile(file, 'utf8'));
    };
    const inspect = () => canvas.evaluate(async (element, path) => (await import(path)).inspect(element), canvasModule);
    const waitFor = async predicate => {
        for (let attempt = 0; attempt < 200; attempt++) {
            const value = await inspect();
            if (predicate(value)) return value;
            await delay(50);
        }
        throw new Error('Expected canvas state was not reached.');
    };
    const restore = async state => {
        await root.locator('input[type=file]').setInputFiles({ name: 'restore.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(state)) });
        for (let attempt = 0; attempt < 30; attempt++) {
            const actual = await exportState();
            if (JSON.stringify(actual.occurrences) === JSON.stringify(state.occurrences) && JSON.stringify(actual.routes) === JSON.stringify(state.routes)
                && JSON.stringify(actual.viewport) === JSON.stringify(state.viewport)) return;
            await delay(50);
        }
        throw new Error('Presentation restore did not reach authoritative state.');
    };
    const drag = async (element, dx, dy) => {
        const box = await element.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await page.mouse.down();
        await page.mouse.move(box.x + box.width / 2 + dx, box.y + box.height / 2 + dy, { steps: 20 });
        await page.mouse.up();
    };
    const node = id => root.locator(engine === 'x6' ? `.x6-node[data-cell-id="${id}"] path` : `[data-node-id="${id}"] .part-svg__header`).first();
    const result = { engine, size, checks, errors, completed: false };
    try {
        await page.goto(`${url}/workspace/modeling`);
        await page.waitForFunction(() => document.documentElement.style.getPropertyValue('--radius') !== '');
        await open(3, title);
        await root.locator(engine === 'x6' ? '.x6-node' : '.part-svg').first().waitFor();
        const scene = JSON.parse(await readFile(join(scenesDirectory, `${size}.presentation.json`), 'utf8'));
        await restore(scene);
        const initial = await exportState(), connected = initial.occurrences.find(node => node.geometry.x === 64 && node.geometry.y === 66);
        await root.getByRole('button', { name: 'Manual bend', exact: true }).click();
        const handle = root.locator(engine === 'x6' ? '.x6-edge-tool-vertices circle' : '.diagram-link-vertex').first();
        await handle.waitFor();
        const beforeBend = await exportState();
        await drag(handle, 20, 10);
        const afterBend = await exportState();
        assert.notDeepEqual(afterBend.routes, beforeBend.routes);
        assert.deepEqual(afterBend.occurrences, beforeBend.occurrences);
        checks.push('Manual waypoint movement changes accepted routes without changing semantic occurrences.');
        await restore(initial);
        const box = await canvas.boundingBox();
        await page.mouse.move(box.x + 20, box.y + 20);
        await page.mouse.down();
        await page.mouse.move(box.x + 60, box.y + 45, { steps: 20 });
        await page.mouse.up();
        const panned = await exportState();
        assert.notDeepEqual(panned.viewport, initial.viewport);
        assert.deepEqual(panned.occurrences, initial.occurrences);
        checks.push('Blank-canvas pan changes viewport only.');
        await restore(initial);
        await node(connected.id).click();
        await page.locator('.mb-details-panel').getByText(/aocs/).first().waitFor();
        checks.push('Canvas selection resolves to canonical Details content.');
        if (engine === 'x6') {
            const route = initial.routes.find(route => route.bends.length), obstacle = initial.occurrences.find(node => node.geometry.x === 334 && node.geometry.y === 366);
            const automatic = { ...initial, routes: initial.routes.map(route => ({ ...route, bends: [] })) };
            const crossings = async geometry => canvas.evaluate(async (element, { path, id, geometry }) => {
                const route = (await import(path)).inspect(element).routes.find(route => route.id === id);
                const shape = document.createElementNS('http://www.w3.org/2000/svg', 'path');
                shape.setAttribute('d', route.path);
                let intersections = 0;
                for (let distance = 0; distance < shape.getTotalLength(); distance++) {
                    const point = shape.getPointAtLength(distance);
                    if (point.x > geometry.x + 1 && point.x < geometry.x + geometry.width - 1
                        && point.y > geometry.y + 1 && point.y < geometry.y + geometry.height - 1) intersections++;
                }
                return { intersections, path: route.path };
            }, { path: canvasModule, id: route.id, geometry });
            await restore(automatic);
            result.routing = { before: await crossings(obstacle.geometry) };
            assert.equal(result.routing.before.intersections, 0);
            await drag(node(obstacle.id), 0, 150);
            await waitFor(value => !value.pending && value.snapshot.state.occurrences.find(node => node.id === obstacle.id).geometry.y === obstacle.geometry.y + 150);
            result.routing.away = await crossings(obstacle.geometry);
            result.routing.awayInvalidation = (await inspect()).metrics.routes.at(-1);
            await drag(node(obstacle.id), 0, -150);
            await waitFor(value => !value.pending && value.snapshot.state.occurrences.find(node => node.id === obstacle.id).geometry.y === obstacle.geometry.y);
            result.routing.back = await crossings(obstacle.geometry);
            result.routing.backInvalidation = (await inspect()).metrics.routes.at(-1);
            assert.equal(result.routing.back.intersections, 0);
            assert.notEqual(result.routing.away.path, result.routing.before.path);
            assert.ok(result.routing.backInvalidation.affected < initial.routes.length);
            await drag(node(obstacle.id), 0, 150);
            const parent = initial.occurrences.find(node => node.geometry.height === 270);
            await drag(node(parent.id), -270, 200);
            const nested = await waitFor(value => !value.pending && value.snapshot.state.occurrences.find(node => node.id === parent.id).geometry.x === parent.geometry.x - 270);
            result.routing.nested = await crossings(nested.snapshot.state.occurrences.find(node => node.id === parent.id).geometry);
            assert.equal(result.routing.nested.intersections, 0);
            await restore(automatic);
            await drag(node(obstacle.id), 220, 0);
            const fallback = await waitFor(value => !value.pending && value.snapshot.state.occurrences.find(node => node.id === obstacle.id).geometry.x === obstacle.geometry.x + 220);
            result.routing.fallbacks = fallback.fallbacks;
            result.routing.fallback = await crossings(fallback.snapshot.state.occurrences.find(node => node.id === obstacle.id).geometry);
            assert.ok(result.routing.fallbacks.length > 0);
            assert.ok(result.routing.fallback.intersections > 0);
            await restore(initial);
            await node(connected.id).click();
            const second = initial.occurrences.find(node => node.geometry.x === 64 && node.geometry.y === 366);
            await node(second.id).click({ modifiers: ['Control'] });
            await drag(node(connected.id), 15, 10);
            const multi = await waitFor(value => !value.pending && value.snapshot.state.occurrences.find(node => node.id === second.id).geometry.x === second.geometry.x + 15);
            assert.deepEqual(multi.snapshot.state.routes.find(item => item.id === route.id).bends, route.bends);
            await restore(initial);
            checks.push('Selective Manhattan invalidation, unrelated/nested obstacles, manual route preservation and two-node movement.');
            checks.push('Overlapping terminal obstacle exposes a tracked orthogonal fallback intersection.');
            const before = await inspect();
            holdCommands = true;
            await drag(node(connected.id), 30, 20);
            for (let attempt = 0; attempt < 50 && !held.length; attempt++) await delay(20);
            assert.ok(held.length, 'A real completed command must be held at the transport boundary.');
            await root.getByRole('checkbox', { name: 'Read-only', exact: true }).check();
            await root.getByRole('button', { name: 'Manual bend', exact: true }).waitFor();
            await waitFor(value => !value.snapshot.canEdit);
            holdCommands = false;
            for (const value of held.splice(0)) value.server.send(value.message);
            const rejected = await waitFor(value => !value.pending && value.metrics.rejected > before.metrics.rejected);
            assert.deepEqual(rejected.snapshot.state.occurrences, initial.occurrences);
            await root.getByRole('checkbox', { name: 'Read-only', exact: true }).uncheck();
            await waitFor(value => value.snapshot.canEdit);
            checks.push('A completed command delayed across a C# permission change is rejected and restores authoritative geometry.');
            if (size === 7) {
                holdCommands = true;
                await drag(node(connected.id), 30, 20);
                await waitFor(value => value.pending);
                await drag(node(connected.id), 20, 10);
                let exported = false;
                const pendingExport = exportState().then(value => { exported = true; return value; });
                await delay(200);
                assert.equal(exported, false, 'Export must wait for accepted queued commands.');
                holdCommands = false;
                for (const value of held.splice(0)) value.server.send(value.message);
                const acceptedExport = await pendingExport;
                assert.equal(acceptedExport.occurrences.find(node => node.id === connected.id).geometry.x, connected.geometry.x + 50);
                await waitFor(value => !value.pending);
                await restore(initial);
                checks.push('Queued completed movements preserve final coordinates and export waits for accepted acknowledgments.');
                interrupted = true;
                await drag(node(connected.id), 30, 20);
                const timedOut = await waitFor(value => value.blocked);
                assert.equal(timedOut.geometry.find(node => node.id === connected.id).geometry.x, connected.geometry.x);
                interrupted = false;
                const recovered = await waitFor(value => !value.blocked);
                assert.deepEqual(recovered.snapshot.state.occurrences, initial.occurrences);
                checks.push('Lost transport command rolls back after acknowledgment timeout and recovers authoritative state.');
                await drag(node(connected.id), 20, 10);
                await waitFor(value => !value.pending && value.snapshot.state.occurrences.find(node => node.id === connected.id).geometry.x === connected.geometry.x + 20);
                await restore(initial);
                suppressReplies = true;
                await drag(node(connected.id), 30, 20);
                await waitFor(value => value.blocked);
                suppressReplies = false;
                const acknowledged = await waitFor(value => !value.blocked);
                assert.equal(acknowledged.snapshot.state.occurrences.find(node => node.id === connected.id).geometry.x, connected.geometry.x + 30);
                assert.equal(acknowledged.geometry.find(node => node.id === connected.id).geometry.x, connected.geometry.x + 30);
                await restore(initial);
                checks.push('A server-accepted movement with lost replies reconciles to accepted C# geometry after timeout.');
            }
        }
        if (size === 7) {
            const socketCount = sockets.length;
            const connection = sockets.at(-1);
            await connection.server.close({ code: 1012, reason: 'Diagnostic reconnect' });
            await connection.socket.close({ code: 1012, reason: 'Diagnostic reconnect' });
            await page.waitForFunction(() => !document.querySelector('#components-reconnect-modal.components-reconnect-show'), null, { timeout: 30000 });
            for (let attempt = 0; attempt < 100 && sockets.length === socketCount; attempt++) await delay(100);
            assert.ok(sockets.length > socketCount, 'A new WebSocket must be established.');
            const afterReconnect = await exportState();
            assert.deepEqual(afterReconnect.occurrences, initial.occurrences);
            checks.push('Actual WebSocket closure reconnects the retained circuit with authoritative presentation intact.');
        }
        await open(1, 'Demonstration Project Browser');
        const browserRoot = page.locator('[data-testid="workspace-project-browser"]');
        await browserRoot.locator('.mb-project-browser-node__title').filter({ hasText: /^aocs$/ }).click();
        await root.getByRole('button', { name: 'Add browser selection', exact: true }).click();
        const added = await exportState();
        assert.equal(added.occurrences.length, initial.occurrences.length + 1);
        assert.equal(added.occurrences.filter(node => node.elementId === connected.elementId).length, size / 7 + 1);
        assert.equal(await browserRoot.locator('.mb-project-browser-node__title').filter({ hasText: /^aocs$/ }).count(), 1);
        await restore(initial);
        checks.push('Browser add creates an occurrence of the existing canonical part, with one browser element.');
        const saved = await exportState();
        const destination = page.getByRole('region', { name: 'Editor group 2', exact: true });
        const sourceTab = page.getByRole('tab', { name: title, exact: true });
        const sourceBox = await sourceTab.boundingBox(), targetBox = await destination.getByRole('tab').first().boundingBox();
        await page.mouse.move(sourceBox.x + sourceBox.width / 2, sourceBox.y + sourceBox.height / 2);
        await page.mouse.down();
        await page.mouse.move(sourceBox.x + sourceBox.width / 2 + 10, sourceBox.y + sourceBox.height / 2, { steps: 5 });
        await page.locator('.mb-editor-workspace__tab-item--dragging').waitFor();
        await page.mouse.move(targetBox.x + targetBox.width / 4, targetBox.y + targetBox.height / 2, { steps: 20 });
        await page.locator('.mb-editor-workspace__tab-drop-marker--active').waitFor();
        await page.mouse.up();
        await destination.getByRole('tab', { name: title, exact: true }).waitFor();
        assert.deepEqual(await exportState(), saved);
        checks.push('Native tab drag between groups remounts with the same tab-owned state.');
        const otherEngine = engine === 'x6' ? 'z' : 'x6';
        const otherTitle = `Interconnection — ${otherEngine === 'x6' ? 'AntV X6' : 'Z.Blazor.Diagrams'}`;
        await open(3, otherTitle);
        const otherRoot = page.locator(`[data-testid="${otherEngine === 'x6' ? 'interconnection-x6' : 'interconnection'}"]`);
        const otherState = await exportState(otherRoot, 'other.presentation.json');
        assert.equal(otherState.occurrences.length, 7);
        assert.notEqual(otherState.diagramId, saved.diagramId);
        assert.deepEqual(await exportState(), saved);
        await page.screenshot({ path: join(directory, 'simultaneous-light.png') });
        checks.push('Both engines coexist in separate groups with independent diagram identities and state.');
        await page.getByRole('button', { name: `Close ${title}`, exact: true }).click();
        await root.waitFor({ state: 'detached' });
        await open(2, title);
        const reopened = await exportState();
        assert.notEqual(reopened.diagramId, saved.diagramId);
        assert.equal(reopened.occurrences.length, size === 7 ? 7 : 126);
        checks.push('Closing disposes the tab; reopening starts an independent factory presentation.');
        assert.deepEqual(errors.filter(error => !error.includes('JsOnClickOutside')), []);
        result.assets = await page.evaluate(() => performance.getEntriesByType('resource')
            .filter(entry => /\/vendor\/x6\/|Z\.Blazor\.Diagrams.*script|blazor\.web\.js/.test(entry.name))
            .map(entry => ({ path: new URL(entry.name).pathname, duration: entry.duration, transferSize: entry.transferSize,
                encodedBodySize: entry.encodedBodySize, decodedBodySize: entry.decodedBodySize })));
        result.completed = true;
        await writeFile(join(directory, 'results.json'), JSON.stringify(result, null, 2));
        return result;
    } catch (error) {
        result.failure = String(error);
        await writeFile(join(directory, 'results.json'), JSON.stringify(result, null, 2));
        await writeFile(join(directory, 'failure.html'), await page.content());
        await page.screenshot({ path: join(directory, 'failure.png') }).catch(() => {});
        throw error;
    } finally {
        interrupted = false;
        await context.close();
        await browser.close();
    }
}
