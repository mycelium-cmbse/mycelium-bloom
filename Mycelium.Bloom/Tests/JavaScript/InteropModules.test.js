import assert from "node:assert/strict";
import { afterEach, before, beforeEach, test } from "node:test";

import { JSDOM } from "jsdom";
import { create as createWorkspaceShell } from "../../Components/UI/Organisms/WorkspaceShell/WorkspaceShell.razor.js";

import {
    clearSearchInputValue,
    disposeEmptySpaceGuard,
    registerEmptySpaceGuard
} from "../../Components/UI/Organisms/ProjectBrowser/ProjectBrowserSearchAssistant.razor.js";
import {
    capturePointer,
    focusElementById,
    registerKeydownGuards,
    releasePointer,
    unregisterKeydownGuards
} from "../../Components/UI/Organisms/EditorWorkspace/EditorWorkspace.razor.js";

import { registerTree, disposeTree, scrollInspectionIntoView } from "../../Components/UI/Organisms/ProjectBrowser/ProjectBrowser.razor.js";

import { observeTrail, stopObserving } from "../../Components/UI/Organisms/DetailsPanel/InspectionTrail.razor.js";

let dom;
let animationFrameQueue;
let nextAnimationFrameId;
let initializeErrorRecovery;

const emptySpaceInputIds = new Set();
const workspaceGuardIds = new Set();
const originalAbortController = globalThis.AbortController;

before(async () => {
    const bootstrap = new JSDOM("<!doctype html><html><body></body></html>");
    globalThis.document = bootstrap.window.document;
    globalThis.window = bootstrap.window;
    ({ initializeErrorRecovery } = await import("../../Components/UI/Molecules/ApplicationErrorState/ApplicationErrorState.razor.js"));
    bootstrap.window.close();
    delete globalThis.document;
    delete globalThis.window;
});

beforeEach(() => {
    dom = new JSDOM("<!doctype html><html><body></body></html>", {
        pretendToBeVisual: true,
        url: "https://bloom.test/"
    });
    animationFrameQueue = [];
    nextAnimationFrameId = 1;

    globalThis.window = dom.window;
    globalThis.document = dom.window.document;
    globalThis.Element = dom.window.Element;
    globalThis.HTMLElement = dom.window.HTMLElement;
    globalThis.HTMLInputElement = dom.window.HTMLInputElement;
    globalThis.InputEvent = dom.window.InputEvent;
    globalThis.MutationObserver = dom.window.MutationObserver;
    globalThis.getComputedStyle = dom.window.getComputedStyle.bind(dom.window);
    globalThis.requestAnimationFrame = callback => {
        animationFrameQueue.push(callback);
        return nextAnimationFrameId++;
    };
});

afterEach(() => {
    for (const inputId of emptySpaceInputIds) {
        disposeEmptySpaceGuard(inputId);
    }

    for (const workspaceId of workspaceGuardIds) {
        unregisterKeydownGuards(workspaceId);
    }

    emptySpaceInputIds.clear();
    workspaceGuardIds.clear();
    dom.window.close();

    delete globalThis.window;
    delete globalThis.document;
    delete globalThis.Element;
    delete globalThis.HTMLElement;
    delete globalThis.HTMLInputElement;
    delete globalThis.InputEvent;
    delete globalThis.MutationObserver;
    delete globalThis.getComputedStyle;
    delete globalThis.requestAnimationFrame;
    delete globalThis.ResizeObserver;
    globalThis.AbortController = originalAbortController;
});

function createResizableShell({ deferRender = false } = {}) {
    document.body.innerHTML = `<section><div class="mb-workspace-shell__body" style="display:grid">
        <aside class="mb-workspace-shell__left-panel"></aside>
        <div class="mb-workspace-shell__main" tabindex="-1" data-narrow-active="true"></div>
        <div class="mb-workspace-shell__right-splitter" tabindex="0"></div>
        <aside class="mb-workspace-shell__right-panel" tabindex="-1"></aside>
    </div></section>`;
    globalThis.AbortController = window.AbortController;
    let observer;
    globalThis.ResizeObserver = class {
        constructor(callback) { this.callback = callback; observer = this; }
        observe() {}
        unobserve() {}
        disconnect() { this.disconnected = true; }
    };
    const root = document.querySelector('section');
    const body = root.firstElementChild;
    const handle = root.querySelector('.mb-workspace-shell__right-splitter');
    const panel = root.querySelector('.mb-workspace-shell__right-panel');
    let available = 1000;
    Object.defineProperty(body, 'clientWidth', { get: () => available });
    root.querySelector('.mb-workspace-shell__left-panel').getBoundingClientRect = () => ({ width: 56 });
    handle.getBoundingClientRect = () => ({ width: 4 });
    panel.getBoundingClientRect = () => ({ width: Number.parseFloat(body.style.getPropertyValue('--workspace-shell-effective-right-width')) || 380 });
    const captures = new Set();
    handle.setPointerCapture = id => captures.add(id);
    handle.hasPointerCapture = id => captures.has(id);
    handle.releasePointerCapture = id => captures.delete(id);
    const changes = [];
    let complete;
    const committed = new Promise(resolve => { complete = resolve; });
    const interaction = createWorkspaceShell(root, {
        async invokeMethodAsync(method, width) {
            assert.equal(method, 'CommitRightPanelWidth');
            changes.push(width);
            if (!deferRender) interaction.update(width, 260, 600, 320, null);
            complete();
        }
    });
    interaction.update(380, 260, 600, 320, null);
    return { body, handle, captures, changes, committed, observer, interaction,
        resize(width) { available = width; observer.callback(); },
        pointer(type, x) {
            const event = new window.Event(type, { bubbles: true, cancelable: true });
            Object.assign(event, { button: 0, isPrimary: true, pointerId: 17, clientX: x });
            handle.dispatchEvent(event);
        }
    };
}

test('workspace right splitter bounds geometry without overwriting the preferred width', () => {
    const shell = createResizableShell();
    try {
        shell.interaction.update(600, 260, 600, 320, null);
        shell.resize(800);
        assert.equal(shell.handle.getAttribute('aria-valuemax'), '420');
        assert.equal(shell.handle.getAttribute('aria-valuenow'), '420');
        assert.deepEqual(shell.changes, []);
        shell.resize(1200);
        assert.equal(shell.handle.getAttribute('aria-valuenow'), '600');
    } finally { shell.interaction.dispose(); }
    assert.equal(shell.observer.disconnected, true);
});

test('workspace right splitter commits captured pointer resizing and releases capture', async () => {
    const shell = createResizableShell();
    try {
        shell.pointer('pointerdown', 600);
        assert.equal(shell.handle.dataset.resizing, 'true');
        shell.pointer('pointermove', 500);
        assert.equal(shell.handle.getAttribute('aria-valuenow'), '480');
        assert.deepEqual(shell.changes, []);
        shell.pointer('pointerup', 500);
        await shell.committed;
        assert.deepEqual(shell.changes, [480]);
        assert.equal(shell.captures.size, 0);
        assert.equal(shell.handle.dataset.resizing, undefined);
    } finally { shell.interaction.dispose(); }
});

test('workspace right splitter cancels drag and ignores compact or disposed keyboard interaction', () => {
    const shell = createResizableShell();
    shell.pointer('pointerdown', 600);
    shell.pointer('pointermove', 400);
    shell.pointer('pointercancel', 400);
    assert.equal(shell.handle.dataset.resizing, undefined);
    assert.equal(shell.handle.getAttribute('aria-valuenow'), '380');
    assert.equal(shell.captures.size, 0);
    shell.body.style.display = 'block';
    shell.handle.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'End', bubbles: true }));
    assert.deepEqual(shell.changes, []);
    shell.interaction.dispose();
    shell.body.style.display = 'grid';
    shell.pointer('pointerdown', 600);
    assert.equal(shell.captures.size, 0);
});

test('workspace right splitter clears drag highlighting on lost capture and disposal', () => {
    const shell = createResizableShell();
    shell.pointer('pointerdown', 600);
    shell.pointer('pointermove', 500);
    shell.pointer('lostpointercapture', 500);
    assert.equal(shell.handle.dataset.resizing, undefined);
    assert.equal(shell.handle.getAttribute('aria-valuenow'), '380');
    assert.deepEqual(shell.changes, []);
    shell.pointer('pointerdown', 600);
    assert.equal(shell.handle.dataset.resizing, 'true');
    shell.interaction.dispose();
    assert.equal(shell.handle.dataset.resizing, undefined);
    assert.equal(shell.captures.size, 0);
});

test('workspace right splitter keyboard steps increase right width toward the left', async () => {
    const shell = createResizableShell();
    try {
        const event = new window.KeyboardEvent('keydown', { key: 'ArrowLeft', shiftKey: true, bubbles: true, cancelable: true });
        shell.handle.dispatchEvent(event);
        await shell.committed;
        assert.equal(event.defaultPrevented, true);
        assert.deepEqual(shell.changes, [430]);
    } finally { shell.interaction.dispose(); }
});

test('workspace right splitter retains rapid keyboard intent across delayed and stale renders', async () => {
    const shell = createResizableShell({ deferRender: true });
    try {
        shell.handle.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'ArrowLeft', shiftKey: true, bubbles: true }));
        await shell.committed;
        await new Promise(resolve => setImmediate(resolve));
        assert.equal(shell.handle.getAttribute('aria-valuenow'), '430');
        shell.interaction.update(380, 260, 600, 320, null);
        shell.body.style.removeProperty('--workspace-shell-effective-right-width');
        shell.handle.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
        await new Promise(resolve => setImmediate(resolve));
        assert.deepEqual(shell.changes, [430, 440]);
        shell.interaction.update(430, 260, 600, 320, null);
        assert.equal(shell.handle.getAttribute('aria-valuenow'), '440');
        shell.interaction.update(440, 260, 600, 320, null);
        shell.body.style.removeProperty('--workspace-shell-effective-right-width');
        shell.handle.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
        await new Promise(resolve => setImmediate(resolve));
        assert.deepEqual(shell.changes, [430, 440, 450]);
        shell.interaction.update(450, 260, 600, 320, null);
        shell.interaction.update(400, 260, 600, 320, null);
        assert.equal(shell.handle.getAttribute('aria-valuenow'), '400');
    } finally { shell.interaction.dispose(); }
});

test("error recovery bootstrap leaves ordinary pages unchanged", async () => {
    await initializeErrorRecovery(document, window);

    assert.equal(document.documentElement.className, "");
    assert.equal(document.body.childElementCount, 0);
});

test("error recovery handles controls inserted after initial page load", async () => {
    let backCalls = 0;
    await initializeErrorRecovery(document, {
        Element,
        history: { back: () => backCalls++ }
    });

    document.body.innerHTML = `
        <section class="application-error-state">
            <button type="button" data-error-action="back">Go back</button>
        </section>`;
    document.querySelector("button").click();

    assert.equal(backCalls, 1);
});

test("error recovery handles nested button content without a Blazor circuit", async () => {
    let backCalls = 0;
    let reloadCalls = 0;
    document.body.innerHTML = `
        <div class="application-error-state">
            <button type="button" data-error-action="back"><span>Go back</span></button>
            <button type="button" data-error-action="reload"><span>Reload</span></button>
            <button type="button" data-error-action="unknown">Unrelated</button>
        </div>
        <button data-error-action="reload">Outside the error state</button>`;

    await initializeErrorRecovery(document, {
        Element,
        history: { back: () => backCalls++ },
        location: { reload: () => reloadCalls++ }
    }, async () => ({ loadTheme: () => null }));

    document.querySelector('[data-error-action="back"] span').click();
    document.querySelector('[data-error-action="reload"] span').click();
    document.querySelector('[data-error-action="unknown"]').click();
    document.body.lastElementChild.click();
    document.dispatchEvent(new dom.window.Event("click"));

    assert.equal(backCalls, 1);
    assert.equal(reloadCalls, 1);
});

for (const isDarkMode of [false, true]) {
    test(`static error recovery restores the Blueprint ${isDarkMode ? "dark" : "light"} preference`, async () => {
        document.body.innerHTML = '<div class="application-error-state"></div>';
        const applied = [];

        await initializeErrorRecovery(document, window, async () => ({
            loadTheme: () => ({ isDarkMode }),
            applyDarkMode: value => applied.push(value)
        }));

        assert.deepEqual(applied, [isDarkMode]);
    });
}

test("error recovery does not load theme tooling on ordinary pages", async () => {
    let themeLoads = 0;

    await initializeErrorRecovery(document, window, async () => themeLoads++);
    document.body.click();

    assert.equal(themeLoads, 0);
});

test("search input synchronization clears stale text and emits one input mutation", () => {
    document.body.innerHTML = "<input id='search' value='stale query'>";
    const input = document.getElementById("search");
    const inputEvents = [];
    input.addEventListener("input", event => inputEvents.push({
        bubbles: event.bubbles,
        inputType: event.inputType
    }));

    clearSearchInputValue(input);
    clearSearchInputValue(document.body);

    assert.equal(input.value, "");
    assert.deepEqual(inputEvents, [{
        bubbles: true,
        inputType: "deleteContentBackward"
    }]);
});

test("empty search Space guard prevents only an unmodified leading Space and detaches independently", () => {
    document.body.innerHTML = `
        <input id="first-search">
        <input id="second-search">
    `;
    const firstInput = document.getElementById("first-search");
    const secondInput = document.getElementById("second-search");

    registerEmptySpace("first-search");

    assert.equal(dispatchKey(firstInput, " ").defaultPrevented, true);
    assert.equal(dispatchKey(secondInput, " ").defaultPrevented, false);
    assert.equal(dispatchKey(firstInput, " ", { ctrlKey: true }).defaultPrevented, false);
    assert.equal(dispatchKey(firstInput, "a").defaultPrevented, false);

    firstInput.value = "multi word";
    assert.equal(dispatchKey(firstInput, " ").defaultPrevented, false);

    firstInput.value = "";
    registerEmptySpace("second-search");
    disposeEmptySpace("first-search");

    assert.equal(dispatchKey(firstInput, " ").defaultPrevented, false);
    assert.equal(dispatchKey(secondInput, " ").defaultPrevented, true);

    disposeEmptySpace("second-search");

    assert.equal(dispatchKey(secondInput, " ").defaultPrevented, false);
});

test("editor workspace pointer capture returns the measured adjacent pair and releases ownership", () => {
    document.body.innerHTML = `
        <section id="left-group"></section>
        <div id="splitter"
             data-left-group-element-id="left-group"
             data-right-group-element-id="right-group"></div>
        <section id="right-group"></section>
    `;
    const leftGroup = document.getElementById("left-group");
    const rightGroup = document.getElementById("right-group");
    const splitter = document.getElementById("splitter");
    const capturedPointerIds = [];
    const releasedPointerIds = [];

    leftGroup.getBoundingClientRect = () => ({ width: 300 });
    rightGroup.getBoundingClientRect = () => ({ width: 320 });
    splitter.setPointerCapture = pointerId => capturedPointerIds.push(pointerId);
    splitter.hasPointerCapture = pointerId => capturedPointerIds.includes(pointerId);
    splitter.releasePointerCapture = pointerId => releasedPointerIds.push(pointerId);

    const measurement = capturePointer("splitter", 17);
    releasePointer("splitter", 17);

    assert.deepEqual(measurement, [300, 320, 620]);
    assert.deepEqual(capturedPointerIds, [17]);
    assert.deepEqual(releasedPointerIds, [17]);
});

test("editor workspace pointer helpers ignore unavailable or unusable elements", () => {
    document.body.innerHTML = `
        <section id="left-group"></section>
        <div id="splitter"
             data-left-group-element-id="left-group"
             data-right-group-element-id="right-group"></div>
        <section id="right-group"></section>
    `;
    const leftGroup = document.getElementById("left-group");
    const rightGroup = document.getElementById("right-group");
    const splitter = document.getElementById("splitter");
    let captureCount = 0;
    let releaseCount = 0;

    leftGroup.getBoundingClientRect = () => ({ width: 0 });
    rightGroup.getBoundingClientRect = () => ({ width: 0 });
    splitter.setPointerCapture = () => captureCount++;
    splitter.hasPointerCapture = () => false;
    splitter.releasePointerCapture = () => releaseCount++;

    assert.equal(capturePointer("missing-splitter", 1), null);
    assert.equal(capturePointer("splitter", 1), null);
    releasePointer("missing-splitter", 1);
    releasePointer("splitter", 1);
    assert.equal(captureCount, 0);
    assert.equal(releaseCount, 0);
});

test("editor workspace pointer capture tolerates a pointer released during the server round trip", () => {
    document.body.innerHTML = `
        <section id="left-group"></section>
        <div id="splitter"
             data-left-group-element-id="left-group"
             data-right-group-element-id="right-group"></div>
        <section id="right-group"></section>
    `;
    const leftGroup = document.getElementById("left-group");
    const rightGroup = document.getElementById("right-group");
    const splitter = document.getElementById("splitter");

    leftGroup.getBoundingClientRect = () => ({ width: 300 });
    rightGroup.getBoundingClientRect = () => ({ width: 320 });
    splitter.setPointerCapture = () => {
        throw new DOMException("The pointer is no longer active.", "NotFoundError");
    };

    assert.equal(capturePointer("splitter", 17), null);
});

test("editor workspace focus targets only the requested live element", () => {
    document.body.innerHTML = '<button id="workspace-tab">Tab</button>';
    const tab = document.getElementById("workspace-tab");
    const nativeFocus = tab.focus.bind(tab);
    let focusOptions;

    tab.focus = options => {
        focusOptions = options;
        nativeFocus();
    };

    assert.equal(focusElementById("missing-tab"), false);
    assert.equal(focusElementById("workspace-tab"), true);
    assert.equal(document.activeElement, tab);
    assert.deepEqual(focusOptions, { preventScroll: true });
});

test("editor workspace key guards stay root-scoped and detach cleanly", () => {
    document.body.innerHTML = `
        <section id="workspace">
            <button id="tab" role="tab">Tab</button>
            <div id="splitter" role="separator" tabindex="0"></div>
            <input id="input">
        </section>
        <button id="external-tab" role="tab">External tab</button>
    `;
    const workspace = document.getElementById("workspace");
    const tab = document.getElementById("tab");
    const splitter = document.getElementById("splitter");
    const input = document.getElementById("input");
    const externalTab = document.getElementById("external-tab");
    const nativeDocumentAddEventListener = document.addEventListener.bind(document);
    let documentListenerCount = 0;

    document.addEventListener = (...args) => {
        documentListenerCount++;
        nativeDocumentAddEventListener(...args);
    };

    registerWorkspaceGuards("workspace");

    assert.equal(dispatchKey(tab, "ArrowRight").defaultPrevented, true);
    assert.equal(dispatchKey(tab, "Home").defaultPrevented, true);
    assert.equal(dispatchKey(tab, "Delete").defaultPrevented, false);
    assert.equal(dispatchKey(splitter, "ArrowLeft").defaultPrevented, true);
    assert.equal(dispatchKey(input, "ArrowRight").defaultPrevented, false);
    assert.equal(dispatchKey(externalTab, "ArrowRight").defaultPrevented, false);
    assert.equal(documentListenerCount, 1); // The Add tab fallback follows the owned trigger into its portal.

    unregisterWorkspaceGuards("workspace");

    assert.equal(dispatchKey(tab, "ArrowRight").defaultPrevented, false);
    assert.equal(dispatchKey(splitter, "ArrowLeft").defaultPrevented, false);

    assert.equal(registerKeydownGuards("missing-workspace"), false);
});

test("Add tab opening fallback preserves End and activation, skips disabled items and detaches", () => {
    createAddTabMenu();
    registerWorkspaceGuards("workspace");
    registerWorkspaceGuards("workspace");
    const menu = document.getElementById("add-menu");
    const last = document.getElementById("browser-action");
    let selections = 0;
    last.addEventListener("click", () => selections++);
    menu.focus();
    assert.equal(dispatchKey(menu, "End").defaultPrevented, true);
    assert.equal(document.activeElement, last);
    assert.equal(dispatchKey(last, "Enter").defaultPrevented, true);
    assert.equal(selections, 1);
    assert.equal(dispatchKey(last, " ").defaultPrevented, true);
    assert.equal(selections, 2);
    unregisterWorkspaceGuards("workspace");
    menu.focus();
    assert.equal(dispatchKey(menu, "End").defaultPrevented, false);
    assert.equal(document.activeElement, menu);
    assert.equal(dispatchKey(last, "Enter").defaultPrevented, false);
    assert.equal(selections, 2);
});

test("Add tab fallback yields to Blueprint and excludes unrelated, stale and closed menus", () => {
    createAddTabMenu();
    registerWorkspaceGuards("workspace");
    const menu = document.getElementById("add-menu");
    const trigger = document.getElementById("add-trigger");
    const first = document.getElementById("empty-action");
    menu.focus();
    menu.addEventListener("keydown", event => { event.preventDefault(); first.focus(); }, { once: true });
    dispatchKey(menu, "End");
    assert.equal(document.activeElement, first);
    let selections = 0;
    first.addEventListener("click", () => selections++);
    menu.addEventListener("keydown", event => { event.preventDefault(); first.click(); }, { once: true });
    dispatchKey(first, "Enter");
    assert.equal(selections, 1);
    trigger.setAttribute("aria-controls", "different-menu");
    assert.equal(dispatchKey(menu, "End").defaultPrevented, false);
    trigger.setAttribute("aria-controls", "add-menu");
    trigger.setAttribute("aria-expanded", "false");
    assert.equal(dispatchKey(menu, "End").defaultPrevented, false);
    trigger.setAttribute("aria-expanded", "true");
    trigger.parentElement.removeAttribute("data-testid");
    assert.equal(dispatchKey(menu, "End").defaultPrevented, false);
    trigger.parentElement.setAttribute("data-testid", "editor-workspace-add-tab-host");
    assert.equal(dispatchKey(menu, "End", { ctrlKey: true }).defaultPrevented, false);
    assert.equal(dispatchKey(document.getElementById("disabled-action"), "Enter").defaultPrevented, false);
    document.body.append(trigger.parentElement);
    assert.equal(dispatchKey(menu, "End").defaultPrevented, false);
});

function createAddTabMenu() {
    document.body.innerHTML = `
        <section id="workspace"><div data-testid="editor-workspace-add-tab-host">
            <button id="add-trigger" aria-controls="add-menu" aria-expanded="true">Add tab</button>
        </div></section>
        <div id="add-menu" role="menu" aria-labelledby="add-trigger" tabindex="-1">
            <div id="empty-action" role="menuitem" tabindex="-1">Empty editor</div>
            <div id="browser-action" role="menuitem" tabindex="-1">Project Browser</div>
            <div id="disabled-action" role="menuitem" aria-disabled="true" tabindex="-1">Unavailable</div>
        </div>
    `;
}

function registerEmptySpace(inputId) {
    emptySpaceInputIds.add(inputId);
    registerEmptySpaceGuard(inputId);
}

function disposeEmptySpace(inputId) {
    disposeEmptySpaceGuard(inputId);
    emptySpaceInputIds.delete(inputId);
}

function registerWorkspaceGuards(workspaceId) {
    workspaceGuardIds.add(workspaceId);
    return registerKeydownGuards(workspaceId);
}

function unregisterWorkspaceGuards(workspaceId) {
    unregisterKeydownGuards(workspaceId);
    workspaceGuardIds.delete(workspaceId);
}

function createKeyEvent(key, options = {}) {
    return new dom.window.KeyboardEvent("keydown", {
        bubbles: true,
        cancelable: true,
        key,
        ...options
    });
}

function dispatchKey(target, key, options = {}) {
    const event = createKeyEvent(key, options);
    target.dispatchEvent(event);
    return event;
}

function trackInputFocus(input) {
    const nativeFocus = input.focus.bind(input);
    const state = { count: 0, options: undefined };

    input.focus = options => {
        state.count++;
        state.options = options;
        nativeFocus();
    };

    return state;
}

function markVisible(element) {
    element.getBoundingClientRect = () => ({
        bottom: 20,
        height: 20,
        left: 0,
        right: 100,
        top: 0,
        width: 100,
        x: 0,
        y: 0,
        toJSON: () => ({})
    });
}

function flushAnimationFrames() {
    while (animationFrameQueue.length > 0) {
        const callbacks = animationFrameQueue.splice(0);

        for (const callback of callbacks) {
            callback(0);
        }
    }
}

async function settleMutationObserver() {
    await new Promise(resolve => setTimeout(resolve, 0));
}

function createBrowserTree() {
    const root = document.createElement("div");
    root.setAttribute("role", "tree");

    function item(name, children = "") {
        return `<div role="treeitem" tabindex="-1" aria-selected="false" ${children ? 'aria-expanded="true"' : ''}>
            <div class="mb-project-browser-node__line">${children ? '<button tabindex="-1" class="mb-project-browser-node__disclosure"></button>' : ''}<button tabindex="-1" class="mb-project-browser-node__row"><span class="mb-project-browser-node__title">${name}</span></button></div>${children ? `<div role="group">${children}</div>` : ''}</div>`;
    }

    root.innerHTML = item("Model", item("Alpha") + item("Beta")) + item("Connections");
    document.body.append(root);
    registerTree(root);

    return root;
}

function treeKey(key, options = {}) {
    const event = new window.KeyboardEvent("keydown", { key, bubbles: true, cancelable: true, ...options });
    document.activeElement.dispatchEvent(event);

    return event;
}

test("Browser tree moves focus independently of activation and has one tab stop", () => {
    const root = createBrowserTree();
    const [model, alpha, beta, group] = root.querySelectorAll('[role="treeitem"]');
    let activations = 0;
    root.addEventListener("click", event => {
        if (event.target.closest('.mb-project-browser-node__row')) {
            activations++;
        }
    });
    registerTree(root);
    model.focus();
    treeKey("ArrowRight");
    assert.equal(document.activeElement, alpha);

    treeKey("ArrowDown");
    assert.equal(document.activeElement, beta);

    treeKey("ArrowUp");
    assert.equal(document.activeElement, alpha);

    treeKey("ArrowLeft");
    assert.equal(document.activeElement, model);

    treeKey("End");
    assert.equal(document.activeElement, group);

    treeKey("ArrowDown");
    assert.equal(document.activeElement, group);

    treeKey("Home");
    assert.equal(document.activeElement, model);

    treeKey("ArrowUp");
    assert.equal(document.activeElement, model);
    assert.equal(activations, 0);

    treeKey("Enter");
    treeKey(" ");
    assert.equal(activations, 2);
    assert.equal(root.querySelectorAll('[role="treeitem"][tabindex="0"]').length, 1);
    assert.equal(treeKey("Tab").defaultPrevented, false);
    assert.equal(treeKey("ArrowDown", { ctrlKey: true }).defaultPrevented, false);
    assert.equal(treeKey("ArrowDown", { isComposing: true }).defaultPrevented, false);

    disposeTree(root);
    treeKey("ArrowDown");
    assert.equal(document.activeElement, model);

    disposeTree(root);
    registerTree(null);
});

test("Browser tree expands and collapses through disclosure and recovers removed focus", async () => {
    const root = createBrowserTree();
    const [model, alpha] = root.querySelectorAll('[role="treeitem"]');
    const children = model.querySelector('[role="group"]');
    model.querySelector('.mb-project-browser-node__disclosure').addEventListener("click", () => {
        const expanded = model.getAttribute("aria-expanded") === "true";
        model.setAttribute("aria-expanded", String(!expanded));
        if (expanded) {
            children.remove();
        } else {
            model.append(children);
        }
    });
    model.focus();
    treeKey("ArrowLeft");
    assert.equal(model.getAttribute("aria-expanded"), "false");

    treeKey("ArrowRight");
    assert.equal(model.getAttribute("aria-expanded"), "true");

    treeKey("ArrowRight");
    assert.equal(document.activeElement, alpha);

    children.remove();
    await Promise.resolve();
    assert.equal(document.activeElement, model);
    assert.equal(model.tabIndex, 0);

    disposeTree(root);
});

test("Browser tree type-ahead, selection changes and independent instances preserve local focus", async () => {
    const first = createBrowserTree(), second = createBrowserTree();
    const [model, alpha, beta, group] = first.querySelectorAll('[role="treeitem"]');
    model.focus();
    treeKey("b");
    assert.equal(document.activeElement, beta);

    treeKey("b");
    assert.equal(document.activeElement, beta);

    treeKey("Home");
    treeKey("c");
    treeKey("o");
    assert.equal(document.activeElement, group);

    treeKey("Home");
    const secondModel = second.querySelector('[role="treeitem"]');
    secondModel.focus();
    alpha.setAttribute("aria-selected", "true");
    await Promise.resolve();
    assert.equal(document.activeElement, secondModel);
    assert.equal(alpha.tabIndex, 0);
    assert.equal(secondModel.tabIndex, 0);

    beta.querySelector('.mb-project-browser-node__row').click();
    assert.equal(document.activeElement, beta);

    alpha.setAttribute("aria-selected", "false");
    group.setAttribute("aria-selected", "true");
    await Promise.resolve();
    assert.equal(document.activeElement, beta);
    assert.equal(beta.tabIndex, 0);

    disposeTree(first);
    disposeTree(second);
});

test("Browser arrow disclosure waits for the Blazor expansion render", async () => {
    const root = createBrowserTree();
    const model = root.querySelector('[role="treeitem"]');
    let toggles = 0;
    model.querySelector('.mb-project-browser-node__disclosure').addEventListener("click", () => toggles++);
    model.focus();
    treeKey("ArrowLeft");
    treeKey("ArrowLeft");
    assert.equal(toggles, 1, "repeated collapse must not enqueue an expansion");

    model.setAttribute("aria-expanded", "false");
    await Promise.resolve();
    treeKey("ArrowRight");
    treeKey("ArrowRight");
    assert.equal(toggles, 2, "repeated expansion must not enqueue a collapse");

    model.setAttribute("aria-expanded", "true");
    await Promise.resolve();
    treeKey("ArrowRight");
    assert.equal(document.activeElement, model.querySelector('[role="treeitem"]'));

    disposeTree(root);
});

test("Browser row reveal scrolls only its viewport and leaves visible rows stationary", () => {
    const root = createBrowserTree();
    const viewport = document.createElement("div");
    viewport.className = "mb-project-browser__tree-viewport";
    root.replaceWith(viewport);
    viewport.append(root);
    viewport.getBoundingClientRect = () => ({ top: 100 });
    Object.defineProperty(viewport, "clientHeight", { value: 100 });
    viewport.scrollTop = 400;
    const model = root.querySelector('[role="treeitem"]');
    model.scrollIntoView = () => assert.fail("scrolling the subtree can leave its heading above the viewport");
    const row = model.querySelector('.mb-project-browser-node__row');
    row.scrollIntoView = () => assert.fail("scrollIntoView may also move workspace ancestors");
    row.getBoundingClientRect = () => ({ top: 300 - viewport.scrollTop, bottom: 332 - viewport.scrollTop });
    model.focus();
    assert.equal(viewport.scrollTop, 200, "Tab/programmatic entry reveals a row above the viewport");

    viewport.scrollTop = 0;
    treeKey("Home");
    assert.equal(viewport.scrollTop, 132, "Home reveals a row below the viewport");

    treeKey("ArrowUp");
    assert.equal(viewport.scrollTop, 132, "a boundary key must not disturb a visible row");
    assert.equal(document.activeElement, model);

    disposeTree(root);
});

test("Browser late content renders preserve the Tab return point without stealing outside focus", async () => {
    const root = createBrowserTree();
    const [model, alpha] = root.querySelectorAll('[role="treeitem"]');
    model.setAttribute("aria-selected", "true");
    await Promise.resolve();
    alpha.focus();
    const outside = document.createElement("button");
    document.body.append(outside);
    outside.focus();
    model.querySelector('.mb-project-browser-node__title').textContent = "Updated model";
    await Promise.resolve();
    assert.equal(document.activeElement, outside);
    assert.equal(alpha.tabIndex, 0);
    assert.equal(model.tabIndex, -1);
    assert.equal(root.querySelectorAll('[tabindex="0"]').length, 1);

    disposeTree(root);
});

test("Browser disclosure guards survive unrelated renders but not replaced controls or disposal", async () => {
    const root = createBrowserTree();
    const model = root.querySelector('[role="treeitem"]');
    let toggles = 0;
    root.addEventListener("click", event => {
        if (event.target.matches('.mb-project-browser-node__disclosure')) {
            toggles++;
        }
    });
    model.focus();
    treeKey("ArrowLeft");
    model.setAttribute("aria-selected", "true");
    await Promise.resolve();
    treeKey("ArrowLeft");
    assert.equal(toggles, 1, "an unrelated render does not release an in-flight toggle");

    const button = model.querySelector('.mb-project-browser-node__disclosure');
    // Filtering can remove a disclosure while its effective aria-expanded remains true.
    button.replaceWith(button.cloneNode(true));
    await Promise.resolve();
    treeKey("ArrowLeft");
    assert.equal(toggles, 2, "a newly rendered disclosure must not inherit stale pending state");

    disposeTree(root);
    registerTree(root);
    treeKey("ArrowLeft");
    assert.equal(toggles, 3, "a fresh registration has no previous in-flight request");

    disposeTree(root);
});

test("Browser focus recovery distinguishes pre-removal blur from leaving the tree", async () => {
    const root = createBrowserTree();
    const [model, alpha] = root.querySelectorAll('[role="treeitem"]');
    alpha.focus();
    // Chromium emits focusout while the removed target is still connected.
    alpha.dispatchEvent(new window.FocusEvent("focusout", { bubbles: true, relatedTarget: null }));
    alpha.remove();
    await Promise.resolve();
    assert.equal(document.activeElement, model);

    const outside = document.createElement("button");
    document.body.append(outside);
    outside.focus();
    model.setAttribute("aria-expanded", "false");
    await Promise.resolve();
    assert.equal(document.activeElement, outside, "an external focus target must not be stolen");

    disposeTree(root);
});

test("diagram mounts share a single script load", async () => {
    const { ensureDiagramScript } = await import("../../Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
    const first = ensureDiagramScript();
    const second = ensureDiagramScript();
    const scripts = document.querySelectorAll("script");
    assert.equal(scripts.length, 1);
    assert.equal(scripts[0].src, "https://bloom.test/_content/Z.Blazor.Diagrams/script.min.js");
    scripts[0].onload();
    await Promise.all([first, second]);
    assert.equal(document.querySelectorAll("script").length, 1);
});

test("a failed diagram script load can be retried", async () => {
    const { ensureDiagramScript } = await import("../../Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
    const first = ensureDiagramScript();
    const failed = assert.rejects(first, /diagram script could not be loaded/);
    document.querySelector("script").onerror();
    await failed;
    assert.equal(document.querySelectorAll("script").length, 0);
    const second = ensureDiagramScript();
    document.querySelector("script").onload();
    await second;
    assert.equal(document.querySelectorAll("script").length, 1);
});

test("an already loaded diagram script is reused", async () => {
    const { ensureDiagramScript } = await import("../../Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
    globalThis.ZBlazorDiagrams = {};
    try {
        await ensureDiagramScript();
        assert.equal(document.querySelectorAll("script").length, 0);
    } finally {
        delete globalThis.ZBlazorDiagrams;
    }
});


test("inspection scrolling is confined to its Browser and never moves focus", () => {
    const root = createBrowserTree();
    const viewport = document.createElement("div");
    viewport.className = "mb-project-browser__tree-viewport";
    document.body.append(viewport);
    viewport.append(root);
    const item = root.querySelector('[role="treeitem"]');
    item.dataset.inspected = "true";
    const row = item.querySelector(".mb-project-browser-node__row");
    Object.defineProperty(viewport, "clientHeight", { value: 100 });
    viewport.getBoundingClientRect = () => ({ top: 100, bottom: 200 });
    row.getBoundingClientRect = () => ({ top: 240, bottom: 270 });
    const focused = document.createElement("button");
    document.body.append(focused);
    focused.focus();
    scrollInspectionIntoView(root);
    assert.equal(viewport.scrollTop, 70);
    assert.equal(document.activeElement, focused);
    row.getBoundingClientRect = () => ({ top: 80, bottom: 110 });
    scrollInspectionIntoView(root);
    assert.equal(viewport.scrollTop, 50);
    row.getBoundingClientRect = () => ({ top: 130, bottom: 160 });
    scrollInspectionIntoView(root);
    assert.equal(viewport.scrollTop, 50);
    delete item.dataset.inspected;
    scrollInspectionIntoView(root);
    scrollInspectionIntoView(null);
    disposeTree(root);
    viewport.remove();
    focused.remove();
});


for (const fontOutcome of ["rejected", "settled after disposal"]) {
    test(`inspection header measures natural names and handles fonts ${fontOutcome}`, async () => {
        const original = globalThis.ResizeObserver;
        const originalFonts = Object.getOwnPropertyDescriptor(document, "fonts");
        const fontReady = Promise.withResolvers();
        Object.defineProperty(document, "fonts", { configurable: true, value: { ready: fontReady.promise } });
        let callback;
        let disconnects = 0;
        globalThis.ResizeObserver = class {
            constructor(report) { callback = report; }
            observe() {}
            disconnect() { disconnects++; }
        };
        try {
            const header = document.createElement("header");
            header.className = "mb-details-panel__header";
            header.style.cssText = "column-gap:8px;padding:0 14px";
            header.innerHTML = `<div style="margin-right:5px"></div><h2 style="position:absolute">Properties</h2>
                <nav><ol style="column-gap:2px"><li><button><span>Root</span></button></li><li><span>Current</span></li></ol></nav>
                <button class="mb-properties-copy" style="margin-left:200px"></button><button></button>`;
            document.body.append(header);
            const root = header.querySelector("nav");
            header.children[0].getBoundingClientRect = () => ({ width: 32 });
            for (const button of header.querySelectorAll(":scope > button")) button.getBoundingClientRect = () => ({ width: 14 });
            for (const item of root.querySelectorAll("li")) item.getBoundingClientRect = () => ({ width: 100 });
            let width = 100;
            const labels = root.querySelectorAll("span");
            for (const label of labels) {
                Object.defineProperty(label, "clientWidth", { get: () => 100 });
                Object.defineProperty(label, "scrollWidth", { get: () => label === labels[0] ? width : 100 });
            }
            const values = [];
            const dotnet = { invokeMethodAsync: (method, value) => { assert.equal(method, "SetRequiredWidth"); values.push(value); return Promise.resolve(); } };
            observeTrail(root, dotnet, 380, 600);
            observeTrail(root, dotnet, 380, 600);
            callback();
            width = 250;
            callback();
            callback();
            width = 500;
            observeTrail(root, dotnet, 380, 600);
            assert.deepEqual(values, [380, 469, 600]);
            if (fontOutcome === "rejected") {
                fontReady.reject(new Error("Font readiness unavailable"));
                await new Promise(resolve => setImmediate(resolve));
            }
            stopObserving(root);
            stopObserving(root);
            assert.equal(disconnects, 1);
            width = 100;
            callback();
            if (fontOutcome === "settled after disposal") {
                fontReady.resolve();
                await new Promise(resolve => setImmediate(resolve));
            }
            assert.deepEqual(values, [380, 469, 600]);
            observeTrail(null, dotnet, 380, 600);
            values.length = 0;
            observeTrail(root, dotnet, 400, 500);
            assert.deepEqual(values, [400]);
            width = 500;
            callback();
            assert.deepEqual(values, [400, 500]);
            stopObserving(root);
            header.remove();
        } finally {
            globalThis.ResizeObserver = original;
            if (originalFonts) {
                Object.defineProperty(document, "fonts", originalFonts);
            } else {
                delete document.fonts;
            }
        }
    });
}

test("diagram assets wait for one shared lazy stylesheet across simultaneous mounts", async () => {
    const { ensureDiagramAssets } = await import("../../Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
    const first = ensureDiagramAssets();
    const second = ensureDiagramAssets();
    const styles = document.querySelectorAll('link[rel="stylesheet"]');
    assert.equal(styles.length, 1);
    assert.equal(styles[0].href, "https://bloom.test/_content/Z.Blazor.Diagrams/style.min.css");
    let ready = false;
    first.then(() => { ready = true; });
    document.querySelector("script").onload();
    await Promise.resolve();
    assert.equal(ready, false);
    styles[0].onload();
    await Promise.all([first, second]);
    await ensureDiagramAssets();
    assert.equal(document.querySelectorAll('link[rel="stylesheet"]').length, 1);
    assert.equal(document.querySelectorAll("script").length, 1);
});

test("failed lazy diagram styles can retry without reloading the successful script", async () => {
    const { ensureDiagramAssets } = await import("../../Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
    const first = ensureDiagramAssets();
    const failure = assert.rejects(first, /diagram stylesheet could not be loaded/);
    document.querySelector("script").onload();
    document.querySelector('link[rel="stylesheet"]').onerror();
    await failure;
    assert.equal(document.querySelectorAll('link[rel="stylesheet"]').length, 0);
    const second = ensureDiagramAssets();
    document.querySelector('link[rel="stylesheet"]').onload();
    await second;
    assert.equal(document.querySelectorAll("script").length, 1);
});

test("diagram modules do not load native styles before a canvas requests them", async () => {
    await import("../../Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
    assert.equal(document.querySelectorAll('link[rel="stylesheet"]').length, 0);
    assert.equal(document.querySelectorAll("script").length, 0);
});

async function createDiagramCapture(parent = document.body) {
    const { attachDiagramPointerCapture } = await import("../../Components/UI/Organisms/Diagram/DiagramCanvas.razor.js");
    const canvas = document.createElement("div");
    canvas.className = "diagram-canvas";
    canvas.tabIndex = -1;
    canvas.innerHTML = "<button>Node</button>";
    parent.append(canvas);
    const child = canvas.querySelector("button");
    const captures = new Map();
    for (const target of [canvas, child]) {
        target.setPointerCapture = id => captures.set(id, target);
        target.hasPointerCapture = id => captures.get(id) === target;
        target.releasePointerCapture = id => captures.delete(id);
    }
    const handle = attachDiagramPointerCapture(canvas);
    const pointer = (type, target = canvas, overrides = {}) => {
        const event = new window.Event(type, { bubbles: true, cancelable: true });
        for (const [key, value] of Object.entries({ pointerId: 1, button: 0, isPrimary: true, ...overrides })) {
            Object.defineProperty(event, key, { value });
        }
        target.dispatchEvent(event);
        return event;
    };
    return { canvas, child, captures, handle, pointer };
}

test("diagram captures the native hit target and allows normal activation", async () => {
    const { child, captures, handle, pointer } = await createDiagramCapture();
    assert.equal(pointer("pointerdown", child).defaultPrevented, false);
    assert.equal(captures.get(1), child);
    child.releasePointerCapture(1);
    pointer("lostpointercapture", child);
    assert.equal(pointer("click", child).defaultPrevented, false);
    assert.equal(captures.size, 0);
    handle.dispose();
});

test("diagram capture prevents middle-button autoscroll and ignores unsupported presses", async () => {
    const { canvas, child, captures, handle, pointer } = await createDiagramCapture();
    assert.equal(pointer("pointerdown", child, { button: 1 }).defaultPrevented, true);
    assert.equal(document.activeElement, canvas);
    assert.equal(handle.hasFocus(), true);
    assert.equal(captures.get(1), child);
    handle.release();
    handle.release();
    assert.equal(pointer("pointerdown", child, { button: 2 }).defaultPrevented, false);
    pointer("pointerdown", child, { isPrimary: false });
    assert.equal(captures.size, 0);
    handle.dispose();
});

test("diagram capture follows browser focus and releases native resources on blur and disposal", async () => {
    const { canvas, child, captures, handle, pointer } = await createDiagramCapture();
    canvas.focus();
    child.focus();
    assert.equal(handle.hasFocus(), true);
    pointer("pointerdown", child);
    assert.equal(captures.get(1), child);
    window.dispatchEvent(new window.Event("blur"));
    assert.equal(handle.hasFocus(), false);
    assert.equal(captures.size, 0);
    pointer("pointerdown", child);
    handle.dispose();
    handle.dispose();
    pointer("pointerdown", child);
    assert.equal(captures.size, 0);
});

test("nested native capture is attached only to the originating canvas", async () => {
    const outer = await createDiagramCapture();
    const inner = await createDiagramCapture(outer.canvas);
    inner.pointer("pointerdown", inner.child);
    assert.equal(outer.captures.size, 0);
    assert.equal(inner.captures.get(1), inner.child);
    outer.handle.dispose();
    assert.equal(inner.captures.get(1), inner.child);
    inner.handle.dispose();
    assert.equal(inner.captures.size, 0);
});
