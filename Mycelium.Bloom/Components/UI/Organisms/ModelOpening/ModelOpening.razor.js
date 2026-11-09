// ------------------------------------------------------------------------------------------------
// <copyright file="ModelOpening.razor.js" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

/**
 * Returns the explicitly selected file for the caller-owned streaming upload.
 * @param {HTMLInputElement} input The dialog file chooser.
 * @returns {File} Selected browser file; throws when selection is empty.
 */
export function openFile(input) {
    const file = input?.files?.[0];
    if (!file) throw new Error("Select a model file first.");
    return file;
}
