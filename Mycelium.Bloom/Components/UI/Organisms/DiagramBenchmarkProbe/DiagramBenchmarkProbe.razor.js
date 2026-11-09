// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

const probes = new WeakMap();
/**
 * Registers a caller-owned .NET probe until unmount.
 * @param {HTMLElement} root Mounted probe host.
 * @param {object} reference The caller-owned .NET callback reference.
 * @returns {void}
 */
export function mount(root, reference) { probes.set(root, reference); }
/**
 * Releases the host’s probe registration without disposing the caller’s reference.
 * @param {HTMLElement} root Mounted probe host.
 * @returns {void}
 */
export function unmount(root) { probes.delete(root); }
/**
 * Reads accepted C# state for a diagnostic occurrence.
 * @param {HTMLElement} root Mounted probe host.
 * @param {string} occurrenceId Graphical occurrence identity.
 * @returns {Promise<object|null>} Callback result, or null when unmounted.
 */
export function read(root, occurrenceId) { return probes.get(root)?.invokeMethodAsync('ReadBenchmarkSnapshot', occurrenceId) ?? Promise.resolve(null); }
/**
 * Measures a diagnostic .NET round trip.
 * @param {HTMLElement} root Mounted probe host.
 * @returns {Promise<object|null>} Callback result, or null when unmounted.
 */
export function ping(root) { return probes.get(root)?.invokeMethodAsync('BenchmarkPing') ?? Promise.resolve(null); }
/**
 * Reads server resource counters through the diagnostic probe.
 * @param {HTMLElement} root Mounted probe host.
 * @returns {Promise<object|null>} Callback result, or null when unmounted.
 */
export function resources(root) { return probes.get(root)?.invokeMethodAsync('ReadBenchmarkResources') ?? Promise.resolve(null); }
/**
 * Starts a diagnostic geometry watch owned by the mounted probe.
 * @param {HTMLElement} root Mounted probe host.
 * @param {string} occurrenceId Graphical occurrence identity.
 * @param {number} x Expected accepted x coordinate.
 * @param {number} y Expected accepted y coordinate.
 * @returns {Promise<object|null>} Callback result, or null when unmounted.
 */
export function watch(root, occurrenceId, x, y) { return probes.get(root)?.invokeMethodAsync('WatchBenchmarkGeometry', occurrenceId, x, y) ?? Promise.resolve(null); }
/**
 * Starts the probe-owned diagnostic trace.
 * @param {HTMLElement} root Mounted probe host.
 * @returns {Promise<object|null>} Callback result, or null when unmounted.
 */
export function startTrace(root) { return probes.get(root)?.invokeMethodAsync('StartBenchmarkTrace') ?? Promise.resolve(null); }
/**
 * Stops and reads the probe-owned diagnostic trace.
 * @param {HTMLElement} root Mounted probe host.
 * @returns {Promise<object|null>} Callback result, or null when unmounted.
 */
export function stopTrace(root) { return probes.get(root)?.invokeMethodAsync('StopBenchmarkTrace') ?? Promise.resolve(null); }
