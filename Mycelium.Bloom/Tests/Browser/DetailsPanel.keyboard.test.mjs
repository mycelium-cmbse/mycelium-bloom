// Run against the published Details app with BLOOM_URL and node --test <this file>.
// Playwright is external tooling; BLOOM_PLAYWRIGHT_MODULE and BLOOM_CHROMIUM optionally locate it.
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { after, before, test } from "node:test";

const { chromium } = createRequire(import.meta.url)(process.env.BLOOM_PLAYWRIGHT_MODULE || "playwright");
assert.ok(process.env.BLOOM_URL, "Set BLOOM_URL to the running published Details app");
let browser;
before(async () => { browser = await chromium.launch({ executablePath: process.env.BLOOM_CHROMIUM }); });
after(async () => { await browser?.close(); });

async function open(t) {
    const page = await browser.newPage({ viewport: { width: 1800, height: 480 } });
    page.setDefaultTimeout(10000);
    const errors = [];
    page.on("pageerror", error => errors.push(error.message));
    t.after(async () => { await page.close(); assert.deepEqual(errors, []); });
    await page.goto(process.env.BLOOM_URL, { waitUntil: "networkidle" });
    for (const name of ["Quantities", "TensorQuantityValue"]) {
        const item = page.getByRole("treeitem", { name: new RegExp(`^${name}(?:[ :«]|$)`) });
        await item.focus(); await page.keyboard.press("ArrowRight");
        await page.waitForFunction(label => [...document.querySelectorAll('[role="treeitem"]')]
            .some(element => element.getAttribute("aria-label") === label && element.getAttribute("aria-expanded") === "true"),
        await item.getAttribute("aria-label"));
    }
    await page.getByRole("treeitem", { name: /^dimensions(?:[ :«]|$)/ }).focus();
    await page.keyboard.press("Enter");
    await summary(page, "dimensions");
    return page;
}
async function summary(page, name) {
    await page.waitForFunction(name => document.querySelector('.mb-details-panel__element-name')?.textContent.trim() === name, name);
}
async function validFocus(page) {
    assert.equal(await page.evaluate(() => {
        const element = document.activeElement;
        return element !== document.body && element.isConnected && !!element.getClientRects().length && !element.closest('[inert]');
    }), true, "focus must remain on a visible, live control or region");
}
async function focusVisible(page) {
    await validFocus(page);
    assert.equal(await page.evaluate(() => {
        const element = document.activeElement, style = getComputedStyle(element);
        return element.matches(':focus-visible') && (style.outlineStyle !== 'none' || style.boxShadow !== 'none');
    }), true, "keyboard focus must be visibly indicated");
}

test("Blueprint automatic tabs keep focus, selection and panels synchronized in both themes", async t => {
    const page = await open(t), panel = page.locator('.mb-details-panel');
    for (const theme of ["light", "dark"]) {
        const toggle = page.getByRole("button", { name: `Switch to ${theme} mode`, exact: true });
        if (await toggle.count()) { await toggle.focus(); await page.keyboard.press("Enter"); }
        await panel.getByRole("button", { name: "Close Properties", exact: true }).focus();
        await page.keyboard.press("Tab");
        assert.equal(await page.evaluate(() => document.activeElement.getAttribute('role')), "tab");
        for (const [key, name] of [["Home", "Properties"], ["ArrowRight", "Relations"], ["ArrowRight", "Comments"],
            ["End", "Attachments"], ["ArrowRight", "Properties"], ["ArrowLeft", "Attachments"], ["Home", "Properties"]]) {
            await page.keyboard.press(key);
            await page.waitForFunction(name => document.querySelector('.mb-details-panel [role="tab"][aria-selected="true"]')?.textContent === name, name);
            const active = panel.getByRole("tab", { name, exact: true });
            assert.equal(await active.evaluate(element => document.activeElement === element), true);
            assert.equal(await panel.locator('[role="tab"][tabindex="0"]').count(), 1);
            assert.equal(await panel.getByRole("tabpanel").getAttribute('aria-labelledby'), await active.getAttribute('id'));
            assert.equal(await active.getAttribute('aria-controls'), await panel.getByRole("tabpanel").getAttribute('id'));
            await focusVisible(page);
        }
        for (const key of ["End", "ArrowLeft"]) {
            await page.keyboard.press(key); // Attachments, then Comments: empty static panels remain reachable.
            await page.waitForFunction(name => document.querySelector('.mb-details-panel [role="tab"][aria-selected="true"]')?.textContent === name,
                key === "End" ? "Attachments" : "Comments");
            await page.keyboard.press("Tab");
            assert.equal(await page.evaluate(() => document.activeElement.getAttribute('role')), "tabpanel");
            await focusVisible(page);
            assert.match(await panel.getByRole('tabpanel').innerText(), /Attachments are not available|No comments available/);
            await page.keyboard.press("Tab");
            assert.equal(await page.evaluate(() => document.activeElement.textContent), "Show in Browser");
            await page.keyboard.press("Shift+Tab"); await page.keyboard.press("Shift+Tab");
        }
        await page.keyboard.press("Home");
        await page.waitForFunction(() => document.querySelector('.mb-details-panel [role="tab"][aria-selected="true"]')?.textContent === 'Properties');
        await page.keyboard.press("Tab");
        await page.keyboard.press("PageDown");
        await page.waitForFunction(() => document.querySelector('.mb-details-panel [role="tabpanel"]').scrollTop > 0);
        await page.keyboard.press("Tab");
        assert.match(await page.evaluate(() => document.activeElement.getAttribute('aria-label')), /^Inspect /);
        await focusVisible(page);
        // Tab reaches the footer after the references; it can also leave the panel.
        for (let count = 0; count < 20 && await page.evaluate(() => document.activeElement.textContent !== 'Show in Browser'); count++) {
            await page.keyboard.press("Tab");
        }
        assert.equal(await page.evaluate(() => document.activeElement.textContent), 'Show in Browser');
        await page.keyboard.press("Tab");
        assert.equal(await page.evaluate(() => !!document.activeElement.closest('.mb-details-panel')), false);
    }
});

for (const key of ["Enter", "Space"]) {
    test(`${key} inspects Properties and Relations references, preserving Browser selection and live focus`, async t => {
        const page = await open(t), panel = page.locator('.mb-details-panel');
        const id = await page.locator('[role="treeitem"][aria-selected="true"]').getAttribute('data-element-id');
        await panel.getByRole("tab", { name: "Properties", exact: true }).focus();
        await page.keyboard.press("Tab"); await page.keyboard.press("Tab");
        assert.equal(await page.evaluate(() => document.activeElement.getAttribute('aria-label')), 'Inspect Value expression: mRef.dimensions');
        await page.keyboard.press(key); await summary(page, "Feature chain");
        await validFocus(page);
        assert.equal(await page.locator('[role="treeitem"][aria-selected="true"]').getAttribute('data-element-id'), id);
        // Select dimensions again, then follow its relation (the target may share its display name).
        await page.getByRole('treeitem', { name: /^dimensions(?:[ :«]|$)/ }).focus();
        await page.keyboard.press('Enter'); await summary(page, 'dimensions');
        await panel.getByRole('tab', { name: 'Properties', exact: true }).focus();
        await page.keyboard.press('ArrowRight');
        await page.waitForFunction(() => document.querySelector('.mb-details-panel [role="tab"][aria-selected="true"]')?.textContent === 'Relations');
        await page.keyboard.press('Tab'); await page.keyboard.press('Tab');
        const link = await page.evaluate(() => ({ label: document.activeElement.getAttribute('aria-label'), id: document.activeElement.closest('[data-element-id]').getAttribute('data-element-id') }));
        assert.match(link.label, /^Inspect /);
        assert.notEqual(link.id, id);
        await page.keyboard.press(key);
        await page.waitForFunction(id => new URL(location.href).searchParams.get('selectedElement') === id, link.id);
        await validFocus(page);
        assert.equal(await page.locator('[role="treeitem"][aria-selected="true"]').getAttribute('data-element-id'), id);
    });
}

test("close, reopen and compact Show in Browser recover focus into visible regions", async t => {
    const page = await open(t), panel = page.locator('.mb-details-panel');
    const id = await page.locator('[role="treeitem"][aria-selected="true"]').getAttribute('data-element-id');
    await panel.getByRole('button', { name: 'Close Properties', exact: true }).focus();
    await page.keyboard.press('Space');
    await page.waitForFunction(() => document.activeElement.getAttribute('aria-label') === 'Expand Properties');
    await page.keyboard.press('Enter');
    await page.getByRole('button', { name: 'Collapse Properties', exact: true }).waitFor();
    await validFocus(page);
    await page.setViewportSize({ width: 700, height: 480 });
    const regions = page.getByRole('group', { name: 'Workspace regions', exact: true });
    await regions.getByRole('button', { name: 'Properties', exact: true }).focus();
    await page.keyboard.press('Enter');
    await panel.getByRole('button', { name: 'Show in Browser', exact: true }).focus();
    await page.keyboard.press('Enter');
    await page.waitForFunction(() => document.activeElement.getAttribute('aria-label') === 'Workspace content');
    await focusVisible(page);
    assert.equal(await page.locator('[role="treeitem"][aria-selected="true"]').getAttribute('data-element-id'), id);
    await page.keyboard.press('Tab'); await validFocus(page);
    await regions.getByRole('button', { name: 'Properties', exact: true }).focus();
    await page.keyboard.press('Space');
    await panel.getByRole('tab', { name: 'Properties', exact: true }).focus();
    await page.setViewportSize({ width: 1800, height: 480 });
    await validFocus(page);
});
