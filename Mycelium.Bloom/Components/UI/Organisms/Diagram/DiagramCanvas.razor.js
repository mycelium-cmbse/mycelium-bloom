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
 * Attaches native pointer capture to one canvas.
 * @param {HTMLElement} canvas The mounted native canvas.
 * @param {object} reference The mount-owned .NET drop receiver.
 * @returns {object} The browser-owned capture and listener lifetime.
 */
export function attachDiagramPointerCapture(canvas, reference) {
    const view = canvas.ownerDocument.defaultView;
    const listeners = new view.AbortController();
    const options = { capture: true, signal: listeners.signal };
    let capture;

    const elementType = 'application/x-mycelium-element';
    const acceptsElement = event => event.target.closest('.diagram-canvas') === canvas
        && canvas.closest('.general-diagram')?.dataset.canAdd === 'true'
        && event.dataTransfer?.types.includes(elementType);
    canvas.addEventListener('dragover', event => {
        if (acceptsElement(event)) {
            event.preventDefault();
            event.stopPropagation();
            event.dataTransfer.dropEffect = 'copy';
        }
    }, options);
    canvas.addEventListener('drop', async event => {
        if (!acceptsElement(event)) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        const bounds = canvas.getBoundingClientRect();
        const scaleX = canvas.clientWidth > 0 && bounds.width > 0 ? canvas.clientWidth / bounds.width : 1;
        const scaleY = canvas.clientHeight > 0 && bounds.height > 0 ? canvas.clientHeight / bounds.height : 1;
        try {
            await reference.invokeMethodAsync('DropElement', event.dataTransfer.getData(elementType),
                (event.clientX - bounds.left) * scaleX, (event.clientY - bounds.top) * scaleY);
        } catch (error) {
            const message = error?.message ?? '';
            const receiverDisposed = /^(?:System\.ArgumentException: )?There is no tracked object with id '[^']+'\. Perhaps the DotNetObjectReference instance was already disposed\./.test(message);
            const disconnected = [
                'The circuit associated with this dispatcher is no longer available.',
                "Cannot send data if the connection is not in the 'Connected' State.",
                'Invocation canceled due to the underlying connection being closed.'
            ].includes(message);

            // Disposal only excuses known interop teardown; projection failures must remain visible.
            if (error?.name !== 'AbortError' && !(listeners.signal.aborted && (receiverDisposed || disconnected))) {
                view.reportError(error);
            }
        }
    }, options);

    const release = () => {
        const captured = capture;
        capture = undefined;
        if (captured?.target.hasPointerCapture(captured.id)) {
            captured.target.releasePointerCapture(captured.id);
        }
    };

    canvas.addEventListener('pointerdown', event => {
        if (event.target.closest('.diagram-canvas') !== canvas || !event.isPrimary || (event.button !== 0 && event.button !== 1)) {
            return;
        }

        if (event.button === 1) {
            event.preventDefault();
            canvas.focus({ preventScroll: true });
        }

        release();
        event.target.setPointerCapture(event.pointerId);
        capture = { target: event.target, id: event.pointerId };
    }, options);

    canvas.addEventListener('lostpointercapture', event => {
        if (capture?.target === event.target && capture.id === event.pointerId) {
            capture = undefined;
        }
    }, options);

    const hasFocus = () => canvas.ownerDocument.activeElement?.closest('.diagram-canvas') === canvas;

    view.addEventListener('blur', () => {
        release();
        if (hasFocus()) {
            canvas.ownerDocument.activeElement.blur();
        }
    }, { signal: listeners.signal });

    return {
        hasFocus,
        release,
        dispose() {
            listeners.abort();
            release();
        }
    };
}
