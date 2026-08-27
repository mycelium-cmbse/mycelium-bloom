const keydownGuards = new Map();

export function capturePointer(separatorId, pointerId) {
    const separator = document.getElementById(separatorId);

    if (!separator) {
        return null;
    }

    const leftGroup = document.getElementById(separator.dataset.leftGroupElementId);
    const rightGroup = document.getElementById(separator.dataset.rightGroupElementId);

    if (!leftGroup || !rightGroup) {
        return null;
    }

    const leftBounds = leftGroup.getBoundingClientRect();
    const rightBounds = rightGroup.getBoundingClientRect();
    const pairWidth = leftBounds.width + rightBounds.width;

    if (!Number.isFinite(leftBounds.width)
        || !Number.isFinite(rightBounds.width)
        || leftBounds.width < 0
        || rightBounds.width < 0
        || !Number.isFinite(pairWidth)
        || pairWidth <= 0) {
        return null;
    }

    try {
        separator.setPointerCapture(pointerId);
    } catch (error) {
        if (error instanceof DOMException) {
            return null;
        }

        throw error;
    }

    return [leftBounds.width, rightBounds.width, pairWidth];
}

export function releasePointer(separatorId, pointerId) {
    const separator = document.getElementById(separatorId);

    if (separator?.hasPointerCapture(pointerId)) {
        separator.releasePointerCapture(pointerId);
    }
}

export function measureAdjacentPairWidth(separatorId) {
    const separator = document.getElementById(separatorId);

    if (!separator) {
        return 0;
    }

    const leftGroup = document.getElementById(separator.dataset.leftGroupElementId);
    const rightGroup = document.getElementById(separator.dataset.rightGroupElementId);

    if (!leftGroup || !rightGroup) {
        return 0;
    }

    const pairWidth = leftGroup.getBoundingClientRect().width + rightGroup.getBoundingClientRect().width;

    return Number.isFinite(pairWidth) && pairWidth > 0 ? pairWidth : 0;
}

export function focusElementById(elementId) {
    const element = document.getElementById(elementId);

    if (!element) {
        return false;
    }

    element.focus({ preventScroll: true });
    return document.activeElement === element;
}

export function registerKeydownGuards(workspaceId) {
    unregisterKeydownGuards(workspaceId);

    const workspace = document.getElementById(workspaceId);

    if (!workspace) {
        return false;
    }

    const handler = event => {
        const role = event.target?.getAttribute?.("role");
        const handledTabKey = role === "tab"
            && ["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key);
        const handledSeparatorKey = role === "separator"
            && ["ArrowLeft", "ArrowRight"].includes(event.key);

        if (handledTabKey || handledSeparatorKey) {
            event.preventDefault();
        }
    };

    workspace.addEventListener("keydown", handler);
    keydownGuards.set(workspaceId, { workspace, handler });
    return true;
}

export function unregisterKeydownGuards(workspaceId) {
    const registration = keydownGuards.get(workspaceId);

    if (!registration) {
        return;
    }

    registration.workspace.removeEventListener("keydown", registration.handler);
    keydownGuards.delete(workspaceId);
}
