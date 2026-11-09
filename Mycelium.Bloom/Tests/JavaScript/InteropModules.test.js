import assert from "node:assert/strict";
import { afterEach, before, beforeEach, test } from "node:test";

import { JSDOM } from "jsdom";
import { openFile } from "../../Components/UI/Organisms/ModelOpening/ModelOpening.razor.js";
import { readScroll, restoreScroll } from "../../Components/UI/Organisms/ProjectBrowser/ProjectBrowser.razor.js";

import { mount, unmount, focusOccurrence } from "../../Components/UI/Organisms/Interconnection/Interconnection.razor.js";

import {
    clearSearchInputValue,
    disposeEmptySpaceGuard,
    disposeSearchShortcut,
    registerEmptySpaceGuard,
    registerSearchShortcut
} from "../../Components/UI/Atoms/SearchInput/SearchInput.razor.js";
import {
    disposeSelectCompatibility,
    registerSelectCompatibility
} from "../../Components/UI/Atoms/SelectInput/SelectInput.razor.js";
import {
    capturePointer,
    focusElementById,
    registerKeydownGuards,
    releasePointer,
    unregisterKeydownGuards
} from "../../Components/UI/Organisms/EditorWorkspace/EditorWorkspace.razor.js";

let dom;
let animationFrameQueue;
let nextAnimationFrameId;
let initializeErrorRecovery;

const searchRegistrationIds = new Set();
const emptySpaceRegistrationIds = new Set();
const selectRegistrationIds = new Set();
const workspaceGuardIds = new Set();

test("Browser scroll restoration retains each tab's independent native position", () => {
    const first = { scrollLeft: 12, scrollTop: 240 };
    const second = { scrollLeft: 0, scrollTop: 30 };
    const saved = readScroll(first);
    restoreScroll(first, { left: 0, top: 0 });
    restoreScroll(first, saved);
    assert.deepEqual(readScroll(first), { left: 12, top: 240 });
    assert.deepEqual(readScroll(second), { left: 0, top: 30 });
});

test("model upload streams only the explicitly selected browser file", () => {
    const file = new Blob(['[]'], { type: 'application/json' });
    assert.throws(() => openFile(null), /Select a model file/);
    assert.throws(() => openFile({ files: [] }), /Select a model file/);
    assert.equal(openFile({ files: [file] }), file);
});

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
    globalThis.XMLSerializer = dom.window.XMLSerializer;
    globalThis.DOMParser = dom.window.DOMParser;
    globalThis.PointerEvent = dom.window.PointerEvent;
    globalThis.getComputedStyle = dom.window.getComputedStyle.bind(dom.window);
    globalThis.requestAnimationFrame = callback => {
        animationFrameQueue.push(callback);
        return nextAnimationFrameId++;
    };
});

afterEach(() => {
    for (const registrationId of emptySpaceRegistrationIds) {
        disposeEmptySpaceGuard(registrationId);
    }

    for (const registrationId of searchRegistrationIds) {
        disposeSearchShortcut(registrationId);
    }

    for (const registrationId of selectRegistrationIds) {
        disposeSelectCompatibility(registrationId);
    }

    for (const workspaceId of workspaceGuardIds) {
        unregisterKeydownGuards(workspaceId);
    }

    emptySpaceRegistrationIds.clear();
    searchRegistrationIds.clear();
    selectRegistrationIds.clear();
    workspaceGuardIds.clear();
    dom.window.close();

    delete globalThis.window;
    delete globalThis.document;
    delete globalThis.Element;
    delete globalThis.HTMLElement;
    delete globalThis.HTMLInputElement;
    delete globalThis.InputEvent;
    delete globalThis.MutationObserver;
    delete globalThis.XMLSerializer;
    delete globalThis.PointerEvent;
    delete globalThis.getComputedStyle;
    delete globalThis.requestAnimationFrame;
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

test("search shortcuts prefer the newest usable registration", () => {
    document.body.innerHTML = `
        <input id="primary-search" value="primary">
        <input id="disabled-search" disabled>
        <input id="secondary-search" value="secondary">
    `;
    const primaryInput = document.getElementById("primary-search");
    const secondaryInput = document.getElementById("secondary-search");
    const primaryFocus = trackInputFocus(primaryInput);
    const secondaryFocus = trackInputFocus(secondaryInput);

    registerSearch("primary-registration", "primary-search");
    registerSearch("disabled-registration", "disabled-search");
    registerSearch("missing-registration", "missing-search");

    const fallbackEvent = dispatchKey(document.body, "K", { ctrlKey: true });

    assert.equal(fallbackEvent.defaultPrevented, true);
    assert.equal(primaryFocus.count, 1);
    assert.equal(secondaryFocus.count, 0);
    assert.deepEqual(primaryFocus.options, { preventScroll: true });
    assert.equal(primaryInput.selectionStart, 0);
    assert.equal(primaryInput.selectionEnd, primaryInput.value.length);

    registerSearch("secondary-registration", "secondary-search");
    dispatchKey(document.body, "k", { metaKey: true });

    assert.equal(secondaryFocus.count, 1);

    registerSearch("primary-registration", "primary-search");
    dispatchKey(document.body, "k", { ctrlKey: true });

    assert.equal(primaryFocus.count, 2);

    const preventedEvent = createKeyEvent("k", { ctrlKey: true });
    preventedEvent.preventDefault();
    document.body.dispatchEvent(preventedEvent);

    assert.equal(primaryFocus.count, 2);
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

    registerEmptySpace("first-registration", "first-search");

    assert.equal(dispatchKey(firstInput, " ").defaultPrevented, true);
    assert.equal(dispatchKey(secondInput, " ").defaultPrevented, false);
    assert.equal(dispatchKey(firstInput, " ", { ctrlKey: true }).defaultPrevented, false);
    assert.equal(dispatchKey(firstInput, "a").defaultPrevented, false);

    firstInput.value = "multi word";
    assert.equal(dispatchKey(firstInput, " ").defaultPrevented, false);

    firstInput.value = "";
    registerEmptySpace("second-registration", "second-search");
    disposeEmptySpace("first-registration");

    assert.equal(dispatchKey(firstInput, " ").defaultPrevented, false);
    assert.equal(dispatchKey(secondInput, " ").defaultPrevented, true);
});

test("search shortcuts honor custom modifiers and detach after disposal", () => {
    document.body.innerHTML = '<input id="custom-search" value="custom">';
    const input = document.getElementById("custom-search");
    const focus = trackInputFocus(input);

    registerSearch("custom-registration", "custom-search", {
        key: "/",
        requiresControlOrMeta: false,
        requiresAlt: true,
        requiresShift: true
    });

    const wrongModifiers = dispatchKey(document.body, "/", { ctrlKey: true });
    const matchingEvent = dispatchKey(document.body, "/", { altKey: true, shiftKey: true });

    assert.equal(wrongModifiers.defaultPrevented, false);
    assert.equal(matchingEvent.defaultPrevented, true);
    assert.equal(focus.count, 1);

    disposeSearch("custom-registration");

    const detachedEvent = dispatchKey(document.body, "/", { altKey: true, shiftKey: true });

    assert.equal(detachedEvent.defaultPrevented, false);
    assert.equal(focus.count, 1);
});

test("select compatibility synchronizes active descendants and owns one observer", async () => {
    document.body.innerHTML = `
        <button id="select-trigger" aria-controls="select-listbox">Select</button>
        <button id="secondary-trigger" aria-controls="not-a-listbox">Secondary</button>
        <div id="not-a-listbox"></div>
        <div id="select-listbox" role="listbox" aria-labelledby="select-trigger">
            <div id="first-option" role="option" data-focused="true">First</div>
        </div>
        <div id="unregistered-listbox" role="listbox" aria-labelledby="unknown-trigger">
            <div id="unregistered-sync-option" role="option">Unknown</div>
        </div>
    `;
    const NativeMutationObserver = dom.window.MutationObserver;
    let disconnectCount = 0;

    globalThis.MutationObserver = class extends NativeMutationObserver {
        disconnect() {
            disconnectCount++;
            super.disconnect();
        }
    };

    registerSelect("primary-select", "select-trigger");
    registerSelect("secondary-select", "secondary-trigger");

    const listbox = document.getElementById("select-listbox");
    const option = document.getElementById("first-option");
    const unregisteredListbox = document.getElementById("unregistered-listbox");
    const unregisteredOption = document.getElementById("unregistered-sync-option");

    assert.equal(listbox.getAttribute("aria-activedescendant"), "first-option");

    unregisteredOption.setAttribute("data-focused", "true");
    await settleMutationObserver();

    assert.equal(unregisteredListbox.hasAttribute("aria-activedescendant"), false);

    disposeSelect("secondary-select");

    assert.equal(disconnectCount, 0);

    option.setAttribute("data-focused", "false");
    await settleMutationObserver();

    assert.equal(listbox.hasAttribute("aria-activedescendant"), false);

    option.setAttribute("data-focused", "true");
    await settleMutationObserver();

    assert.equal(listbox.getAttribute("aria-activedescendant"), "first-option");

    disposeSelect("primary-select");

    assert.equal(disconnectCount, 1);

    const detachedEvent = dispatchKey(document.getElementById("select-trigger"), "Enter");
    assert.equal(detachedEvent.defaultPrevented, false);
});

test("select compatibility preserves trigger and listbox tab order", () => {
    document.body.innerHTML = `
        <button id="before-trigger">Before</button>
        <button id="select-trigger" aria-controls="select-listbox">Select</button>
        <button id="display-none" style="display: none">Hidden</button>
        <button id="visibility-hidden" style="visibility: hidden">Invisible</button>
        <button id="zero-size">Zero size</button>
        <div role="dialog"><button id="dialog-button">Dialog action</button></div>
        <input id="after-trigger">
        <div id="select-listbox" role="listbox" aria-labelledby="select-trigger">
            <button id="portal-button">Portal action</button>
            <div id="active-option" role="option">Active option</div>
        </div>
        <button id="last-trigger">Last</button>
        <div id="last-listbox" role="listbox" aria-labelledby="last-trigger">
            <div id="last-option" role="option">Last option</div>
        </div>
        <button id="hidden-trigger" style="display: none">Hidden trigger</button>
        <div id="missing-trigger-listbox" role="listbox" aria-labelledby="missing-trigger">
            <div id="missing-trigger-option" role="option">Missing trigger option</div>
        </div>
        <div id="unregistered-listbox" role="listbox" aria-labelledby="unknown-trigger">
            <div id="unregistered-option" role="option">Unknown option</div>
        </div>
        <div id="unlabelled-listbox" role="listbox">
            <div id="unlabelled-option" role="option">Unlabelled option</div>
        </div>
    `;

    for (const id of [
        "before-trigger",
        "select-trigger",
        "visibility-hidden",
        "dialog-button",
        "after-trigger",
        "portal-button",
        "last-trigger"
    ]) {
        markVisible(document.getElementById(id));
    }

    registerSelect("tab-select", "select-trigger");
    registerSelect("last-select", "last-trigger");
    registerSelect("hidden-select", "hidden-trigger");
    registerSelect("missing-select", "missing-trigger");

    const trigger = document.getElementById("select-trigger");
    const beforeTrigger = document.getElementById("before-trigger");
    const afterTrigger = document.getElementById("after-trigger");
    const activeOption = document.getElementById("active-option");

    assert.equal(dispatchKey(trigger, "Enter").defaultPrevented, true);
    assert.equal(dispatchKey(trigger, " ").defaultPrevented, true);

    const forwardTab = dispatchKey(trigger, "Tab");
    flushAnimationFrames();

    assert.equal(forwardTab.defaultPrevented, true);
    assert.equal(document.activeElement, afterTrigger);

    const backwardTab = dispatchKey(trigger, "Tab", { shiftKey: true });
    flushAnimationFrames();

    assert.equal(backwardTab.defaultPrevented, true);
    assert.equal(document.activeElement, beforeTrigger);

    const listboxTab = dispatchKey(activeOption, "Tab");
    flushAnimationFrames();

    assert.equal(listboxTab.defaultPrevented, true);
    assert.equal(document.activeElement, afterTrigger);
    assert.equal(dispatchKey(activeOption, "Escape").defaultPrevented, false);
    assert.equal(dispatchKey(document.getElementById("unregistered-option"), "Tab").defaultPrevented, false);
    assert.equal(dispatchKey(document.getElementById("unlabelled-option"), "Tab").defaultPrevented, false);
    assert.equal(dispatchKey(document.getElementById("last-option"), "Tab").defaultPrevented, false);
    assert.equal(dispatchKey(document.getElementById("missing-trigger-option"), "Tab").defaultPrevented, false);
    assert.equal(dispatchKey(document.getElementById("last-trigger"), "Tab").defaultPrevented, false);
    assert.equal(dispatchKey(document.getElementById("hidden-trigger"), "Tab").defaultPrevented, false);
    assert.equal(dispatchKey(document.getElementById("before-trigger"), "Enter").defaultPrevented, false);
    assert.equal(dispatchKey(document, "Enter").defaultPrevented, false);
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
    assert.equal(documentListenerCount, 0);

    unregisterWorkspaceGuards("workspace");

    assert.equal(dispatchKey(tab, "ArrowRight").defaultPrevented, false);
    assert.equal(dispatchKey(splitter, "ArrowLeft").defaultPrevented, false);

    assert.equal(registerKeydownGuards("missing-workspace"), false);
});

function registerSearch(registrationId, inputId, shortcut) {
    searchRegistrationIds.add(registrationId);
    registerSearchShortcut(registrationId, inputId, shortcut);
}

function disposeSearch(registrationId) {
    disposeSearchShortcut(registrationId);
    searchRegistrationIds.delete(registrationId);
}

function registerEmptySpace(registrationId, inputId) {
    emptySpaceRegistrationIds.add(registrationId);
    registerEmptySpaceGuard(registrationId, inputId);
}

function disposeEmptySpace(registrationId) {
    disposeEmptySpaceGuard(registrationId);
    emptySpaceRegistrationIds.delete(registrationId);
}

function registerSelect(registrationId, triggerId) {
    selectRegistrationIds.add(registrationId);
    registerSelectCompatibility(registrationId, triggerId);
}

function disposeSelect(registrationId) {
    disposeSelectCompatibility(registrationId);
    selectRegistrationIds.delete(registrationId);
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

test("diagram forwards an owned gesture to the remounted canvas and stops after release or disposal", () => {
    const root = document.createElement("section");
    root.innerHTML = '<div class="diagram-canvas"></div>';
    document.body.append(root);
    const original = root.firstChild;
    original.setPointerCapture = () => {};
    original.hasPointerCapture = () => false;
    mount(root);
    original.dispatchEvent(new PointerEvent("pointerdown", { bubbles: true, pointerId: 7, button: 0 }));
    const current = document.createElement("div");
    current.className = "diagram-canvas";
    original.replaceWith(current);
    const received = [];
    for (const type of ["pointermove", "pointerup"]) current.addEventListener(type, event => received.push([type, event.clientX]));
    document.body.dispatchEvent(new PointerEvent("pointermove", { bubbles: true, pointerId: 8, clientX: 10 }));
    document.body.dispatchEvent(new PointerEvent("pointermove", { bubbles: true, pointerId: 7, clientX: 20 }));
    document.body.dispatchEvent(new PointerEvent("pointerup", { bubbles: true, pointerId: 7, clientX: 30 }));
    document.body.dispatchEvent(new PointerEvent("pointermove", { bubbles: true, pointerId: 7, clientX: 40 }));
    assert.deepEqual(received, [["pointermove", 20], ["pointerup", 30]]);
    unmount(root);
    document.body.dispatchEvent(new PointerEvent("pointerup", { bubbles: true, pointerId: 7, clientX: 50 }));
    assert.equal(received.length, 2);
    root.remove();
});

test("diagram mounts own independent pointer capture and release it on disposal", () => {
    document.body.innerHTML = `<section><div class="diagram-canvas"><svg><rect/></svg></div><button>Toolbar</button></section><section><div class="diagram-canvas"><svg><rect/></svg></div></section>`;
    const roots = [...document.querySelectorAll("section")];
    const captures = roots.map(root => {
        const active = new Set();
        const canvas = root.querySelector(".diagram-canvas");
        canvas.setPointerCapture = id => active.add(id);
        canvas.hasPointerCapture = id => active.has(id);
        canvas.releasePointerCapture = id => active.delete(id);
        mount(root);
        mount(root);
        return active;
    });
    const pointer = (target, type, id, button = 0) => target.dispatchEvent(new PointerEvent(type, {
        bubbles: true, pointerId: id, button, clientX: 10, clientY: 20
    }));
    pointer(roots[0].querySelector("button"), "pointerdown", 1);
    pointer(roots[0].querySelector("rect"), "pointerdown", 2, 1);
    assert.equal(captures[0].size, 0);
    pointer(roots[0].querySelector("rect"), "pointerdown", 3);
    pointer(roots[1].querySelector("rect"), "pointerdown", 4);
    assert.deepEqual([...captures[0]], [3]);
    unmount(roots[0]);
    unmount(roots[0]);
    assert.equal(captures[0].size, 0);
    assert.deepEqual([...captures[1]], [4]);
    pointer(roots[0].querySelector("rect"), "pointerdown", 5);
    assert.equal(captures[0].size, 0);
    let cancelledCompletion;
    roots[1].addEventListener("pointerup", event => { cancelledCompletion = event.pointerId; });
    pointer(roots[1].querySelector("rect"), "pointercancel", 4);
    assert.equal(cancelledCompletion, 4);
    captures[1].delete(4);
    pointer(roots[1].querySelector("rect"), "pointerdown", 6);
    pointer(roots[1].querySelector("rect"), "pointerup", 6);
    captures[1].delete(6);
    unmount(roots[1]);
    assert.equal(captures[1].size, 0);
});

test("diagram focus ignores absent occurrences and disposed mounts", () => {
    document.body.innerHTML = '<section><svg><g data-node-id="part"/></svg></section>';
    const root = document.querySelector('section'), node = root.querySelector('g');
    let calls = 0;
    node.focus = () => calls++;
    assert.equal(focusOccurrence(root, 'part'), false);
    assert.equal(calls, 0);
    mount(root);
    assert.equal(focusOccurrence(root, 'absent'), false);
    assert.equal(focusOccurrence(root, 'part'), true);
    assert.equal(calls, 1);
    unmount(root);
    assert.equal(focusOccurrence(root, 'part'), false);
    assert.equal(calls, 1);
});
