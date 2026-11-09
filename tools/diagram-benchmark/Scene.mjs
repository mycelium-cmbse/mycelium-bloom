// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import { createHash } from 'node:crypto';

const identity = text => {
    const value = createHash('sha256').update(text).digest('hex').slice(0, 32);
    return `${value.slice(0, 8)}-${value.slice(8, 12)}-${value.slice(12, 16)}-${value.slice(16, 20)}-${value.slice(20)}`;
};

export function createScene(seed, count, largeBenchmark = false) {
    const template = seed.occurrences.slice(0, 7), routes = seed.routes.slice(0, 5);
    if (template.length !== 7 || !Number.isInteger(count) || count < 7 || count > (largeBenchmark ? 10000 : 500)
        || (!largeBenchmark && count % 7)) throw new Error('Scene exceeds the selected benchmark capacity.');
    const occurrences = [], connections = [];
    for (let tile = 0; tile < count / 7; tile++) {
        const remaining = count - occurrences.length;
        const parts = remaining >= 7 ? template : [...template].sort((a, b) => !!a.parentId - !!b.parentId).slice(0, remaining);
        const ids = new Map(parts.map(node => [node.id, identity(`occurrence:${tile}:${node.elementId}`)]));
        const columns = largeBenchmark ? Math.ceil(Math.sqrt(Math.ceil(count / 7))) : 4;
        const dx = tile % columns * 1000, dy = Math.floor(tile / columns) * 850;
        occurrences.push(...parts.map(node => ({ ...node, id: ids.get(node.id), parentId: node.parentId ? ids.get(node.parentId) : null,
            geometry: { ...node.geometry, x: node.geometry.x + dx, y: node.geometry.y + dy } })));
        connections.push(...routes.filter(route => ids.has(route.sourceId) && ids.has(route.targetId)).map(route => ({ ...route, id: identity(`route:${tile}:${route.elementId}`), sourceId: ids.get(route.sourceId),
            targetId: ids.get(route.targetId), bends: route.bends.map(point => ({ x: point.x + dx, y: point.y + dy })) })));
    }
    return { ...seed, diagramId: identity('benchmark-presentation'), occurrences, routes: connections, viewport: { x: 0, y: 0, zoom: 1 } };
}
