// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { registerHooks } from 'node:module';
import { JSDOM } from 'jsdom';
import { installSvgDom } from './SvgDom.js';
import { CompletedChanges, portPosition, boundaryPositions, intersects, mount, update, unmount, inspect, action, standaloneSvg, download, settle } from '../../Components/UI/Organisms/InterconnectionCanvas/InterconnectionCanvas.razor.js';
import * as probe from '../../Components/UI/Organisms/DiagramBenchmarkProbe/DiagramBenchmarkProbe.razor.js';

test('diagnostic probes forward bounded reads only during their own mount lifetime', async () => {
    const root = {}, other = {}, calls = [];
    const reference = { invokeMethodAsync: async (...values) => { calls.push(values); return 'snapshot'; } };
    assert.equal(await probe.read(root, 'occurrence'), null);
    assert.equal(await probe.ping(root), null);
    assert.equal(await probe.resources(root), null);
    assert.equal(await probe.startTrace(root), null);
    assert.equal(await probe.stopTrace(root), null);
    assert.equal(await probe.watch(root, 'occurrence', 1, 2), null);
    probe.mount(root, reference);
    assert.equal(await probe.read(root, 'occurrence'), 'snapshot');
    assert.equal(await probe.ping(root), 'snapshot');
    assert.equal(await probe.resources(root), 'snapshot');
    assert.equal(await probe.watch(root, 'occurrence', 1, 2), 'snapshot');
    assert.equal(await probe.startTrace(root), 'snapshot');
    assert.equal(await probe.stopTrace(root), 'snapshot');
    assert.deepEqual(calls, [['ReadBenchmarkSnapshot', 'occurrence'], ['BenchmarkPing'], ['ReadBenchmarkResources'], ['WatchBenchmarkGeometry', 'occurrence', 1, 2], ['StartBenchmarkTrace'], ['StopBenchmarkTrace']]);
    assert.equal(await probe.ping(other), null);
    probe.unmount(root);
    probe.unmount(root);
    assert.equal(await probe.read(root, 'occurrence'), null);
    assert.equal(await probe.startTrace(root), null);
    assert.equal(await probe.stopTrace(root), null);
});

const snapshot = (revision = 1, sequence = 0) => ({ sessionId: 'mount', revision, sequence, state: {}, selection: [], canEdit: true });
const deferred = () => { let resolve; const promise = new Promise(done => resolve = done); return { promise, resolve }; };

test('completed commands coalesce while awaiting acknowledgment and use the accepted revision', async () => {
    const pending = deferred(), calls = [], rendered = [];
    const queue = new CompletedChanges(snapshot(), (_, command) => {
        calls.push(command);
        return calls.length === 1 ? pending.promise : Promise.resolve({ accepted: true, snapshot: snapshot(3, 2) });
    }, state => rendered.push(state), () => {});
    const first = queue.complete([{ id: 'a', geometry: { x: 1 } }], [], null);
    await queue.complete([{ id: 'a', geometry: { x: 2 } }], [], null);
    await queue.complete([{ id: 'a', geometry: { x: 3 } }], [{ id: 'r', bends: [] }], { x: 0, y: 0, zoom: 1 });
    assert.equal(calls.length, 1);
    queue.update(snapshot(2, 1));
    assert.equal(rendered.length, 0);
    pending.resolve({ accepted: true, snapshot: snapshot(2, 1) });
    await first;
    assert.equal(calls.length, 2);
    assert.equal(calls[1].nodes[0].geometry.x, 3);
    assert.equal(calls[1].revision, 2);
    assert.equal(calls[1].sequence, 2);
    assert.equal(queue.metrics.accepted, 2);
    queue.dispose();
});

test('external snapshots clear optimistic proposals and stale snapshots cannot roll them back', async () => {
    const pending = deferred(), rendered = [];
    const queue = new CompletedChanges(snapshot(), () => pending.promise, (state, force) => rendered.push({ state, force }), () => {});
    const operation = queue.complete([], [], { x: 1, y: 2, zoom: 1 });
    await queue.complete([{ id: 'a' }], [], null);
    queue.update(snapshot(4, 0));
    queue.update(snapshot(2, 0));
    queue.update({ ...snapshot(5, 0), sessionId: 'other' });
    assert.equal(queue.snapshot.revision, 4);
    assert.equal(queue.nodes.size, 0);
    assert.equal(rendered.at(-1).force, true);
    pending.resolve({ accepted: false, snapshot: snapshot(3, 1) });
    await operation;
    assert.equal(queue.snapshot.revision, 4);
    assert.equal(queue.metrics.rejected, 1);
    queue.dispose();
});

test('rejected changes restore authority and discard queued layout', async () => {
    const states = [], notices = [];
    const queue = new CompletedChanges(snapshot(), async () => ({ accepted: false, snapshot: snapshot(1, 1) }),
        (state, force) => states.push({ state, force }), message => notices.push(message));
    await queue.complete([{ id: 'bad' }], [], null);
    assert.equal(states.at(-1).force, true);
    assert.match(notices.at(-1), /rejected/);
    assert.equal(queue.nodes.size, 0);
    queue.dispose();
});

test('permission changes immediately cancel optimistic visuals while a command is pending', async () => {
    const pending = deferred(), rendered = [];
    const queue = new CompletedChanges(snapshot(), () => pending.promise, (state, force) => rendered.push({ state, force }), () => {});
    const operation = queue.complete([{ id: 'node' }], [], null);
    queue.update({ ...snapshot(2, 1), canEdit: false });
    assert.equal(rendered.at(-1).force, true);
    assert.equal(rendered.at(-1).state.canEdit, false);
    pending.resolve({ accepted: true, snapshot: snapshot(1, 1) });
    await operation;
    assert.equal(queue.snapshot.canEdit, false);
    queue.dispose();
});

test('disconnect rolls back visuals, blocks mutations and recovers current authoritative state', async () => {
    const states = [], notices = [];
    const queue = new CompletedChanges(snapshot(), async name => {
        if (name === 'PresentationChanged') throw Error('disconnected');
        return snapshot(3, 1);
    }, (state, force) => states.push({ state, force }), notice => notices.push(notice));
    await queue.complete([], [], { x: 1, y: 1, zoom: 1 });
    assert.equal(queue.blocked, true);
    await queue.complete([{ id: 'ignored' }], [], null);
    assert.equal(queue.nodes.size, 0);
    assert.equal(states.at(-1).force, true);
    await queue.recover();
    assert.equal(queue.blocked, false);
    assert.equal(queue.snapshot.revision, 3);
    assert.match(notices.at(-1), /synchronized/);
    queue.dispose();
});

test('acknowledgment timeout blocks further layout instead of silently diverging', async () => {
    const queue = new CompletedChanges(snapshot(), () => new Promise(() => {}), () => {}, () => {}, 5);
    await queue.complete([], [], { x: 2, y: 3, zoom: 1 });
    assert.equal(queue.blocked, true);
    queue.dispose();
});

test('late callbacks after disposal do not publish browser state', async () => {
    const pending = deferred(), rendered = [];
    const queue = new CompletedChanges(snapshot(), () => pending.promise, state => rendered.push(state), () => {});
    const operation = queue.complete([], [], { x: 1, y: 1, zoom: 1 });
    queue.dispose();
    queue.dispose();
    queue.update(snapshot(8, 1));
    await queue.recover();
    pending.resolve({ accepted: true, snapshot: snapshot(2, 1) });
    await operation;
    assert.deepEqual(rendered, []);
    assert.equal(queue.snapshot.revision, 1);
});

test('recovery failures and invalid mount responses cannot authorize local changes', async () => {
    const queue = new CompletedChanges(snapshot(), async name => {
        if (name === 'GetSnapshot') throw Error('still disconnected');
        return { accepted: true, snapshot: { ...snapshot(2, 1), sessionId: 'old' } };
    }, () => {}, () => {});
    await queue.complete([], [], { x: 0, y: 0, zoom: 2 });
    await queue.recover();
    assert.equal(queue.blocked, true);
    queue.dispose();
});

test('stalled recovery times out so reconnect can retry and disposal cancels an outstanding recovery', async () => {
    let connected = false;
    const queue = new CompletedChanges(snapshot(), async name => {
        if (name === 'PresentationChanged') throw Error('disconnected');
        return connected ? snapshot(2, 1) : new Promise(() => {});
    }, () => {}, () => {}, 5);
    await queue.complete([], [], { x: 1, y: 1, zoom: 1 });
    await queue.recover();
    assert.equal(queue.recovering, false);
    connected = true;
    await queue.recover();
    assert.equal(queue.blocked, false);
    queue.blocked = true;
    connected = false;
    const pending = queue.recover();
    queue.dispose();
    await pending;
    assert.equal(queue.recovering, false);
});

test('port placement follows occurrence endpoints across repeated semantic elements', () => {
    const first = { id: 'one', geometry: { x: 0, y: 0, width: 200, height: 100 } };
    const second = { id: 'two', geometry: { x: 400, y: 0, width: 200, height: 100 } };
    const port = { elementId: 'port' };
    const state = { occurrences: [first, second], routes: [{ sourceId: 'one', sourcePortId: 'port', targetId: 'two', targetPortId: 'port' }] };
    assert.deepEqual(portPosition(first, port, state), { x: 200, y: 50 });
    assert.deepEqual(portPosition(second, port, state), { x: 0, y: 50 });
    second.geometry.y = 500;
    assert.deepEqual(portPosition(first, port, state), { x: 100, y: 100 });
    assert.deepEqual(portPosition(second, port, state), { x: 100, y: 0 });
    assert.deepEqual(portPosition(first, { elementId: 'unconnected' }, state), { x: 200, y: 50 });
});

test('route invalidation includes old and new obstacle clearance and excludes distant edges', () => {
    const route = { x: 100, y: 100, width: 300, height: 1 };
    assert.equal(intersects(route, { x: 200, y: 108, width: 100, height: 100 }), true);
    assert.equal(intersects(route, { x: 200, y: 110, width: 100, height: 100 }), false);
    assert.equal(intersects(route, { x: 900, y: 100, width: 100, height: 100 }), false);
});

test('unmounted adapter operations are harmless', async () => {
    const root = {};
    unmount(root);
    update(root, snapshot());
    action(root, 'fit');
    assert.equal(inspect(root), null);
    assert.equal(await settle(root), false);
});

test('multiple unconnected ports retain separate boundary positions', () => {
    const occurrence = { id: 'one', geometry: { x: 0, y: 0, width: 200, height: 120 } };
    const part = { ports: [{ elementId: 'one' }, { elementId: 'two' }] };
    assert.deepEqual(boundaryPositions(occurrence, part, { occurrences: [occurrence], routes: [] }),
        [{ id: 'one', x: 200, y: 40 }, { id: 'two', x: 200, y: 80 }]);
});

test('native canvas owns rendering resources, reconciles accepted state and exports independently', async context => {
    const dom = new JSDOM('<html><body><div id="canvas"></div></body></html>', { pretendToBeVisual: true, url: 'https://bloom.test/' });
    const previous = new Map();
    const bind = (key, value) => {
        previous.set(key, Object.getOwnPropertyDescriptor(globalThis, key));
        Object.defineProperty(globalThis, key, { value, configurable: true, writable: true });
    };
    for (const key of Object.getOwnPropertyNames(dom.window)) {
        if (!(key in globalThis)) {
            try { bind(key, dom.window[key]); } catch {}
        }
    }
    for (const key of ['window', 'document', 'navigator', 'getComputedStyle', 'requestAnimationFrame', 'cancelAnimationFrame']) {
        if (!previous.has(key)) bind(key, typeof dom.window[key] === 'function' ? dom.window[key].bind(dom.window) : dom.window[key]);
    }
    let disconnected = 0;
    bind('ResizeObserver', class { observe() {} disconnect() { disconnected++; } });
    installSvgDom(dom.window);
    document.elementFromPoint = () => document.querySelector('.x6-node');
    const hooks = registerHooks({ resolve(specifier, caller, next) {
        return specifier === '../../../../vendor/x6/x6.js'
            ? { url: new URL('../../wwwroot/vendor/x6/x6.js', import.meta.url).href, shortCircuit: true }
            : next(specifier, caller);
    } });
    const root = document.getElementById('canvas');
    Object.defineProperties(root, { clientWidth: { value: 800 }, clientHeight: { value: 700 } });
    context.after(() => {
        unmount(root);
        hooks.deregister();
        dom.window.close();
        for (const [key, descriptor] of previous) descriptor ? Object.defineProperty(globalThis, key, descriptor) : delete globalThis[key];
    });
    let authority = { ...snapshot(), state: { occurrences: [
        { id: 'a', elementId: 'part-a', geometry: { x: 20, y: 20, width: 240, height: 300 }, parentId: null },
        { id: 'b', elementId: 'part-b', geometry: { x: 40, y: 170, width: 180, height: 100 }, parentId: 'a' },
        { id: 'c', elementId: 'part-b', geometry: { x: 400, y: 200, width: 180, height: 100 }, parentId: null }
    ], routes: [{ id: 'route', elementId: 'connection', sourceId: 'a', sourcePortId: 'port-a', targetId: 'c', targetPortId: 'port-b', bends: [{ x: 300, y: 160 }] }],
    viewport: { x: 0, y: 0, zoom: 1 } }, projection: { parts: {
        'part-a': { name: 'Container', attributes: ['mass : 1 kg'], ports: [{ elementId: 'port-a' }] },
        'part-b': { name: 'Part', attributes: [], ports: [{ elementId: 'port-b' }] }
    }, connections: { connection: { name: 'Connection', sourceMultiplicity: '[1]', targetMultiplicity: '[1]' } } } };
    const calls = [];
    const reference = { async invokeMethodAsync(name, command, selection) {
        calls.push({ name, command });
        if (name === 'PresentationChanged') {
            authority = { ...authority, revision: authority.revision + 1, sequence: command.sequence,
                state: { ...authority.state, viewport: command.viewport ?? authority.state.viewport } };
            return { accepted: true, snapshot: authority };
        }
        if (name === 'SelectionChanged') authority = { ...authority, selection };
        return authority;
    } };
    await mount(root, reference, authority);
    assert.equal(root.querySelectorAll('.x6-node').length, 3);
    assert.equal(root.querySelectorAll('.x6-port').length, 3);
    assert.equal(inspect(root).geometry.length, 3);
    authority = { ...authority, revision: 2, selection: ['route'], state: { ...authority.state,
        occurrences: authority.state.occurrences.map(node => node.id === 'c' ? { ...node, geometry: { ...node.geometry, x: 460 } } : node) } };
    update(root, authority);
    assert.ok(root.querySelector('.x6-edge-tool-vertices'));
    assert.equal(inspect(root).geometry.find(node => node.id === 'c').geometry.x, 460);
    await action(root, 'zoom', 0.5);
    await action(root, 'fit');
    assert.ok(calls.some(call => call.name === 'PresentationChanged'));
    const selected = root.querySelector('.x6-node');
    selected.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, clientX: 50, clientY: 40, button: 0 }));
    document.dispatchEvent(new MouseEvent('mouseup', { bubbles: true, clientX: 50, clientY: 40, button: 0 }));
    await new Promise(resolve => setTimeout(resolve, 0));
    assert.ok(calls.some(call => call.name === 'SelectionChanged'));
    const text = standaloneSvg(root);
    assert.match(text, /Container/);
    assert.match(text, /mass\s:\s1\skg/);
    assert.match(text, /Connection/);
    assert.match(text, /\[1\]/);
    assert.doesNotMatch(text, /foreignObject|x6-cell-tools/);
    const originalCreate = URL.createObjectURL, originalRevoke = URL.revokeObjectURL;
    let blob, revoked;
    URL.createObjectURL = value => { blob = value; return 'blob:diagram'; };
    URL.revokeObjectURL = value => { revoked = value; };
    dom.window.HTMLAnchorElement.prototype.click = () => {};
    try {
        await action(root, 'export');
        assert.match(await blob.text(), /Container/);
        download('presentation.json', 'application/json', '{}');
        assert.equal(await blob.text(), '{}');
        await new Promise(resolve => setTimeout(resolve, 1100));
        assert.equal(revoked, 'blob:diagram');
    } finally {
        URL.createObjectURL = originalCreate;
        URL.revokeObjectURL = originalRevoke;
    }
    authority = { ...authority, revision: 5, canEdit: false, selection: [], state: { ...authority.state, routes: [] } };
    update(root, authority);
    assert.equal(root.querySelectorAll('.x6-edge').length, 0);
    assert.equal(root.querySelectorAll('.x6-cell-tools').length, 0);
    unmount(root);
    assert.equal(disconnected, 1);
    assert.equal(root.children.length, 0);
    assert.equal(inspect(root), null);
    const pending = mount(root, reference, authority);
    unmount(root);
    await pending;
    assert.equal(root.children.length, 0);
});
