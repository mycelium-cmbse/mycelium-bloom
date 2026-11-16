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
