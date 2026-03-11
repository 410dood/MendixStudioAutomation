import { runPowerShellScript } from "./powershell.mjs";
import { clearLastKnownActiveTab, readLastKnownActiveTab, writeLastKnownActiveTab } from "./state-store.mjs";
import { HybridExtensionClient } from "./extension-client.mjs";

export class StudioProClient {
    constructor() {
        this.extensionClient = new HybridExtensionClient();
    }

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

    async createPage(options = {}) {
        const result = await runPowerShellScript("scripts/automation/Create-StudioProPage.ps1", normalizeCreatePageOptions(options));
        await rememberActiveTabFromPayload(result);
        if (options.pageName) {
            await rememberActiveTabByItemName(this, options.pageName, options);
        }
        return result;
    }

    async createClientsPage(options = {}) {
        const normalized = normalizeCreateClientsPageOptions(options);
        const createResult = await this.createPage({
            processId: normalized.processId,
            title: normalized.title,
            module: normalized.module,
            pageName: normalized.pageName,
            template: normalized.template,
            delayMs: normalized.delayMs,
            timeoutMs: normalized.timeoutMs
        });

        if (!createResult?.pageCreated && !createResult?.ok) {
            return {
                ok: false,
                action: "create-clients-page",
                stage: "create-page",
                error: createResult?.error ?? "Studio Pro did not confirm that the new page was opened.",
                createResult
            };
        }

        const pageExplorer = await this.listPageExplorerItems({
            processId: normalized.processId,
            title: normalized.title,
            page: normalized.pageName,
            limit: numberOrDefault(normalized.pageExplorerLimit, 200)
        });

        const pageExplorerItems = Array.isArray(pageExplorer?.items) ? pageExplorer.items : [];
        const targetName = normalized.target || pickDefaultPageExplorerTarget(pageExplorerItems);
        if (!targetName) {
            return {
                ok: false,
                action: "create-clients-page",
                stage: "locate-target",
                pageName: normalized.pageName,
                createResult,
                pageExplorer,
                error: "Could not detect a valid page explorer insertion target."
            };
        }

        const insertResult = await this.insertWidget({
            processId: normalized.processId,
            title: normalized.title,
            page: normalized.pageName,
            target: targetName,
            widget: normalized.widget,
            delayMs: normalized.delayMs
        });

        let navigation = null;
        let navigationError = null;
        if (normalized.addNavigation) {
            try {
                const status = await this.getExtensionStatus({
                    processId: normalized.processId,
                    title: normalized.title
                });
                if (!status?.available) {
                    navigationError = `Navigation update skipped: extension not available (${status?.reason || "not available"}).`;
                }
                else if (!await this.hasExtensionCapability(normalized.processId, normalized.title, "navigation.populate")) {
                    navigationError = "Navigation update skipped: extension capabilities do not include navigation.populate.";
                }
                else {
                    navigation = await this.addNavigationShortcut({
                        processId: normalized.processId,
                        title: normalized.title,
                        pageName: normalized.pageName,
                        page: normalized.pageName,
                        caption: normalized.navigationCaption || normalized.pageName,
                        module: normalized.module
                    });
                }
            }
            catch (error) {
                navigationError = error instanceof Error ? error.message : String(error);
            }
        }

        let capabilities = null;
        try {
            capabilities = await this.getExtensionCapabilities({
                processId: normalized.processId,
                title: normalized.title
            });
        } catch (error) {
            capabilities = {
                ok: false,
                error: error instanceof Error ? error.message : String(error)
            };
        }

        return {
            ok: createResult?.ok && insertResult?.ok,
            action: "create-clients-page",
            pageName: normalized.pageName,
            module: normalized.module,
            selectedTarget: targetName,
            insertWidget: normalized.widget,
            navigation: {
                requested: normalized.addNavigation,
                caption: normalized.navigationCaption,
                result: navigation,
                warning: navigationError
            },
            createResult,
            pageExplorer,
            insertResult,
            capabilities
        };
    }

    async addNavigationShortcut(options = {}) {
        const normalized = normalizeNavigationShortcutOptions(options);

        if (!normalized.page) {
            return {
                ok: false,
                action: "add-navigation-shortcut",
                error: "A --page (or --name) argument is required."
            };
        }

        return this.extensionClient.addNavigationShortcut({
            ...options,
            processId: normalized.processId,
            title: normalized.title,
            page: normalized.page,
            caption: normalized.caption,
            module: normalized.module,
            type: normalized.type
        });
    }

    async hasExtensionCapability(processId, title, capability) {
        const capabilities = await this.getExtensionCapabilities({
            processId,
            title
        });

        const values = Array.isArray(capabilities?.capabilities)
            ? capabilities.capabilities
            : Array.isArray(capabilities?.payload?.capabilities)
                ? capabilities.payload.capabilities
                : [];

        return values.includes(capability);
    }

    async openProperties(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/Open-StudioProProperties.ps1", {
            ProcessId: options.processId,
            WindowTitlePattern: options.title,
            Page: resolvedOptions.page,
            Microflow: resolvedOptions.microflow,
            Item: options.item ?? options.widget ?? options.node,
            Scope: options.scope || "editor",
            DelayMs: numberOrDefault(options.delayMs, 250)
        });
        await rememberActiveTabFromPayload(result?.openMethod);
        if (result?.openMethod?.method === "goTo") {
            await rememberActiveTabByItemName(this, resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item, options);
        }
        return result;
    }

    async openItem(options = {}) {
        const extensionOpenResult = await tryOpenItemViaExtension(this, options);
        if (extensionOpenResult) {
            return extensionOpenResult;
        }

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

    async listScopeElements(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/List-StudioProScopeElements.ps1", {
            ProcessId: options.processId,
            WindowTitlePattern: options.title,
            Item: resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item,
            Scope: options.scope || "editor",
            ControlType: options.controlType,
            NearName: options.nearName ?? options.element ?? options.node ?? options.widget,
            Radius: numberOrDefault(options.radius, 0),
            Limit: numberOrDefault(options.limit, 200)
        });
        await rememberActiveTabFromPayload(result?.openMethod);
        return result;
    }

    async invokeScopeElementAction(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/Invoke-StudioProScopeElementAction.ps1", {
            ProcessId: options.processId,
            WindowTitlePattern: options.title,
            Item: resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item,
            Scope: options.scope || "editor",
            RuntimeId: options.runtimeId,
            Action: options.action || "click",
            DelayMs: numberOrDefault(options.delayMs, 250)
        });
        await rememberActiveTabFromPayload(result?.openMethod);
        return result;
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

    async setDialogField(options = {}) {
        return runPowerShellScript("scripts/automation/Set-StudioProDialogField.ps1", normalizeDialogFieldOptions(options));
    }

    async listEditorMenuItems(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/List-StudioProEditorMenuItems.ps1", {
            ProcessId: options.processId,
            WindowTitlePattern: options.title,
            Item: resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item,
            Element: options.element ?? options.itemName ?? options.node ?? options.widget ?? options.item,
            RuntimeId: options.runtimeId,
            OffsetX: numberOrDefault(options.offsetX, 0),
            OffsetY: numberOrDefault(options.offsetY, 0),
            DelayMs: numberOrDefault(options.delayMs, 250)
        });
        await rememberActiveTabFromPayload(result?.openMethod);
        return result;
    }

    async invokeEditorMenuItem(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/Invoke-StudioProEditorMenuItem.ps1", {
            ProcessId: options.processId,
            WindowTitlePattern: options.title,
            Item: resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item,
            Element: options.element ?? options.itemName ?? options.node ?? options.widget ?? options.item,
            RuntimeId: options.runtimeId,
            OffsetX: numberOrDefault(options.offsetX, 0),
            OffsetY: numberOrDefault(options.offsetY, 0),
            MenuItem: options.menuItem,
            DelayMs: numberOrDefault(options.delayMs, 250)
        });
        await rememberActiveTabFromPayload(result?.openMethod);
        return result;
    }

    async invokeEditorMenuPath(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/Invoke-StudioProEditorMenuPath.ps1", {
            ProcessId: options.processId,
            WindowTitlePattern: options.title,
            Item: resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item,
            Element: options.element ?? options.itemName ?? options.node ?? options.widget ?? options.item,
            RuntimeId: options.runtimeId,
            OffsetX: numberOrDefault(options.offsetX, 0),
            OffsetY: numberOrDefault(options.offsetY, 0),
            MenuPath: options.menuPath,
            DelayMs: numberOrDefault(options.delayMs, 250),
            DryRun: Boolean(options.dryRun)
        });
        await rememberActiveTabFromPayload(result?.openMethod);
        return result;
    }

    async clickEditorOffset(options = {}) {
        const resolvedOptions = await resolveContextItemOption(this, options);
        const result = await runPowerShellScript("scripts/automation/Click-StudioProEditorOffset.ps1", {
            ProcessId: options.processId,
            WindowTitlePattern: options.title,
            Item: resolvedOptions.page ?? resolvedOptions.microflow ?? resolvedOptions.item,
            Element: options.element ?? options.itemName ?? options.node ?? options.widget ?? options.item,
            OffsetX: numberOrDefault(options.offsetX, 0),
            OffsetY: numberOrDefault(options.offsetY, 0),
            DelayMs: numberOrDefault(options.delayMs, 250)
        });
        await rememberActiveTabFromPayload(result?.openMethod);
        return result;
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
        const uiContext = tab ? parseStudioProTabContext(tab.name) : null;
        const extension = await safeGetExtensionContext(this, options);
        const context = mergeActiveContexts(uiContext, extension?.context ?? null);

        return {
            ...result,
            context,
            uiContext,
            extensionContext: extension?.context ?? null,
            contextSource: extension?.context ? "extension+uia" : "uia"
        };
    }

    async getExtensionStatus(options = {}) {
        return this.extensionClient.getStatus(options);
    }

    async getExtensionContext(options = {}) {
        return this.extensionClient.getContext(options);
    }

    async getExtensionCapabilities(options = {}) {
        return this.extensionClient.getCapabilities(options);
    }

    async searchExtensionDocuments(options = {}) {
        return this.extensionClient.searchDocuments(options);
    }

    async openExtensionDocument(options = {}) {
        return this.extensionClient.openDocument(options);
    }

    async getHybridContext(options = {}) {
        const extensionStatus = await this.getExtensionStatus(options);
        if (extensionStatus?.available) {
            try {
                const extension = await this.getExtensionContext(options);
                return {
                    ok: true,
                    source: "extension-webserver",
                    extension
                };
            }
            catch (error) {
                return {
                    ok: true,
                    source: "uia-fallback",
                    extensionError: error instanceof Error ? error.message : String(error),
                    fallback: await this.getActiveContext(options)
                };
            }
        }

        return {
            ok: true,
            source: "uia-fallback",
            extension: extensionStatus,
            fallback: await this.getActiveContext(options)
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

function parseExtensionDocumentKind(documentType, documentName) {
    if (!documentType) {
        return inferStudioProDocumentKind(documentName);
    }

    const normalized = String(documentType).toLowerCase();
    if (normalized.includes("microflow")) {
        return "microflow";
    }

    if (normalized.includes("page")) {
        return "page-or-document";
    }

    if (normalized.includes("snippet")) {
        return "snippet";
    }

    if (normalized.includes("domainmodel") || normalized.includes("domain model")) {
        return "domain-model";
    }

    return inferStudioProDocumentKind(documentName);
}

function mergeActiveContexts(uiContext, extensionContext) {
    if (!extensionContext?.activeDocument) {
        return uiContext;
    }

    const activeDocument = extensionContext.activeDocument;
    const documentName = activeDocument.documentName ?? uiContext?.documentName ?? null;
    const moduleName = activeDocument.moduleName ?? uiContext?.moduleName ?? null;
    const kind = parseExtensionDocumentKind(activeDocument.documentType, documentName);
    const selectionSource = activeDocument.selectionSource && activeDocument.selectionSource !== "not-yet-implemented"
        ? activeDocument.selectionSource
        : (activeDocument.documentName ? "extension-active-document" : uiContext?.selectionSource ?? null);

    return {
        tabName: uiContext?.tabName ?? (documentName && moduleName ? `${documentName} [${moduleName}]` : documentName),
        documentName,
        moduleName,
        kind,
        documentId: activeDocument.documentId ?? null,
        documentType: activeDocument.documentType ?? null,
        selectedElementName: activeDocument.selectedElementName ?? null,
        selectionSource
    };
}

async function safeGetExtensionContext(client, options) {
    try {
        const status = await client.getExtensionStatus(options);
        if (!status?.available) {
            return null;
        }

        const extension = await client.getExtensionContext(options);
        return extension?.available ? extension : null;
    }
    catch {
        return null;
    }
}

async function tryOpenItemViaExtension(client, options) {
    if (!options?.item) {
        return null;
    }

    try {
        const status = await client.getExtensionStatus(options);
        if (!status?.available) {
            return null;
        }

        const openResult = await client.openExtensionDocument({
            ...options,
            name: options.item
        });

        if (!openResult?.payload?.opened) {
            return null;
        }

        const matchedTab = await waitForOpenTabByItemName(client, options.item, options);
        if (matchedTab) {
            await writeLastKnownActiveTab(matchedTab);
        }

        return {
            ok: true,
            method: "extensionOpenDocument",
            verifiedOpen: Boolean(matchedTab),
            attempts: 1,
            tab: matchedTab ?? null,
            extension: openResult.payload
        };
    }
    catch {
        return null;
    }
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

function normalizeCreatePageOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Module: options.module,
        PageName: options.pageName ?? options.name,
        Template: options.template,
        DelayMs: numberOrDefault(options.delayMs, 250),
        TimeoutMs: numberOrDefault(options.timeoutMs, 15000)
    };
}

function normalizeCreateClientsPageOptions(options) {
    return {
        processId: options.processId,
        title: options.title,
        module: options.module || "Az_ClientManagement",
        pageName: options.pageName || options.name || "Clients",
        template: options.template,
        target: options.target,
        widget: options.widget || "Data Grid 2",
        addNavigation: options.addNavigation || options.navigation || options.navigationShortcut,
        navigationCaption: options.navigationCaption || options.navigationItem || options.menuItem,
        delayMs: numberOrDefault(options.delayMs, 250),
        timeoutMs: numberOrDefault(options.timeoutMs, 15000),
        pageExplorerLimit: numberOrDefault(options.pageExplorerLimit, 200),
        capabilities: options.capabilities
    };
}

function normalizeNavigationShortcutOptions(options) {
    return {
        processId: options.processId,
        title: options.title,
        page: options.page ?? options.pageName ?? options.name,
        type: options.type || "Page",
        module: options.module,
        caption: options.caption || options.navigationCaption
    };
}

function pickDefaultPageExplorerTarget(items) {
    if (!Array.isArray(items) || items.length === 0) {
        return null;
    }

    const normalized = items
        .map(item => {
            if (typeof item === "string") {
                return {
                    name: item,
                    lower: item.toLowerCase()
                };
            }

            const name = item?.name ?? "";
            return {
                name,
                lower: String(name ?? "").toLowerCase()
            };
        })
        .filter(item => item.name);

    const containerCandidates = normalized
        .filter(item => /^container\d+$/i.test(item.name) || item.lower.startsWith("container"))
        .sort((left, right) => (left.name.length - right.name.length) || left.name.localeCompare(right.name));

    if (containerCandidates.length > 0) {
        return containerCandidates[0].name;
    }

    return normalized[0].name;
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

function normalizeDialogFieldOptions(options) {
    return {
        ProcessId: options.processId,
        WindowTitlePattern: options.title,
        Dialog: options.dialog,
        Label: options.label,
        Value: options.value ?? "",
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
