const registrations = new Map();

let documentKeydownHandler;
let mutationObserver;

// Blazor Blueprint Primitives 3.15.0 sets its keydown prevent-default flag
// after the native event has already been dispatched. Preventing the native
// button activation here allows the primitive's own handler to open the Select
// exactly once for Enter and Space.
export function registerSelectCompatibility(registrationId, triggerId) {
    registrations.set(registrationId, triggerId);
    ensureDocumentKeydownHandler();
    ensureMutationObserver();
    synchronizeRegisteredListboxes();
}

export function disposeSelectCompatibility(registrationId) {
    registrations.delete(registrationId);

    if (registrations.size !== 0) {
        return;
    }

    if (documentKeydownHandler) {
        document.removeEventListener("keydown", documentKeydownHandler, true);
        documentKeydownHandler = undefined;
    }

    mutationObserver?.disconnect();
    mutationObserver = undefined;
}

function ensureDocumentKeydownHandler() {
    if (documentKeydownHandler) {
        return;
    }

    documentKeydownHandler = handleDocumentKeyDown;
    document.addEventListener("keydown", documentKeydownHandler, true);
}

function handleDocumentKeyDown(event) {
    if (!(event.target instanceof HTMLElement)) {
        return;
    }

    const triggerId = findRegisteredTriggerId(event.target);

    if (triggerId) {
        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            return;
        }

        if (event.key === "Tab") {
            const focusTarget = findAdjacentTabTarget(triggerId, event.shiftKey);

            if (focusTarget) {
                // The primitive's deferred prevent-default flag can otherwise
                // consume the first Tab after a handled trigger key.
                event.preventDefault();
                scheduleFocus(focusTarget);
            }

            return;
        }
    }

    const listbox = event.target.closest('[role="listbox"]');

    if (event.key !== "Tab" || !listbox) {
        return;
    }

    const labelledBy = listbox.getAttribute("aria-labelledby");

    if (!labelledBy || !hasRegisteredTrigger(labelledBy)) {
        return;
    }

    const focusTarget = findAdjacentTabTarget(labelledBy, event.shiftKey);

    if (!focusTarget) {
        return;
    }

    // Let Blueprint process Tab and close its portal, but replace the browser's
    // portal-relative tab destination with the control adjacent to the trigger.
    event.preventDefault();
    scheduleFocus(focusTarget);
}

function scheduleFocus(focusTarget) {
    requestAnimationFrame(() => {
        requestAnimationFrame(() => focusTarget.focus({preventScroll: true}));
    });
}

function findRegisteredTriggerId(target) {
    for (const triggerId of registrations.values()) {
        if (target.id === triggerId) {
            return triggerId;
        }
    }

    return undefined;
}

function hasRegisteredTrigger(triggerId) {
    for (const registeredTriggerId of registrations.values()) {
        if (registeredTriggerId === triggerId) {
            return true;
        }
    }

    return false;
}

function findAdjacentTabTarget(triggerId, moveBackward) {
    const trigger = document.getElementById(triggerId);

    if (!trigger) {
        return undefined;
    }

    const candidates = Array.from(document.querySelectorAll([
        "a[href]",
        "button:not([disabled])",
        "input:not([disabled]):not([type='hidden'])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "[tabindex]:not([tabindex='-1'])"
    ].join(","))).filter(isTabbable);
    const triggerIndex = candidates.indexOf(trigger);

    if (triggerIndex < 0) {
        return undefined;
    }

    return candidates[triggerIndex + (moveBackward ? -1 : 1)];
}

function isTabbable(element) {
    if (!(element instanceof HTMLElement)
        || element.closest('[role="menu"], [role="listbox"], [role="dialog"], [role="tooltip"]')) {
        return false;
    }

    const style = getComputedStyle(element);
    const rect = element.getBoundingClientRect();

    return style.display !== "none"
        && style.visibility !== "hidden"
        && rect.width > 0
        && rect.height > 0;
}

function ensureMutationObserver() {
    if (mutationObserver) {
        return;
    }

    mutationObserver = new MutationObserver(mutations => {
        for (const mutation of mutations) {
            const listbox = mutation.target instanceof Element
                ? mutation.target.closest('[role="listbox"]')
                : undefined;

            if (listbox) {
                synchronizeListbox(listbox);
            }
        }

        synchronizeRegisteredListboxes();
    });

    mutationObserver.observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: ["data-focused"]
    });
}

function synchronizeRegisteredListboxes() {
    for (const triggerId of registrations.values()) {
        const trigger = document.getElementById(triggerId);
        const contentId = trigger?.getAttribute("aria-controls");
        const listbox = contentId ? document.getElementById(contentId) : undefined;

        if (listbox?.getAttribute("role") === "listbox") {
            synchronizeListbox(listbox);
        }
    }
}

function synchronizeListbox(listbox) {
    const labelledBy = listbox.getAttribute("aria-labelledby");

    if (!labelledBy || !hasRegisteredTrigger(labelledBy)) {
        return;
    }

    const activeOption = listbox.querySelector('[role="option"][data-focused="true"]');

    if (activeOption?.id) {
        listbox.setAttribute("aria-activedescendant", activeOption.id);
    } else {
        listbox.removeAttribute("aria-activedescendant");
    }
}
