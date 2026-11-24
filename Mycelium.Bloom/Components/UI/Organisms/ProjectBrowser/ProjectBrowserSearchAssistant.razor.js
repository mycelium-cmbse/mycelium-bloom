/**
 * Browser helpers of the Project Browser search field: they clear the native value after a criterion is committed and refuse a
 * leading space, two things Blazor cannot do before the browser shows the typed character.
 * @module ProjectBrowserSearchAssistant
 */

const guardedInputIds = new Set();
let documentKeydownHandler;

/**
 * Empties a search field and emits one input event, so that Blazor and Blueprint read the empty value.
 * @param {unknown} input The search field.
 * @returns {void}
 */
export function clearSearchInputValue(input) {
    if (!(input instanceof HTMLInputElement)) {
        return;
    }

    input.value = "";
    input.dispatchEvent(new InputEvent("input", {
        bubbles: true,
        inputType: "deleteContentBackward"
    }));
}

/**
 * Refuses an unmodified Space typed into an empty search field.
 * @param {string} inputId The identifier of the search field.
 * @returns {void}
 */
export function registerEmptySpaceGuard(inputId) {
    guardedInputIds.add(inputId);

    if (!documentKeydownHandler) {
        documentKeydownHandler = handleDocumentKeyDown;
        document.addEventListener("keydown", documentKeydownHandler, true);
    }
}

/**
 * Stops guarding a search field, and removes the document listener with the last one.
 * @param {string} inputId The identifier of the search field.
 * @returns {void}
 */
export function disposeEmptySpaceGuard(inputId) {
    guardedInputIds.delete(inputId);

    if (guardedInputIds.size === 0 && documentKeydownHandler) {
        document.removeEventListener("keydown", documentKeydownHandler, true);
        documentKeydownHandler = undefined;
    }
}

/**
 * Prevents the default action of a key event that the guard refuses.
 * @param {KeyboardEvent} event The key event.
 * @returns {void}
 */
function handleDocumentKeyDown(event) {
    if (!event.defaultPrevented && matchesEmptySpaceGuard(event)) {
        event.preventDefault();
    }
}

/**
 * Tells whether a key event is an unmodified Space typed into an empty registered search field.
 * @param {KeyboardEvent} event The key event.
 * @returns {boolean} Whether the guard refuses the key.
 */
function matchesEmptySpaceGuard(event) {
    if (!(event.target instanceof HTMLInputElement)
        || event.key !== " "
        || event.isComposing
        || event.ctrlKey
        || event.metaKey
        || event.altKey
        || event.shiftKey
        || event.target.value.length !== 0) {
        return false;
    }

    return guardedInputIds.has(event.target.id);
}
