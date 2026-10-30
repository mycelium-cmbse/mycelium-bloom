const trees = new WeakMap();

// Focus is local presentation state. Activation uses the existing Blazor handlers;
// moving focus never publishes canonical model selection.
export function registerTree(root) {
    if (!root || trees.has(root)) {
        return;
    }

    const items = () => [...root.querySelectorAll('[role="treeitem"]')];
    const parent = item => item.parentElement?.closest('[role="treeitem"]');
    const row = item => item.querySelector(':scope > .mb-project-browser-node__line > .mb-project-browser-node__row');
    const disclosure = item => item.querySelector(':scope > .mb-project-browser-node__line > .mb-project-browser-node__disclosure');
    let active = null;
    let hasFocus = false;
    let ancestors = [];
    let prefix = "";
    let lastTyped = 0;
    const pendingDisclosures = new WeakMap();

    function toggleDisclosure(item) {
        const button = disclosure(item);
        const expanded = item.getAttribute("aria-expanded");
        const pending = pendingDisclosures.get(item);
        if (!button || (pending?.button === button && pending.expanded === expanded)) {
            return;
        }
        // Blazor updates aria-expanded asynchronously. Repeating an arrow before
        // that render must not enqueue a second toggle that reverses the first.
        // A replacement disclosure (for example after filtering) starts fresh.
        pendingDisclosures.set(item, { button, expanded });
        button.click();
    }

    function revealRow(item) {
        const viewport = root.closest('.mb-project-browser__tree-viewport');
        const bounds = row(item)?.getBoundingClientRect();
        if (!viewport || !bounds) {
            return;
        }
        // Restrict scrolling to this Browser. A treeitem includes its descendants,
        // and scrollIntoView can also move the surrounding workspace or document.
        const top = viewport.getBoundingClientRect().top + viewport.clientTop;
        const bottom = top + viewport.clientHeight;
        if (bounds.top < top) {
            viewport.scrollTop += bounds.top - top;
        } else if (bounds.bottom > bottom) {
            viewport.scrollTop += bounds.bottom - bottom;
        }
    }

    function setActive(item, moveFocus = false) {
        if (!item || !root.contains(item)) {
            return;
        }
        active = item;
        ancestors = [];
        for (let node = parent(item); node; node = parent(node)) {
            ancestors.push(node);
        }
        for (const node of items()) {
            node.tabIndex = node === item ? 0 : -1;
        }
        if (moveFocus) {
            item.focus({ preventScroll: true });
            revealRow(item);
        }
    }

    function repair(selectionChanged = false) {
        const visible = items();
        const focused = hasFocus || root.contains(document.activeElement);
        if (active && root.contains(active) && (focused || !selectionChanged)) {
            setActive(active);
            if (focused) {
                revealRow(active);
            }
            return;
        }
        const target = focused
            ? ancestors.find(node => root.contains(node)) ?? visible[0]
            : visible.find(node => node.getAttribute("aria-selected") === "true") ?? visible[0];
        setActive(target, focused);
    }

    function onFocus(event) {
        hasFocus = true;
        const item = event.target.closest('[role="treeitem"]');
        if (item && root.contains(item)) {
            setActive(item);
            revealRow(item);
        }
    }

    function onClick(event) {
        const item = event.target.closest('[role="treeitem"]');
        if (item && root.contains(item)) {
            setActive(item, true);
        }
    }

    function onFocusOut(event) {
        if (event.relatedTarget) {
            hasFocus = root.contains(event.relatedTarget);
        } else {
            // Chromium fires blur before detaching a focused row. Check after
            // removal so repair can recover its ancestor without stealing a Tab exit.
            queueMicrotask(() => {
                if (event.target.isConnected && !root.contains(document.activeElement)) {
                    hasFocus = false;
                }
            });
        }
    }

    function onKey(event) {
        const item = event.target.closest('[role="treeitem"]');
        if (!item || !root.contains(item) || event.altKey || event.ctrlKey || event.metaKey || event.isComposing) {
            return;
        }
        const visible = items();
        const index = visible.indexOf(item);
        let target;
        switch (event.key) {
            case "ArrowDown": target = visible[index + 1]; break;
            case "ArrowUp": target = visible[index - 1]; break;
            case "Home": target = visible[0]; break;
            case "End": target = visible.at(-1); break;
            case "ArrowRight":
                if (item.getAttribute("aria-expanded") === "false") {
                    toggleDisclosure(item);
                } else {
                    target = item.querySelector(':scope > [role="group"] > [role="treeitem"]');
                }
                break;
            case "ArrowLeft":
                if (item.getAttribute("aria-expanded") === "true" && disclosure(item)) {
                    toggleDisclosure(item);
                } else {
                    target = parent(item);
                }
                break;
            case "Enter":
            case " ":
                row(item)?.click();
                break;
            default: {
                if (event.key.length !== 1 || event.key.trim().length === 0) {
                    return;
                }
                // A one-second type-ahead window combines characters; repeated letters cycle matches.
                prefix = event.timeStamp - lastTyped < 1000 ? prefix + event.key.toLocaleLowerCase() : event.key.toLocaleLowerCase();
                lastTyped = event.timeStamp;
                const query = [...prefix].every(character => character === prefix[0]) ? prefix[0] : prefix;
                const ordered = [...visible.slice(index + 1), ...visible.slice(0, index + 1)];
                target = ordered.find(node => node.querySelector('.mb-project-browser-node__title')?.textContent.trim().toLocaleLowerCase().startsWith(query));
                event.preventDefault();
                setActive(target, true);
                return;
            }
        }
        prefix = "";
        event.preventDefault();
        setActive(target ?? item, true);
    }

    const observer = new MutationObserver(records => {
        for (const record of records) {
            if (record.attributeName === "aria-expanded") {
                pendingDisclosures.delete(record.target);
            }
        }
        // Disclosure/content renders must retain the Tab return point. A genuine
        // external selection may still choose a new entry point while focus is outside.
        repair(records.some(record => record.attributeName === "aria-selected"));
    });
    observer.observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ["aria-selected", "aria-expanded"] });
    root.addEventListener("keydown", onKey);
    root.addEventListener("focusin", onFocus);
    root.addEventListener("focusout", onFocusOut);
    root.addEventListener("click", onClick);
    trees.set(root, () => {
        observer.disconnect();
        root.removeEventListener("keydown", onKey);
        root.removeEventListener("focusin", onFocus);
        root.removeEventListener("focusout", onFocusOut);
        root.removeEventListener("click", onClick);
    });
    repair();
}

export function disposeTree(root) {
    trees.get(root)?.();
    trees.delete(root);
}
