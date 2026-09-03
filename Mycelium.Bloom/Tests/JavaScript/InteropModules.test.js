import assert from "node:assert/strict";
import { afterEach, beforeEach, test } from "node:test";

import { JSDOM } from "jsdom";

import {
    applyTheme,
    releaseTheme
} from "../../Components/Pages/DesignSystem.razor.js";
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

const searchRegistrationIds = new Set();
const emptySpaceRegistrationIds = new Set();
const selectRegistrationIds = new Set();
const themeOwnerIds = new Set();
const workspaceGuardIds = new Set();

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
    for (const registrationId of emptySpaceRegistrationIds) {
        disposeEmptySpaceGuard(registrationId);
    }

    for (const registrationId of searchRegistrationIds) {
        disposeSearchShortcut(registrationId);
    }

    for (const registrationId of selectRegistrationIds) {
        disposeSelectCompatibility(registrationId);
    }

    for (const ownerId of Array.from(themeOwnerIds).reverse()) {
        releaseTheme(ownerId);
    }

    for (const workspaceId of workspaceGuardIds) {
        unregisterKeydownGuards(workspaceId);
    }

    emptySpaceRegistrationIds.clear();
    searchRegistrationIds.clear();
    selectRegistrationIds.clear();
    themeOwnerIds.clear();
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
});

test("theme preview validates ownership and restores a pre-existing theme", () => {
    const root = document.documentElement;
    root.dataset.theme = "system";
    root.classList.add("dark");

    applyOwnedTheme("theme-owner", "light");

    assert.equal(root.dataset.theme, "light");
    assert.equal(root.classList.contains("dark"), false);
    assert.equal(root.dataset.mbDesignSystemThemeOwner, "theme-owner");

    applyOwnedTheme("theme-owner", "dark");
    releaseTheme("different-owner");

    assert.equal(root.dataset.theme, "dark");
    assert.equal(root.classList.contains("dark"), true);

    releaseOwnedTheme("theme-owner");

    assert.equal(root.dataset.theme, "system");
    assert.equal(root.classList.contains("dark"), true);
    assert.equal(Object.hasOwn(root.dataset, "mbDesignSystemThemeOwner"), false);
});

test("theme preview removes temporary state and rejects unsupported names", () => {
    const root = document.documentElement;

    assert.throws(
        () => applyTheme("invalid-owner", "contrast"),
        error => error instanceof RangeError && error.message.includes("contrast"));

    applyOwnedTheme("temporary-owner", "dark");
    releaseOwnedTheme("temporary-owner");

    assert.equal(Object.hasOwn(root.dataset, "theme"), false);
    assert.equal(root.classList.contains("dark"), false);
    assert.equal(Object.hasOwn(root.dataset, "mbDesignSystemThemeOwner"), false);
});

test("theme preview preserves an empty theme across repeated ownership cycles", () => {
    const root = document.documentElement;
    root.dataset.theme = "";

    applyOwnedTheme("empty-theme-owner", "dark");

    assert.equal(root.dataset.theme, "dark");
    assert.equal(root.classList.contains("dark"), true);

    releaseOwnedTheme("empty-theme-owner");

    assert.equal(Object.hasOwn(root.dataset, "theme"), true);
    assert.equal(root.dataset.theme, "");

    applyOwnedTheme("empty-theme-owner", "light");
    releaseOwnedTheme("empty-theme-owner");

    assert.equal(Object.hasOwn(root.dataset, "theme"), true);
    assert.equal(root.dataset.theme, "");
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

function applyOwnedTheme(ownerId, themeName) {
    themeOwnerIds.add(ownerId);
    applyTheme(ownerId, themeName);
}

function releaseOwnedTheme(ownerId) {
    releaseTheme(ownerId);
    themeOwnerIds.delete(ownerId);
}

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
