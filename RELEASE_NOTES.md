# Release Notes

## 0.1.0

Initial public checkpoint for the Mendix Studio Pro automation repo.

Included in this release:

- Node-based CLI for driving the automation layer
- PowerShell UI Automation helpers for Studio Pro window discovery and interaction
- Studio Pro attach, snapshot, search, click, and focus support
- popup inspection and wait-until-ready commands
- generic Studio Pro key-chord sending
- first-pass shortcuts for local run, stop, and responsive web
- first-pass native properties-dialog opening from selected editor targets
- document opening through Studio Pro `Go to`
- open-editor tab listing and direct tab activation
- best-known active editor tab reporting with last-known fallback
- active editor context parsing from the current tab title
- close-tab command with a safe dry-run path
- close-tab can now target the active editor tab when no explicit tab name is supplied
- open-tab commands now support kind filtering and document-name or partial-name resolution
- open-tab commands now support module-based filtering and disambiguation
- `find` now handles single-match results correctly
- first-pass editor selection for page widgets
- native Studio Pro dialog discovery, dialog item listing, and dialog control invocation
- first-pass selection for:
  - App Explorer items
  - Page Explorer items
  - Toolbox items
- page and toolbox pane inspection now search from the active dock container instead of the whole Studio Pro window
- editor inspection can now scope itself to the active microflow editor container
- editor inspection now scopes correctly to the active page designer for `Client_ClinicalDocument_V3`
- first-pass `insert-widget` flow with `--dry-run`
- `insert-widget` now reaches the native `Select Widget` dialog from Page Explorer targets
- `insert-widget` now disambiguates duplicate widget names in the `Select Widget` dialog by testing whether the `Select` button becomes enabled
- `open-properties` is now validated against editor-surface targets that open `Edit Template Grid 'templateGrid1'`
- page-side widget selection is now validated against live page-designer controls and page-explorer rows
- first-pass microflow commands:
  - `select-microflow-node`
  - `insert-action` with `--dry-run`
- scoped commands now fail fast if they cannot confirm that the requested page or microflow actually opened

Not included in this release:

- Mendix project commit support
- Mendix branch merge support
- guaranteed insertion reliability across every Studio Pro pane/layout state
- verified end-to-end local runtime health checks after `F5`/`F9`
- stable automation for all unopened microflows/documents

Known limitations:

- Studio Pro UI Automation structure changes depending on active panes, popups, and editor type.
- `open-item` is more reliable for already-known or already-open documents than for every unopened asset.
- `select-app-explorer-item` still needs more hardening against alternate left-pane states.
- page explorer can still report Studio Pro's empty-state placeholder for some page tabs; the command now reports that cleanly instead of scraping unrelated panes.
- page-designer validation is currently strongest on `Client_ClinicalDocument_V3`; other pages may still need selector tuning.
- `insert-widget` can now open the native widget picker dialog, but the final page-model mutation is not yet reliable enough to treat as complete authoring.
- run and responsive-browser commands currently verify shortcut delivery, not full runtime/browser readiness.
- `open-properties` is strongest on editor-surface targets right now; `pageExplorer` property opening still needs more hardening.
- unopened documents that Studio Pro does not resolve through `Go to` now fail explicitly instead of returning misleading editor results.
