import { execFile } from "node:child_process";
import { resolve } from "node:path";
import { promisify } from "node:util";

const execFileAsync = promisify(execFile);

export async function runPowerShellScript(scriptRelativePath, options = {}) {
    const scriptPath = resolve(process.cwd(), scriptRelativePath);
    const args = buildScriptArgs(scriptPath, options);
    const { stdout, stderr } = await execFileAsync("powershell.exe", args, {
        windowsHide: true,
        maxBuffer: 10 * 1024 * 1024
    });

    if (stderr && stderr.trim()) {
        throw new Error(stderr.trim());
    }

    const text = stdout.trim();
    if (!text) {
        return { ok: true };
    }

    try {
        return JSON.parse(text);
    } catch (error) {
        throw new Error(`Failed to parse PowerShell JSON output: ${text}`);
    }
}

function buildScriptArgs(scriptPath, options) {
    const args = [
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        scriptPath
    ];

    for (const [key, value] of Object.entries(options)) {
        if (value === undefined || value === null || value === false) {
            continue;
        }

        const parameterName = `-${key[0].toUpperCase()}${key.slice(1)}`;
        args.push(parameterName);

        if (value !== true) {
            args.push(String(value));
        }
    }

    return args;
}
