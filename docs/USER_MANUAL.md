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
```

Use these first when Studio Pro behaves unexpectedly or a command seems blocked by a popup.

### Generic discovery

```powershell
npm run find -- --name "Page Explorer"
npm run find -- --control-type TabItem --max-results 50
npm run click -- --name "Toolbox" --control-type TabItem
```

Use `find` to inspect what Studio Pro is exposing through UI Automation before building new selectors.

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
```

Activate one directly:

```powershell
npm run select-tab -- --tab "Client_ClinicalDocument_V3 [Az_ClientManagement]"
```

### Page authoring helpers

Select visible designer text:

```powershell
npm run select-widget -- --page "Client_ClinicalDocument_V4" --widget "Olari_Popup_Default"
```

Select visible Page Explorer rows:

```powershell
npm run select-explorer-item -- --page "Client_ClinicalDocument_V4" --item "container34"
npm run select-explorer-item -- --page "Client_ClinicalDocument_V4" --item "SNIP_PopupSubHeader_Program_StageTemplate"
```

Select a Toolbox item:

```powershell
npm run select-toolbox-item -- --item "Deeply Nested List/Data View"
```

Prepare a widget insertion without changing the page:

```powershell
npm run insert-widget -- --page "Client_ClinicalDocument_V4" --target "container34" --widget "Deeply Nested List/Data View" --dry-run
```

Remove `--dry-run` only when you want to execute the current first-pass insertion flow.

### Microflow helpers

Select a visible microflow node label:

```powershell
npm run select-microflow-node -- --microflow "ClinicalDocument_ShowPage" --node "Call microflow"
```

Prepare a microflow action insertion:

```powershell
npm run insert-action -- --microflow "ClinicalDocument_ShowPage" --target "Call microflow" --action-name "Show page" --dry-run
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
- The active pane layout affects which selectors are valid.
- Open editor tabs can be detected and selected, but Studio Pro may still report them as `isOffscreen` even when their bounds are usable.
- `App Explorer` selection is present but still less reliable than `Page Explorer` and `Toolbox` selection in the current repo state.
- `open-item` still needs additional hardening for all unopened assets, especially microflows.

## Non-Goal

This project does not add Mendix project commit capability. Review and commit model changes manually in Studio Pro or Git after inspection.
