const keyPreventionHandlers = new WeakMap();

export function registerKeyPrevention(rootElement, rules) {
    disposeKeyPrevention(rootElement);

    const handler = event => {
        if (event.defaultPrevented || !(event.target instanceof Element)) {
            return;
        }

        if (rules.some(rule => matchesRule(rootElement, event.target, event.key, rule))) {
            event.preventDefault();
        }
    };

    keyPreventionHandlers.set(rootElement, handler);
    rootElement.addEventListener("keydown", handler);
}

export function disposeKeyPrevention(rootElement) {
    const handler = keyPreventionHandlers.get(rootElement);

    if (!handler) {
        return;
    }

    rootElement.removeEventListener("keydown", handler);
    keyPreventionHandlers.delete(rootElement);
}

function matchesRule(rootElement, eventTarget, key, rule) {
    if (!rule.keys.includes(key)) {
        return false;
    }

    if (!rule.selector) {
        return eventTarget === rootElement || rootElement.contains(eventTarget);
    }

    const matchingTarget = eventTarget.closest(rule.selector);

    return matchingTarget !== null
        && (matchingTarget === rootElement || rootElement.contains(matchingTarget));
}
