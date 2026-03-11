import { runPowerShellScript } from "./powershell.mjs";
import { clearLastKnownActiveTab, readLastKnownActiveTab, writeLastKnownActiveTab } from "./state-store.mjs";

export class StudioProClient {
    async snapshot(options = {}) {
        return runPowerShellScript("scripts/automation/Get-StudioProSnapshot.ps1", normalizeSnapshotOptions(options));
    }

    async findElement(options = {}) {
        return runPowerShellScript("scripts/automation/Find-StudioProElement.ps1", normalizeFindOptions(options));
    }

    async clickElement(options = {}) {
        return runPowerShellScript("scripts/automation/Invoke-StudioProAction.ps1", normalizeActionOptions(options));
    }

    async sendKeys(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/Send-StudioProKeys.ps1", normalizeSendKeysOptions(resolvedOptions));
        await rememberActiveTabFromPayload(result?.openMethod);
        await rememberActiveTabFromPayload(result);
        if (result?.openMethod?.method === "goTo") {
            await rememberActiveTabByItemName(this, resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item, options);
        }
        return result;
    }

    async runLocalApp(options = {}) {
        return this.sendKeys({
            ...options,
            keys: "{F5}"
        });
    }

    async stopLocalApp(options = {}) {
        return this.sendKeys({
            ...options,
            keys: "+{F5}"
        });
    }

    async showResponsiveWeb(options = {}) {
        return this.sendKeys({
            ...options,
            keys: "{F9}"
        });
    }

    async openItem(options = {}) {
        const normalizedOptions = normalizeOpenItemOptions(options);
        let result = await runPowerShellScript("scripts/automation/Open-StudioProItem.ps1", normalizedOptions);
        await rememberActiveTabFromPayload(result);

        if (result?.method !== "goTo" || !options.item) {
            return {
                ...result,
                verifiedOpen: Boolean(result?.tab?.name),
                attempts: 1
            };
        }

        let matchedTab = await waitForOpenTabByItemName(this, options.item, options);
        let attempts = 1;

        if (!matchedTab) {
            result = await runPowerShellScript("scripts/automation/Open-StudioProItem.ps1", normalizedOptions);
            attempts = 2;
            matchedTab = await waitForOpenTabByItemName(this, options.item, options);
        }

        if (matchedTab) {
            await writeLastKnownActiveTab(matchedTab);
        }

        return {
            ...result,
            tab: matchedTab ?? result.tab ?? null,
            verifiedOpen: Boolean(matchedTab),
            attempts
        };
    }

    async selectWidget(options = {}) {
        const resolvedOptions = await resolvePageOption(this, options, { required: false });
        const result = await runPowerShellScript("scripts/automation/Select-StudioProPageWidget.ps1", normalizeSelectWidgetOptions(resolvedOptions));
        await rememberActiveTabFromPayload(result?.openMethod);
        if (result?.openMethod?.method === "goTo" && resolvedOptions.page) {
            await rememberActiveTabByItemName(this, resolvedOptions.page, options);
        }
        return result;
    }

    async getPopupStatus(options = {}) {
        return runPowerShellScript("scripts/automation/Get-StudioProPopupStatus.ps1", normalizeProcessOptions(options));
    }

    async waitUntilReady(options = {}) {
        return runPowerShellScript("scripts/automation/Wait-StudioProReady.ps1", normalizeWaitOptions(options));
    }

    async selectAppExplorerItem(options = {}) {
        return runPowerShellScript("scripts/automation/Select-StudioProAppExplorerItem.ps1", normalizeSelectAppExplorerItemOptions(options));
    }

    async selectExplorerItem(options = {}) {
        const resolvedOptions = await resolvePageOption(this, options, { required: false });
        const result = await runPowerShellScript("scripts/automation/Select-StudioProExplorerItem.ps1", normalizeSelectExplorerItemOptions(resolvedOptions));
        await rememberActiveTabFromPayload(result?.openMethod);
        if (result?.openMethod?.method === "goTo" && resolvedOptions.page) {
            await rememberActiveTabByItemName(this, resolvedOptions.page, options);
        }
        return result;
    }

    async selectToolboxItem(options = {}) {
        return runPowerShellScript("scripts/automation/Select-StudioProToolboxItem.ps1", normalizeSelectToolboxItemOptions(options));
    }

    async listAppExplorerItems(options = {}) {
        return runPowerShellScript("scripts/automation/List-StudioProVisibleTexts.ps1", normalizeVisibleTextOptions(options, "appExplorer"));
    }

    async listDialogs(options = {}) {
        return runPowerShellScript("scripts/automation/List-StudioProDialogs.ps1", normalizeProcessOptions(options));
    }

    async listDialogItems(options = {}) {
        return runPowerShellScript("scripts/automation/List-StudioProDialogItems.ps1", normalizeDialogItemOptions(options));
    }

    async invokeDialogControl(options = {}) {
        return runPowerShellScript("scripts/automation/Invoke-StudioProDialogControl.ps1", normalizeDialogControlOptions(options));
    }

    async listPageExplorerItems(options = {}) {
        const resolvedOptions = await resolvePageOption(this, options, { required: false });
        const result = await runPowerShellScript("scripts/automation/List-StudioProVisibleTexts.ps1", normalizeVisibleTextOptions(resolvedOptions, "pageExplorer"));
        await rememberActiveTabFromPayload(result?.openMethod);
        await rememberActiveTabFromPayload(result);
        if (result?.openMethod?.method === "goTo" && resolvedOptions.page) {
            await rememberActiveTabByItemName(this, resolvedOptions.page, options);
        }
        return result;
    }

    async listToolboxItems(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/List-StudioProVisibleTexts.ps1", normalizeVisibleTextOptions(resolvedOptions, "toolbox"));
        await rememberActiveTabFromPayload(result?.openMethod);
        await rememberActiveTabFromPayload(result);
        if (result?.openMethod?.method === "goTo") {
            await rememberActiveTabByItemName(this, resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item, options);
        }
        return result;
    }

    async listEditorLabels(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/List-StudioProVisibleTexts.ps1", normalizeVisibleTextOptions(resolvedOptions, "editor"));
        await rememberActiveTabFromPayload(result?.openMethod);
        await rememberActiveTabFromPayload(result);
        if (result?.openMethod?.method === "goTo") {
            await rememberActiveTabByItemName(this, resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item, options);
        }
        return result;
    }

    async listOpenTabs(options = {}) {
        const result = await runPowerShellScript("scripts/automation/List-StudioProOpenTabs.ps1", normalizeProcessOptions(options));
        const remembered = await readLastKnownActiveTab();
        const rememberedName = remembered?.tab?.name ?? null;
        const items = Array.isArray(result?.items)
            ? result.items.map(tab => ({
                ...tab,
                context: parseStudioProTabContext(tab.name),
                isActive: tab.isSelected === true || (rememberedName ? tab.name === rememberedName : false),
                activeSource: tab.isSelected === true ? "uia" : (rememberedName && tab.name === rememberedName ? "lastKnown" : null)
            }))
            : [];

        const filtered = filterOpenTabs(items, options);

        return {
            ...result,
            items: filtered,
            count: filtered.length
        };
    }

    async getActiveTab(options = {}) {
        const result = await runPowerShellScript("scripts/automation/Get-StudioProActiveTab.ps1", normalizeProcessOptions(options));
        if (result?.activeTab?.name) {
            return {
                ...result,
                activeTabSource: "uia"
            };
        }

        const remembered = await readLastKnownActiveTab();
        if (!remembered?.tab?.name) {
            return result;
        }

        const openTabs = await this.listOpenTabs(options);
        const match = Array.isArray(openTabs?.items)
            ? openTabs.items.find(tab => tab?.name === remembered.tab.name)
            : null;

        if (!match) {
            return result;
        }

        return {
            ...result,
            activeTab: match,
            activeTabSource: "lastKnown"
        };
    }

    async getActiveContext(options = {}) {
        const result = await this.getActiveTab(options);
        const tab = result?.activeTab ?? null;

        return {
            ...result,
            context: tab ? parseStudioProTabContext(tab.name) : null
        };
    }

    async selectTab(options = {}) {
        const resolvedOptions = await resolveOpenTabOption(this, options, { required: true, allowActive: false });
        const result = await runPowerShellScript("scripts/automation/Select-StudioProTab.ps1", {
            ...normalizeProcessOptions(options),
            Tab: resolvedOptions.tab,
            DelayMs: numberOrDefault(options.delayMs, 250)
        });
        await rememberActiveTabFromPayload(result);
        return {
            ...result,
            tabResolvedFrom: resolvedOptions.tabResolvedFrom ?? null
        };
    }

    async closeTab(options = {}) {
        const resolvedOptions = await resolveTabOption(this, options, { required: true });
        const result = await runPowerShellScript("scripts/automation/Close-StudioProTab.ps1", {
            ...normalizeProcessOptions(options),
            Tab: resolvedOptions.tab,
            DelayMs: numberOrDefault(options.delayMs, 250),
            DryRun: Boolean(options.dryRun)
        });
        if (!options.dryRun) {
            const remembered = await readLastKnownActiveTab();
            if (remembered?.tab?.name === resolvedOptions.tab) {
                await clearLastKnownActiveTab();
            }
        }
        return {
            ...result,
            tabResolvedFrom: resolvedOptions.tabResolvedFrom ?? null
        };
    }

    async insertWidget(options = {}) {
        const resolvedOptions = await resolvePageOption(this, options, { required: true });
        const result = await runPowerShellScript("scripts/automation/Insert-StudioProWidget.ps1", normalizeInsertWidgetOptions(resolvedOptions));
        await rememberActiveTabFromPayload(result?.openMethod);
        if (result?.openMethod?.method === "goTo" && resolvedOptions.page) {
            await rememberActiveTabByItemName(this, resolvedOptions.page, options);
        }
        return result;
    }

    async selectMicroflowNode(options = {}) {
        const resolvedOptions = await resolveMicroflowOption(this, options, { required: true });
        const result = await runPowerShellScript("scripts/automation/Select-StudioProMicroflowNode.ps1", normalizeSelectMicroflowNodeOptions(resolvedOptions));
        await rememberActiveTabFromPayload(result?.openMethod);
        if (result?.openMethod?.method === "goTo" && resolvedOptions.microflow) {
            await rememberActiveTabByItemName(this, resolvedOptions.microflow, options);
        }
        return result;
    }

    async insertAction(options = {}) {
        const resolvedOptions = await resolveMicroflowOption(this, options, { required: true });
        const result = await runPowerShellScript("scripts/automation/Insert-StudioProMicroflowAction.ps1", normalizeInsertActionOptions(resolvedOptions));
        await rememberActiveTabFromPayload(result?.openMethod);
        if (result?.openMethod?.method === "goTo" && resolvedOptions.microflow) {
            await rememberActiveTabByItemName(this, resolvedOptions.microflow, options);
        }
        return result;
    }
}

async function rememberActiveTabFromPayload(payload) {
    const tab = isTabShape(payload?.target)
        ? payload.target
        : isTabShape(payload?.tab)
            ? payload.tab
            : null;

    if (tab?.controlType === "TabItem" && tab?.name) {
        await writeLastKnownActiveTab(tab);
    }
}

async function rememberActiveTabByItemName(client, itemName, options) {
    if (!itemName) {
        return;
    }

    const openTabs = await client.listOpenTabs(options);
    if (!Array.isArray(openTabs?.items)) {
        return;
    }

    const exact = openTabs.items.find(tab => tab?.name === itemName);
    const moduleQualified = openTabs.items.find(tab => typeof tab?.name === "string" && tab.name.startsWith(`${itemName} [`));
    const partial = openTabs.items.find(tab => typeof tab?.name === "string" && tab.name.includes(itemName));
    const match = exact ?? moduleQualified ?? partial ?? null;

    if (match) {
        await writeLastKnownActiveTab(match);
    }
}

async function waitForOpenTabByItemName(client, itemName, options, timeoutMs = 3500, pollMs = 350) {
    const deadline = Date.now() + timeoutMs;
    do {
        const openTabs = await client.listOpenTabs(options);
        if (Array.isArray(openTabs?.items)) {
            const exact = openTabs.items.find(tab => tab?.name === itemName);
            const moduleQualified = openTabs.items.find(tab => typeof tab?.name === "string" && tab.name.startsWith(`${itemName} [`));
            const partial = openTabs.items.find(tab => typeof tab?.name === "string" && tab.name.includes(itemName));
            const match = exact ?? moduleQualified ?? partial ?? null;
            if (match) {
                return match;
            }
        }

        await sleep(pollMs);
    } while (Date.now() < deadline);

    return null;
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function isTabShape(value) {
    return Boolean(value && typeof value === "object" && value.controlType === "TabItem" && value.name);
}

async function resolvePageOption(client, options, { required }) {
    if (options.page) {
        return options;
    }

    const active = await client.getActiveContext(options);
    if (active?.context?.kind === "page-or-document" && active.context.documentName) {
        return {
            ...options,
            page: active.context.documentName,
            pageResolvedFrom: "activeContext"
        };
    }

    if (!required) {
        return options;
    }

    throw new Error("A page is required, or the active tab must be a page-like editor.");
}

async function resolveMicroflowOption(client, options, { required }) {
    if (options.microflow) {
        return options;
    }

    const active = await client.getActiveContext(options);
    if (active?.context?.kind === "microflow" && active.context.documentName) {
        return {
            ...options,
            microflow: active.context.documentName,
            microflowResolvedFrom: "activeContext"
        };
    }

    if (!required) {
        return options;
    }

    throw new Error("A microflow is required, or the active tab must be a microflow editor.");
}

async function resolveTabOption(client, options, { required }) {
    if (options.tab) {
        return resolveOpenTabOption(client, options, { required, allowActive: false });
    }

    const active = await client.getActiveContext(options);
    if (active?.activeTab?.name) {
        return {
            ...options,
            tab: active.activeTab.name,
            tabResolvedFrom: "activeContext"
        };
    }

    if (!required) {
        return options;
    }

    throw new Error("A tab is required, or the automation must know the active editor tab.");
}

async function resolveContextItemOption(client, options) {
    if (options.item || options.page || options.microflow) {
        return options;
    }

    const active = await client.getActiveContext(options);
    if (!active?.context?.documentName) {
        return options;
    }

    if (active.context.kind === "microflow") {
        return {
            ...options,
            microflow: active.context.documentName,
            contextResolvedFrom: "activeContext"
        };
    }

    if (active.context.kind === "page-or-document" || active.context.kind === "snippet") {
        return {
            ...options,
            page: active.context.documentName,
            contextResolvedFrom: "activeContext"
        };
    }

    return {
        ...options,
        item: active.context.documentName,
        contextResolvedFrom: "activeContext"
    };
}

async function resolveOpenTabOption(client, options, { required, allowActive }) {
    const openTabs = await client.listOpenTabs(options);
    const items = Array.isArray(openTabs?.items) ? openTabs.items : [];

    if (!options.tab) {
        if (allowActive) {
            const active = await client.getActiveContext(options);
            if (active?.activeTab?.name) {
                return {
                    ...options,
                    tab: active.activeTab.name,
                    tabResolvedFrom: "activeContext"
                };
            }
        }

        if (!required) {
            return options;
        }

        throw new Error("A tab is required.");
    }

    const query = options.tab;
    const exactName = items.find(tab => tab?.name === query);
    if (exactName) {
        return {
            ...options,
            tab: exactName.name,
            tabResolvedFrom: "exactName"
        };
    }

    const exactDocument = items.find(tab => tab?.context?.documentName === query);
    if (exactDocument) {
        return {
            ...options,
            tab: exactDocument.name,
            tabResolvedFrom: "documentName"
        };
    }

    const partialMatches = items.filter(tab =>
        typeof tab?.name === "string" &&
        (tab.name.includes(query) || tab.context?.documentName?.includes(query))
    );

    if (partialMatches.length === 1) {
        return {
            ...options,
            tab: partialMatches[0].name,
            tabResolvedFrom: "partialName"
        };
    }

    if (partialMatches.length > 1) {
        throw new Error(`Tab query '${query}' matched multiple open tabs: ${partialMatches.map(tab => tab.name).join(", ")}`);
    }

    if (!required) {
        return options;
    }

    throw new Error(`Could not find an open tab matching '${query}'.`);
}

function parseStudioProTabContext(tabName) {
    const moduleMatch = /^(.*)\s\[(.+)\]$/.exec(tabName ?? "");
    const documentName = moduleMatch ? moduleMatch[1] : tabName;
    const moduleName = moduleMatch ? moduleMatch[2] : null;

    return {
        tabName,
        documentName,
        moduleName,
        kind: inferStudioProDocumentKind(documentName)
    };
}

function filterOpenTabs(items, options) {
    return items.filter(tab => {
        if (options.kind && tab.context?.kind !== options.kind) {
            return false;
        }

        if (options.module && tab.context?.moduleName !== options.module) {
            return false;
        }

        return true;
    });
}

function inferStudioProDocumentKind(documentName) {
    if (!documentName) {
        return "unknown";
    }

    if (documentName.startsWith("SNIP_")) {
        return "snippet";
    }

    if (/^(ACT_|SUB_|DS_|IVK_|OCH_|BCO_|BDE_|ADE_|BCe_)/.test(documentName)) {
        return "microflow";
    }

    if (/(ShowPage|Save|Delete|Create|Retrieve|Commit|Validate|Unlock|SignAndLock)$/.test(documentName)) {
        return "microflow";
    }

    if (documentName.includes("Domain model")) {
        return "domain-model";
    }

    return "page-or-document";
}

function normalizeSnapshotOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Depth: numberOrDefault(options.depth, 2),
        MaxChildren: numberOrDefault(options.maxChildren, 25)
    };
}

function normalizeFindOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Depth: numberOrDefault(options.depth, 5),
        MaxResults: numberOrDefault(options.maxResults, 20),
        Name: options.name,
        AutomationId: options.automationId,
        ClassName: options.className,
        ControlType: options.controlType,
        RuntimeId: options.runtimeId
    };
}

function normalizeActionOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        RuntimeId: options.runtimeId,
        Name: options.name,
        AutomationId: options.automationId,
        ClassName: options.className,
        ControlType: options.controlType,
        Action: options.action || "click"
    };
}

function normalizeSendKeysOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Item: options.item,
        Page: options.page,
        Microflow: options.microflow,
        Scope: options.scope,
        Keys: options.keys,
        DelayMs: numberOrDefault(options.delayMs, 250)
    };
}

function normalizeOpenItemOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Item: options.item,
        DelayMs: numberOrDefault(options.delayMs, 250)
    };
}

function normalizeSelectWidgetOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Page: options.page,
        Widget: options.widget,
        Surface: options.surface || "editor",
        DelayMs: numberOrDefault(options.delayMs, 250)
    };
}

function normalizeProcessOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title
    };
}

function normalizeVisibleTextOptions(options, scope) {
    return {
        ...normalizeProcessOptions(options),
        Item: options.item,
        Page: options.page,
        Microflow: options.microflow,
        Scope: scope,
        Limit: numberOrDefault(options.limit, 200)
    };
}

function normalizeWaitOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        TimeoutMs: numberOrDefault(options.timeoutMs, 60000),
        PollMs: numberOrDefault(options.pollMs, 1000)
    };
}

function normalizeSelectExplorerItemOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Page: options.page,
        Item: options.item,
        DelayMs: numberOrDefault(options.delayMs, 250)
    };
}

function normalizeSelectAppExplorerItemOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Item: options.item,
        DelayMs: numberOrDefault(options.delayMs, 250)
    };
}

function normalizeSelectToolboxItemOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Item: options.item,
        DelayMs: numberOrDefault(options.delayMs, 250)
    };
}

function normalizeDialogItemOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Dialog: options.dialog,
        Name: options.name,
        Limit: numberOrDefault(options.limit, 200)
    };
}

function normalizeDialogControlOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Dialog: options.dialog,
        Control: options.control,
        ControlType: options.controlType,
        DelayMs: numberOrDefault(options.delayMs, 250)
    };
}

function normalizeInsertWidgetOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Page: options.page,
        Target: options.target,
        Widget: options.widget,
        DelayMs: numberOrDefault(options.delayMs, 250),
        DryRun: Boolean(options.dryRun)
    };
}

function normalizeSelectMicroflowNodeOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Microflow: options.microflow,
        Node: options.node,
        DelayMs: numberOrDefault(options.delayMs, 250)
    };
}

function normalizeInsertActionOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Microflow: options.microflow,
        Target: options.target,
        ActionName: options.actionName,
        DelayMs: numberOrDefault(options.delayMs, 250),
        DryRun: Boolean(options.dryRun)
    };
}

function numberOrDefault(value, fallback) {
    if (value === undefined) {
        return fallback;
    }

    const parsed = Number.parseInt(String(value), 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}
