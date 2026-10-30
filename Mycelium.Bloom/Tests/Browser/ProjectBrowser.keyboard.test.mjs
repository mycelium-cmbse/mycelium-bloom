// Run against a published application: BLOOM_URL=http://localhost:5192 node --test <this file>.
// Playwright is external test tooling; BLOOM_PLAYWRIGHT_MODULE and BLOOM_CHROMIUM may override its installation.
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { after, before, test } from "node:test";

const { chromium } = createRequire(import.meta.url)(process.env.BLOOM_PLAYWRIGHT_MODULE || "playwright");
const url = process.env.BLOOM_URL;
assert.ok(url, "Set BLOOM_URL to the running published app");
let browser;
before(async () => { browser = await chromium.launch({ executablePath: process.env.BLOOM_CHROMIUM }); });
after(async () => { await browser?.close(); });

async function open(t, delayed = false) {
    const page = await browser.newPage({ viewport: { width: 1800, height: 480 } });
    page.setDefaultTimeout(10000);
    const errors = [];
    page.on("pageerror", error => errors.push(error.message));
    t.after(async () => { await page.close(); assert.deepEqual(errors, []); });
    await page.addInitScript(() => {
        window.browserKeyboardRoots = new WeakSet();
        const add = EventTarget.prototype.addEventListener;
        EventTarget.prototype.addEventListener = function (type, handler, options) {
            const result = add.call(this, type, handler, options);
            if (type === "click" && this instanceof Element && this.getAttribute("role") === "tree") {
                window.browserKeyboardRoots.add(this);
            }
            return result;
        };
    });
    if (delayed) {
        await page.routeWebSocket(/_blazor/, socket => {
            const server = socket.connectToServer();
            socket.onMessage(message => setTimeout(() => server.send(message), 150));
            server.onMessage(message => setTimeout(() => socket.send(message), 150));
        });
    }
    await page.goto(url, { waitUntil: "networkidle" });
    await page.waitForFunction(() => window.browserKeyboardRoots.has(document.querySelector('[role="tree"]')));
    return page;
}

const node = (page, name) => page.getByRole("treeitem", { name: new RegExp(`^${name}(?:[ :«]|$)`) });
async function expanded(item, value) {
    await item.page().waitForFunction(({ label, value }) =>
        [...document.querySelectorAll('[role="treeitem"]')].some(element =>
            element.getAttribute("aria-label") === label && element.getAttribute("aria-expanded") === value),
    { label: await item.getAttribute("aria-label"), value: String(value) });
}
async function focused(item) {
    assert.equal(await item.evaluate(element => document.activeElement === element), true);
    assert.equal(await item.locator('xpath=ancestor::*[@role="tree"]').locator('[tabindex="0"]').count(), 1);
}
async function visibleRow(item) {
    assert.equal(await item.evaluate(element => {
        const row = element.querySelector(':scope > .mb-project-browser-node__line').getBoundingClientRect();
        const viewport = element.closest('.mb-project-browser__tree-viewport').getBoundingClientRect();
        return row.top >= viewport.top - 1 && row.bottom <= viewport.bottom + 1;
    }), true, "the focused row must remain in the scrolling viewport");
}

async function expandTallTree(page) {
    for (let count = 0; count < 50; count++) {
        const collapsed = page.locator('[role="treeitem"][aria-expanded="false"]').first();
        if (!await collapsed.count()) { break; }
        // The collapsed locator points at the next node as soon as Blazor expands this one.
        const item = page.getByRole("treeitem", { name: await collapsed.getAttribute("aria-label"), exact: true }).first();
        await item.focus(); await page.keyboard.press("ArrowRight"); await expanded(item, true);
    }
    assert.ok(await page.getByRole("treeitem").count() >= 40);
    assert.equal(await page.locator('.mb-project-browser__tree-viewport').evaluate(element =>
        element.scrollHeight > element.clientHeight * 3), true);
}

test("repeated ArrowUp, ArrowLeft and Home keep the focused row inside a tall tree viewport", async t => {
    const page = await open(t);
    await expandTallTree(page);
    const pagePosition = await page.evaluate(() => [scrollX, scrollY]);
    await page.keyboard.press("End");
    const count = await page.getByRole("treeitem").count();
    for (let index = 0; index < count; index++) {
        await page.keyboard.press("ArrowUp");
        await visibleRow(page.locator('[role="treeitem"]:focus'));
    }
    await page.keyboard.press("End"); await page.keyboard.press("ArrowLeft");
    await focused(node(page, "Quantities")); await visibleRow(node(page, "Quantities"));
    await page.keyboard.press("Home"); await visibleRow(page.getByRole("treeitem").first());
    const viewport = page.locator('.mb-project-browser__tree-viewport');
    const scrollTop = await viewport.evaluate(element => element.scrollTop);
    await page.keyboard.press("Home"); // Already visible: no unnecessary scroll.
    assert.equal(await viewport.evaluate(element => element.scrollTop), scrollTop);
    assert.deepEqual(await page.evaluate(() => [scrollX, scrollY]), pagePosition);
});

test("search Tab, arrows, Enter, Tab out and Shift+Tab return preserve the selected occurrence", async t => {
    const page = await open(t);
    const search = page.getByRole("combobox", { name: "Filter project browser by name or qualified name" });
    await search.focus(); await page.keyboard.press("Tab");
    await focused(page.getByRole("treeitem").first());
    await page.keyboard.press("ArrowDown");
    const quantities = node(page, "Quantities");
    await page.keyboard.press("ArrowRight"); await expanded(quantities, true);
    await page.keyboard.press("ArrowRight");
    const tensor = node(page, "TensorQuantityValue");
    await focused(tensor);
    assert.equal(await tensor.getAttribute("aria-selected"), "false");
    await page.keyboard.press("Enter");
    const id = await tensor.getAttribute("data-element-id");
    await page.waitForFunction(id => new URL(location.href).searchParams.get("selectedElement") === id, id);
    assert.equal(await page.locator('.mb-details-panel__element-name').innerText(), "TensorQuantityValue");
    await page.keyboard.press("Tab");
    assert.equal(await page.evaluate(() => document.activeElement.getAttribute("role")), "separator");
    await page.keyboard.press("Shift+Tab"); await focused(tensor); await visibleRow(tensor);
    await page.keyboard.press("ArrowDown"); await page.keyboard.press("ArrowUp"); await focused(tensor);
    assert.equal(await tensor.getAttribute("aria-selected"), "true");
    await page.keyboard.press("Tab"); await page.keyboard.press("Tab");
    await page.waitForFunction(() => document.activeElement.getAttribute("role") === "tab" &&
        document.activeElement.textContent.trim() === "Editor 2");
    await page.keyboard.press("Shift+Tab"); await page.keyboard.press("Shift+Tab");
    await focused(tensor); await visibleRow(tensor);
});

test("Tab re-entry reveals a parent row after native keyboard scrolling of its expanded subtree", async t => {
    const page = await open(t);
    await expandTallTree(page);
    await page.keyboard.press("Home");
    const root = page.getByRole("treeitem").first();
    await page.keyboard.press("PageDown"); await page.keyboard.press("PageDown");
    await page.waitForFunction(() => document.querySelector('.mb-project-browser__tree-viewport').scrollTop > 300);
    // Native scrolling can animate; wait for its endpoint before testing re-entry.
    await page.waitForTimeout(350);
    await page.keyboard.press("Tab");
    assert.equal(await page.evaluate(() => document.activeElement.getAttribute("role")), "separator");
    await page.keyboard.press("Shift+Tab"); await focused(root); await visibleRow(root);
    await page.keyboard.press("ArrowDown"); await focused(node(page, "Quantities"));
});

test("a disclosure render after Tab exit preserves the surviving return node", async t => {
    const page = await open(t, true);
    const quantities = node(page, "Quantities");
    await quantities.focus(); await page.keyboard.press("ArrowRight"); await expanded(quantities, true);
    assert.equal(await quantities.getAttribute("aria-selected"), "false");
    await page.keyboard.press("ArrowLeft"); await page.keyboard.press("Tab");
    await expanded(quantities, false);
    assert.equal(await page.evaluate(() => document.activeElement.getAttribute("role")), "separator");
    assert.equal(await quantities.getAttribute("tabindex"), "0", "a content render must not replace the last focus with an unrelated selection");
    await page.keyboard.press("Shift+Tab"); await focused(quantities); await visibleRow(quantities);
    await page.keyboard.press("ArrowRight"); await expanded(quantities, true);
});

test("real keys enter/exit the tree, navigate, select and update Details in both themes", async t => {
    const page = await open(t);
    for (const theme of ["light", "dark"]) {
        const toggle = page.getByRole("button", { name: `Switch to ${theme} mode`, exact: true });
        if (await toggle.count()) { await toggle.focus(); await page.keyboard.press("Enter"); }
        const search = page.getByRole("combobox", { name: "Filter project browser by name or qualified name" });
        await search.focus(); await page.keyboard.press("Tab");
        assert.equal(await page.evaluate(() => document.activeElement.getAttribute("role")), "treeitem");
        await page.keyboard.press("Home");
        const root = page.getByRole("treeitem").first();
        await focused(root);
        if (await root.getAttribute("aria-expanded") === "false") {
            await page.keyboard.press("ArrowRight"); await expanded(root, true);
        }
        await page.keyboard.press("ArrowRight");
        const quantities = node(page, "Quantities");
        await focused(quantities);
        await page.keyboard.press("Enter");
        await page.waitForFunction(() => document.querySelector('.mb-details-panel__element-name')?.textContent.trim() === "Quantities");
        assert.equal(await quantities.getAttribute("aria-selected"), "true");
        if (await quantities.getAttribute("aria-expanded") === "false") {
            await page.keyboard.press("ArrowRight"); await expanded(quantities, true);
        }
        await page.keyboard.press("ArrowRight");
        const tensor = node(page, "TensorQuantityValue");
        await focused(tensor);
        assert.equal(await quantities.getAttribute("aria-selected"), "true", "focus movement does not select");
        await page.keyboard.press("Space");
        await page.waitForFunction(() => document.querySelector('.mb-details-panel__element-name')?.textContent.trim() === "TensorQuantityValue");
        assert.equal(await tensor.getAttribute("aria-selected"), "true");
        await page.keyboard.press("ArrowDown"); await page.keyboard.press("ArrowUp"); await focused(tensor);
        await page.keyboard.press("ArrowLeft"); await focused(quantities);
        await page.keyboard.press("ArrowLeft"); await expanded(quantities, false); await focused(quantities);
        const line = quantities.locator(':scope > .mb-project-browser-node__line');
        assert.equal(await line.evaluate(element => getComputedStyle(element).outlineStyle), "solid");
        await page.keyboard.press("Tab");
        assert.equal(await page.evaluate(() => !!document.activeElement.closest('[role="tree"]')), false);
    }
});

test("Home and parent navigation reveal their own row in a long expanded tree", async t => {
    const page = await open(t);
    for (const name of ["Quantities", "TensorQuantityValue", "ScalarQuantityValue", "VectorQuantityValue"]) {
        const item = node(page, name);
        await item.focus(); await page.keyboard.press("ArrowRight"); await expanded(item, true);
    }
    await page.keyboard.press("End");
    await visibleRow(page.locator('[role="treeitem"]:focus'));
    await page.keyboard.press("Home");
    await visibleRow(page.getByRole("treeitem").first());
    await page.keyboard.press("ArrowRight"); await visibleRow(node(page, "Quantities"));
    await page.keyboard.press("ArrowRight"); await visibleRow(node(page, "TensorQuantityValue"));
});

test("repeated arrows cannot reverse a pending Blazor disclosure", async t => {
    const page = await open(t, true);
    const quantities = node(page, "Quantities");
    await quantities.focus();
    await page.keyboard.press("ArrowRight"); await page.keyboard.press("ArrowRight");
    await expanded(quantities, true);
    // Allow the second server round trip as well: an intermediate expanded render is not success.
    await page.waitForTimeout(800);
    assert.equal(await quantities.getAttribute("aria-expanded"), "true");
    await page.keyboard.press("ArrowLeft"); await page.keyboard.press("ArrowLeft");
    await expanded(quantities, false); await page.waitForTimeout(800);
    assert.equal(await quantities.getAttribute("aria-expanded"), "false");
});

test("a focused descendant removed by an in-flight collapse recovers to its parent", async t => {
    const page = await open(t, true);
    const quantities = node(page, "Quantities");
    await quantities.focus(); await page.keyboard.press("ArrowRight"); await expanded(quantities, true);
    await page.keyboard.press("ArrowLeft"); // Server collapse is pending.
    await page.keyboard.press("ArrowDown"); // Move into a child before it disappears.
    await focused(node(page, "TensorQuantityValue"));
    await expanded(quantities, false);
    await focused(quantities);
    await page.keyboard.press("Enter");
    await page.waitForFunction(() => document.querySelector('.mb-details-panel__element-name')?.textContent.trim() === "Quantities");
});

test("filtered hierarchy preserves forced expansion and canonical keyboard selection", async t => {
    const page = await open(t);
    const search = page.getByRole("combobox", { name: "Filter project browser by name or qualified name" });
    await search.focus(); await page.keyboard.type("dimensions");
    await page.getByText('Contains "dimensions"', { exact: true }).waitFor();
    await page.keyboard.press("Enter");
    await node(page, "dimensions").first().waitFor();
    await page.keyboard.press("Escape"); await page.keyboard.press("Tab");
    await page.keyboard.press("Home");
    const root = page.getByRole("treeitem").first();
    await page.keyboard.press("ArrowLeft");
    assert.equal(await root.getAttribute("aria-expanded"), "true");
    await page.keyboard.press("End");
    const occurrences = node(page, "dimensions");
    assert.equal(await occurrences.count(), 2, "the fixture includes repeated canonical occurrences");
    const dimensions = occurrences.last(); await focused(dimensions);
    const id = await dimensions.getAttribute("data-element-id");
    await page.keyboard.press("Enter");
    await page.waitForFunction(() => document.querySelector('.mb-details-panel__element-name')?.textContent.trim() === "dimensions");
    assert.equal(await page.locator('[role="treeitem"][aria-selected="true"]').getAttribute("data-element-id"), id);
    await page.keyboard.press("ArrowLeft");
    await focused(node(page, "QuantityValue"));
    assert.equal(await node(page, "QuantityValue").getAttribute("aria-expanded"), "true");
    await page.keyboard.press("Home");
    const firstIndex = await occurrences.first().evaluate(element => [...element.closest('[role="tree"]').querySelectorAll('[role="treeitem"]')].indexOf(element));
    for (let index = 0; index < firstIndex; index++) { await page.keyboard.press("ArrowDown"); }
    await focused(occurrences.first());
    assert.equal(await occurrences.first().getAttribute("data-element-id"), id);
    await page.keyboard.press("Space");
    await page.waitForFunction(() => document.querySelector('[role="treeitem"][aria-selected="true"]')?.closest('[aria-label^="TensorQuantityValue "]'));
});

test("independent Browsers and editor-tab remounts preserve local focus and selection", async t => {
    const page = await open(t);
    const first = page.getByRole("tree").first();
    const firstSelected = await first.locator('[aria-selected="true"]').getAttribute("data-element-id");
    await page.getByRole("button", { name: "Add tab to Editor group 2", exact: true }).focus();
    await page.keyboard.press("Enter");
    await page.waitForFunction(() => document.activeElement.getAttribute("role") === "menu");
    await page.keyboard.press("End");
    await page.waitForFunction(() => document.activeElement.getAttribute("role") === "menuitem" &&
        document.activeElement.textContent.trim() === "Project Browser");
    await page.keyboard.press("Enter");
    await page.waitForFunction(() => document.querySelectorAll('[role="tree"]').length === 2 &&
        [...document.querySelectorAll('[role="tree"]')].every(tree => window.browserKeyboardRoots.has(tree)));
    const second = page.getByRole("tree").nth(1);
    const quantities = second.getByRole("treeitem", { name: /^Quantities «/ });
    await quantities.focus(); await page.keyboard.press("Enter");
    await page.waitForFunction(() => document.querySelector('.mb-details-panel__element-name')?.textContent.trim() === "Quantities");
    assert.equal(await first.locator('[aria-selected="true"]').getAttribute("data-element-id"), firstSelected);
    assert.equal(await first.locator('[tabindex="0"]').count(), 1);
    await focused(quantities);
    await page.getByRole("tab", { name: "Editor 2", exact: true }).focus();
    await page.keyboard.press("Enter");
    await page.waitForFunction(() => document.querySelectorAll('[role="tree"]').length === 1);
    await page.getByRole("tab", { name: "Project Browser", exact: true }).nth(1).focus();
    await page.keyboard.press("Enter");
    await page.waitForFunction(() => document.querySelectorAll('[role="tree"]').length === 2 &&
        [...document.querySelectorAll('[role="tree"]')].every(tree => window.browserKeyboardRoots.has(tree)));
    await quantities.focus(); await page.keyboard.press("ArrowRight"); await expanded(quantities, true);
    await page.keyboard.press("ArrowRight");
    await focused(second.getByRole("treeitem", { name: /^TensorQuantityValue «/ }));
    assert.equal(await first.locator('[aria-selected="true"]').getAttribute("data-element-id"), firstSelected);
});
