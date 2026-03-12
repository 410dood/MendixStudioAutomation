import { execFile } from "node:child_process";
import { access } from "node:fs/promises";
import { resolve } from "node:path";
import { promisify } from "node:util";

const execFileAsync = promisify(execFile);
const projectPath = resolve(process.cwd(), "tools", "MendixStudioAutomation.FlaUI", "MendixStudioAutomation.FlaUI.csproj");
const runnerDllPath = resolve(process.cwd(), "tools", "MendixStudioAutomation.FlaUI", "bin", "Debug", "net8.0-windows", "MendixStudioAutomation.FlaUI.dll");

export class FlaUIClient {
    async build() {
        try {
            const { stdout } = await execFileAsync("dotnet", [
                "build",
                projectPath,
                "--verbosity",
                "minimal"
            ], {
                windowsHide: true,
                maxBuffer: 20 * 1024 * 1024
            });

            return {
                ok: true,
                action: "build-flaui-runner",
                project: projectPath,
                output: stdout.trim()
            };
        } catch (error) {
            const stdout = error?.stdout?.trim?.() ?? "";
            const stderr = error?.stderr?.trim?.() ?? "";
            return {
                ok: false,
                action: "build-flaui-runner",
                project: projectPath,
                error: stderr || stdout || (error instanceof Error ? error.message : String(error))
            };
        }
    }

    async snapshot(options = {}) {
        return this.run("snapshot", options);
    }

    async listDialogs(options = {}) {
        return this.run("list-dialogs", options);
    }

    async findElements(options = {}) {
        return this.run("find-elements", options);
    }

    async run(command, options = {}) {
        await this.ensureBuilt();
        const args = [
            runnerDllPath,
            command,
            ...buildArgs(options)
        ];

        try {
            const { stdout, stderr } = await execFileAsync("dotnet", args, {
                windowsHide: true,
                maxBuffer: 20 * 1024 * 1024
            });

            if (stderr && stderr.trim()) {
                throw new Error(stderr.trim());
            }

            return parseRunnerOutput(command, stdout);
        } catch (error) {
            if (error?.stdout) {
                try {
                    return parseRunnerOutput(command, error.stdout);
                } catch {
                    // Fall through to the generic error payload below.
                }
            }

            const stdout = error?.stdout?.trim?.() ?? "";
            const stderr = error?.stderr?.trim?.() ?? "";
            return {
                ok: false,
                action: `flaui-${command}`,
                error: stderr || stdout || (error instanceof Error ? error.message : String(error))
            };
        }
    }

    async ensureBuilt() {
        try {
            await access(runnerDllPath);
        } catch {
            const buildResult = await this.build();
            if (!buildResult?.ok) {
                throw new Error(buildResult?.error || "Failed to build the FlaUI runner.");
            }
        }
    }
}

function parseRunnerOutput(command, stdout) {
    const text = stdout.trim();
    if (!text) {
        return {
            ok: true,
            action: `flaui-${command}`
        };
    }

    try {
        return JSON.parse(text);
    } catch (error) {
        throw new Error(`Failed to parse FlaUI JSON output: ${text}`);
    }
}

function buildArgs(options) {
    const args = [];
    for (const [key, value] of Object.entries(options)) {
        if (value === undefined || value === null || value === false) {
            continue;
        }

        args.push(`--${toKebabCase(key)}`);
        if (value !== true) {
            args.push(String(value));
        }
    }

    return args;
}

function toKebabCase(value) {
    return value.replace(/[A-Z]/g, (match) => `-${match.toLowerCase()}`);
}
