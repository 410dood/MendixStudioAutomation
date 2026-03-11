import { access, readFile, readdir, stat } from "node:fs/promises";
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
    "MendixStudioAutomation_Extension",
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

    async getCapabilities(options = {}) {
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

        const capabilities = await fetchJson(discovery.endpoints.capabilitiesUrl, options.timeoutMs);
        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            capabilities
        };
    }

    async searchDocuments(options = {}) {
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

        const documents = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "documents/search", {
            query: options.query ?? options.q,
            module: options.module,
            type: options.type,
            limit: options.limit
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            documents
        };
    }

    async openDocument(options = {}) {
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

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "documents/open", {
            name: options.name,
            module: options.module,
            type: options.type
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowDeleteObject(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/delete-object", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            variable: options.variable
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowCommitObject(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/commit-object", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            variable: options.variable,
            withEvents: options.withEvents,
            refreshInClient: options.refreshInClient
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowChangeAttribute(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/change-attribute", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            attribute: options.attribute,
            variable: options.variable,
            value: options.value,
            changeType: options.changeType,
            commit: options.commit
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addNavigationShortcut(options = {}) {
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

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "navigation/populate", {
            page: options.page ?? options.pageName ?? options.name,
            caption: options.caption,
            module: options.module,
            type: options.type ?? "Page"
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowCreateObject(options = {}) {
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

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/create-object", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            outputVariableName: options.outputVariableName ?? options.outputVariable,
            commit: options.commit,
            refreshInClient: options.refreshInClient,
            initialValues: options.initialValues
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowCreateList(options = {}) {
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

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/create-list", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            outputVariableName: options.outputVariableName ?? options.outputVariable
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowRetrieveDatabase(options = {}) {
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

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/retrieve-database", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            outputVariableName: options.outputVariableName ?? options.outputVariable,
            xPathConstraint: options.xPathConstraint ?? options.xpath,
            retrieveFirst: options.retrieveFirst
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
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
                contextUrl: `${endpointUrl}/mendix-studio-automation/context`,
                capabilitiesUrl: `${endpointUrl}/mendix-studio-automation/capabilities`,
                baseUrl: endpointUrl,
                routePrefix: "mendix-studio-automation"
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

    const parsed = await readJsonFile(endpointFile);

    return {
        available: true,
        source: "endpointFile",
        endpointFile,
        endpoints: {
            healthUrl: parsed.healthUrl,
            contextUrl: parsed.contextUrl,
            capabilitiesUrl: parsed.capabilitiesUrl
                ?? `${(parsed.baseUrl ?? "").replace(/\/$/, "")}/mendix-studio-automation/capabilities`,
                navigationPopulateUrl: parsed.navigationPopulateUrl,
                microflowCreateObjectUrl: parsed.microflowCreateObjectUrl,
                microflowCreateListUrl: parsed.microflowCreateListUrl,
                microflowRetrieveDatabaseUrl: parsed.microflowRetrieveDatabaseUrl,
                microflowDeleteObjectUrl: parsed.microflowDeleteObjectUrl,
                microflowCommitObjectUrl: parsed.microflowCommitObjectUrl,
                microflowChangeAttributeUrl: parsed.microflowChangeAttributeUrl,
                baseUrl: parsed.baseUrl,
                routePrefix: parsed.routePrefix
            }
        };
}

async function resolveEndpointFileFromInstallMetadata() {
    try {
        await access(installMetadataFile, fsConstants.F_OK);
        const parsed = await readJsonFile(installMetadataFile);
        if (parsed.endpointFile && await pathExists(parsed.endpointFile)) {
            return parsed.endpointFile;
        }

        if (parsed.appDirectory) {
            const discovered = await findEndpointFileInExtensionCache(parsed.appDirectory);
            if (discovered) {
                return discovered;
            }
        }

        if (process.env.USERPROFILE) {
            const userRoot = path.join(process.env.USERPROFILE, "Mendix");
            const fallback = await findEndpointFileInTopLevelAppRoots(userRoot);
            if (fallback) {
                return fallback;
            }
        }

        return parsed.endpointFile || null;
    }
    catch {
        return null;
    }
}

async function findEndpointFileInExtensionCache(appDirectory) {
    const cacheRoot = path.join(appDirectory, ".mendix-cache", "extensions-cache");

    if (!(await pathExists(cacheRoot))) {
        return null;
    }

    const cacheEntries = await readdir(cacheRoot, {
        withFileTypes: true
    });

    const candidates = [];
    for (const entry of cacheEntries) {
        if (!entry.isDirectory()) {
            continue;
        }

        const endpointFile = path.join(cacheRoot, entry.name, "runtime", "endpoint.json");
        if (!(await pathExists(endpointFile))) {
            continue;
        }

        try {
            const parsed = await readJsonFile(endpointFile);
            if (parsed.extensionName === "Mendix Studio Automation") {
                const endpointStat = await stat(endpointFile);
                candidates.push({
                    endpointFile,
                    rank: endpointStat.mtimeMs
                });
            }
        }
        catch {
            // Ignore malformed endpoint files from unrelated extensions.
        }
    }

    if (candidates.length === 0) {
        return null;
    }

    candidates.sort((left, right) => right.rank - left.rank);
    return candidates[0].endpointFile;
}

async function findEndpointFileInTopLevelAppRoots(rootDirectory) {
    if (!(await pathExists(rootDirectory))) {
        return null;
    }

    const appCandidates = await readdir(rootDirectory, {
        withFileTypes: true
    });
    for (const app of appCandidates) {
        if (!app.isDirectory()) {
            continue;
        }

        const appDirectory = path.join(rootDirectory, app.name);
        const discovered = await findEndpointFileInExtensionCache(appDirectory);
        if (discovered) {
            return discovered;
        }
    }

    return null;
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

async function readJsonFile(targetPath) {
    const raw = await readFile(targetPath, "utf8");
    return JSON.parse(raw.replace(/^\uFEFF/, ""));
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

function buildExtensionUrl(baseUrl, routeSuffix, query = {}) {
    if (!baseUrl) {
        throw new Error("The extension base URL is missing.");
    }

    const url = new URL(`mendix-studio-automation/${routeSuffix.replace(/^\/+/, "")}`, ensureTrailingSlash(baseUrl));
    for (const [key, value] of Object.entries(query)) {
        if (value === undefined || value === null || value === "") {
            continue;
        }

        url.searchParams.set(key, String(value));
    }

    return url.toString();
}

function ensureTrailingSlash(value) {
    return value.endsWith("/") ? value : `${value}/`;
}

function numberOrDefault(value, fallback) {
    if (value === undefined || value === null || value === "") {
        return fallback;
    }

    const parsed = Number.parseInt(String(value), 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}
