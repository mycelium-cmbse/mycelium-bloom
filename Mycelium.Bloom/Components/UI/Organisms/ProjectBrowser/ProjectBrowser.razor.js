const trees = new WeakMap();

/**
 * Scrolls only this Browser's viewport by the amount needed to expose the row.
 * Descendants and the surrounding page do not participate in the visibility calculation.
 * @param {HTMLElement|null} root The Browser tree whose viewport may scroll.
 * @param {HTMLElement|null} item The occurrence whose own row must be visible.
 * @returns {void}
 */
function revealRow(root, item) {
    const viewport = root?.closest('.mb-project-browser__tree-viewport');
    const bounds = item?.querySelector(':scope > .mb-project-browser-node__line > .mb-project-browser-node__row')?.getBoundingClientRect();
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

/**
 * Registers one tree's roving focus and delegated keyboard handlers, once per root.
 * Focus is local presentation state; activation delegates canonical selection to Blazor.
 * @param {HTMLElement|null} root The rendered tree owned by this Browser instance.
 * @returns {void}
 */
export function registerTree(root) {
    if (!root || trees.has(root)) {
        return;
    }

    /** @returns {HTMLElement[]} Rendered occurrences in navigation order; collapsed children are absent. */
    const items = () => [...root.querySelectorAll('[role="treeitem"]')];
    /**
     * @param {HTMLElement} item A rendered occurrence.
     * @returns {HTMLElement|null|undefined} Its owning occurrence, excluding the item itself.
     */
    const parent = item => item.parentElement?.closest('[role="treeitem"]');
    /**
     * @param {HTMLElement} item A rendered occurrence.
     * @returns {HTMLButtonElement|null} Its activation target, excluding descendant rows.
     */
    const row = item => item.querySelector(':scope > .mb-project-browser-node__line > .mb-project-browser-node__row');
    /**
     * @param {HTMLElement} item A rendered occurrence.
     * @returns {HTMLButtonElement|null} Its disclosure control, absent when expansion is unavailable.
     */
    const disclosure = item => item.querySelector(':scope > .mb-project-browser-node__line > .mb-project-browser-node__disclosure');
    let active = null;
    let hasFocus = false;
    let ancestors = [];
    let prefix = "";
    let lastTyped = 0;
    const pendingDisclosures = new WeakMap();

    /**
     * Requests a Blazor toggle once for the current button and expansion state.
     * The observer clears the pending request after expansion changes; a replaced button starts fresh.
     * @param {HTMLElement} item The occurrence to expand or collapse without selecting it.
     * @returns {void}
     */
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

    /**
     * Sets the single Tab entry point and records ancestors for recovery after removal.
     * This never changes canonical selection, even when DOM focus moves.
     * @param {HTMLElement|null|undefined} item The next rendered focus occurrence, if any.
     * @param {boolean} [moveFocus=false] Whether to focus and reveal the occurrence immediately.
     * @returns {void}
     */
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
            revealRow(root, item);
        }
    }

    /**
     * Retains the Tab return point after renders or recovers a surviving ancestor/first row.
     * External selection can change the entry point only while focus is outside the tree.
     * @param {boolean} [selectionChanged=false] Whether a render changed an aria-selected attribute.
     * @returns {void}
     */
    function repair(selectionChanged = false) {
        const visible = items();
        const focused = hasFocus || root.contains(document.activeElement);
        if (active && root.contains(active) && (focused || !selectionChanged)) {
            setActive(active);
            if (focused) {
                revealRow(root, active);
            }
            return;
        }
        const target = focused
            ? ancestors.find(node => root.contains(node)) ?? visible[0]
            : visible.find(node => node.getAttribute("aria-selected") === "true") ?? visible[0];
        setActive(target, focused);
    }

    /**
     * Adopts keyboard or restored focus as this tree's visible Tab return point.
     * @param {FocusEvent} event The bubbling focus-in event.
     * @returns {void}
     */
    function onFocus(event) {
        hasFocus = true;
        const item = event.target.closest('[role="treeitem"]');
        if (item && root.contains(item)) {
            setActive(item);
            revealRow(root, item);
        }
    }

    /**
     * Aligns focus with pointer activation while leaving selection to the Blazor handler.
     * @param {MouseEvent} event The bubbling row or disclosure click.
     * @returns {void}
     */
    function onClick(event) {
        const item = event.target.closest('[role="treeitem"]');
        if (item && root.contains(item)) {
            setActive(item, true);
        }
    }

    /**
     * Distinguishes a normal Tab exit from focus lost while Blazor removes a row.
     * A deferred connectivity check lets render recovery retain ownership without stealing an exit.
     * @param {FocusEvent} event The bubbling focus-out event.
     * @returns {void}
     */
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

    /**
     * Navigates rendered occurrences, requests disclosure or activates through existing Blazor callbacks.
     * Tab remains native; arrows and type-ahead move focus without selecting a model element.
     * @param {KeyboardEvent} event The key event originating within this tree.
     * @returns {void}
     */
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

    const observer = new MutationObserver(
        /**
         * Acknowledges completed disclosure renders and repairs focus after DOM/selection changes.
         * @param {MutationRecord[]} records The observed changes within this root.
         * @returns {void}
         */
        records => {
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
    trees.set(root,
        /**
         * Disconnects this registration's observer and listeners without affecting other trees.
         * @returns {void}
         */
        () => {
            observer.disconnect();
            root.removeEventListener("keydown", onKey);
            root.removeEventListener("focusin", onFocus);
            root.removeEventListener("focusout", onFocusOut);
            root.removeEventListener("click", onClick);
        });
    repair();
}

/**
 * Releases a root's registration; repeated disposal is harmless and permits later registration.
 * @param {HTMLElement} root The tree whose listeners and observer should be removed.
 * @returns {void}
 */
export function disposeTree(root) {
    trees.get(root)?.();
    trees.delete(root);
}

/**
 * Scrolls the inspected row within this Browser without changing the keyboard focus.
 * @param {HTMLElement|null} root The Browser tree whose inspected row may scroll into view.
 * @returns {void}
 */
export function scrollInspectionIntoView(root) {
    revealRow(root, root?.querySelector('[data-inspected="true"]'));
}
