const shortcutRegistrations = new Map();
const emptySpaceRegistrations = new Map();

let documentKeydownHandler;

// Blazor defers value-attribute updates while a focused field is being edited.
// Synchronize an explicitly cleared controlled value without replacing the
// Blueprint input, its event listener, or its popover anchor.
export function clearSearchInputValue(input) {
    if (!(input instanceof HTMLInputElement)) {
        return;
    }

    input.value = "";
    input.dispatchEvent(new InputEvent("input", {
        bubbles: true,
        inputType: "deleteContentBackward"
    }));
}

// Called from SearchInput.razor.cs through JS interop. Re-inserting an existing
// registration moves it to the newest position without affecting other inputs.
export function registerSearchShortcut(registrationId, inputId, shortcut) {
    shortcutRegistrations.delete(registrationId);
    shortcutRegistrations.set(registrationId, {
        inputId,
        key: (shortcut?.key ?? "k").toLowerCase(),
        requiresControlOrMeta: shortcut?.requiresControlOrMeta ?? true,
        requiresAlt: shortcut?.requiresAlt ?? false,
        requiresShift: shortcut?.requiresShift ?? false
    });

    ensureDocumentHandler();
}

export function disposeSearchShortcut(registrationId) {
    shortcutRegistrations.delete(registrationId);

    disposeDocumentHandlerWhenUnused();
}

export function registerEmptySpaceGuard(registrationId, inputId) {
    emptySpaceRegistrations.set(registrationId, inputId);
    ensureDocumentHandler();
}

export function disposeEmptySpaceGuard(registrationId) {
    emptySpaceRegistrations.delete(registrationId);

    disposeDocumentHandlerWhenUnused();
}

function disposeDocumentHandlerWhenUnused() {
    if (shortcutRegistrations.size === 0 && emptySpaceRegistrations.size === 0 && documentKeydownHandler) {
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

    if (matchesEmptySpaceGuard(event)) {
        event.preventDefault();
        return;
    }

    const registrationList = Array.from(shortcutRegistrations.values());

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

function matchesEmptySpaceGuard(event) {
    if (!(event.target instanceof HTMLInputElement)
        || event.key !== " "
        || event.isComposing
        || event.ctrlKey
        || event.metaKey
        || event.altKey
        || event.shiftKey
        || event.target.value.length !== 0) {
        return false;
    }

    for (const inputId of emptySpaceRegistrations.values()) {
        if (event.target.id === inputId) {
            return true;
        }
    }

    return false;
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
