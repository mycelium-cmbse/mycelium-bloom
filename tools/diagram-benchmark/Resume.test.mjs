// Copyright 2026 Starion Group S.A. SPDX-License-Identifier: Apache-2.0

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, readFile, rm } from 'node:fs/promises';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { validateManifest, readResumeCases } from './Resume.mjs';

const manifest = () => ({ sourceHash: 'source', base: 'base', sdk: 'sdk', runtimes: 'runtime', node: 'node', machine: { cpu: 4 },
    samples: 24, warmups: 32, rtts: [0, 80, 160], sizes: [126], environments: ['docker'], capacity: false,
    viewport: false, loading: false, eagerLoading: false, virtualization: true, asyncRendering: false,
    simpleRouting: false, diagnosticProfile: false, verification: false, interactionsOnly: false, recovery: false,
    sceneHashes: {}, image: 'image', docker: { image: 'sha256:reviewed-image' },
    cases: [{ environment: 'docker', engine: 'z', size: 126, directory: 'docker-126-z', completed: true }] });

test('matching manifest identity is reusable independently of timestamps', () => {
    validateManifest({ ...manifest(), started: 'earlier' }, { ...manifest(), started: 'later' });
});

test('changed source or run configuration is rejected', () => {
    for (const key of Object.keys(manifest()).filter(key => key !== 'cases')) {
        assert.throws(() => validateManifest({ ...manifest(), [key]: 'changed' }, manifest()), /Cannot resume/);
    }
});

test('missing required manifest provenance is rejected', () => {
    for (const key of Object.keys(manifest())) {
        const previous = manifest();
        delete previous[key];
        assert.throws(() => validateManifest(previous, manifest()), /Cannot resume/);
    }
});

test('completed result identity is checked without changing original evidence', async () => {
    const output = await mkdtemp(join(tmpdir(), 'bloom-resume-'));
    try {
        await mkdir(join(output, 'docker-126-z'));
        const original = JSON.stringify(manifest());
        await writeFile(join(output, 'manifest.json'), original);
        const path = join(output, 'docker-126-z', 'results.json');
        const result = { sourceHash: 'source', environment: 'docker', engine: 'z', size: 126, completed: true };
        await writeFile(path, JSON.stringify(result));
        assert.deepEqual(await readResumeCases(output, manifest()), new Set(['docker-126-z']));
        for (const invalid of [{ ...result, sourceHash: 'other' }, { ...result, sourceHash: undefined },
            { ...result, size: 7 }, { ...result, environment: undefined }, { ...result, engine: 'x6' }, { ...result, completed: false }]) {
            await writeFile(path, JSON.stringify(invalid));
            await assert.rejects(readResumeCases(output, manifest()), /Cannot resume/);
            assert.equal(await readFile(join(output, 'manifest.json'), 'utf8'), original);
        }
        await writeFile(path, JSON.stringify(result));
        await mkdir(join(output, 'docker-126-x6'));
        await assert.rejects(readResumeCases(output, manifest()), /missing case provenance/);
    } finally {
        await rm(output, { recursive: true, force: true });
    }
});
