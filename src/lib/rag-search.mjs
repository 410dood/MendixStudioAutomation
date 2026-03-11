import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const DEFAULT_KNOWLEDGE_SOURCES = [
    "README.md",
    "RELEASE_NOTES.md",
    "docs",
    "extensions/MendixStudioAutomation_Extension/README.md",
    "src/lib/operations.mjs",
    "src/cli.mjs"
];

const DEFAULT_EXTENSIONS = new Set([".md", ".mjs", ".cs", ".ps1", ".json"]);

const CURRENT_FILE = fileURLToPath(import.meta.url);
const REPO_ROOT = path.resolve(path.dirname(CURRENT_FILE), "..", "..");

export async function searchAutomationKnowledgeBase(options = {}) {
    const query = String(options.query ?? options.q ?? "").trim();
    if (!query) {
        return {
            ok: false,
            action: "rag-search",
            error: "A --query argument is required."
        };
    }

    const limit = normalizePositiveNumber(options.limit, 20);
    const perFileLimit = normalizePositiveNumber(options.perFileLimit ?? options.perFile, 8);
    const scopeItems = normalizeScopeItems(options.scope ?? options.scopes ?? options.sources);
    const requestedSources = scopeItems.length > 0 ? scopeItems : DEFAULT_KNOWLEDGE_SOURCES;
    const tokens = tokenize(query);
    const queryLower = query.toLowerCase();

    const files = collectKnowledgeFiles(requestedSources);
    const matches = [];
    for (const filePath of files) {
        const relativePath = path.relative(REPO_ROOT, filePath).replace(/\\/g, "/");
        const text = readTextFile(filePath);
        if (!text) {
            continue;
        }

        const fileMatches = scoreFileLines(relativePath, text, tokens, queryLower, perFileLimit);
        matches.push(...fileMatches);
    }

    const ranked = matches
        .sort((left, right) => {
            if (right.score !== left.score) {
                return right.score - left.score;
            }

            if (left.file !== right.file) {
                return left.file.localeCompare(right.file);
            }

            return left.line - right.line;
        });

    const items = ranked.slice(0, limit);

    return {
        ok: true,
        action: "rag-search",
        query,
        tokens,
        searchedFileCount: files.length,
        totalMatches: ranked.length,
        count: items.length,
        items
    };
}

function normalizeScopeItems(value) {
    if (!value) {
        return [];
    }

    return String(value)
        .split(",")
        .map(part => part.trim())
        .filter(Boolean);
}

function collectKnowledgeFiles(sourceItems) {
    const collected = new Set();

    for (const item of sourceItems) {
        const candidatePath = path.resolve(REPO_ROOT, item);
        if (!candidatePath.startsWith(REPO_ROOT)) {
            continue;
        }

        if (!fs.existsSync(candidatePath)) {
            continue;
        }

        const stat = fs.statSync(candidatePath);
        if (stat.isFile()) {
            if (DEFAULT_EXTENSIONS.has(path.extname(candidatePath).toLowerCase())) {
                collected.add(candidatePath);
            }
            continue;
        }

        collectFilesRecursive(candidatePath, collected);
    }

    return [...collected];
}

function collectFilesRecursive(directoryPath, collected) {
    const entries = fs.readdirSync(directoryPath, { withFileTypes: true });
    for (const entry of entries) {
        if (entry.name === "node_modules" || entry.name === "bin" || entry.name === "obj" || entry.name === ".git") {
            continue;
        }

        const entryPath = path.join(directoryPath, entry.name);
        if (entry.isDirectory()) {
            collectFilesRecursive(entryPath, collected);
            continue;
        }

        if (!DEFAULT_EXTENSIONS.has(path.extname(entryPath).toLowerCase())) {
            continue;
        }

        collected.add(entryPath);
    }
}

function readTextFile(filePath) {
    try {
        return fs.readFileSync(filePath, "utf8");
    } catch {
        return null;
    }
}

function scoreFileLines(relativePath, text, tokens, queryLower, perFileLimit) {
    const lines = text.split(/\r?\n/);
    const scored = [];

    for (let index = 0; index < lines.length; index += 1) {
        const line = lines[index];
        if (!line || !line.trim()) {
            continue;
        }

        const lower = line.toLowerCase();
        const tokenHits = tokens.filter(token => lower.includes(token));
        if (tokenHits.length === 0) {
            continue;
        }

        let score = tokenHits.length * 5;
        if (lower.includes(queryLower)) {
            score += 12;
        }

        const normalizedLine = line.trim().replace(/\s+/g, " ");
        scored.push({
            file: relativePath,
            line: index + 1,
            score,
            text: normalizedLine.length > 280 ? `${normalizedLine.slice(0, 277)}...` : normalizedLine
        });
    }

    return scored
        .sort((left, right) => right.score - left.score)
        .slice(0, perFileLimit);
}

function tokenize(text) {
    return [...new Set(
        text
            .toLowerCase()
            .split(/[^a-z0-9_./-]+/g)
            .map(token => token.trim())
            .filter(token => token.length >= 2)
    )];
}

function normalizePositiveNumber(value, fallback) {
    const parsed = Number.parseInt(String(value ?? fallback), 10);
    if (!Number.isFinite(parsed) || parsed <= 0) {
        return fallback;
    }

    return parsed;
}
