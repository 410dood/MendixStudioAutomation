# User Manual

## Purpose

`MendixStudioAutomation` is a Windows automation layer for controlling the real Mendix Studio Pro application.

It is designed to help with:

- opening assets
- reading hybrid in-Studio context through a supported extension route
- selecting visible page and microflow elements
- navigating common Studio Pro panes
- executing real insertion flows for validated widget and microflow targets

It is not a Mendix replacement and it does not perform Mendix version-control commits.

## Working Style

Use the CLI from the repo root:

```powershell
cd C:\Users\willi\Mendix\MyPluggableWidgets\MendixStudioAutomation
```

Every command talks to the currently running Studio Pro instance.

## Command Categories

### Studio Pro health

```powershell
npm run status
npm run snapshot -- --depth 2 --max-children 20
npm run popup-status
npm run wait-ready -- --timeout-ms 15000
npm run send-keys -- --keys "{ESC}"
npm run run-local
npm run stop-local
npm run show-responsive-web
npm run create-page -- --module "Az_ClientManagement" --page-name "Clients_Auto3"
npm run open-properties -- --page "Client_ClinicalDocument_V3" --item "Structure mode" --scope editor
```

Use these first when Studio Pro behaves unexpectedly or a command seems blocked by a popup.

### Hybrid extension context

Check whether the Mendix extension is loaded:

```powershell
npm run extension-status
```

Read the context directly from the in-Studio extension:

```powershell
npm run extension-context
```

Prefer the extension context and fall back to UI automation if the extension is not active:

```powershell
npm run hybrid-context
```

The extension project lives in `extensions\MendixStudioAutomation_Extension` and is installed into the Mendix app with `scripts\Install-MendixStudioAutomationExtension.ps1`.

### Generic discovery

```powershell
npm run find -- --name "Page Explorer"
npm run find -- --control-type TabItem --max-results 50
npm run click -- --name "Toolbox" --control-type TabItem
```

Use `find` to inspect what Studio Pro is exposing through UI Automation before building new selectors.

### Dialog inspection and control

```powershell
npm run list-dialogs
npm run list-dialog-items -- --dialog "Select Widget" --limit 40
npm run invoke-dialog-control -- --dialog "Select Widget" --control "Text"
npm run invoke-dialog-control -- --dialog "Select Widget" --control "Select" --control-type Button
npm run set-dialog-field -- --dialog "Edit Container 'container39'" --label "Name" --value "container39_test" --control-type Edit
npm run list-editor-menu-items -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType"
npm run invoke-editor-menu-path -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType" --menu-path "Add>Activity"
npm run invoke-editor-menu-path -- --microflow "ClinicalDocument_ShowPage" --menu-path "Add>Activity" --dry-run
npm run click-editor-offset -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType" --offset-x 220 --offset-y 0
```

Use these commands whenever Studio Pro opens a native WPF dialog and you want to inspect or drive it directly.
`set-dialog-field` is currently experimental and is best treated as a targeted helper while the dialog-field heuristics are still being widened.

### Properties dialogs

```powershell
npm run open-properties -- --page "Client_ClinicalDocument_V3" --item "Structure mode" --scope editor
npm run open-properties -- --page "Client_ClinicalDocument_V3" --item "Parameters (8)" --scope editor
npm run open-properties -- --page "Client_ClinicalDocument_V3" --item "container34" --scope pageExplorer
```

This is now validated both for editor-surface targets and for `pageExplorer` rows that expose `Properties` on the context menu.

### Open assets

```powershell
npm run open-item -- --item "Client_ClinicalDocument_V4"
npm run open-item -- --item "ClinicalDocument_ShowPage"
```

If a matching page or microflow is already open, the automation now reuses that tab first. Otherwise it falls back to Studio Pro's built-in `Go to` flow.

### Page creation

Create a new page through Studio Pro's native wizards:

```powershell
npm run create-page -- --module "Az_ClientManagement" --page-name "Clients_Auto3"
```

You can also request a specific visible template name when the right-hand template card is already present in the `Create Page` wizard:

```powershell
npm run create-page -- --module "Az_ClientManagement" --page-name "Clients_Auto4" --template "Dashboard Action Center"
```

The current flow is validated for page creation in `Az_ClientManagement`. It is currently strongest on the default visible right-hand template cards in the `Create Page` dialog.

Create-and-wire a Clients page scaffold with one command:

```powershell
npm run create-clients-page -- --module Az_ClientManagement --page-name Clients --widget "Data Grid 2" --add-navigation --navigation-caption "Clients"
```

Manually add any page to web navigation when the extension is already loaded:

```powershell
npm run add-navigation-shortcut -- --page Client_ClinicalDocument_V3 --module Az_ClientManagement --caption "Clinical Documents"
```

`add-navigation-shortcut` uses the hybrid extension route and writes through the active app's navigation profile.

Open the in-Studio quick create-object modal dialog (prefilled from CLI arguments):

```powershell
npm run open-quick-create-object-dialog -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "CreatedObject"
```

This dialog is also exposed directly from the Studio Pro menu and from microflow document context menus.

### Open tab control

List the currently open page and microflow tabs:

```powershell
npm run list-open-tabs
npm run list-open-tabs -- --kind microflow
npm run list-open-tabs -- --module Az_ClientManagement
```

Report the best-known active tab:

```powershell
npm run active-tab
```

Parse the current tab into document and module context:

```powershell
npm run active-context
```

Activate one directly:

```powershell
npm run select-tab -- --tab "Client_ClinicalDocument_V3" --module "Az_ClientManagement"
```

Prepare closing one without executing the close:

```powershell
npm run close-tab -- --tab "Client_ClinicalDocument_V3 [Az_ClientManagement]" --dry-run
npm run close-tab -- --dry-run
```

### Page authoring helpers

Select visible designer text:

```powershell
npm run select-widget -- --page "Client_ClinicalDocument_V3" --widget "Olari_Popup_Default"
npm run select-widget -- --page "Client_ClinicalDocument_V3" --widget "Structure mode"
```

Select visible Page Explorer rows:

```powershell
npm run select-explorer-item -- --page "Client_ClinicalDocument_V3" --item "container34"
npm run list-page-explorer-items -- --page "Client_ClinicalDocument_V3"
```

Select a Toolbox item:

```powershell
npm run select-toolbox-item -- --item "Text"
npm run list-toolbox-items -- --page "Client_ClinicalDocument_V3"
```

Prepare a widget insertion without changing the page:

```powershell
npm run insert-widget -- --page "Client_ClinicalDocument_V3" --target "container34" --widget "Text" --dry-run
```

Remove `--dry-run` only when you want to execute the current first-pass insertion flow.

The current insertion path prefers the native `Page Explorer -> Add widget... -> Select Widget` dialog. It is now producing confirmed Page Explorer mutations on `Client_ClinicalDocument_V3` for visible targets such as `container39` and `container38`.

### Microflow helpers

Select a visible microflow node label:

```powershell
npm run select-microflow-node -- --microflow "ClinicalDocument_ShowPage" --node "DocumentType"
```

Prepare a microflow action insertion:

```powershell
npm run insert-action -- --microflow "ClinicalDocument_ShowPage" --target "DocumentType" --action-name "Create object" --dry-run
```

Create an object activity in a microflow through the extension API:

```powershell
npm run add-microflow-create-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --commit "YesWithoutEvents" --refresh-in-client false --initial-values '{\"Name\":\"Acme\"}'
```

Create a list activity in a microflow through the extension API:

```powershell
npm run add-microflow-create-list -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "ClientDocumentList"
```

Retrieve objects from database in a microflow through the extension API:

```powershell
npm run add-microflow-retrieve-database -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "ClientDocumentList" --x-path-constraint "[Status='Draft']" --retrieve-first false
```

Retrieve objects by association in a microflow through the extension API:

```powershell
npm run add-microflow-retrieve-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --entity-variable "ClientDocumentObj" --output-variable-name "ClientObj"
```

Filter a list by association in a microflow through the extension API:

```powershell
npm run add-microflow-filter-by-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --list-variable "ClientDocumentList" --output-variable-name "FilteredClientDocumentList" --filter-expression "$ClientObj"
```

Find a list item by association in a microflow through the extension API:

```powershell
npm run add-microflow-find-by-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --list-variable "ClientDocumentList" --output-variable-name "FoundClientDocument" --find-expression "$ClientObj"
```

Delete a scoped variable in a microflow through the extension API:

```powershell
npm run add-microflow-delete-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj"
```

Commit a scoped variable in a microflow through the extension API:

```powershell
npm run add-microflow-commit-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj" --with-events false --refresh-in-client false
```

Rollback a scoped variable in a microflow through the extension API:

```powershell
npm run add-microflow-rollback-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj" --refresh-in-client false
```

Change an attribute on a scoped variable in a microflow through the extension API:

```powershell
npm run add-microflow-change-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "Status" --variable "ClientDocumentObj" --value "Draft" --change-type Set --commit No
```

Change an association on a scoped variable in a microflow through the extension API:

```powershell
npm run add-microflow-change-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --variable "ClientDocumentObj" --value "$ClientObj" --change-type Set --commit No
```

As with page insertion, keep `--dry-run` on until the selector path is confirmed.
The real `insert-action` path now returns before/after microflow-editor snapshots and any post-action Studio Pro dialog, which is useful when the gesture does something unexpected on the canvas.

Inspect or traverse editor context menus directly:

```powershell
npm run list-editor-menu-items -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType"
npm run invoke-editor-menu-path -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType" --menu-path "Add>Activity"
npm run invoke-editor-menu-path -- --microflow "ClinicalDocument_ShowPage" --menu-path "Add>Activity" --dry-run
```

Click a placement point relative to a visible editor element:

```powershell
npm run click-editor-offset -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType" --offset-x 220 --offset-y 0
```

### Knowledge gap tracking

Record and track capability gaps while developing automation:

```powershell
npm run record-knowledge-gap -- --requested-capability "page.insertWidget" --observed-issue "Intermittent selector drift in Select Widget dialog" --impact "Blocks repeatable page automation" --context "Client_ClinicalDocument_V3"
npm run list-knowledge-gaps -- --status open --limit 20
npm run summarize-knowledge-gaps
```

## Recommended Operating Sequence

1. Run `popup-status` or `wait-ready`.
2. Open the intended asset.
3. Use the relevant selection command to verify the target.
4. Use `--dry-run` for insertion commands.
5. Execute the real insertion only after the dry run resolves the correct targets.
6. Review the change inside Studio Pro.

## Known Limits

- Some Studio Pro panes expose rows as text, some as data rows, and some as custom WPF controls.
- The Toolbox pane is now discovered from its own dock container, which is substantially more reliable than the earlier whole-window scan.
- The page-designer path is now validated on `Client_ClinicalDocument_V3`, including `Structure mode`, `Olari_Popup_Default`, and real Page Explorer rows like `container34`.
- `create-page` is now validated for native page creation in `Az_ClientManagement`, including pages like `Clients`, `Clients_Auto2`, and `Clients_Auto3`.
- Native dialog commands are now reliable enough to inspect and drive WPF dialogs like `Select Widget`.
- `insert-widget` now performs real native widget insertion on validated visible page-explorer targets, not just dry-run resolution.
- the repo now includes a real Mendix Studio Pro hybrid extension project using `WebServerExtension` and `MenuExtension`
- `extension-status`, `extension-context`, and `hybrid-context` are now available for in-process context discovery
- navigation insertion (`create-clients-page --add-navigation` and `add-navigation-shortcut`) requires the extension endpoint to be available and works against the active app context
- `open-quick-create-object-dialog` only opens the modal; final mutation still requires clicking `Insert Create Object` in the dialog
- `invoke-dialog-control` now reports whether the target dialog actually closed after the control click.
- Editor-surface property opening is validated on `Client_ClinicalDocument_V3` for targets like `Structure mode` and `Parameters (8)`.
- Nested editor context-menu traversal now works, including `Add > Activity` on `ClinicalDocument_ShowPage`.
- Selected microflow labels may open properties on `Shift+F10`; the editor-menu helpers now retry with native right-click when that happens.
- `click-editor-offset` is available for placement experiments relative to visible page and microflow labels.
- The active pane layout affects which selectors are valid.
- Open editor tabs can be detected and selected, but Studio Pro may still report them as `isOffscreen` even when their bounds are usable.
- `active-tab` falls back to the last tab explicitly selected by this automation if Studio Pro does not expose a selected tab through UI Automation.
- `active-context` is a best-effort parser based on the open tab title. It is useful for command routing, but it is not yet a full Mendix document classifier.
- `select-tab` and `close-tab` accept the full tab title, the document name, or a unique partial match across open tabs.
- `--module` can be used with open-tab commands to disambiguate tabs that share the same document name.
- `App Explorer` selection is present but still less reliable than `Page Explorer` and `Toolbox` selection in the current repo state.
- `open-item` still needs additional hardening for all unopened assets, especially microflows.
- `add-microflow-create-object` inserts `Create object` activities only at the start of the selected microflow.
- `add-microflow-create-list` inserts `Create list` activities only at the start of the selected microflow.
- `add-microflow-retrieve-database` inserts `Retrieve from database` activities only at the start of the selected microflow.
- `add-microflow-retrieve-association` inserts `Retrieve by association` activities only at the start of the selected microflow.
- `add-microflow-filter-by-association` inserts `Filter by association` activities only at the start of the selected microflow.
- `add-microflow-find-by-association` inserts `Find by association` activities only at the start of the selected microflow.
- `add-microflow-delete-object` inserts `Delete object` activities only at the start of the selected microflow.
- `add-microflow-commit-object` inserts `Commit object` activities only at the start of the selected microflow.
- `add-microflow-rollback-object` inserts `Rollback object` activities only at the start of the selected microflow.
- `add-microflow-change-attribute` inserts `Change attribute` activities only at the start of the selected microflow.
- `add-microflow-change-association` inserts `Change association` activities only at the start of the selected microflow.
- Commands that specify `--page` or `--microflow` now fail explicitly if the requested editor tab could not be confirmed after opening.
- `set-dialog-field` is currently experimental and needs more validation across a wider set of Studio Pro dialogs.
- `create-page` currently assumes the target template is already visible in the right-hand template panel. Left-pane template-category switching still needs more hardening.
- `run-local`, `stop-local`, and `show-responsive-web` now expose blocking Studio Pro dialogs cleanly, but they still do not verify runtime readiness or browser content.
- the hybrid extension currently reports active app and document context, but not selected-element identity or Mendix error count yet.
- `open-properties` is currently validated on the page designer and `pageExplorer`. Other scopes may still need tuning.

## Non-Goal

This project does not add Mendix project commit capability. Review and commit model changes manually in Studio Pro or Git after inspection.
