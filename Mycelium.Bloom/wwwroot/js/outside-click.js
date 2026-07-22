const registrations = new Map();

let documentPointerHandler;

export function registerOutsideClick(registrationId, rootElement, componentReference) {
    disposeOutsideClick(registrationId);

    registrations.set(registrationId, {
        rootElement,
        componentReference,
        pending: false
    });

    ensureDocumentHandler();
}

export function disposeOutsideClick(registrationId) {
    registrations.delete(registrationId);

    if (registrations.size === 0 && documentPointerHandler) {
        document.removeEventListener("pointerdown", documentPointerHandler, true);
        documentPointerHandler = undefined;
    }
}

function ensureDocumentHandler() {
    if (documentPointerHandler) {
        return;
    }

    documentPointerHandler = handleDocumentPointerDown;
    document.addEventListener("pointerdown", documentPointerHandler, true);
}

function handleDocumentPointerDown(event) {
    if (!(event.target instanceof Node)) {
        return;
    }

    for (const registration of registrations.values()) {
        if (registration.pending
            || !registration.rootElement.isConnected
            || registration.rootElement.contains(event.target)
            || !registration.rootElement.querySelector('[aria-expanded="true"]')) {
            continue;
        }

        registration.pending = true;

        registration.componentReference
            .invokeMethodAsync("DismissFromOutsideClickAsync")
            .catch(error => console.error("Failed to dismiss popup after outside click.", error))
            .finally(() => {
                registration.pending = false;
            });
    }
}
