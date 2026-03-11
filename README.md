# Mendix Studio Automation

This project is the first slice of an automation layer over Mendix Studio Pro.

It is not a Mendix replacement. It attaches to the real Studio Pro window on Windows and exposes a scriptable control plane that can grow into higher-level actions like:

- opening modules, pages, and microflows
- selecting widgets and toolbox items
- inspecting page explorer or App Explorer state
- building repeatable UI workflows for page editing and microflow construction

## Why this shape

Building a Studio Pro clone is not practical. Driving the real editor is.

This scaffold uses:

- `Node.js` for orchestration and CLI
- `PowerShell` + Windows UI Automation for Studio Pro inspection and interaction

That keeps the core editable in this repo without waiting on a local .NET SDK or external packages.

## Current capabilities

- detect a running Studio Pro process
- attach to its main window
- dump a bounded UI automation tree snapshot
- search Studio Pro UI elements by name, automation id, class, or control type
- invoke or click a matched UI element
- open a document by name through Studio Pro's built-in `Go to` workflow
- select a visible named widget or page element on the active designer surface
- inspect and wait for Studio Pro popups to clear
- list open page and microflow editor tabs
- report the best-known active editor tab
- parse the active editor tab into document/module context
- activate an already open editor tab directly
- prepare or execute closing a specific open editor tab
- select an `App Explorer` row by exact visible name
- select a `Page Explorer` row by exact visible name
- select an exact visible `Toolbox` item by name
- list visible labels from App Explorer, Page Explorer, Toolbox, and the active editor
- scope page/toolbox/editor discovery to the active Studio Pro dock container instead of scanning the whole window
- fail fast when a page or microflow command cannot confirm that Studio Pro actually opened the requested document
- prepare or execute first-pass widget insertion through Page Explorer + Toolbox
- select a visible microflow node/action label
- prepare or execute first-pass microflow action insertion through the Toolbox

## Commands

From this folder:

```powershell
npm run status
npm run snapshot -- --depth 2 --max-children 15
npm run find -- --name "App Explorer"
npm run find -- --control-type TreeItem --name "Document"
npm run click -- --runtime-id "42.333896.3.1"
npm run open-item -- --item "Client_ClinicalDocument_V4"
npm run select-widget -- --page "Client_ClinicalDocument_V3" --widget "Page is empty" --surface pageExplorer
npm run popup-status
npm run wait-ready -- --timeout-ms 60000
npm run list-open-tabs
npm run list-open-tabs -- --kind microflow
npm run list-open-tabs -- --module Az_ClientManagement
npm run active-tab
npm run active-context
npm run select-tab -- --tab "Client_ClinicalDocument_V3" --module "Az_ClientManagement"
npm run close-tab -- --tab "Client_ClinicalDocument_V3 [Az_ClientManagement]" --dry-run
npm run close-tab -- --dry-run
npm run select-app-explorer-item -- --item "Client_ClinicalDocument_V3"
npm run select-explorer-item -- --page "Client_ClinicalDocument_V3" --item "Page is empty"
npm run select-toolbox-item -- --item "Create object"
npm run list-app-explorer-items
npm run list-page-explorer-items -- --page "Client_ClinicalDocument_V3"
npm run list-toolbox-items -- --microflow "ClinicalDocument_ShowPage"
npm run list-editor-labels -- --microflow "ClinicalDocument_ShowPage"
npm run insert-widget -- --page "Client_ClinicalDocument_V4" --target "container34" --widget "Deeply Nested List/Data View" --dry-run
npm run select-microflow-node -- --microflow "ClinicalDocument_ShowPage" --node "DocumentType"
npm run insert-action -- --microflow "ClinicalDocument_ShowPage" --target "DocumentType" --action-name "Create object" --dry-run
```

You can also target the active Studio Pro window by title:

```powershell
node src/cli.mjs snapshot --title "Olari"
```

## Documentation

- [Setup](docs/SETUP.md)
- [User Manual](docs/USER_MANUAL.md)
- [Release Notes](RELEASE_NOTES.md)

## Roadmap

Phase 1:

- reliable attach, snapshot, search, click, focus, invoke
- stable selectors for Studio Pro panes and editors

Phase 2:

- higher-level operations: open page, open microflow, expand module, open toolbox category
- current `open-item`, `select-widget`, `select-explorer-item`, `insert-widget`, `select-microflow-node`, and `insert-action` now prefer an already open matching editor tab before falling back to `Go to`
- current `active-tab` uses the true UI Automation selection state when available, and otherwise falls back to the last tab explicitly activated by this automation
- current `select-widget` can target both the editor surface and a named alternate surface like `pageExplorer`
- current `select-explorer-item` targets exact visible Page Explorer rows from the Page Explorer dock container
- current `select-toolbox-item` targets exact visible Toolbox items from the Toolbox dock container
- current `insert-widget` supports a dry-run verification mode and an execution mode based on toolbox double-click insertion
- current `select-microflow-node` uses the active microflow editor container instead of the whole window
- current `insert-action` follows the same pattern for microflow actions and is verified in `--dry-run` mode
- operation recorder and selector stabilization

## Explicit Non-Goal

This automation project does not add Mendix project commit or branch merge operations. The intent is to let you review model changes in Studio Pro before any version-control action happens.

Phase 3:

- visual workflow runner for page editing and microflow building
- guardrails, replay, verification, and undo-aware operation batches

## Notes

- Studio Pro must already be open on the same Windows desktop session.
- UI Automation trees vary across Mendix versions and screen states, so selectors must be refined against the real app.
- Commands that specify a page or microflow now error if the target could not be confirmed as an open Studio Pro editor tab. This avoids scraping the `Go to` dialog by accident.
- The first goal is dependable control primitives, not yet full end-to-end authoring.
