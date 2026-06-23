let keydownHandler;

export function registerSearchShortcut(inputId) {
    disposeSearchShortcut();

    keydownHandler = function (event) {
        const key = event.key?.toLowerCase();

        const isSearchShortcut = (event.ctrlKey || event.metaKey)
            && key === "k"
            && !event.altKey
            && !event.shiftKey;

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