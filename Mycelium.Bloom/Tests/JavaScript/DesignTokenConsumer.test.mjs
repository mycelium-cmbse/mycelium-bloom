import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { EventEmitter } from 'node:events';
import { readFile, readdir } from 'node:fs/promises';
import { join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';
import tailwind from '@tailwindcss/cli/package.json' with { type: 'json' };
import { buildCss } from '../../Scripts/build-css.mjs';

const projectDirectory = fileURLToPath(new URL('../../', import.meta.url));
const runtimePath = join(projectDirectory, 'wwwroot/css/tokens.css');
const themePath = join(projectDirectory, 'Styles/Generated/MyceliumTokens/theme.css');
const artifactPaths = [runtimePath, themePath];

test('checked-in runtime and theme artifacts have generated headers, LF bytes and separate roles', async () => {
    const [runtime, theme] = await Promise.all(artifactPaths.map(path => readFile(path, 'utf8')));
    for (const content of [runtime, theme]) {
        assert.match(content, /^\/\*\n \* AUTO-GENERATED FROM MYCELIUM DTCG DESIGN TOKENS\.\n \* DO NOT EDIT MANUALLY\.\n \*\/\n\n/);
        assert.ok(!content.includes('\r'));
    }
    assert.match(runtime, /\n:root \{/);
    assert.match(runtime, /\n\.dark \{/);
    assert.doesNotMatch(runtime, /@theme/);
    assert.match(theme, /\n@theme inline \{/);
    assert.doesNotMatch(theme, /:root|\.dark/);
});

test('CSS compilation uses checked-in artifacts without an SDK or package discovery on PATH', async () => {
    const before = await Promise.all(artifactPaths.map(path => readFile(path)));
    const result = spawnSync(process.execPath, ['Scripts/build-css.mjs'], {
        cwd: projectDirectory,
        env: { ...process.env, PATH: '' },
        encoding: 'utf8'
    });
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.deepEqual(await Promise.all(artifactPaths.map(path => readFile(path))), before);
    const compiled = await readFile(join(projectDirectory, 'wwwroot/css/app.css'), 'utf8');
    assert.ok(compiled.length > 0);
    assert.doesNotMatch(compiled, /@theme inline/);
});

for (const missingArtifact of artifactPaths) {
    for (const state of ['missing', 'empty']) {
        const name = relative(projectDirectory, missingArtifact).replaceAll('\\', '/');
        test(`CSS compilation rejects ${state} ${name} before invoking Tailwind`, async () => {
            const messages = [];
            let compilerStarted = false;
            const exitCode = await buildCss([], {
                readArtifact: async (url, encoding) => {
                    assert.equal(encoding, 'utf8');
                    if (fileURLToPath(url) !== missingArtifact) return readFile(url, encoding);
                    if (state === 'missing') throw new Error('Artifact unavailable');
                    return ' \r\n\t';
                },
                startCompiler: () => { compilerStarted = true; },
                reportError: message => messages.push(message)
            });
            assert.equal(exitCode, 1);
            assert.equal(compilerStarted, false);
            assert.deepEqual(messages, [state === 'missing'
                ? `Cannot read ${name}. Restore the checked-in generated artifact before building CSS.`
                : `${name} is empty. Replace it with validated upstream output; do not edit token values manually.`]);
        });
    }
}

for (const argumentsToForward of [[], ['--watch'], ['--watch', '--watch']]) {
    test(`CSS compilation resolves Tailwind and forwards ${JSON.stringify(argumentsToForward)}`, async () => {
        const calls = [];
        const exitCode = await buildCss(argumentsToForward, {
            startCompiler: (...argumentsToSpawn) => {
                calls.push(argumentsToSpawn);
                const child = new EventEmitter();
                queueMicrotask(() => child.emit('exit', 0));
                return child;
            }
        });
        const compiler = fileURLToPath(new URL(tailwind.bin.tailwindcss, import.meta.resolve('@tailwindcss/cli/package.json')));
        assert.equal(exitCode, 0);
        assert.deepEqual(calls, [[process.execPath, [
            compiler, '-i', './Styles/tailwind.css', '-o', './wwwroot/css/app.css',
            argumentsToForward.length === 0 ? '--minify' : '--watch'
        ], { cwd: new URL('../../', import.meta.url), stdio: 'inherit', windowsHide: true }]]);
    });
}

for (const argumentsToForward of [['--minify'], ['--watch=true'], ['--watch', 'unsupported']]) {
    test(`CSS compilation rejects unsupported arguments ${JSON.stringify(argumentsToForward)} before reading artifacts`, async () => {
        const operations = [];
        const messages = [];
        const exitCode = await buildCss(argumentsToForward, {
            readArtifact: async () => { operations.push('read'); },
            startCompiler: () => { operations.push('spawn'); },
            reportError: message => messages.push(message)
        });
        assert.equal(exitCode, 1);
        assert.deepEqual(operations, []);
        assert.deepEqual(messages, ['The CSS build supports only the optional --watch argument.']);
    });
}

for (const childExitCode of [0, 7, null]) {
    test(`CSS compilation propagates child exit code ${childExitCode}`, async () => {
        const messages = [];
        const exitCode = await buildCss([], {
            startCompiler: () => {
                const child = new EventEmitter();
                queueMicrotask(() => child.emit('exit', childExitCode));
                return child;
            },
            reportError: message => messages.push(message)
        });
        assert.equal(exitCode, childExitCode ?? 1);
        assert.deepEqual(messages, []);
    });
}

for (const failure of ['throw', 'error event']) {
    test(`CSS compilation reports child-process ${failure} and fails`, async () => {
        const messages = [];
        const exitCode = await buildCss([], {
            startCompiler: () => {
                const error = new Error('Compiler could not start');
                if (failure === 'throw') throw error;
                const child = new EventEmitter();
                queueMicrotask(() => child.emit('error', error));
                return child;
            },
            reportError: message => messages.push(message)
        });
        assert.equal(exitCode, 1);
        assert.deepEqual(messages, ['Compiler could not start']);
    });
}

test('the CSS CLI rejects unsupported arguments with a diagnostic and failure exit code', () => {
    const result = spawnSync(process.execPath, ['Scripts/build-css.mjs', '--minify'], {
        cwd: projectDirectory,
        encoding: 'utf8'
    });
    assert.equal(result.status, 1);
    assert.equal(result.stderr.trim(), 'The CSS build supports only the optional --watch argument.');
});

test('canonical spacing preserves the consumer layout dimensions without compatibility aliases', async () => {
    const runtime = await readFile(runtimePath, 'utf8');
    const foundation = await readFile(join(projectDirectory, 'Styles/variables.css'), 'utf8');
    const values = new Map([...runtime.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(match => [match[1], match[2]]));
    for (const pixels of [4, 8, 12, 16, 24, 32, 40, 48]) {
        const reference = values.get(`--spacing-${pixels}`);
        const target = /^var\((--[\w-]+)\)$/.exec(reference);
        assert.ok(target, `Expected a shared spacing reference for ${pixels}`);
        assert.equal(values.get(target[1]), `${pixels}px`);
    }
    assert.doesNotMatch(foundation, /var\(/);
    assert.doesNotMatch(foundation, /#[0-9a-f]{3,8}\b|(?:rgb|color-mix)\(/i);
});

test('first-party source and tests forbid the retired custom-property namespace', async () => {
    const repository = resolve(projectDirectory, '..');
    const excludedDirectories = new Set(['bin', 'obj', 'node_modules']);
    const externalSources = new Set(['Mycelium.Bloom/wwwroot/lib', 'Mycelium.Bloom/wwwroot/_content']);
    const generatedAssets = new Set(['Mycelium.Bloom/wwwroot/css/app.css']);
    const violations = [];
    async function inspect(directory) {
        for (const entry of await readdir(directory, { withFileTypes: true })) {
            const file = join(directory, entry.name);
            const name = relative(repository, file).replaceAll('\\', '/');
            if (entry.isDirectory()) {
                if (!excludedDirectories.has(entry.name) && !externalSources.has(name)) await inspect(file);
                continue;
            }
            if (!entry.isFile() || !/\.(?:css|razor|cs|mjs|js|ts|tsx|jsx|html|cshtml)$/.test(entry.name) || generatedAssets.has(name)) continue;
            const content = await readFile(file, 'utf8');
            const match = /--m[b]-/.exec(content);
            if (match) violations.push(`${name}:${content.slice(0, match.index).split('\n').length}: ${match[0]}`);
        }
    }
    await inspect(projectDirectory);
    await inspect(join(repository, 'Mycelium.Bloom.Tests'));
    assert.deepEqual(violations, [], 'Retired custom-property namespace is forbidden. Use canonical generated tokens for shared visual values, or a component-scoped property for component state/layout.');
});

test('shared border and motion values come from the generated canonical artifacts', async () => {
    const runtime = await readFile(runtimePath, 'utf8');
    const theme = await readFile(themePath, 'utf8');
    const foundation = await readFile(join(projectDirectory, 'Styles/variables.css'), 'utf8');
    assert.match(runtime, /--border-width-default: 1px;/);
    assert.match(runtime, /--motion-duration-fast: 150ms;/);
    assert.match(runtime, /--motion-easing-standard: cubic-bezier\(0.25, 0.1, 0.25, 1\);/);
    assert.match(theme, /--border-width-token-default: var\(--border-width-default\);/);
    assert.match(theme, /--transition-duration-fast: var\(--motion-duration-fast\);/);
    assert.match(theme, /--ease-standard: var\(--motion-easing-standard\);/);
    assert.doesNotMatch(foundation, /--[\w-]+\s*:/);
});

test('runtime and Tailwind inputs keep curated typography and domain tokens without promoting inventory', async () => {
    const runtime = await readFile(runtimePath, 'utf8');
    const theme = await readFile(themePath, 'utf8');
    const curated = new Set([...runtime.matchAll(/--typography-typography-([\w-]+)-font-family:/g)].map(match => match[1]));
    assert.equal(curated.size, 20);
    assert.doesNotMatch(runtime + theme, /typography-raw|typography-typography-raw/);
    for (const name of curated) {
        assert.ok(theme.includes(`--text-typography-typography-${name}:`));
    }
    for (const name of ['ownership-aocs-base', 'collaborator-c06', 'lifecycle-open', 'sysml-structure-header']) {
        assert.ok(runtime.includes(`--${name}:`));
        assert.ok(theme.includes(`--color-${name}: var(--${name})`));
    }
    const untriaged = [...runtime.matchAll(/--mycelium-primitive-color-untriaged-([\w-]+):/g)];
    assert.equal(new Set(untriaged.map(match => match[1])).size, 6);
    assert.doesNotMatch(theme, /untriaged/);
    assert.doesNotMatch(runtime, /var\(--mycelium-primitive-color-untriaged-/);
    assert.match(runtime, /--mycelium-primitive-radius-full: 999px;/);
    assert.match(runtime, /--secondary: var\(--muted\);/);
    assert.match(runtime, /--secondary-foreground: var\(--foreground\);/);
    assert.match(theme, /\n@theme inline \{/);
    for (const line of theme.split('\n').filter(line => line.trim().startsWith('--'))) {
        assert.match(line, /^\s*--[\w-]+: var\(--[\w-]+\);\s*$/);
    }
});
