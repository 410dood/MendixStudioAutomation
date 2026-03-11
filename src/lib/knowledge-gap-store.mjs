import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const defaultStorePath = path.resolve(__dirname, "..", "..", ".automation-state", "knowledge-gaps.json");

export async function recordKnowledgeGap(options = {}) {
    const storePath = options.storePath || defaultStorePath;
    const gaps = await readKnowledgeGapStore(storePath);
    const now = new Date().toISOString();

    const gap = {
        id: createGapId(),
        createdAtUtc: now,
        updatedAtUtc: now,
        status: normalizeStatus(options.status),
        topic: normalizeText(options.topic),
        requestedCapability: normalizeText(options.requestedCapability),
        observedIssue: normalizeText(options.observedIssue),
        impact: normalizeText(options.impact),
        context: normalizeText(options.context),
        source: normalizeText(options.source) || "manual"
    };

    gaps.push(gap);
    await writeKnowledgeGapStore(storePath, gaps);
    return gap;
}

export async function listKnowledgeGaps(options = {}) {
    const storePath = options.storePath || defaultStorePath;
    const status = normalizeStatus(options.status, { allowNull: true });
    const limit = numberOrDefault(options.limit, 100);
    const gaps = await readKnowledgeGapStore(storePath);
    const filtered = status
        ? gaps.filter(gap => normalizeStatus(gap.status) === status)
        : gaps;

    return filtered
        .sort((left, right) => (right.createdAtUtc || "").localeCompare(left.createdAtUtc || ""))
        .slice(0, Math.max(1, limit));
}

export async function summarizeKnowledgeGaps(options = {}) {
    const storePath = options.storePath || defaultStorePath;
    const gaps = await readKnowledgeGapStore(storePath);

    const byStatus = {};
    const byCapability = {};
    for (const gap of gaps) {
        const status = normalizeStatus(gap.status);
        byStatus[status] = (byStatus[status] || 0) + 1;

        const capability = gap.requestedCapability || "unspecified";
        byCapability[capability] = (byCapability[capability] || 0) + 1;
    }

    const topCapabilities = Object.entries(byCapability)
        .sort((left, right) => right[1] - left[1])
        .slice(0, 10)
        .map(([capability, count]) => ({
            capability,
            count
        }));

    return {
        total: gaps.length,
        byStatus,
        topCapabilities
    };
}

async function readKnowledgeGapStore(storePath) {
    try {
        const raw = await readFile(storePath, "utf8");
        const parsed = JSON.parse(raw);
        return Array.isArray(parsed) ? parsed : [];
    }
    catch {
        return [];
    }
}

async function writeKnowledgeGapStore(storePath, gaps) {
    await mkdir(path.dirname(storePath), {
        recursive: true
    });

    await writeFile(storePath, JSON.stringify(gaps, null, 2));
}

function createGapId() {
    return `gap-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function normalizeStatus(value, options = {}) {
    if (value === undefined || value === null || value === "") {
        return options.allowNull ? null : "open";
    }

    const normalized = String(value).trim().toLowerCase();
    if (["open", "in_progress", "resolved", "blocked"].includes(normalized)) {
        return normalized;
    }

    return options.allowNull ? null : "open";
}

function normalizeText(value) {
    if (value === undefined || value === null) {
        return null;
    }

    const text = String(value).trim();
    return text.length > 0 ? text : null;
}

function numberOrDefault(value, fallback) {
    if (value === undefined || value === null || value === "") {
        return fallback;
    }

    const parsed = Number.parseInt(String(value), 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}
