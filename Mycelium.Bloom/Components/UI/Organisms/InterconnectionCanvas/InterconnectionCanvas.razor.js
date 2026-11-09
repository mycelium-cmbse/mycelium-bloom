// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

const mounts = new WeakMap();
const equal = (a, b) => JSON.stringify(a) === JSON.stringify(b);
const clamp = (value, min, max) => Math.max(min, Math.min(max, value));

/**
 * Resolves a port’s boundary position from occurrence geometry and connections.
 * @param {object} occurrence Graphical occurrence.
 * @param {object} port Canonical port presentation.
 * @param {object} state Accepted presentation snapshot.
 * @returns {{x:number,y:number}} Position relative to the occurrence.
 */
export function portPosition(occurrence, port, state) {
    const route = state.routes.find(item => item.sourceId === occurrence.id && item.sourcePortId === port.elementId
        || item.targetId === occurrence.id && item.targetPortId === port.elementId);
    const geometry = occurrence.geometry;
    if (!route) return { x: geometry.width, y: geometry.height / 2 };
    const other = state.occurrences.find(item => item.id === (route.sourceId === occurrence.id ? route.targetId : route.sourceId)).geometry;
    const dx = other.x + other.width / 2 - geometry.x - geometry.width / 2;
    const dy = other.y + other.height / 2 - geometry.y - geometry.height / 2;
    return Math.abs(dx) >= Math.abs(dy)
        ? { x: dx >= 0 ? geometry.width : 0, y: geometry.height / 2 }
        : { x: geometry.width / 2, y: dy >= 0 ? geometry.height : 0 };
}

/**
 * Tests padded axis-aligned rectangle intersection.
 * @param {object} a First rectangle.
 * @param {object} b Second rectangle.
 * @param {number} clearance Additional route clearance.
 * @returns {boolean} Whether the rectangles intersect within the clearance.
 */
export function intersects(a, b, clearance = 8) {
    return a.x <= b.x + b.width + clearance && a.x + a.width >= b.x - clearance
        && a.y <= b.y + b.height + clearance && a.y + a.height >= b.y - clearance;
}

/**
 * Calculates stable boundary positions for an occurrence’s ports.
 * @param {object} occurrence Graphical occurrence.
 * @param {object} part Canonical part presentation.
 * @param {object} state Accepted presentation snapshot.
 * @returns {object[]} Port positions and side orientations.
 */
export function boundaryPositions(occurrence, part, state) {
    const positions = part.ports.map(port => ({ id: port.elementId, ...portPosition(occurrence, port, state) }));
    const geometry = occurrence.geometry;
    for (const position of positions) {
        const vertical = position.x === 0 || position.x === geometry.width;
        const peers = positions.filter(other => vertical ? other.x === position.x : other.y === position.y);
        const offset = (peers.indexOf(position) + 1) / (peers.length + 1);
        if (vertical) position.y = geometry.height * offset;
        else position.x = geometry.width * offset;
    }
    return positions;
}

/** Owns completed presentation commands and acknowledgment recovery until canvas disposal. */
export class CompletedChanges {
    /**
     * @param {object} snapshot Initial authoritative C# state.
     * @param {Function} invoke Async callback to the owning C# view.
     * @param {Function} apply Applies accepted state to the graph.
     * @param {Function} status Reports synchronization status.
     * @param {number} timeout Acknowledgment timeout in milliseconds.
     */
    constructor(snapshot, invoke, apply, status, timeout = 5000) {
        this.snapshot = snapshot;
        this.invoke = invoke;
        this.apply = apply;
        this.status = status;
        this.timeout = timeout;
        this.nodes = new Map();
        this.routes = new Map();
        this.metrics = { sent: 0, accepted: 0, rejected: 0, acknowledgments: [] };
    }

    /**
     * Applies newer accepted state while preserving pending local changes where valid.
     * @param {object} snapshot Authoritative C# snapshot.
     * @returns {void}
     */
    update(snapshot) {
        if (this.disposed || !snapshot || snapshot.sessionId !== this.snapshot.sessionId || snapshot.revision < this.snapshot.revision) return;
        const own = this.pending && snapshot.sequence === this.pending.sequence;
        const external = snapshot.canEdit !== this.snapshot.canEdit || snapshot.revision !== this.snapshot.revision && !own;
        this.snapshot = snapshot;
        if (external) {
            this.nodes.clear();
            this.routes.clear();
            this.viewport = null;
            this.apply(snapshot, true);
        } else if (!this.pending && !this.nodes.size && !this.routes.size && !this.viewport) {
            this.apply(snapshot, false);
        }
    }

    /**
     * Batches completed gestures into the next owned acknowledgment operation.
     * @param {object[]} nodes Completed occurrence geometries.
     * @param {object[]} routes Completed bend changes.
     * @param {object|null} viewport Completed viewport change.
     * @returns {Promise<void>|undefined} Active flush completion when a batch starts.
     */
    complete(nodes, routes, viewport) {
        if (this.disposed || this.blocked) return;
        for (const change of nodes) this.nodes.set(change.id, change);
        for (const change of routes) this.routes.set(change.id, change);
        if (viewport) this.viewport = viewport;
        if (this.pending) return;
        this.completion = this.flush();
        return this.completion;
    }

    /**
     * Sends one batch and restores accepted state after rejection or transport failure.
     * @returns {Promise<void>} Completion including any subsequently queued batch.
     */
    async flush() {
        if (this.disposed || this.pending || this.blocked || (!this.nodes.size && !this.routes.size && !this.viewport)) return;
        const command = { sessionId: this.snapshot.sessionId, revision: this.snapshot.revision,
            sequence: this.snapshot.sequence + 1, nodes: [...this.nodes.values()], routes: [...this.routes.values()], viewport: this.viewport ?? null };
        this.nodes.clear();
        this.routes.clear();
        this.viewport = null;
        this.pending = command;
        this.metrics.sent++;
        const start = performance.now();
        let timer;
        try {
            const result = await Promise.race([
                this.invoke('PresentationChanged', command),
                new Promise((_, reject) => {
                    this.cancel = () => reject(new Error('mount ended'));
                    timer = setTimeout(() => reject(new Error('acknowledgment timeout')), this.timeout);
                })
            ]);
            if (this.disposed) return;
            if (!result?.snapshot || result.snapshot.sessionId !== this.snapshot.sessionId) throw new Error('mount ended');
            this.metrics.acknowledgments.push(performance.now() - start);
            if (this.metrics.acknowledgments.length > 256) this.metrics.acknowledgments.shift();
            if (result.snapshot.revision >= this.snapshot.revision) this.snapshot = result.snapshot;
            if (!result.accepted) {
                this.metrics.rejected++;
                this.nodes.clear();
                this.routes.clear();
                this.viewport = null;
                this.apply(this.snapshot, true);
                this.status('Presentation change rejected; authoritative state restored.');
            } else {
                this.metrics.accepted++;
                if (!this.nodes.size && !this.routes.size && !this.viewport) this.apply(this.snapshot, false);
                this.status('Presentation accepted.');
            }
        } catch {
            if (!this.disposed) {
                this.blocked = true;
                this.nodes.clear();
                this.routes.clear();
                this.viewport = null;
                this.apply(this.snapshot, true);
                this.status('Connection interrupted; layout awaiting synchronization.');
            }
        } finally {
            clearTimeout(timer);
            this.cancel = null;
            this.pending = null;
        }
        if (!this.disposed && !this.blocked) return this.flush();
    }

    /**
     * Retrieves accepted state after interruption without replaying rejected changes.
     * @returns {Promise<void>} Completion of the bounded recovery attempt.
     */
    async recover() {
        if (this.disposed || !this.blocked || this.recovering) return;
        this.recovering = true;
        let timer;
        try {
            const snapshot = await Promise.race([
                this.invoke('GetSnapshot'),
                new Promise((_, reject) => {
                    this.cancelRecovery = () => reject(new Error('mount ended'));
                    timer = setTimeout(() => reject(new Error('recovery timeout')), this.timeout);
                })
            ]);
            if (!this.disposed && snapshot?.sessionId === this.snapshot.sessionId) {
                this.snapshot = snapshot;
                this.apply(snapshot, true);
                this.blocked = false;
                this.status('Authoritative presentation synchronized.');
            }
        } catch {
        } finally {
            clearTimeout(timer);
            this.cancelRecovery = null;
            this.recovering = false;
        }
    }

    /** Cancels pending acknowledgment/recovery and prevents later application. @returns {void} */
    dispose() {
        this.disposed = true;
        this.cancel?.();
        this.cancelRecovery?.();
        this.nodes.clear();
        this.routes.clear();
    }
}

/**
 * Resolves the mounted theme’s canonical tokens.
 * @param {HTMLElement} root Mounted canvas host.
 * @returns {object} Theme colors.
 */
function palette(root) {
    const css = getComputedStyle(root);
    const token = name => css.getPropertyValue(name).trim();
    return { body: token('--card'), border: token('--sysml-structure-border'), header: token('--sysml-structure-header'),
        foreground: token('--foreground-muted'), primary: token('--primary'), white: token('--primary-foreground'),
        font: `${token('--typography-typography-label-sm-font-family') || 'sans-serif'}, sans-serif`,
        mono: `${token('--typography-typography-mono-code-11-5-font-family') || 'monospace'}, monospace` };
}

/**
 * Builds native SVG node attributes without changing semantic state.
 * @param {object} part Part presentation.
 * @param {object} geometry Occurrence bounds.
 * @param {object} colors Resolved theme colors.
 * @param {boolean} selected Local selection state.
 * @returns {object} SVG attribute mapping.
 */
function nodeAttributes(part, geometry, colors, selected) {
    const width = geometry.width;
    const attributes = {
        body: { width, height: geometry.height, rx: 6, fill: colors.body, stroke: selected ? colors.primary : colors.border, strokeWidth: selected ? 2 : 1 },
        header: { d: `M6 0 H${width - 6} Q${width} 0 ${width} 6 V40 H0 V6 Q0 0 6 0`, fill: colors.header },
        kind: { x: 25, y: 14, text: '«part»', fill: colors.white, fontFamily: colors.font, fontSize: 10, textAnchor: 'start' },
        name: { x: 8, y: 32, text: part.name, fill: colors.white, fontFamily: colors.font, fontSize: 12, fontWeight: 500, textAnchor: 'start' },
        icon: { d: 'M8 7h5v5H8z M15 7h5v5h-5z M11.5 3h5v5h-5z', fill: 'none', stroke: colors.white, strokeWidth: 1 }
    };
    part.attributes.forEach((text, index) => attributes[`attribute${index}`] = {
        x: 8, y: 60 + index * 16, text, fill: colors.foreground, fontFamily: colors.mono, fontSize: 11, textAnchor: 'start'
    });
    return attributes;
}

/**
 * Builds SVG markup for the part header and compartments.
 * @param {object} part Part presentation.
 * @returns {object[]} Native SVG markup descriptors.
 */
function markup(part) {
    return [{ tagName: 'rect', selector: 'body' }, { tagName: 'path', selector: 'header' },
        { tagName: 'path', selector: 'icon' }, { tagName: 'text', selector: 'kind' }, { tagName: 'text', selector: 'name' },
        ...part.attributes.map((_, index) => ({ tagName: 'text', selector: `attribute${index}` }))];
}

/**
 * Creates connector labels from supported relationship notation.
 * @param {object} connection Canonical connection presentation.
 * @param {object} colors Theme colors.
 * @returns {object[]} Label descriptors.
 */
function routeLabels(connection, colors) {
    return [connection.name, connection.sourceMultiplicity, connection.targetMultiplicity].map((text, index) => ({
        position: { distance: [0.5, 0.07, 0.93][index], offset: index ? -10 : 0 },
        attrs: { label: { text, fill: colors.foreground, fontSize: index ? 9 : 10, fontFamily: colors.font },
            body: { fill: index ? 'none' : colors.body, stroke: index ? 'none' : colors.border, rx: 4, ry: 4 } }
    }));
}

/**
 * Updates local graph selection without publishing semantic changes.
 * @param {object} mount Owning canvas resources.
 * @returns {void}
 */
function renderSelection(mount) {
    const selection = new Set(mount.snapshot.selection);
    const colors = palette(mount.root);
    for (const node of mount.graph.getNodes()) {
        node.attr('body/stroke', selection.has(node.id) ? colors.primary : colors.border);
        node.attr('body/strokeWidth', selection.has(node.id) ? 2 : 1);
    }
    for (const edge of mount.graph.getEdges()) {
        edge.attr('line/stroke', selection.has(edge.id) ? colors.primary : colors.foreground);
        edge.attr('line/strokeWidth', selection.has(edge.id) ? 2 : 1);
        const editable = selection.has(edge.id) && mount.snapshot.canEdit && !mount.commands.blocked;
        if (!editable) edge.removeTools();
        else if (!edge.hasTool('vertices')) edge.addTools([{ name: 'vertices', args: {
            addable: false, removable: true, stopPropagation: false, attrs: { fill: colors.body, stroke: colors.primary }
        } }]);
    }
}

/**
 * Reads a node’s current browser geometry.
 * @param {object} node X6 node.
 * @returns {object} Position and size.
 */
function currentGeometry(node) {
    return { ...node.position(), ...node.size() };
}

/**
 * Reads the graph’s actual transform.
 * @param {object} graph Mounted X6 graph.
 * @returns {object} Zoom and translation.
 */
function currentViewport(graph) {
    const pan = graph.translate();
    return { x: pan.tx, y: pan.ty, zoom: graph.scale().sx };
}

/**
 * Applies accepted geometry and presentation while retaining owned graph resources.
 * @param {object} mount Owning canvas resources.
 * @param {object} snapshot Accepted state.
 * @param {boolean} force Whether pending local geometry must be replaced.
 * @returns {void}
 */
function render(mount, snapshot, force = false) {
    if (mount.disposed) return;
    mount.snapshot = snapshot;
    if (mount.dragging && !force) return;
    if (force) mount.dragging = false;
    mount.applying = true;
    const graph = mount.graph;
    const state = snapshot.state;
    const colors = palette(mount.root);
    const selected = new Set(snapshot.selection);
    try {
        const ids = new Set([...state.occurrences, ...state.routes].map(item => item.id));
        for (const cell of graph.getCells()) if (!ids.has(cell.id)) {
            cell.remove({ deep: false });
            mount.fallbacks.delete(cell.id);
        }
        for (const occurrence of [...state.occurrences].sort((a, b) => !!a.parentId - !!b.parentId)) {
            const part = snapshot.projection.parts[occurrence.elementId];
            let node = graph.getCellById(occurrence.id);
            if (!node) {
                node = graph.addNode({ shape: 'bloom-part', id: occurrence.id, ...occurrence.geometry,
                    markup: markup(part), data: { parentId: occurrence.parentId }, zIndex: occurrence.parentId ? 2 : 1,
                    ports: { groups: { boundary: { position: 'absolute', markup: [{ tagName: 'rect', selector: 'portBody' }],
                        attrs: { portBody: { width: 8, height: 8, x: -4, y: -4, magnet: false } } } },
                    items: boundaryPositions(occurrence, part, state).map(({ id, x, y }) => ({ id, group: 'boundary', args: { x, y } })) } });
            }
            const geometry = occurrence.geometry;
            if (!equal(node.position(), { x: geometry.x, y: geometry.y })) node.position(geometry.x, geometry.y, { deep: false });
            if (!equal(node.size(), { width: geometry.width, height: geometry.height })) node.resize(geometry.width, geometry.height);
            node.attr(nodeAttributes(part, geometry, colors, selected.has(node.id)));
            for (const { id, x, y } of boundaryPositions(occurrence, part, state)) {
                if (!equal(node.getPortProp(id, 'args'), { x, y })) node.setPortProp(id, 'args', { x, y });
                node.setPortProp(id, 'attrs/portBody', { fill: colors.body, stroke: colors.primary, strokeWidth: 1 });
            }
            if (occurrence.parentId && node.getParentId() !== occurrence.parentId) graph.getCellById(occurrence.parentId).addChild(node);
        }
        for (const route of state.routes) {
            const connection = snapshot.projection.connections[route.elementId];
            let edge = graph.getCellById(route.id);
            if (!edge) {
                edge = graph.addEdge({ id: route.id, source: { cell: route.sourceId, port: route.sourcePortId },
                    target: { cell: route.targetId, port: route.targetPortId }, vertices: route.bends,
                    connector: 'normal', router: mount.simpleRouting ? { name: 'orth' } : { name: 'manhattan', args: { step: 10, padding: 8, maxLoopCount: 2000,
                        fallbackRouter(vertices, options, view) {
                            mount.fallbacks.add(route.id);
                            return mount.orth.call(this, vertices, options, view);
                        } } }, zIndex: 3 });
            }
            if (!equal(edge.getVertices(), route.bends)) edge.setVertices(route.bends);
            edge.attr({ line: { fill: 'none', stroke: selected.has(edge.id) ? colors.primary : colors.foreground,
                strokeWidth: selected.has(edge.id) ? 2 : 1, targetMarker: { name: 'classic', size: 7 } } });
            edge.setLabels(routeLabels(connection, colors));
        }
        const viewport = state.viewport;
        if (graph.scale().sx !== viewport.zoom) graph.scale(viewport.zoom, viewport.zoom);
        const pan = graph.translate();
        if (pan.tx !== viewport.x || pan.ty !== viewport.y) graph.translate(viewport.x, viewport.y);
    } finally {
        mount.applying = false;
    }
    renderSelection(mount);
    mount.root.dataset.ready = 'true';
    mount.root.dataset.nodes = state.occurrences.length;
    mount.root.dataset.edges = state.routes.length;
}

/**
 * Invalidates connected or intersecting routes for completed geometry changes.
 * @param {object} mount Owning canvas resources.
 * @param {object[]} changed Changed node geometries.
 * @param {object} previous Previous geometry used to identify intersecting routes.
 * @returns {void}
 */
function reroute(mount, changed, previous) {
    if (!changed.length) return;
    const start = performance.now();
    const ids = new Set(changed.map(item => item.id));
    const obstacles = changed.flatMap(item => [item.geometry, previous.occurrences.find(node => node.id === item.id)?.geometry].filter(Boolean));
    let count = 0;
    for (const edge of mount.graph.getEdges()) {
        const view = mount.graph.findViewByCell(edge);
        const box = view?.getBBox();
        if (ids.has(edge.getSourceCellId()) || ids.has(edge.getTargetCellId())
            || box && obstacles.some(obstacle => intersects(box, obstacle))) {
            mount.fallbacks.delete(edge.id);
            view?.update();
            count++;
        }
    }
    mount.metrics.routes.push({ milliseconds: performance.now() - start, affected: count, fallbacks: mount.fallbacks.size });
    if (mount.metrics.routes.length > 256) mount.metrics.routes.shift();
    mount.root.dataset.routingFallbacks = mount.fallbacks.size;
}

/**
 * Submits completed local geometry through the owning command coordinator.
 * @param {object} mount Owning canvas resources.
 * @returns {Promise<void>|undefined} Acknowledgment completion if changes are queued.
 */
function finish(mount) {
    if (mount.disposed || mount.applying) return;
    mount.dragging = false;
    const state = mount.baseline;
    const nodes = mount.graph.getNodes().map(node => ({ id: node.id, geometry: currentGeometry(node) }))
        .filter(node => !equal(node.geometry, state.occurrences.find(item => item.id === node.id)?.geometry));
    const routes = mount.graph.getEdges().map(edge => ({ id: edge.id, bends: edge.getVertices() }))
        .filter(route => !equal(route.bends, state.routes.find(item => item.id === route.id)?.bends));
    const viewport = currentViewport(mount.graph);
    mount.baseline = { ...state,
        occurrences: state.occurrences.map(node => nodes.some(item => item.id === node.id) ? { ...node, geometry: nodes.find(item => item.id === node.id).geometry } : node),
        routes: state.routes.map(route => routes.some(item => item.id === route.id) ? { ...route, bends: routes.find(item => item.id === route.id).bends } : route), viewport };
    if (nodes.length) {
        for (const occurrence of mount.baseline.occurrences) {
            const node = mount.graph.getCellById(occurrence.id);
            for (const { id, x, y } of boundaryPositions(occurrence, mount.snapshot.projection.parts[occurrence.elementId], mount.baseline)) {
                if (!equal(node.getPortProp(id, 'args'), { x, y })) node.setPortProp(id, 'args', { x, y });
            }
        }
    }
    reroute(mount, nodes, state);
    renderSelection(mount);
    return mount.commands.complete(nodes, routes, equal(viewport, state.viewport) ? null : viewport);
}

/**
 * Updates local occurrence selection and notifies the owning C# view.
 * @param {object} mount Owning canvas resources.
 * @param {string} id Occurrence identity.
 * @param {boolean} additive Whether existing selection is retained.
 * @returns {void}
 */
function select(mount, id, additive) {
    if (mount.disposed) return;
    const selection = additive ? new Set(mount.snapshot.selection) : new Set();
    if (id) selection.has(id) ? selection.delete(id) : selection.add(id);
    mount.snapshot = { ...mount.snapshot, selection: [...selection] };
    renderSelection(mount);
    mount.reference.invokeMethodAsync('SelectionChanged', mount.snapshot.sessionId, [...selection], selection.has(id) ? id : null)
        .catch(() => mount.status('Selection unavailable while disconnected.'));
}

/**
 * Imports the bundled renderer and owns its graph, observers and handlers until unmount.
 * @param {HTMLElement} root Component-owned host element.
 * @param {object} reference Caller-owned .NET callback reference.
 * @param {object} snapshot Accepted C# presentation and model projection.
 * @param {number} minimumZoom Lower detailed-rendering zoom bound.
 * @param {boolean} asyncRendering Diagnostic asynchronous-rendering option.
 * @param {boolean} simpleRouting Diagnostic routing control.
 * @returns {Promise<void>} Completion of mounting; obsolete mounts release their resources.
 */
export async function mount(root, reference, snapshot, minimumZoom = 0.1, asyncRendering = false, simpleRouting = false) {
    unmount(root);
    const owner = { root, reference, snapshot, minimumZoom, simpleRouting, baseline: snapshot.state, fallbacks: new Set(), metrics: { routes: [] }, disposed: false };
    mounts.set(root, owner);
    const { Graph, routerPresets } = await import('../../../../vendor/x6/x6.js');
    if (owner.disposed) return;
    if (!Graph.registry?.nodes?.get?.('bloom-part')) Graph.registerNode('bloom-part', {}, true);
    owner.orth = routerPresets.orth;
    owner.status = message => { if (!owner.disposed) root.dataset.synchronization = message; };
    owner.container = document.createElement('div');
    root.appendChild(owner.container);
    owner.graph = new Graph({ container: owner.container, width: root.clientWidth, height: root.clientHeight, async: asyncRendering, virtual: false, grid: { size: 1, visible: false },
        panning: { enabled: true, eventTypes: ['leftMouseDown', 'mouseWheel'] },
        mousewheel: { enabled: true, modifiers: ['ctrl', 'meta'], minScale: minimumZoom, maxScale: 4 },
        interacting(cellView) { return { nodeMovable: owner.snapshot.canEdit && !owner.commands?.blocked && !cellView.cell.getData()?.parentId,
            edgeMovable: false, vertexMovable: owner.snapshot.canEdit && !owner.commands?.blocked,
            vertexAddable: false, vertexDeletable: owner.snapshot.canEdit && !owner.commands?.blocked,
            arrowheadMovable: false, magnetConnectable: false, edgeLabelMovable: false }; },
        connecting: { allowBlank: false, allowNode: false, allowEdge: false } });
    owner.commands = new CompletedChanges(snapshot, (...args) => reference.invokeMethodAsync(...args), (state, force) => {
        render(owner, state, force);
        if (!owner.dragging) owner.baseline = state.state;
    }, owner.status);
    owner.graph.on('node:mousedown', ({ node, e }) => {
        if (!owner.snapshot.selection.includes(node.id) || e.ctrlKey || e.metaKey) select(owner, node.id, e.ctrlKey || e.metaKey);
    });
    owner.graph.on('edge:mousedown', ({ edge, e }) => select(owner, edge.id, e.ctrlKey || e.metaKey));
    owner.graph.on('blank:click', () => select(owner, null, false));
    owner.graph.on('node:change:position', ({ node, current, previous }) => {
        if (!owner.dragging || owner.applying || owner.movingSelection || !owner.snapshot.selection.includes(node.id)) return;
        owner.movingSelection = true;
        try {
            for (const id of owner.snapshot.selection) {
                const selected = owner.graph.getCellById(id);
                if (id !== node.id && selected?.isNode() && !selected.getParentId()) {
                    selected.translate(current.x - previous.x, current.y - previous.y);
                }
            }
        } finally {
            owner.movingSelection = false;
        }
    });
    owner.pointerDown = () => { owner.dragging = true; };
    owner.pointerUp = () => { if (owner.dragging) queueMicrotask(() => finish(owner)); };
    owner.wheel = () => { owner.dragging = true; clearTimeout(owner.wheelTimer); owner.wheelTimer = setTimeout(() => finish(owner), 150); };
    root.addEventListener('mousedown', owner.pointerDown, true);
    root.addEventListener('wheel', owner.wheel, { passive: true });
    document.addEventListener('mouseup', owner.pointerUp);
    owner.resize = new ResizeObserver(() => { if (!owner.disposed) owner.graph.resize(root.clientWidth, root.clientHeight); });
    owner.resize.observe(root);
    owner.theme = new MutationObserver(() => render(owner, owner.commands.snapshot));
    owner.theme.observe(document.documentElement, { attributes: true, attributeFilter: ['class', 'data-theme', 'style'] });
    owner.recover = () => owner.commands.recover();
    window.addEventListener('online', owner.recover);
    owner.recoveryTimer = setInterval(owner.recover, 2000);
    render(owner, snapshot);
}

/**
 * Applies an accepted C# snapshot through the mounted command coordinator.
 * @param {HTMLElement} root Component-owned host element.
 * @param {object} snapshot Authoritative accepted state.
 * @returns {void}
 */
export function update(root, snapshot) {
    const owner = mounts.get(root);
    owner?.commands?.update(snapshot);
}

/**
 * Reads mounted renderer state for diagnostics without accepting semantic changes.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {object|null} Geometry, pending-command state and routing observations.
 */
export function inspect(root) {
    const owner = mounts.get(root);
    if (!owner?.graph) return null;
    return { snapshot: owner.commands.snapshot, geometry: owner.graph.getNodes().map(node => ({ id: node.id, geometry: currentGeometry(node) })),
        metrics: { ...owner.metrics, ...owner.commands.metrics }, fallbacks: [...owner.fallbacks], pending: !!owner.commands.pending, blocked: !!owner.commands.blocked,
        routes: owner.graph.getEdges().map(edge => ({ id: edge.id, path: owner.graph.findViewByCell(edge)?.getConnectionPathData() })) };
}

/**
 * Serializes the mounted graph with resolved styles for internal diagnostics.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {string} Standalone SVG; throws if the graph is not mounted.
 */
export function standaloneSvg(root) {
    const source = root.querySelector('svg.x6-graph-svg');
    if (!source) throw new Error('Canvas is not mounted.');
    const stage = source.querySelector('.x6-graph-svg-stage');
    const box = stage.getBBox();
    const copy = source.cloneNode(true);
    const originals = [source, ...source.querySelectorAll('*')];
    const clones = [copy, ...copy.querySelectorAll('*')];
    const styles = ['fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-dasharray', 'opacity', 'font-family', 'font-size', 'font-weight', 'text-anchor', 'dominant-baseline', 'visibility', 'display', 'pointer-events'];
    originals.forEach((element, index) => {
        const css = getComputedStyle(element);
        for (const property of styles) clones[index].style.setProperty(property, css.getPropertyValue(property));
        for (const attribute of [...clones[index].attributes]) {
            if (attribute.name.startsWith('data-') || attribute.name.startsWith('on')) clones[index].removeAttribute(attribute.name);
        }
    });
    copy.querySelectorAll('.x6-cell-tools,.x6-edge-tools,.x6-edge-wrap,.x6-graph-svg-decorator,.x6-graph-svg-background').forEach(element => element.remove());
    copy.querySelector('.x6-graph-svg-viewport')?.removeAttribute('transform');
    copy.querySelector('.x6-graph-svg-stage')?.removeAttribute('transform');
    copy.setAttributeNS('http://www.w3.org/2000/xmlns/', 'xmlns', 'http://www.w3.org/2000/svg');
    copy.setAttribute('viewBox', `${box.x - 24} ${box.y - 24} ${box.width + 48} ${box.height + 48}`);
    copy.setAttribute('width', String(box.width + 48));
    copy.setAttribute('height', String(box.height + 48));
    copy.style.removeProperty('width');
    copy.style.removeProperty('height');
    copy.style.setProperty('background', palette(root).body);
    return new XMLSerializer().serializeToString(copy);
}

/**
 * Starts a diagnostic download and releases its object URL after use.
 * @param {string} name Download filename.
 * @param {string} type MIME type.
 * @param {string} contents Serialized content.
 * @returns {void}
 */
export function download(name, type, contents) {
    const url = URL.createObjectURL(new Blob([contents], { type }));
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/**
 * Completes pending presentation commands before diagnostic operations.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {Promise<boolean>} Whether the graph remains mounted with accepted state.
 */
export async function settle(root) {
    const owner = mounts.get(root);
    if (!owner?.graph || owner.disposed) return false;
    await finish(owner);
    await owner.commands.completion;
    return !owner.disposed && !owner.commands.blocked;
}

/**
 * Dispatches a local viewport or internal export action and completes its presentation command.
 * @param {HTMLElement} root Component-owned host element.
 * @param {string} actionName Supported zoom, fit or diagnostic export action.
 * @param {number} value Requested zoom for a zoom action.
 * @returns {Promise<void>|undefined} Completion when an active graph accepts the action.
 */
export function action(root, actionName, value) {
    const owner = mounts.get(root);
    if (!owner?.graph || owner.disposed) return;
    if (actionName === 'export') return settle(root).then(ready => {
        if (ready) download('interconnection.svg', 'image/svg+xml', standaloneSvg(root));
    });
    if (actionName === 'zoom') {
        const zoom = clamp(value, owner.minimumZoom, 4);
        owner.graph.scale(zoom, zoom);
    } else if (actionName === 'fit') {
        const box = owner.graph.getContentBBox({ useCellGeometry: false });
        const zoom = clamp(Math.min(root.clientWidth / (box.width + 80), root.clientHeight / (box.height + 80)), owner.minimumZoom, 4);
        owner.graph.scale(zoom, zoom);
        owner.graph.translate((root.clientWidth - box.width * zoom) / 2 - box.x * zoom,
            (root.clientHeight - box.height * zoom) / 2 - box.y * zoom);
    }
    return finish(owner);
}

/**
 * Disposes the graph, command coordinator, observers, timers and handlers owned by this host.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {void}
 */
export function unmount(root) {
    const owner = mounts.get(root);
    if (!owner) return;
    owner.disposed = true;
    owner.commands?.dispose();
    clearTimeout(owner.wheelTimer);
    clearInterval(owner.recoveryTimer);
    owner.resize?.disconnect();
    owner.theme?.disconnect();
    root.removeEventListener('mousedown', owner.pointerDown, true);
    root.removeEventListener('wheel', owner.wheel);
    document.removeEventListener('mouseup', owner.pointerUp);
    window.removeEventListener('online', owner.recover);
    owner.graph?.dispose();
    owner.container?.remove();
    mounts.delete(root);
}
