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
  open-item                   Open a Studio Pro document by name with Ctrl+G
  select-widget               Select a visible named widget or page element
  popup-status                Inspect current Studio Pro popup windows
  wait-ready                  Wait until Studio Pro popups are cleared
  select-app-explorer-item   Select an App Explorer row by exact name
  select-explorer-item        Select a Page Explorer row by exact name
  select-toolbox-item         Select a Toolbox item by exact name
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
  --item <name>               Document, page, snippet, microflow, or entity name to open
  --page <name>               Page to open before selecting a widget
  --widget <name>             Visible widget or element name to select
  --surface <name>            editor or any (default: editor)
  --item <name>               Explorer row name or document name, depending on command
  --timeout-ms <n>            Wait timeout in milliseconds
  --poll-ms <n>               Wait poll interval in milliseconds
  --target <name>             Page Explorer target container/row for insertion
  --dry-run                   Resolve the insertion path without executing it
  --microflow <name>          Microflow to open before node/action operations
  --action-name <name>        Toolbox action name for microflow insertion
  --node <name>               Visible microflow node label to select
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
