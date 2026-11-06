import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { once } from 'node:events';
import { copyFile, cp, mkdir, mkdtemp, readFile, readdir, rm, stat, utimes, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, relative, resolve } from 'node:path';
import { createInterface } from 'node:readline';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

const projectDirectory = fileURLToPath(new URL('../../', import.meta.url));
const runtimeName = 'wwwroot/css/tokens-bloom.css';
const themeName = 'Styles/Generated/MyceliumDesign/theme-mycelium.css';
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
    const root = await mkdtemp(join(tmpdir(), 'bloom-tailwind-test-'));
    const directory = join(root, 'Mycelium.Bloom');
    await mkdir(directory);
    context.after(() => rm(root, { recursive: true, force: true }));
    await copyFile(join(projectDirectory, '../NOTICE'), join(root, 'NOTICE'));
    await copyFile(join(projectDirectory, '../Nuget.Config'), join(root, 'Nuget.Config'));
    if (fullSource) {
        const excluded = new Set(['bin', 'obj', 'node_modules', 'TestResults', '.local']);
        await cp(projectDirectory, directory, { recursive: true, filter: source => !excluded.has(relative(projectDirectory, source).split(/[\\/]/)[0]) });
        const restored = msbuild(directory, '-t:Restore');
        assert.equal(restored.status, 0, output(restored));
        return directory;
    }
    for (const name of ['Mycelium.Bloom.csproj', 'Build/Tailwind.targets', runtimeName, themeName, appName]) {
        const destination = join(directory, name);
        await mkdir(dirname(destination), { recursive: true });
        await copyFile(join(projectDirectory, name), destination);
    }
    const restored = msbuild(directory, '-t:Restore');
    assert.equal(restored.status, 0, output(restored));
    return directory;
}

test('restored runtime and theme artifacts have release headers and separate roles', async () => {
    const [runtime, theme] = await Promise.all(tokenPaths.map(path => readFile(path, 'utf8')));
    for (const content of [runtime, theme]) {
        assert.match(content, /mycelium-packageId: Mycelium.Design.Bloom/);
        const project = await readFile(join(projectDirectory, 'Mycelium.Bloom.csproj'), 'utf8');
        const version = /Include="Mycelium.Design.Bloom" Version="([^"]+)"/.exec(project)[1];
        assert.ok(content.includes(`mycelium-version: ${version}`));
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
    const result = msbuild(projectDirectory, '-t:VerifyMyceliumStyles');
    assert.equal(result.status, 0, output(result));
    assert.deepEqual(await Promise.all(artifactPaths.map(path => readFile(path))), before);
    const compiled = await readFile(join(projectDirectory, 'wwwroot/css/app.css'), 'utf8');
    assert.ok(compiled.length > 0);
    assert.doesNotMatch(compiled, /@theme inline/);
});

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

test('normal preparation regenerates missing assets from the restored package', async context => {
    const directory = await fixture(context, true);
    for (const name of [runtimeName, themeName, appName]) await rm(join(directory, name));
    const executable = resolve(projectDirectory, properties(projectDirectory).TailwindExecutable);
    const result = msbuild(directory, '-t:PrepareForBuild', `-p:TailwindToolsDirectory=${dirname(executable)}`);
    assert.equal(result.status, 0, output(result));
    assert.deepEqual(await readFile(join(directory, appName)), await readFile(appPath));
    assert.deepEqual(await readFile(join(directory, runtimeName)), await readFile(runtimePath));
});

for (const property of ['MyceliumDesignBloomTokensCssPath', 'MyceliumDesignBloomThemeCssPath']) {
    test(`missing package asset ${property} fails even with stale staged CSS`, async context => {
        const directory = await fixture(context);
        const result = msbuild(directory, '-t:PrepareForBuild', `-p:${property}=missing-package-asset.css`);
        assert.notEqual(result.status, 0, output(result));
        assert.match(output(result), /Mycelium.Design.Bloom .* is missing: missing-package-asset.css/);
    });
}

for (const property of ['MyceliumDesignBloomTokensCssPath', 'MyceliumDesignBloomThemeCssPath']) {
    test(`empty package asset ${property} fails before staging`, async context => {
        const directory = await fixture(context);
        const empty = join(directory, 'empty.css');
        await writeFile(empty, ' \r\n\t');
        const result = msbuild(directory, '-t:PrepareForBuild', `-p:${property}=${empty}`);
        assert.notEqual(result.status, 0, output(result));
        assert.match(output(result), /Mycelium.Design.Bloom package CSS is empty/);
        assert.deepEqual(await readFile(join(directory, appName)), await readFile(appPath));
    });
}

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
    await writeFile(join(directory, 'Styles/tailwind.css'), '@import "tailwindcss";\n@import "./Generated/MyceliumDesign/theme-mycelium.css";\n');
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

test('runtime spacing uses pixel dimensions while Tailwind maps quarter-scale steps', async () => {
    const runtime = await readFile(runtimePath, 'utf8');
    const theme = await readFile(themePath, 'utf8');
    for (const pixels of [4, 8, 12, 16, 24, 32, 40, 48]) {
        assert.ok(runtime.includes(`--spacing-${pixels}: ${pixels}px;`));
        assert.ok(theme.includes(`--spacing-${pixels / 4}: var(--spacing-${pixels});`));
    }
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

test('runtime composite typography and semantic domain tokens are published in both modes', async () => {
    const runtime = await readFile(runtimePath, 'utf8');
    const theme = await readFile(themePath, 'utf8');
    const modes = runtime.split('.dark {');
    assert.equal(modes.length, 2);
    for (const mode of modes) {
        for (const [scale, tracking] of [['display-lg', '-0.25px'], ['label-sm', '0.1px'], ['label-xs', '0.5px'], ['body-md', '0px']]) {
            assert.ok(mode.includes(`--typography-typography-${scale}-letter-spacing: ${tracking};`));
            assert.ok(theme.includes(`--tracking-${scale}: var(--typography-typography-${scale}-letter-spacing);`));
        }
        assert.match(mode, /--typography-typography-body-md: 400 14px\/20px Inter;/);
        for (const name of ['ownership-aocs-base', 'collaborator-c06', 'lifecycle-open', 'sysml-structure-header']) {
            assert.ok(mode.includes(`--${name}:`));
            assert.ok(theme.includes(`--color-${name}: var(--${name})`));
        }
    }
});

test('changed package inputs including older timestamps replace staged and compiled CSS', async context => {
    const directory = await fixture(context, true);
    const executable = resolve(projectDirectory, properties(projectDirectory).TailwindExecutable);
    const packageDirectory = join(directory, 'package-fixture');
    await mkdir(packageDirectory);
    await copyFile(runtimePath, join(packageDirectory, 'tokens-bloom.css'));
    const original = await readFile(themePath, 'utf8');
    const theme = join(packageDirectory, 'theme-mycelium.css');
    const args = ['-t:PrepareForBuild', `-p:TailwindToolsDirectory=${dirname(executable)}`, `-p:MyceliumDesignBloomCssDir=${packageDirectory}/`];
    await writeFile(join(directory, 'Components/PackageProbe.razor'), '<div class="bg-primary"></div>');
    for (const mapping of ['var(--destructive)', 'var(--primary)']) {
        await writeFile(theme, original.replace('--color-primary: var(--primary)', `--color-primary: ${mapping}`));
        await utimes(theme, new Date(0), new Date(0));
        const result = msbuild(directory, ...args);
        assert.equal(result.status, 0, output(result));
        assert.match(await readFile(join(directory, appName), 'utf8'), new RegExp('\\.bg-primary\\{background-color:' + mapping.replace(/[()]/g, '\\$&') + '\\}'));
    }
});

for (const target of ['Build', 'Publish']) {
    test(`first ${target} discovers generated static web assets`, async context => {
        const directory = await fixture(context, true);
        for (const name of [runtimeName, themeName, appName]) await rm(join(directory, name));
        const executable = resolve(projectDirectory, properties(projectDirectory).TailwindExecutable);
        const result = msbuild(directory, `-t:${target}`, `-p:TailwindToolsDirectory=${dirname(executable)}`);
        assert.equal(result.status, 0, output(result));
        const manifest = await readFile(join(directory, 'obj/Debug/net10.0/staticwebassets.build.json'), 'utf8');
        for (const name of ['tokens-bloom.css', 'app.css']) assert.ok(manifest.includes(name));
        if (target === 'Publish') {
            for (const name of ['tokens-bloom.css.gz', 'app.css.gz']) {
                assert.ok((await stat(join(directory, 'bin/Debug/net10.0/publish/wwwroot/css', name))).size > 0);
            }
        }
    });
}

test('selected navigation weight compiles from the official mapping without a font shorthand', async () => {
    const theme = await readFile(themePath, 'utf8');
    const weight = /--font-weight-label-xs:\s*([^;]+);/.exec(theme)[1];
    const css = await readFile(appPath, 'utf8');
    assert.ok(css.includes(`{font-weight:${weight}}`));
    const rail = await readFile(join(projectDirectory, 'Components/UI/Organisms/NavigationRail/NavigationRail.razor.css'), 'utf8');
    const selected = /control-option--selected:focus\s*\{([^}]+)\}/.exec(rail)[1];
    assert.doesNotMatch(selected, /font(?:-size|-family|-weight)?\s*:|line-height\s*:/);
});

test('organization headers resolve runtime references and preserve separate typography metrics', async () => {
    const runtime = await readFile(runtimePath, 'utf8');
    for (const component of ['Molecules/PageHeader/PageHeader', 'Organisms/OrganizationHeader/OrganizationHeader']) {
        const css = await readFile(join(projectDirectory, 'Components/UI', component + '.razor.css'), 'utf8');
        for (const [, name] of css.matchAll(/var\((--[\w-]+)/g)) assert.ok(runtime.includes(name + ':'), name);
    }
    const page = await readFile(join(projectDirectory, 'Components/UI/Molecules/PageHeader/PageHeader.razor'), 'utf8');
    assert.match(page, /leading-display-xl/);
    assert.match(page, /--font-weight-heading-md/);
    assert.match(page, /text-body-sm/);
    assert.match(page, /--font-weight-body-sm/);
    const organization = await readFile(join(projectDirectory, 'Components/UI/Organisms/OrganizationHeader/OrganizationHeader.razor'), 'utf8');
    assert.match(organization, /--font-weight-body-md-medium/);
});
