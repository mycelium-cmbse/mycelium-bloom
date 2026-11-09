// ------------------------------------------------------------------------------------------------
// <copyright file="Interconnection.razor.js" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

const mounts = new WeakMap();

/**
 * Installs pointer handlers until the matching unmount call.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {void}
 */
export function mount(root) {
    if (mounts.has(root)) return;
    const captures = new Map();
    const down = event => {
        const canvas = event.target.closest(".diagram-canvas");
        if (canvas && event.button === 0) {
            canvas.setPointerCapture(event.pointerId);
            captures.set(event.pointerId, canvas);
        }
    };
    const up = event => captures.delete(event.pointerId);
    const cancel = event => {
        const canvas = event.target.closest(".diagram-canvas");
        if (canvas) canvas.dispatchEvent(new PointerEvent("pointerup", {
            bubbles: true, pointerId: event.pointerId, clientX: event.clientX, clientY: event.clientY
        }));
    };
    const outside = event => {
        if (!captures.has(event.pointerId) || root.contains(event.target)) return;
        const canvas = root.querySelector(".diagram-canvas");
        if (canvas) canvas.dispatchEvent(new PointerEvent(event.type === "pointercancel" ? "pointerup" : event.type, {
            bubbles: true, pointerId: event.pointerId, clientX: event.clientX, clientY: event.clientY,
            button: event.button, buttons: event.buttons, pointerType: event.pointerType,
            ctrlKey: event.ctrlKey, shiftKey: event.shiftKey, altKey: event.altKey, metaKey: event.metaKey
        }));
        if (event.type !== "pointermove") captures.delete(event.pointerId);
    };
    root.addEventListener("pointerdown", down, true);
    root.addEventListener("pointercancel", cancel, true);
    root.addEventListener("pointerup", up, true);
    for (const type of ["pointermove", "pointerup", "pointercancel"]) root.ownerDocument.addEventListener(type, outside, true);
    mounts.set(root, { down, cancel, up, captures, outside });
}

/**
 * Releases this host’s handlers and any retained pointer captures; repeated calls are safe.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {void}
 */
export function unmount(root) {
    const owned = mounts.get(root);
    if (!owned) return;
    root.removeEventListener("pointerdown", owned.down, true);
    root.removeEventListener("pointercancel", owned.cancel, true);
    root.removeEventListener("pointerup", owned.up, true);
    for (const type of ["pointermove", "pointerup", "pointercancel"]) root.ownerDocument.removeEventListener(type, owned.outside, true);
    for (const [id, canvas] of owned.captures) {
        if (canvas.hasPointerCapture(id)) canvas.releasePointerCapture(id);
    }
    mounts.delete(root);
}

/**
 * Focuses a mounted occurrence without scrolling or changing the viewport.
 * @param {HTMLElement} root Component-owned host element.
 * @param {string} id Graphical occurrence identity.
 * @returns {boolean} Whether the host and occurrence were mounted.
 */
export function focusOccurrence(root, id) {
    if (!mounts.has(root)) return false;
    const node = [...root.querySelectorAll('[data-node-id], [data-group-id]')]
        .find(element => element.dataset.nodeId === id || element.dataset.groupId === id);
    if (node) {
        node.setAttribute('tabindex', '-1');
        node.focus({ preventScroll: true });
        return true;
    }
    return false;
}
