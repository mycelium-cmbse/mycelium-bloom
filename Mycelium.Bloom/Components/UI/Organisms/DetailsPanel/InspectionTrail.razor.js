const observers = new WeakMap();

/**
 * Measures the full root/overflow/current header without stretching the visible steps.
 * @param {HTMLElement|null} root The component-local trail whose names are measured.
 * @param {{invokeMethodAsync: Function}} callback The owning component's width callback.
 * @returns {void}
 */
export function observeTrail(root, callback) {
    if (!root) {
        return;
    }
    const existing = observers.get(root);
    if (existing) {
        existing.report();
        return;
    }
    const header = root.closest('.mb-details-panel__header');
    if (!header) {
        return;
    }
    let previous;
    let active = true;
    /**
     * @param {string} value A computed CSS dimension.
     * @returns {number} Its pixel value, or zero.
     */
    const pixels = value => Number.parseFloat(value) || 0;
    /**
     * @param {HTMLElement} element The measured header child.
     * @returns {number} Its width including margins.
     */
    const outerWidth = element => {
        const style = getComputedStyle(element);
        return element.getBoundingClientRect().width + pixels(style.marginLeft) + pixels(style.marginRight);
    };
    /** @returns {void} Reports only a changed bounded minimum while this observer remains owned. */
    const report = () => {
        if (!active || !root.isConnected) {
            return;
        }
        const list = root.querySelector('ol');
        if (!list) {
            return;
        }
        const items = [...list.children];
        const trailWidth = items.reduce((width, item) => {
            const label = item.querySelector('span');
            return width + outerWidth(item) + (label ? Math.max(0, label.scrollWidth - label.clientWidth) : 0);
        }, 0) + Math.max(0, items.length - 1) * pixels(getComputedStyle(list).columnGap);
        const style = getComputedStyle(header);
        const siblings = [...header.children].filter(element => {
            const computed = getComputedStyle(element);
            return element !== root && computed.position !== 'absolute' && computed.display !== 'none';
        });
        // The Copy action's automatic margin is spare space, not required content width.
        const fixedWidth = siblings.reduce((width, element) => width +
            (element.classList.contains('mb-properties-copy') ? element.getBoundingClientRect().width : outerWidth(element)), 0);
        const width = Math.max(380, Math.min(600, Math.ceil(trailWidth + fixedWidth +
            siblings.length * pixels(style.columnGap) + pixels(style.paddingLeft) + pixels(style.paddingRight))));
        if (width !== previous) {
            previous = width;
            // A queued observer report can outlive the receiver during circuit/component disposal.
            callback.invokeMethodAsync('SetRequiredWidth', width).catch(() => {});
        }
    };
    const observer = new ResizeObserver(report);
    observers.set(root, { report, disconnect() { active = false; observer.disconnect(); } });
    observer.observe(root);
    observer.observe(header);
    document.fonts?.ready.then(report);
    report();
}

/**
 * Releases only this panel's measurement observer.
 * @param {HTMLElement|null} root The registered component-local trail root.
 * @returns {void}
 */
export function stopObserving(root) {
    observers.get(root)?.disconnect();
    observers.delete(root);
}
