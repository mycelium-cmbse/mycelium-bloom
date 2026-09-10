import { spawn } from 'node:child_process';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import tailwind from '@tailwindcss/cli/package.json' with { type: 'json' };

const projectDirectory = new URL('../', import.meta.url);

export async function buildCss(argumentsToForward, { readArtifact = readFile, startCompiler = spawn, reportError = console.error } = {}) {
    try {
        if (argumentsToForward.some(argument => argument !== '--watch')) {
            throw new Error('The CSS build supports only the optional --watch argument.');
        }

        for (const artifact of ['wwwroot/css/tokens.css', 'Styles/Generated/MyceliumTokens/theme.css']) {
            let content;
            try {
                content = await readArtifact(new URL(artifact, projectDirectory), 'utf8');
            } catch (error) {
                throw new Error(`Cannot read ${artifact}. Restore the checked-in generated artifact before building CSS.`, { cause: error });
            }
            if (!content.trim()) {
                throw new Error(`${artifact} is empty. Replace it with validated upstream output; do not edit token values manually.`);
            }
        }
        const compiler = fileURLToPath(new URL(tailwind.bin.tailwindcss, import.meta.resolve('@tailwindcss/cli/package.json')));
        const child = startCompiler(process.execPath, [
            compiler, '-i', './Styles/tailwind.css', '-o', './wwwroot/css/app.css',
            ...(argumentsToForward.includes('--watch') ? ['--watch'] : ['--minify'])
        ], { cwd: projectDirectory, stdio: 'inherit', windowsHide: true });
        return await new Promise((resolve, reject) => {
            child.once('error', reject);
            child.once('exit', code => resolve(code ?? 1));
        });
    } catch (error) {
        reportError(error.message);
        return 1;
    }
}

if (import.meta.main) {
    process.exitCode = await buildCss(process.argv.slice(2));
}
