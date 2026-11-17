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
