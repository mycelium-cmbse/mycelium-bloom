// ------------------------------------------------------------------------------------------------
// <copyright file="ProjectBrowser.razor.js" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

/**
 * Reads scroll state for retention by the owning Browser tab.
 * @param {HTMLElement} viewport Mounted tree viewport.
 * @returns {{left:number,top:number}} Current scroll offsets.
 */
export function readScroll(viewport) {
    return { left: viewport.scrollLeft, top: viewport.scrollTop };
}

/**
 * Restores a tab-owned offset without changing another Browser instance.
 * @param {HTMLElement} viewport Mounted tree viewport.
 * @param {{left:number,top:number}} position Previously captured scroll offsets.
 * @returns {void}
 */
export function restoreScroll(viewport, position) {
    viewport.scrollLeft = position.left;
    viewport.scrollTop = position.top;
}
