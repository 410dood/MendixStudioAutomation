import { runPowerShellScript } from "./powershell.mjs";

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

    async openItem(options = {}) {
        return runPowerShellScript("scripts/automation/Open-StudioProItem.ps1", normalizeOpenItemOptions(options));
    }

    async selectWidget(options = {}) {
        return runPowerShellScript("scripts/automation/Select-StudioProPageWidget.ps1", normalizeSelectWidgetOptions(options));
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
        return runPowerShellScript("scripts/automation/Select-StudioProExplorerItem.ps1", normalizeSelectExplorerItemOptions(options));
    }

    async selectToolboxItem(options = {}) {
        return runPowerShellScript("scripts/automation/Select-StudioProToolboxItem.ps1", normalizeSelectToolboxItemOptions(options));
    }

    async listAppExplorerItems(options = {}) {
        return runPowerShellScript("scripts/automation/List-StudioProVisibleTexts.ps1", {
            ...normalizeProcessOptions(options),
            Scope: "appExplorer",
            Limit: numberOrDefault(options.limit, 200)
        });
    }

    async listPageExplorerItems(options = {}) {
        return runPowerShellScript("scripts/automation/List-StudioProVisibleTexts.ps1", {
            ...normalizeProcessOptions(options),
            Scope: "pageExplorer",
            Limit: numberOrDefault(options.limit, 200)
        });
    }

    async listToolboxItems(options = {}) {
        return runPowerShellScript("scripts/automation/List-StudioProVisibleTexts.ps1", {
            ...normalizeProcessOptions(options),
            Scope: "toolbox",
            Limit: numberOrDefault(options.limit, 200)
        });
    }

    async listEditorLabels(options = {}) {
        return runPowerShellScript("scripts/automation/List-StudioProVisibleTexts.ps1", {
            ...normalizeProcessOptions(options),
            Scope: "editor",
            Limit: numberOrDefault(options.limit, 200)
        });
    }

    async listOpenTabs(options = {}) {
        return runPowerShellScript("scripts/automation/List-StudioProOpenTabs.ps1", normalizeProcessOptions(options));
    }

    async selectTab(options = {}) {
        return runPowerShellScript("scripts/automation/Select-StudioProTab.ps1", {
            ...normalizeProcessOptions(options),
            Tab: options.tab,
            DelayMs: numberOrDefault(options.delayMs, 250)
        });
    }

    async insertWidget(options = {}) {
        return runPowerShellScript("scripts/automation/Insert-StudioProWidget.ps1", normalizeInsertWidgetOptions(options));
    }

    async selectMicroflowNode(options = {}) {
        return runPowerShellScript("scripts/automation/Select-StudioProMicroflowNode.ps1", normalizeSelectMicroflowNodeOptions(options));
    }

    async insertAction(options = {}) {
        return runPowerShellScript("scripts/automation/Insert-StudioProMicroflowAction.ps1", normalizeInsertActionOptions(options));
    }
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
