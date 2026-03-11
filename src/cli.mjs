#!/usr/bin/env node

import { formatOutput, parseArgs } from "./lib/cli-helpers.mjs";
import { StudioProClient } from "./lib/studio-pro-client.mjs";
import { operationCatalog } from "./lib/operations.mjs";

async function main() {
    const { command, options } = parseArgs(process.argv.slice(2));
    const client = new StudioProClient();

    switch (command) {
        case "status":
        case "snapshot": {
            const snapshot = await client.snapshot(options);
            formatOutput(snapshot);
            return;
        }
        case "find": {
            const result = await client.findElement(options);
            formatOutput(result);
            return;
        }
        case "click": {
            const result = await client.clickElement(options);
            formatOutput(result);
            return;
        }
        case "send-keys": {
            const result = await client.sendKeys(options);
            formatOutput(result);
            return;
        }
        case "run-local": {
            const result = await client.runLocalApp(options);
            formatOutput(result);
            return;
        }
        case "stop-local": {
            const result = await client.stopLocalApp(options);
            formatOutput(result);
            return;
        }
        case "show-responsive-web": {
            const result = await client.showResponsiveWeb(options);
            formatOutput(result);
            return;
        }
        case "open-properties": {
            const result = await client.openProperties(options);
            formatOutput(result);
            return;
        }
        case "open-item": {
            const result = await client.openItem(options);
            formatOutput(result);
            return;
        }
        case "select-widget": {
            const result = await client.selectWidget(options);
            formatOutput(result);
            return;
        }
        case "popup-status": {
            const result = await client.getPopupStatus(options);
            formatOutput(result);
            return;
        }
        case "wait-ready": {
            const result = await client.waitUntilReady(options);
            formatOutput(result);
            return;
        }
        case "select-app-explorer-item": {
            const result = await client.selectAppExplorerItem(options);
            formatOutput(result);
            return;
        }
        case "select-explorer-item": {
            const result = await client.selectExplorerItem(options);
            formatOutput(result);
            return;
        }
        case "select-toolbox-item": {
            const result = await client.selectToolboxItem(options);
            formatOutput(result);
            return;
        }
        case "list-dialogs": {
            const result = await client.listDialogs(options);
            formatOutput(result);
            return;
        }
        case "list-scope-elements": {
            const result = await client.listScopeElements(options);
            formatOutput(result);
            return;
        }
        case "invoke-scope-element-action": {
            const result = await client.invokeScopeElementAction(options);
            formatOutput(result);
            return;
        }
        case "list-dialog-items": {
            const result = await client.listDialogItems(options);
            formatOutput(result);
            return;
        }
        case "invoke-dialog-control": {
            const result = await client.invokeDialogControl(options);
            formatOutput(result);
            return;
        }
        case "set-dialog-field": {
            const result = await client.setDialogField(options);
            formatOutput(result);
            return;
        }
        case "list-editor-menu-items": {
            const result = await client.listEditorMenuItems(options);
            formatOutput(result);
            return;
        }
        case "invoke-editor-menu-item": {
            const result = await client.invokeEditorMenuItem(options);
            formatOutput(result);
            return;
        }
        case "invoke-editor-menu-path": {
            const result = await client.invokeEditorMenuPath(options);
            formatOutput(result);
            return;
        }
        case "click-editor-offset": {
            const result = await client.clickEditorOffset(options);
            formatOutput(result);
            return;
        }
        case "list-app-explorer-items": {
            const result = await client.listAppExplorerItems(options);
            formatOutput(result);
            return;
        }
        case "list-page-explorer-items": {
            const result = await client.listPageExplorerItems(options);
            formatOutput(result);
            return;
        }
        case "list-toolbox-items": {
            const result = await client.listToolboxItems(options);
            formatOutput(result);
            return;
        }
        case "list-editor-labels": {
            const result = await client.listEditorLabels(options);
            formatOutput(result);
            return;
        }
        case "list-open-tabs": {
            const result = await client.listOpenTabs(options);
            formatOutput(result);
            return;
        }
        case "active-tab": {
            const result = await client.getActiveTab(options);
            formatOutput(result);
            return;
        }
        case "active-context": {
            const result = await client.getActiveContext(options);
            formatOutput(result);
            return;
        }
        case "select-tab": {
            const result = await client.selectTab(options);
            formatOutput(result);
            return;
        }
        case "close-tab": {
            const result = await client.closeTab(options);
            formatOutput(result);
            return;
        }
        case "insert-widget": {
            const result = await client.insertWidget(options);
            formatOutput(result);
            return;
        }
        case "select-microflow-node": {
            const result = await client.selectMicroflowNode(options);
            formatOutput(result);
            return;
        }
        case "insert-action": {
            const result = await client.insertAction(options);
            formatOutput(result);
            return;
        }
        case "operations": {
            formatOutput(operationCatalog);
            return;
        }
        case "help":
        default:
            printHelp();
    }
}

function printHelp() {
    console.log(`Mendix Studio Automation

Usage:
  node src/cli.mjs <command> [options]

Commands:
  status                      Snapshot the attached Studio Pro window
  snapshot                    Alias for status
  find                        Search for an element in Studio Pro
  click                       Invoke or click a matched element
  send-keys                   Send a Studio Pro key chord to the foreground editor
  run-local                   Run the current app locally in Studio Pro
  stop-local                  Stop a locally running app in Studio Pro
  show-responsive-web         Open the app in Studio Pro's responsive browser view
  open-properties             Open the properties dialog for a selected Studio Pro item
  open-item                   Open a Studio Pro document by name with Ctrl+G
  select-widget               Select a visible named widget or page element
  popup-status                Inspect current Studio Pro popup windows
  wait-ready                  Wait until Studio Pro popups are cleared
  select-app-explorer-item   Select an App Explorer row by exact name
  select-explorer-item        Select a Page Explorer row by exact name
  select-toolbox-item         Select a Toolbox item by exact name
  list-dialogs                List open Studio Pro modal/editor dialogs
  list-scope-elements         List visible elements inside a scoped Studio Pro surface
  invoke-scope-element-action Click or invoke a scoped Studio Pro element by runtime id
  list-dialog-items           List visible named controls inside a Studio Pro dialog
  invoke-dialog-control       Click/select a visible named control in a Studio Pro dialog
  set-dialog-field            Set a native Studio Pro dialog field by its visible label
  list-editor-menu-items      Open an editor element context menu and list its items
  invoke-editor-menu-item     Invoke a named editor element context-menu item
  invoke-editor-menu-path     Invoke a multi-step editor context-menu path
  click-editor-offset         Click a point relative to a visible editor element
  list-app-explorer-items     List visible App Explorer labels
  list-page-explorer-items    List visible Page Explorer labels
  list-toolbox-items          List visible Toolbox labels
  list-editor-labels          List visible editor labels
  list-open-tabs              List open page and microflow editor tabs
  active-tab                  Get the currently active open editor tab
  active-context              Parse the active tab into document/module context
  select-tab                  Activate an open Studio Pro editor tab
  close-tab                   Close a specific open Studio Pro editor tab
  insert-widget              Prepare or execute first-pass widget insertion
  select-microflow-node      Select a visible microflow node label
  insert-action              Prepare or execute first-pass microflow action insertion
  operations                  List planned high-level operations

Options:
  --title <text>              Match Studio Pro main window title
  --process-id <id>           Target a specific Studio Pro process
  --depth <n>                 UI tree traversal depth
  --max-children <n>          Max children emitted per node
  --name <text>               Element name filter
  --automation-id <text>      AutomationId filter
  --class-name <text>         Class name filter
  --control-type <text>       Control type filter, e.g. TreeItem
  --runtime-id <a.b.c>        Runtime id returned by find/snapshot
  --max-results <n>           Max matches returned by find
  --keys <value>              Studio Pro key chord for send-keys, e.g. "{F5}" or "^,"
  --scope <name>              editor, pageExplorer, toolbox, or another named scope
  --item <name>               Document, page, snippet, microflow, or entity name to open
  --page <name>               Page to open before selecting a widget
  --widget <name>             Visible widget or element name to select
  --surface <name>            editor or any (default: editor)
  --timeout-ms <n>            Wait timeout in milliseconds
  --poll-ms <n>               Wait poll interval in milliseconds
  --dialog <name>             Open Studio Pro dialog window name
  --near-name <name>          Sort/filter scope elements around a visible named element
  --radius <n>                Optional max pixel distance from --near-name
  --control <name>            Visible control name inside a Studio Pro dialog
  --label <name>              Visible field label inside a Studio Pro dialog
  --value <text>              Value to set into a native Studio Pro dialog field
  --element <name>            Visible editor element name to target for a context menu
  --menu-item <name>          Editor context-menu item to invoke
  --menu-path <a>b>           Editor context-menu path, e.g. "Add>Activity"
  --offset-x <n>              Horizontal pixel offset from the target element center
  --offset-y <n>              Vertical pixel offset from the target element center
  --target <name>             Page Explorer target container/row for insertion
  --dry-run                   Resolve the insertion path without executing it
  --microflow <name>          Microflow to open before node/action operations
  --action-name <name>        Toolbox action name for microflow insertion
  --node <name>               Visible microflow node label to select
  --tab <name>                Open Studio Pro editor tab name to activate or close
  --kind <name>               Filter open tabs by kind, e.g. microflow or page-or-document
  --module <name>             Filter or disambiguate by Mendix module name
  --control-type <name>       Optional dialog field type filter such as Edit or ComboBox
`);
}

main().catch(error => {
    const payload = {
        ok: false,
        error: error instanceof Error ? error.message : String(error)
    };
    console.error(JSON.stringify(payload, null, 2));
    process.exitCode = 1;
});
