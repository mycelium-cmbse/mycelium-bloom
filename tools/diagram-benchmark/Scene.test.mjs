// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createScene } from './Scene.mjs';

const seed = { version: 1, modelScope: 'canonical', diagramId: 'source', viewport: { x: 10, y: 20, zoom: 2 },
    occurrences: Array.from({ length: 7 }, (_, index) => ({ id: `occurrence-${index}`, elementId: `element-${index}`, parentId: index === 1 ? 'occurrence-0' : null,
        geometry: { x: index * 20, y: index * 30, width: 200, height: 100 } })),
    routes: Array.from({ length: 5 }, (_, index) => ({ id: `route-${index}`, elementId: `relationship-${index}`, sourceId: 'occurrence-0', targetId: `occurrence-${index + 1}`,
        sourcePortId: 'canonical-port-a', targetPortId: 'canonical-port-b', bends: [{ x: 5, y: 10 }] })) };

test('benchmark tiles preserve canonical identities and translate occurrence geometry deterministically', () => {
    const original = structuredClone(seed), scene = createScene(seed, 126);
    assert.deepEqual(seed, original);
    assert.deepEqual(scene, createScene(seed, 126));
    assert.equal(scene.occurrences.length, 126);
    assert.equal(scene.routes.length, 90);
    assert.equal(new Set(scene.occurrences.map(node => node.id)).size, 126);
    assert.equal(new Set(scene.routes.map(route => route.id)).size, 90);
    assert.equal(new Set(scene.occurrences.map(node => node.elementId)).size, 7);
    assert.equal(new Set(scene.routes.map(route => route.elementId)).size, 5);
    assert.equal(scene.occurrences[8].parentId, scene.occurrences[7].id);
    assert.equal(scene.routes[5].sourceId, scene.occurrences[7].id);
    assert.deepEqual(scene.routes[5].bends, [{ x: 1005, y: 10 }]);
    assert.equal(scene.modelScope, seed.modelScope);
    assert.deepEqual(scene.viewport, { x: 0, y: 0, zoom: 1 });
});

test('larger scenes remain within the existing presentation contract', () => {
    assert.equal(createScene(seed, 350).occurrences.length, 350);
    for (const count of [0, 8, 501, 700, Number.NaN, undefined]) assert.throws(() => createScene(seed, count));
});

test('explicit large benchmark contains exactly ten thousand occurrences without orphan containment or connections', () => {
    assert.throws(() => createScene(seed, 10000));
    const scene = createScene(seed, 10000, true);
    const ids = new Set(scene.occurrences.map(node => node.id));
    assert.equal(ids.size, 10000);
    assert.equal(new Set(scene.occurrences.map(node => node.elementId)).size, 7);
    assert.ok(scene.occurrences.every(node => !node.parentId || ids.has(node.parentId)));
    assert.ok(scene.routes.every(route => ids.has(route.sourceId) && ids.has(route.targetId)));
    assert.deepEqual(scene, createScene(seed, 10000, true));
    assert.throws(() => createScene(seed, 10001, true));
});
