import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { once } from 'node:events';
import { copyFile, cp, mkdir, mkdtemp, readFile, readdir, rm, stat, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, relative, resolve } from 'node:path';
import { createInterface } from 'node:readline';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

const projectDirectory = fileURLToPath(new URL('../../', import.meta.url));
const runtimeName = 'wwwroot/css/tokens.css';
const themeName = 'Styles/Generated/MyceliumTokens/theme.css';
const runtimePath = join(projectDirectory, runtimeName);
const themePath = join(projectDirectory, themeName);
const appName = 'wwwroot/css/app.css';
const appPath = join(projectDirectory, appName);
const tokenPaths = [runtimePath, themePath];
const artifactPaths = [...tokenPaths, appPath];

function msbuild(directory, ...args) {
    return spawnSync('dotnet', ['msbuild', 'Mycelium.Bloom.csproj', '-nologo', ...args], {
        cwd: directory, encoding: 'utf8', windowsHide: true, timeout: 120_000
    });
}

function output(result) {
    assert.ifError(result.error);
    return result.stdout + result.stderr;
}

function properties(directory, ...args) {
    const result = msbuild(directory, '-getProperty:TailwindVersion,TailwindFileName,TailwindExpectedSha256,TailwindExecutable,TailwindArguments', ...args);
    assert.equal(result.status, 0, output(result));
    return JSON.parse(result.stdout).Properties;
}

async function fixture(context, fullSource = false) {
    const directory = await mkdtemp(join(tmpdir(), 'bloom-tailwind-test-'));
    context.after(() => rm(directory, { recursive: true, force: true }));
    if (fullSource) {
        const excluded = new Set(['bin', 'obj', 'node_modules', 'TestResults', '.local']);
        await cp(projectDirectory, directory, { recursive: true, filter: source => !excluded.has(relative(projectDirectory, source).split(/[\\/]/)[0]) });
        return directory;
    }
    for (const name of ['Mycelium.Bloom.csproj', 'Build/Tailwind.targets', runtimeName, themeName, appName]) {
        const destination = join(directory, name);
        await mkdir(dirname(destination), { recursive: true });
        await copyFile(join(projectDirectory, name), destination);
    }
    return directory;
}

test('checked-in runtime and theme artifacts have generated headers, LF bytes and separate roles', async () => {
    const [runtime, theme] = await Promise.all(tokenPaths.map(path => readFile(path, 'utf8')));
    for (const content of [runtime, theme]) {
        assert.match(content, /^\/\*\n \* AUTO-GENERATED FROM MYCELIUM DTCG DESIGN TOKENS\.\n \* DO NOT EDIT MANUALLY\.\n \*\/\n\n/);
        assert.ok(!content.includes('\r'));
    }
    assert.match(runtime, /\n:root \{/);
    assert.match(runtime, /\n\.dark \{/);
    assert.doesNotMatch(runtime, /@theme/);
    assert.match(theme, /\n@theme inline \{/);
    assert.doesNotMatch(theme, /:root|\.dark/);
    assert.match(await readFile(appPath, 'utf8'), /^\/\*! tailwindcss v4\.3\.3 \|/);
});

test('explicit stale-output verification preserves all committed CSS bytes', async () => {
    const before = await Promise.all(artifactPaths.map(path => readFile(path)));
    const modified = await Promise.all(artifactPaths.map(async path => (await stat(path)).mtimeMs));
    const result = msbuild(projectDirectory, '-t:VerifyMyceliumStyles');
    assert.equal(result.status, 0, output(result));
    assert.deepEqual(await Promise.all(artifactPaths.map(path => readFile(path))), before);
    assert.deepEqual(await Promise.all(artifactPaths.map(async path => (await stat(path)).mtimeMs)), modified);
    const compiled = await readFile(join(projectDirectory, 'wwwroot/css/app.css'), 'utf8');
    assert.ok(compiled.length > 0);
    assert.doesNotMatch(compiled, /@theme inline/);
});

for (const name of [runtimeName, themeName]) {
    for (const state of ['missing', 'empty', 'whitespace']) {
        test(`CSS compilation rejects ${state} ${name} before provisioning Tailwind`, async context => {
            const directory = await fixture(context);
            const artifact = join(directory, name);
            if (state === 'missing') await rm(artifact);
            else await writeFile(artifact, state === 'empty' ? '' : ' \r\n\t');
            const result = msbuild(directory, '-t:RegenerateMyceliumStyles');
            assert.notEqual(result.status, 0, output(result));
            assert.ok(output(result).includes(state === 'missing'
                ? `Cannot read ${name}. Restore the checked-in generated artifact before building Bloom.`
                : `${name} is empty. Replace it with validated upstream output; do not edit token values manually.`));
            await assert.rejects(stat(join(directory, 'obj/tailwind')), { code: 'ENOENT' });
            assert.deepEqual(await readFile(join(directory, appName)), await readFile(appPath));
        });
    }
}

for (const [watch, expected] of [['', '--minify'], ['false', '--minify'], ['true', '--watch=always']]) {
    test(`TailwindWatch=${JSON.stringify(watch)} selects ${expected}`, () => {
        assert.equal(properties(projectDirectory, `-p:TailwindWatch=${watch}`).TailwindArguments, expected);
    });
}

test('unsupported watch configuration fails before provisioning Tailwind', async context => {
    const directory = await fixture(context);
    const result = msbuild(directory, '-t:RegenerateMyceliumStyles', '-p:TailwindWatch=unsupported');
    assert.notEqual(result.status, 0, output(result));
    assert.match(output(result), /TailwindWatch must be true or false\./);
    await assert.rejects(stat(join(directory, 'obj/tailwind')), { code: 'ENOENT' });
});

for (const [platform, asset] of [
    ['win-x64', 'windows-x64.exe'], ['linux-x64', 'linux-x64'], ['linux-arm64', 'linux-arm64'],
    ['linux-musl-x64', 'linux-x64-musl'], ['linux-musl-arm64', 'linux-arm64-musl'],
    ['osx-x64', 'macos-x64'], ['osx-arm64', 'macos-arm64']
]) {
    test(`SDK platform ${platform} selects a pinned standalone asset and SHA-256`, () => {
        const settings = properties(projectDirectory, `-p:NETCoreSdkRuntimeIdentifier=${platform}`);
        assert.equal(settings.TailwindVersion, '4.3.3');
        assert.equal(settings.TailwindFileName, `tailwindcss-${asset}`);
        assert.match(settings.TailwindExpectedSha256, /^[a-f0-9]{64}$/);
        assert.ok(settings.TailwindExecutable.endsWith(settings.TailwindFileName));
    });
}

test('unsupported SDK platforms fail before attempting a download', async context => {
    const directory = await fixture(context);
    const result = msbuild(directory, '-t:RegenerateMyceliumStyles', '-p:NETCoreSdkRuntimeIdentifier=unsupported');
    assert.notEqual(result.status, 0, output(result));
    assert.match(output(result), /No standalone Tailwind CLI asset is mapped for SDK platform unsupported\./);
    await assert.rejects(stat(join(directory, 'obj/tailwind')), { code: 'ENOENT' });
});

test('a cached executable with the wrong checksum is discarded without execution', async context => {
    const directory = await fixture(context);
    const executable = resolve(directory, properties(directory).TailwindExecutable);
    await mkdir(dirname(executable), { recursive: true });
    await writeFile(executable, 'This is not the verified Tailwind release.');
    const result = msbuild(directory, '-t:RegenerateMyceliumStyles');
    assert.notEqual(result.status, 0, output(result));
    assert.match(output(result), /Tailwind CLI checksum mismatch/);
    await assert.rejects(stat(executable), { code: 'ENOENT' });
    assert.deepEqual(await readFile(join(directory, appName)), await readFile(appPath));
});

test('compiler errors propagate through the MSBuild entry point', async context => {
    const directory = await fixture(context);
    const executable = resolve(projectDirectory, properties(projectDirectory).TailwindExecutable);
    await writeFile(join(directory, 'Styles/tailwind.css'), '@import "missing-stylesheet.css";\n');
    const result = msbuild(directory, '-t:RegenerateMyceliumStyles', `-p:TailwindToolsDirectory=${dirname(executable)}`);
    assert.notEqual(result.status, 0, output(result));
    assert.match(output(result), /missing-stylesheet\.css/);
    assert.match(output(result), /MSB3073/);
    assert.deepEqual(await readFile(join(directory, appName)), await readFile(appPath));
});

test('watch mode rebuilds after a source change even when MSBuild closes compiler stdin', async context => {
    const directory = await fixture(context);
    const executable = resolve(projectDirectory, properties(projectDirectory).TailwindExecutable);
    const source = join(directory, 'Styles/tailwind.css');
    const compiled = join(directory, 'wwwroot/css/app.css');
    await writeFile(source, '@import "tailwindcss";\n@source inline("block");\n');
    const child = spawn('dotnet', [
        'msbuild', 'Mycelium.Bloom.csproj', '-nologo', '-tl:off', '-t:RegenerateMyceliumStyles',
        '-p:TailwindWatch=true', `-p:TailwindToolsDirectory=${dirname(executable)}`
    ], { cwd: directory, stdio: ['ignore', 'pipe', 'pipe'], windowsHide: true, detached: process.platform !== 'win32' });
    let errors = '';
    child.stderr.on('data', chunk => { errors += chunk; });
    const closed = once(child, 'close');
    const lines = createInterface({ input: child.stdout, signal: AbortSignal.timeout(30_000) });
    const iterator = lines[Symbol.asyncIterator]();
    async function nextBuild() {
        while (true) {
            const { value, done } = await iterator.next();
            assert.equal(done, false, `Compiler stopped before rebuilding: ${errors}`);
            if (/Done in/.test(value)) return;
        }
    }
    try {
        await nextBuild();
        assert.match(await readFile(compiled, 'utf8'), /display: block/);
        await writeFile(source, '@import "tailwindcss";\n@source inline("hidden");\n');
        await nextBuild();
        assert.match(await readFile(compiled, 'utf8'), /display: none/);
        assert.equal(child.exitCode, null);
    } finally {
        if (child.exitCode === null) {
            if (process.platform === 'win32') spawnSync('taskkill', ['/pid', String(child.pid), '/t', '/f'], { windowsHide: true });
            else process.kill(-child.pid, 'SIGTERM');
        }
        await closed;
        lines.close();
    }
});

test('IDE design-time builds do not provision Tailwind or require runtime artifacts', async context => {
    const directory = await fixture(context);
    await rm(join(directory, runtimeName));
    const result = msbuild(directory, '-t:ValidateMyceliumStyles', '-p:DesignTimeBuild=true');
    assert.equal(result.status, 0, output(result));
    await assert.rejects(stat(join(directory, 'obj/tailwind')), { code: 'ENOENT' });
});

test('normal build preparation consumes committed CSS without consulting a CLI or watch configuration', async context => {
    const directory = await fixture(context);
    const before = await Promise.all([runtimeName, themeName, appName].map(name => readFile(join(directory, name))));
    const result = msbuild(directory, '-t:PrepareForBuild', '-p:NETCoreSdkRuntimeIdentifier=unsupported', '-p:TailwindFeedUrl=unavailable', '-p:TailwindWatch=unsupported');
    assert.equal(result.status, 0, output(result));
    assert.deepEqual(await Promise.all([runtimeName, themeName, appName].map(name => readFile(join(directory, name)))), before);
    await assert.rejects(stat(join(directory, 'obj/tailwind')), { code: 'ENOENT' });
});

for (const name of [runtimeName, themeName, appName]) {
    test(`normal build preparation rejects missing committed ${name} without provisioning a CLI`, async context => {
        const directory = await fixture(context);
        await rm(join(directory, name));
        const result = msbuild(directory, '-t:PrepareForBuild');
        assert.notEqual(result.status, 0, output(result));
        assert.ok(output(result).includes(name));
        await assert.rejects(stat(join(directory, 'obj/tailwind')), { code: 'ENOENT' });
    });
}

test('normal build preparation rejects an empty committed application stylesheet', async context => {
    const directory = await fixture(context);
    await writeFile(join(directory, appName), ' \r\n\t');
    const result = msbuild(directory, '-t:PrepareForBuild');
    assert.notEqual(result.status, 0, output(result));
    assert.match(output(result), /Committed wwwroot\/css\/app\.css is empty/);
    await assert.rejects(stat(join(directory, 'obj/tailwind')), { code: 'ENOENT' });
});

test('explicit regeneration from the full source reproduces the committed application stylesheet', async context => {
    const directory = await fixture(context, true);
    const executable = resolve(projectDirectory, properties(projectDirectory).TailwindExecutable);
    const before = await Promise.all(tokenPaths.map(path => readFile(path)));
    await rm(join(directory, appName));
    const result = msbuild(directory, '-t:RegenerateMyceliumStyles', `-p:TailwindToolsDirectory=${dirname(executable)}`);
    assert.equal(result.status, 0, output(result));
    assert.deepEqual(await readFile(join(directory, appName)), await readFile(appPath));
    assert.deepEqual(await Promise.all([runtimeName, themeName].map(name => readFile(join(directory, name)))), before);
});

test('stale verification detects a Razor utility change without overwriting CSS and regeneration repairs it', async context => {
    const directory = await fixture(context);
    const executable = resolve(projectDirectory, properties(projectDirectory).TailwindExecutable);
    const tools = `-p:TailwindToolsDirectory=${dirname(executable)}`;
    await writeFile(join(directory, 'Styles/tailwind.css'), '@import "tailwindcss";\n@import "./Generated/MyceliumTokens/theme.css";\n');
    await rm(join(directory, appName));
    let result = msbuild(directory, '-t:RegenerateMyceliumStyles', tools);
    assert.equal(result.status, 0, output(result));
    const before = await readFile(join(directory, appName));
    result = msbuild(directory, '-t:VerifyMyceliumStyles', tools, '-p:TailwindWatch=true');
    assert.equal(result.status, 0, output(result));
    await mkdir(join(directory, 'Components'));
    const utility = 'z-[' + '123456' + ']';
    await writeFile(join(directory, 'Components/Probe.razor'), `<div class="${utility}"></div>\n`);
    result = msbuild(directory, '-t:VerifyMyceliumStyles', tools);
    assert.notEqual(result.status, 0, output(result));
    assert.match(output(result), /Committed app\.css is stale\. Run dotnet msbuild -t:RegenerateMyceliumStyles and commit wwwroot\/css\/app\.css\./);
    assert.deepEqual(await readFile(join(directory, appName)), before);
    result = msbuild(directory, '-t:RegenerateMyceliumStyles', tools);
    assert.equal(result.status, 0, output(result));
    assert.notDeepEqual(await readFile(join(directory, appName)), before);
    result = msbuild(directory, '-t:VerifyMyceliumStyles', tools);
    assert.equal(result.status, 0, output(result));
    assert.deepEqual(await readFile(join(directory, runtimeName)), await readFile(runtimePath));
    assert.deepEqual(await readFile(join(directory, themeName)), await readFile(themePath));
});

test('an unavailable CLI download fails without changing committed CSS', async context => {
    const directory = await fixture(context);
    const result = msbuild(directory, '-t:RegenerateMyceliumStyles', '-p:TailwindFeedUrl=unavailable');
    assert.notEqual(result.status, 0, output(result));
    assert.match(output(result), /DownloadFile|URI|MSB392/);
    assert.deepEqual(await readFile(join(directory, appName)), await readFile(appPath));
});

test('a verified cached CLI works without contacting the configured feed', () => {
    const result = msbuild(projectDirectory, '-t:VerifyMyceliumStyles', '-p:TailwindFeedUrl=unavailable');
    assert.equal(result.status, 0, output(result));
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
