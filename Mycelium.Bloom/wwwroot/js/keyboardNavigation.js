const navigationKeyPreventionHandlers = new WeakMap();
const navigationKeys = new Set(["ArrowDown", "ArrowUp", "ArrowLeft", "ArrowRight", "Home", "End"]);

function handleNavigationKeyDown(event) {
    if (navigationKeys.has(event.key) && !shouldPreserveNativeKeyHandling(event)) {
        event.preventDefault();
    }
}

export function registerNavigationKeyPrevention(element) {
    if (!(element instanceof HTMLElement)) {
        return;
    }

    disposeNavigationKeyPrevention(element);

    element.addEventListener("keydown", handleNavigationKeyDown, true);
    navigationKeyPreventionHandlers.set(element, handleNavigationKeyDown);
}

export function disposeNavigationKeyPrevention(element) {
    const keydownHandler = navigationKeyPreventionHandlers.get(element);

    if (!keydownHandler) {
        return;
    }

    element.removeEventListener("keydown", keydownHandler, true);
    navigationKeyPreventionHandlers.delete(element);
}

function shouldPreserveNativeKeyHandling(event) {
    return event.defaultPrevented
        || event.isComposing
        || event.altKey
        || event.ctrlKey
        || event.metaKey
        || event.shiftKey
        || isEditableEventTarget(event.target);
}

function isEditableEventTarget(target) {
    if (!(target instanceof Element)) {
        return false;
    }

    if (target.closest("input, textarea, select")) {
        return true;
    }

    const editableElement = target.closest("[contenteditable]");

    return editableElement instanceof HTMLElement && editableElement.isContentEditable;
}
