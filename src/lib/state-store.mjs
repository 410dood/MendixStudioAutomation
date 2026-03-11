import { mkdir, readFile, rm, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const stateDir = resolve(process.cwd(), ".automation-state");
const activeTabStatePath = resolve(stateDir, "last-known-active-tab.json");

export async function readLastKnownActiveTab() {
    try {
        const raw = await readFile(activeTabStatePath, "utf8");
        return JSON.parse(raw);
    } catch (error) {
        return null;
    }
}

export async function writeLastKnownActiveTab(tab) {
    if (!tab?.name) {
        return;
    }

    await mkdir(stateDir, { recursive: true });
    const payload = {
        updatedAt: new Date().toISOString(),
        tab
    };
    await writeFile(activeTabStatePath, JSON.stringify(payload, null, 2));
}

export async function clearLastKnownActiveTab() {
    try {
        await rm(activeTabStatePath, { force: true });
    } catch (error) {
        return;
    }
}
