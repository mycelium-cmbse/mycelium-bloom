import { spawn } from 'node:child_process';
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import tailwind from '@tailwindcss/cli/package.json' with { type: 'json' };

const projectDirectory = new URL('../', import.meta.url);

try {
    const argumentsToForward = process.argv.slice(2);
    if (argumentsToForward.some(argument => argument !== '--watch')) {
        throw new Error('The CSS build supports only the optional --watch argument.');
    }

    for (const artifact of ['wwwroot/css/tokens.css', 'Styles/Generated/MyceliumTokens/theme.css']) {
        let content;
        try {
            content = await readFile(new URL(artifact, projectDirectory), 'utf8');
        } catch (error) {
            throw new Error(`Cannot read ${artifact}. Restore the checked-in generated artifact before building CSS.`, { cause: error });
        }
        if (!content.trim()) {
            throw new Error(`${artifact} is empty. Replace it with validated upstream output; do not edit token values manually.`);
        }
    }
    const compiler = fileURLToPath(new URL(tailwind.bin.tailwindcss, import.meta.resolve('@tailwindcss/cli/package.json')));
    const child = spawn(process.execPath, [
        compiler, '-i', './Styles/tailwind.css', '-o', './wwwroot/css/app.css',
        ...(argumentsToForward.includes('--watch') ? ['--watch'] : ['--minify'])
    ], { cwd: projectDirectory, stdio: 'inherit', windowsHide: true });
    process.exitCode = await new Promise((resolve, reject) => {
        child.once('error', reject);
        child.once('exit', code => resolve(code ?? 1));
    });
} catch (error) {
    console.error(error.message);
    process.exitCode = 1;
}
