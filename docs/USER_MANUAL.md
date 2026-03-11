# User Manual

## Purpose

`MendixStudioAutomation` is a Windows automation layer for controlling the real Mendix Studio Pro application.

It is designed to help with:

- opening assets
- selecting visible page and microflow elements
- navigating common Studio Pro panes
- preparing insertion flows for widgets and microflow actions

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
npm run open-properties -- --page "Client_ClinicalDocument_V3" --item "Structure mode" --scope editor
```

Use these first when Studio Pro behaves unexpectedly or a command seems blocked by a popup.

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
```

Use these commands whenever Studio Pro opens a native WPF dialog and you want to inspect or drive it directly.

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

The current insertion path prefers the native `Page Explorer -> Add widget... -> Select Widget` dialog. It can open and inspect that dialog, but the final page mutation is still being hardened.

### Microflow helpers

Select a visible microflow node label:

```powershell
npm run select-microflow-node -- --microflow "ClinicalDocument_ShowPage" --node "DocumentType"
```

Prepare a microflow action insertion:

```powershell
npm run insert-action -- --microflow "ClinicalDocument_ShowPage" --target "DocumentType" --action-name "Create object" --dry-run
```

As with page insertion, keep `--dry-run` on until the selector path is confirmed.

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
- Native dialog commands are now reliable enough to inspect and drive WPF dialogs like `Select Widget`.
- Editor-surface property opening is validated on `Client_ClinicalDocument_V3` for targets like `Structure mode` and `Parameters (8)`.
- The active pane layout affects which selectors are valid.
- Open editor tabs can be detected and selected, but Studio Pro may still report them as `isOffscreen` even when their bounds are usable.
- `active-tab` falls back to the last tab explicitly selected by this automation if Studio Pro does not expose a selected tab through UI Automation.
- `active-context` is a best-effort parser based on the open tab title. It is useful for command routing, but it is not yet a full Mendix document classifier.
- `select-tab` and `close-tab` accept the full tab title, the document name, or a unique partial match across open tabs.
- `--module` can be used with open-tab commands to disambiguate tabs that share the same document name.
- `App Explorer` selection is present but still less reliable than `Page Explorer` and `Toolbox` selection in the current repo state.
- `open-item` still needs additional hardening for all unopened assets, especially microflows.
- Commands that specify `--page` or `--microflow` now fail explicitly if the requested editor tab could not be confirmed after opening.
- `run-local`, `stop-local`, and `show-responsive-web` currently verify that the correct Studio Pro shortcuts were sent, but they do not yet verify runtime readiness or browser content.
- `open-properties` is currently validated on the page designer and `pageExplorer`. Other scopes may still need tuning.

## Non-Goal

This project does not add Mendix project commit capability. Review and commit model changes manually in Studio Pro or Git after inspection.
