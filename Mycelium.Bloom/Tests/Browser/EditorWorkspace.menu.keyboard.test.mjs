// Run against a published application with BLOOM_URL and the existing external Playwright tooling.
import assert from "node:assert/strict";
import { EventEmitter, once } from "node:events";
import { createRequire } from "node:module";
import { after, before, test } from "node:test";

const { chromium } = createRequire(import.meta.url)(process.env.BLOOM_PLAYWRIGHT_MODULE || "playwright");
let browser;
before(async () => {
    assert.ok(process.env.BLOOM_URL, "Set BLOOM_URL to the running published app");
    browser = await chromium.launch({ executablePath: process.env.BLOOM_CHROMIUM });
});
after(async () => { await browser?.close(); });

test("Add tab accepts End and Enter while Blueprint keyboard initialization is pending", async t => {
    const menu = await openHeldMenu(t);
    await menu.focusLast();
    menu.assertPending();
    await menu.activateOnce();
    await menu.release();
    await menu.assertOneAdditionalBrowser();
});

test("Add tab hands keyboard control to Blueprint without resetting focus or activating twice", async t => {
    const menu = await openHeldMenu(t);
    await menu.focusLast();
    menu.assertPending();
    const beforeRelease = await menu.page.evaluate(() => window.addTabKeyboardEvidence.focus.length);
    await menu.release();
    // Observe DOM listener installation only after the cold fallback has been exercised.
    // This records browser events; it does not inspect Blueprint's private instance state.
    await menu.page.waitForFunction(id => window.addTabKeyboardEvidence.installed.includes(id), menu.menuId);
    await menu.assertLastFocused();
    const handoffFocus = await menu.page.evaluate(start => window.addTabKeyboardEvidence.focus.slice(start), beforeRelease);
    assert.ok(handoffFocus.every(event => event.text === "Project Browser" && event.role === "menuitem"));

    await menu.page.keyboard.press("ArrowUp");
    await menu.page.waitForFunction(id => {
        const menu = document.getElementById(id);
        return document.activeElement === menu.querySelector('[role="menuitem"]')
            && document.activeElement.textContent.trim() === "Empty editor";
    }, menu.menuId);
    await menu.focusLast();
    await menu.activateOnce();
});

async function openHeldMenu(t) {
    const page = await browser.newPage({ viewport: { width: 1800, height: 480 } });
    page.setDefaultTimeout(10000);
    const errors = [];
    page.on("pageerror", error => errors.push(error.message));
    page.on("console", message => { if (message.type() === "error") errors.push(message.text()); });
    await page.addInitScript(() => {
        window.addTabKeyboardEvidence = { installed: [], focus: [], activations: [] };
        const ownedMenu = () => {
            const trigger = document.querySelector('[aria-label="Add tab to Editor group 2"]');
            return document.getElementById(trigger?.getAttribute("aria-controls"));
        };
        const add = EventTarget.prototype.addEventListener;
        EventTarget.prototype.addEventListener = function (type, handler, options) {
            const result = add.call(this, type, handler, options);
            if (type === "keydown" && this === ownedMenu()) {
                window.addTabKeyboardEvidence.installed.push(this.id);
            }
            return result;
        };
        document.addEventListener("focusin", event => {
            if (ownedMenu()?.contains(event.target)) {
                window.addTabKeyboardEvidence.focus.push({ time: performance.now(), role: event.target.getAttribute("role"), text: event.target.textContent.trim() });
            }
        }, true);
        document.addEventListener("click", event => {
            const item = event.target.closest?.('[role="menuitem"]');
            if (item && ownedMenu()?.contains(item)) {
                window.addTabKeyboardEvidence.activations.push({ time: performance.now(), text: item.textContent.trim() });
            }
        }, true);
    });

    const gate = Promise.withResolvers();
    const requestEvents = new EventEmitter();
    const intercepted = once(requestEvents, "intercepted", { signal: AbortSignal.timeout(10000) });
    let interceptedCount = 0;
    let released = false;
    let finished = false;
    page.on("requestfinished", request => {
        if (request.url().endsWith("/js/primitives/menu-keyboard.js")) finished = true;
    });
    await page.route("**/js/primitives/menu-keyboard.js", async route => {
        interceptedCount++;
        requestEvents.emit("intercepted");
        await gate.promise;
        await route.continue();
    });
    t.after(async () => {
        gate.resolve();
        await page.unrouteAll({ behavior: "wait" });
        t.diagnostic(JSON.stringify(await page.evaluate(() => window.addTabKeyboardEvidence)));
        await page.close();
        assert.deepEqual(errors, [], "Browser errors through module release and handoff");
    });

    await page.goto(process.env.BLOOM_URL, { waitUntil: "networkidle" });
    const initialBrowserCount = await page.getByRole("tree").count();
    assert.equal(initialBrowserCount, 1);
    const trigger = page.getByRole("button", { name: "Add tab to Editor group 2", exact: true });
    await trigger.focus();
    await page.keyboard.press("Enter");
    await intercepted;
    const menuId = await trigger.getAttribute("aria-controls");
    await page.waitForFunction(id => document.activeElement === document.getElementById(id), menuId);
    assert.equal(await trigger.getAttribute("aria-expanded"), "true");
    const menu = page.locator(`[id="${menuId}"][role="menu"]`);
    assert.equal((await menu.getByRole("menuitem").last().textContent()).trim(), "Project Browser");

    function assertPending() {
        assert.equal(interceptedCount, 1, "The keyboard-module request was intercepted exactly once");
        assert.equal(released, false, "The intercepted request is still held");
        assert.equal(finished, false, "The keyboard-module request has not completed");
    }
    async function assertLastFocused() {
        await page.waitForFunction(id => {
            const menu = document.getElementById(id);
            return document.activeElement === menu.querySelectorAll('[role="menuitem"]')[1]
                && document.activeElement.textContent.trim() === "Project Browser";
        }, menuId);
    }
    async function assertOneAdditionalBrowser() {
        await page.waitForFunction(count => document.querySelectorAll('[role="tree"]').length === count, initialBrowserCount + 1);
        assert.equal(await page.getByRole("tree").count(), initialBrowserCount + 1);
        const activations = await page.evaluate(() => window.addTabKeyboardEvidence.activations);
        assert.equal(activations.length, 1);
        assert.equal(activations[0].text, "Project Browser");
    }
    assertPending();
    return {
        page, menuId, assertPending, assertLastFocused, assertOneAdditionalBrowser,
        async focusLast() {
            await page.keyboard.press("End");
            await assertLastFocused();
        },
        async activateOnce() {
            await page.keyboard.press("Enter");
            await assertOneAdditionalBrowser();
            await page.waitForFunction(id => document.getElementById(id)?.getAttribute("aria-expanded") === "false", await trigger.getAttribute("id"));
        },
        async release() {
            const completed = page.waitForEvent("requestfinished", { predicate: request => request.url().endsWith("/js/primitives/menu-keyboard.js") });
            released = true;
            gate.resolve();
            await completed;
            assert.equal(finished, true);
        }
    };
}
