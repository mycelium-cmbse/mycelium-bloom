const registrations = new Map();

let documentKeydownHandler;

// Called from SearchInput.razor.cs through JS interop. Re-inserting an existing
// registration moves it to the newest position without affecting other inputs.
export function registerSearchShortcut(registrationId, inputId, shortcut) {
    registrations.delete(registrationId);
    registrations.set(registrationId, {
        inputId,
        key: (shortcut?.key ?? "k").toLowerCase(),
        requiresControlOrMeta: shortcut?.requiresControlOrMeta ?? true,
        requiresAlt: shortcut?.requiresAlt ?? false,
        requiresShift: shortcut?.requiresShift ?? false
    });

    ensureDocumentHandler();
}

export function disposeSearchShortcut(registrationId) {
    registrations.delete(registrationId);

    if (registrations.size === 0 && documentKeydownHandler) {
        document.removeEventListener("keydown", documentKeydownHandler, true);
        documentKeydownHandler = undefined;
    }
}

function ensureDocumentHandler() {
    if (documentKeydownHandler) {
        return;
    }

    documentKeydownHandler = handleDocumentKeyDown;
    document.addEventListener("keydown", documentKeydownHandler, true);
}

function handleDocumentKeyDown(event) {
    if (event.defaultPrevented) {
        return;
    }

    const registrationList = Array.from(registrations.values());

    for (let index = registrationList.length - 1; index >= 0; index--) {
        const registration = registrationList[index];

        if (!matchesShortcut(event, registration)) {
            continue;
        }

        const input = document.getElementById(registration.inputId);

        if (!(input instanceof HTMLInputElement) || input.disabled) {
            continue;
        }

        event.preventDefault();
        event.stopPropagation();
        input.focus({preventScroll: true});
        input.select();

        return;
    }
}

function matchesShortcut(event, registration) {
    const hasControlOrMeta = event.ctrlKey || event.metaKey;
    const matchesControlOrMeta = registration.requiresControlOrMeta
        ? hasControlOrMeta
        : !hasControlOrMeta;

    return event.key?.toLowerCase() === registration.key
        && matchesControlOrMeta
        && event.altKey === registration.requiresAlt
        && event.shiftKey === registration.requiresShift;
}
