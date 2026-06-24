let keydownHandler;

// Called from SearchInput.razor.cs through JS interop.
export function registerSearchShortcut(inputId, shortcut) {
    disposeSearchShortcut();

    const shortcutKey = (shortcut?.key ?? "k").toLowerCase();
    const requiresControlOrMeta = shortcut?.requiresControlOrMeta ?? true;
    const requiresAlt = shortcut?.requiresAlt ?? false;
    const requiresShift = shortcut?.requiresShift ?? false;

    keydownHandler = function (event) {
        const key = event.key?.toLowerCase();
        const hasControlOrMeta = event.ctrlKey || event.metaKey;

        const matchesControlOrMeta = requiresControlOrMeta
            ? hasControlOrMeta
            : !hasControlOrMeta;

        const isSearchShortcut = key === shortcutKey
            && matchesControlOrMeta
            && event.altKey === requiresAlt
            && event.shiftKey === requiresShift;

        if (!isSearchShortcut) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        const input = document.getElementById(inputId);

        if (input instanceof HTMLInputElement) {
            input.focus({preventScroll: true});
            input.select();
        }
    };

    document.addEventListener("keydown", keydownHandler, true);
}

export function disposeSearchShortcut() {
    if (keydownHandler) {
        document.removeEventListener("keydown", keydownHandler, true);
        keydownHandler = undefined;
    }
}