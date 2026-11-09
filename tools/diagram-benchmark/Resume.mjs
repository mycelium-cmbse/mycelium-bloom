// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import { readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';
import { isDeepStrictEqual } from 'node:util';

const identityKeys = ['sourceHash', 'base', 'sdk', 'runtimes', 'node', 'machine',
    'samples', 'warmups', 'rtts', 'sizes', 'environments', 'capacity', 'viewport',
    'loading', 'eagerLoading', 'virtualization', 'asyncRendering', 'simpleRouting', 'diagnosticProfile',
    'verification', 'interactionsOnly', 'recovery', 'sceneHashes', 'image'];

/**
 * Validates persisted run provenance without modifying historical files.
 * @param {object} previous Saved manifest.
 * @param {object} current Requested source and settings.
 * @returns {void}
 * @throws {Error} When provenance is missing or incompatible.
 */
export function validateManifest(previous, current) {
    for (const key of identityKeys) {
        if (previous?.[key] === undefined || current?.[key] === undefined
            || !isDeepStrictEqual(previous[key], current[key])) {
            throw new Error(`Cannot resume: missing or incompatible ${key}. Use a new output directory.`);
        }
    }
    if (!Array.isArray(previous.cases)) throw new Error('Cannot resume: missing case provenance.');
    if (current.environments.includes('docker') && (!previous.docker?.image || !current.docker?.image
        || previous.docker.image !== current.docker.image)) {
        throw new Error('Cannot resume: missing or incompatible Docker image identity.');
    }
}

/**
 * Verifies every reusable result before the runner writes its manifest.
 * @param {string} output Existing evidence directory.
 * @param {object} current Requested run identity.
 * @returns {Promise<Set<string>>} Verified completed case directories.
 * @throws {Error} When a manifest or completed result cannot be verified.
 */
export async function readResumeCases(output, current) {
    const previous = JSON.parse(await readFile(join(output, 'manifest.json'), 'utf8'));
    validateManifest(previous, current);
    const completed = new Set();
    for (const directory of await readdir(output, { withFileTypes: true })) {
        if (directory.isDirectory() && /^(debug|release|publish|docker)-\d+-(z|x6)$/.test(directory.name)
            && !previous.cases.some(entry => entry.directory === directory.name)) {
            throw new Error(`Cannot resume: missing case provenance for ${directory.name}.`);
        }
    }
    for (const entry of previous.cases) {
        if (!entry.completed) continue;
        const directory = `${entry.environment}-${entry.size}-${entry.engine}`;
        if (entry.directory !== directory || !current.environments.includes(entry.environment)
            || !current.sizes.includes(entry.size) || !['z', 'x6'].includes(entry.engine)) {
            throw new Error('Cannot resume: incompatible case identity.');
        }
        const result = JSON.parse(await readFile(join(output, directory, 'results.json'), 'utf8'));
        if (!result.completed || result.sourceHash !== current.sourceHash
            || result.environment !== entry.environment || result.engine !== entry.engine || result.size !== entry.size) {
            throw new Error(`Cannot resume: missing or incompatible result provenance for ${directory}.`);
        }
        completed.add(directory);
    }
    return completed;
}
