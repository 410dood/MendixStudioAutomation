import { access, readFile } from "node:fs/promises";
import { constants as fsConstants } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const defaultEndpointFile = path.resolve(
    __dirname,
    "..",
    "..",
    "extensions",
    "MendixStudioAutomation.Extension",
    "runtime",
    "endpoint.json"
);
const installMetadataFile = path.resolve(
    __dirname,
    "..",
    "..",
    ".automation-state",
    "hybrid-extension-install.json"
);

export class HybridExtensionClient {
    async getStatus(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: true,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        try {
            const health = await fetchJson(discovery.endpoints.healthUrl, options.timeoutMs);
            return {
                ok: true,
                available: true,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                endpoints: discovery.endpoints,
                health
            };
        }
        catch (error) {
            return {
                ok: true,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                endpoints: discovery.endpoints,
                reason: error instanceof Error ? error.message : String(error)
            };
        }
    }

    async getContext(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const context = await fetchJson(discovery.endpoints.contextUrl, options.timeoutMs);
        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            context
        };
    }
}

async function resolveEndpointDiscovery(options) {
    if (options.endpointUrl) {
        const endpointUrl = String(options.endpointUrl).replace(/\/$/, "");
        return {
            available: true,
            source: "explicitUrl",
            endpointFile: null,
            endpoints: {
                healthUrl: `${endpointUrl}/mendix-studio-automation/health`,
                contextUrl: `${endpointUrl}/mendix-studio-automation/context`
            }
        };
    }

    const endpointFile = options.endpointFile
        || process.env.MENDIX_EXTENSION_ENDPOINT_FILE
        || defaultEndpointFile;

    if (!(await pathExists(endpointFile))) {
        const installedEndpointFile = await resolveEndpointFileFromInstallMetadata();
        if (installedEndpointFile && installedEndpointFile !== endpointFile) {
            return resolveEndpointDiscovery({
                ...options,
                endpointFile: installedEndpointFile
            });
        }
    }

    try {
        await access(endpointFile, fsConstants.F_OK);
    }
    catch {
        return {
            available: false,
            source: "endpointFile",
            endpointFile,
            reason: "Endpoint file not found. Build and load the Mendix Studio Automation extension in Studio Pro first."
        };
    }

    const raw = await readFile(endpointFile, "utf8");
    const parsed = JSON.parse(raw);

    return {
        available: true,
        source: "endpointFile",
        endpointFile,
        endpoints: {
            healthUrl: parsed.healthUrl,
            contextUrl: parsed.contextUrl,
            baseUrl: parsed.baseUrl,
            routePrefix: parsed.routePrefix
        }
    };
}

async function resolveEndpointFileFromInstallMetadata() {
    try {
        await access(installMetadataFile, fsConstants.F_OK);
        const raw = await readFile(installMetadataFile, "utf8");
        const parsed = JSON.parse(raw);
        return parsed.endpointFile || null;
    }
    catch {
        return null;
    }
}

async function pathExists(targetPath) {
    try {
        await access(targetPath, fsConstants.F_OK);
        return true;
    }
    catch {
        return false;
    }
}

async function fetchJson(url, timeoutMs = 3000) {
    if (!url) {
        throw new Error("The extension endpoint URL is missing.");
    }

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), numberOrDefault(timeoutMs, 3000));
    try {
        const response = await fetch(url, {
            method: "GET",
            signal: controller.signal
        });
        if (!response.ok) {
            throw new Error(`The extension endpoint returned HTTP ${response.status}.`);
        }

        return response.json();
    }
    finally {
        clearTimeout(timeout);
    }
}

function numberOrDefault(value, fallback) {
    if (value === undefined || value === null || value === "") {
        return fallback;
    }

    const parsed = Number.parseInt(String(value), 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}
