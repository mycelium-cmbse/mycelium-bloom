const initializations = new WeakMap();

/**
 * Loads the native diagram script once when the first interactive canvas needs it.
 * Concurrent mounts share the load, and a failed load can be retried.
 * @returns {Promise<void>} Completion of native diagram script initialization.
 */
export function ensureDiagramScript() {
    if (globalThis.ZBlazorDiagrams) {
        return Promise.resolve();
    }

    const owner = document;
    const pending = initializations.get(owner);

    if (pending) {
        return pending;
    }

    const initialization = new Promise((resolve, reject) => {
        const script = owner.createElement("script");
        script.src = new URL("_content/Z.Blazor.Diagrams/script.min.js", owner.baseURI).href;
        script.onload = () => resolve();
        script.onerror = () => {
            script.remove();
            reject(new Error("The diagram script could not be loaded."));
        };
        owner.head.append(script);
    }).catch(error => {
        initializations.delete(owner);
        throw error;
    });

    initializations.set(owner, initialization);
    return initialization;
}

const stylesheets = new WeakMap();

/**
 * Loads shared diagram CSS only when an interactive canvas needs it.
 * Loaded assets remain document-owned so closing one tab cannot invalidate another.
 * @returns {Promise<void>} Completion of the native diagram stylesheet load.
 */
export function ensureDiagramStyles() {
    const owner = document;
    const pending = stylesheets.get(owner);

    if (pending) {
        return pending;
    }

    const initialization = new Promise((resolve, reject) => {
        const link = owner.createElement("link");
        link.rel = "stylesheet";
        link.href = new URL("_content/Z.Blazor.Diagrams/style.min.css", owner.baseURI).href;
        link.onload = () => resolve();
        link.onerror = () => {
            link.remove();
            reject(new Error("The diagram stylesheet could not be loaded."));
        };
        owner.head.append(link);
    }).catch(error => {
        stylesheets.delete(owner);
        throw error;
    });

    stylesheets.set(owner, initialization);
    return initialization;
}

/**
 * Ensures both native assets are available before canvas initialization.
 * @returns {Promise<void>} Completion of shared script and stylesheet initialization.
 */
export async function ensureDiagramAssets() {
    await Promise.all([ensureDiagramScript(), ensureDiagramStyles()]);
}

/**
 * Adds browser-owned capture, Space navigation and cancellation to one native canvas.
 * Geometry and zoom remain owned by the native diagram behaviors and tab presentation.
 * @param {HTMLElement} canvas The mounted native canvas element.
 * @param {object} reference The mount-owned .NET interaction callback reference.
 * @returns {{dispose: function(): void}} The listener and pointer-capture lifetime.
 */
export function attachDiagramInteraction(canvas, reference) {
    let disposed = false;
    let space = false;
    let pointer = null;
    let captureTarget = null;
    let start = null;
    let suppressClick = false;
    const listeners = new canvas.ownerDocument.defaultView.AbortController();
    const options = { capture: true, signal: listeners.signal };
    const owns = event => event.target.closest?.('.diagram-canvas') === canvas;
    const invoke = async (method, ...args) => {
        if (!disposed) {
            try {
                await reference.invokeMethodAsync(method, ...args);
            } catch (error) {
                if (!disposed) {
                    throw error;
                }
            }
        }
    };
    const release = () => {
        const captured = pointer;
        const target = captureTarget;
        pointer = null;
        captureTarget = null;
        start = null;
        delete canvas.dataset.dragging;
        if (captured !== null && target?.hasPointerCapture(captured)) {
            target.releasePointerCapture(captured);
        }
    };
    const cancel = async () => {
        space = false;
        suppressClick = true;
        delete canvas.dataset.temporaryPan;
        release();
        await invoke('CancelInteraction');
    };
    canvas.addEventListener('keydown', async event => {
        if (!owns(event) || event.target.closest('input, textarea, select, [contenteditable=true]')) {
            return;
        }
        if (event.code === 'Space') {
            event.preventDefault();
            event.stopImmediatePropagation();
            if (!space) {
                space = true;
                canvas.dataset.temporaryPan = '';
                await invoke('SetTemporaryPan', true);
            }
        } else if (event.key === 'Escape') {
            await cancel();
        }
    }, options);
    document.addEventListener('keyup', async event => {
        if (event.code === 'Space' && space) {
            space = false;
            delete canvas.dataset.temporaryPan;
            await invoke('SetTemporaryPan', false);
        }
    }, options);
    canvas.addEventListener('pointerdown', event => {
        if (!owns(event)) {
            return;
        }
        if (pointer !== null || !event.isPrimary || ![0, 1].includes(event.button)) {
            event.stopImmediatePropagation();
            return;
        }
        canvas.focus({ preventScroll: true });
        suppressClick = space || event.button === 1 || canvas.closest('.general-diagram')?.dataset.tool === 'pan';
        pointer = event.pointerId;
        captureTarget = event.target;
        start = { x: event.clientX, y: event.clientY };
        captureTarget.setPointerCapture(pointer);
        canvas.dataset.dragging = '';
        if (event.button === 1) {
            event.preventDefault();
        }
    }, options);
    canvas.addEventListener('pointermove', event => {
        if (event.pointerId === pointer && start && (event.clientX !== start.x || event.clientY !== start.y)) {
            suppressClick = true;
        }
    }, options);
    canvas.addEventListener('pointerup', event => {
        if (event.pointerId === pointer) {
            release();
        }
    }, options);
    canvas.addEventListener('click', event => {
        if (owns(event) && suppressClick) {
            event.preventDefault();
            event.stopImmediatePropagation();
            suppressClick = false;
        }
    }, options);
    canvas.addEventListener('pointercancel', async event => {
        if (event.pointerId === pointer) {
            await cancel();
        }
    }, options);
    canvas.addEventListener('lostpointercapture', async event => {
        if (event.pointerId === pointer) {
            await cancel();
        }
    }, options);
    canvas.addEventListener('focusout', async event => {
        if ((space || pointer !== null) && !canvas.contains(event.relatedTarget)) {
            await cancel();
        }
    }, options);
    window.addEventListener('blur', cancel, { signal: listeners.signal });
    return {
        dispose() {
            disposed = true;
            listeners.abort();
            space = false;
            delete canvas.dataset.temporaryPan;
            release();
        }
    };
}
