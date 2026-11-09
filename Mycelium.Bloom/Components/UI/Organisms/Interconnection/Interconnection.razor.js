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

const properties = ["fill", "fill-opacity", "stroke", "stroke-width", "stroke-opacity",
    "stroke-dasharray", "stroke-linecap", "stroke-linejoin", "font-family", "font-size",
    "font-weight", "font-style", "letter-spacing", "text-anchor", "dominant-baseline", "opacity"];

/**
 * Measures mounted SVG bounds independently of the canvas viewport.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {number[]} Scene x, y, width, height and viewport width, height.
 */
export function measureScene(root) {
    const canvas = root.querySelector(".diagram-canvas");
    const bounds = root.querySelector(".diagram-svg-layer").getBBox();
    return [bounds.x, bounds.y, bounds.width, bounds.height, canvas.clientWidth, canvas.clientHeight];
}

/**
 * Serializes mounted SVG content with resolved styles for internal diagnostic export.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {string} Portable SVG; throws for missing or non-native content.
 */
export function exportSvg(root) {
    const source = root.querySelector(".diagram-svg-layer");
    if (!source) throw new Error("The diagram is not mounted.");
    if (source.querySelector("foreignObject")) throw new Error("Portable SVG requires native SVG content.");
    const clone = source.cloneNode(true);
    const originals = [source, ...source.querySelectorAll("*")];
    const copies = [clone, ...clone.querySelectorAll("*")];
    originals.forEach((element, index) => {
        const style = getComputedStyle(element);
        copies[index].removeAttribute("style");
        for (const property of properties) {
            copies[index].style.setProperty(property, style.getPropertyValue(property));
        }
    });
    clone.removeAttribute("class");
    clone.removeAttribute("style");
    clone.removeAttribute("transform");
    for (const element of clone.querySelectorAll(".diagram-link-vertex, .connection-svg__hit")) element.remove();
    const bounds = source.getBBox();
    const margin = 24;
    clone.setAttribute("viewBox", [bounds.x - margin, bounds.y - margin, bounds.width + margin * 2, bounds.height + margin * 2].join(" "));
    clone.setAttribute("width", String(Math.ceil(bounds.width + margin * 2)));
    clone.setAttribute("height", String(Math.ceil(bounds.height + margin * 2)));
    for (const element of [clone, ...clone.querySelectorAll("*")]) {
        for (const attribute of [...element.attributes]) {
            if (attribute.name.startsWith("on") || attribute.name.startsWith("data-") || attribute.name.startsWith("b-") || attribute.name.startsWith("_bl_")) {
                element.removeAttribute(attribute.name);
            }
        }
    }
    return new XMLSerializer().serializeToString(clone);
}

/**
 * Starts a diagnostic download and schedules release of its temporary object URL.
 * @param {string} name Download filename.
 * @param {string} type Content MIME type.
 * @param {string} content Serialized file content.
 * @returns {void}
 */
export function download(name, type, content) {
    const url = URL.createObjectURL(new Blob([content], { type }));
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = name;
    anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 0);
}

/**
 * Downloads mounted content for the internal diagnostic facility.
 * @param {HTMLElement} root Component-owned host element.
 * @returns {void}
 */
export function exportSvgFile(root) {
    download("interconnection.svg", "image/svg+xml", exportSvg(root));
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

/**
 * Resolves theme and scoped styles on detached full-state SVG without mounting it in the editor.
 * @param {HTMLElement} root Component-owned host element.
 * @param {string} content Detached renderer output.
 * @returns {string} Portable complete SVG; throws for invalid content or unresolved styles.
 */
export function completeSvg(root, content) {
    const document = new DOMParser().parseFromString(content, 'text/html');
    if (document.querySelector('parsererror, foreignObject, script')) throw new Error('Invalid portable diagram SVG.');
    const svg = document.querySelector('svg');
    if (!svg) throw new Error('Missing diagram SVG.');
    const theme = getComputedStyle(root);
    const resolve = value => {
        for (let depth = 0; depth < 20 && value.includes('var('); depth++) {
            value = value.replace(/var\((--[\w-]+)\)/g, (_, name) => theme.getPropertyValue(name).trim());
        }
        if (value.includes('var(')) throw new Error('Unresolved diagram export style.');
        return value;
    };
    const rules = [];
    const collect = sheet => {
        for (const rule of sheet.cssRules) {
            if (rule.selectorText && /\.(part-svg|connection-svg)/.test(rule.selectorText)) {
                rules.push(resolve(rule.cssText.replace(/\[b-[\w-]+\]/g, '')));
            } else if (rule.cssRules) collect(rule);
        }
    };
    for (const sheet of root.ownerDocument.styleSheets) collect(sheet);
    const style = document.createElementNS('http://www.w3.org/2000/svg', 'style');
    style.textContent = rules.join('\n');
    svg.prepend(style);
    svg.style.background = resolve(theme.getPropertyValue('--background'));
    for (const element of svg.querySelectorAll('.diagram-link-vertex, .connection-svg__hit')) element.remove();
    for (const element of [svg, ...svg.querySelectorAll('*')]) {
        for (const attribute of [...element.attributes]) {
            if (/^(on|data-|b-|_bl_)/.test(attribute.name)) element.removeAttribute(attribute.name);
        }
    }
    return new XMLSerializer().serializeToString(svg);
}

/**
 * Downloads detached full-state SVG through the internal diagnostic facility.
 * @param {HTMLElement} root Component-owned host element.
 * @param {string} content Detached renderer output.
 * @returns {void}
 */
export function exportCompleteSvg(root, content) {
    download('interconnection.svg', 'image/svg+xml', completeSvg(root, content));
}
