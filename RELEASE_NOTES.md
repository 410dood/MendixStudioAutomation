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
- experimental native dialog field editing by visible label
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
- `insert-widget` now records accept-strategy attempts and before/after Page Explorer snapshots for mutation debugging
- `insert-widget` is now validated for real Page Explorer mutations on visible page containers such as `container39` and `container38`
- dialog-control invocation now reports whether the dialog actually closed
- local-run validation can now surface and inspect Studio Pro `Information` dialogs when deployment is blocked
- `open-properties` is now validated against editor-surface targets that open `Edit Template Grid 'templateGrid1'`
- `open-properties` is now validated against `pageExplorer` targets such as `container34`
- page-side widget selection is now validated against live page-designer controls and page-explorer rows
- first-pass microflow commands:
  - `select-microflow-node`
  - `insert-action` with `--dry-run`
- `insert-action` now records before/after microflow-editor snapshots plus any post-action dialog for live microflow mutation debugging
- editor context-menu automation now supports nested menu paths such as `Add > Activity`
- editor context-menu automation now falls back from `Shift+F10` to native right-click when selected microflow labels open the properties dialog instead of a menu
- editor context-menu automation now supports runtime-id targeting and offset-aware right-click probing for microflow surfaces
- scoped editor inspection can now enumerate and click raw runtime-id elements near a visible label for Mendix microflow reverse engineering
- `click-editor-offset` now provides a placement primitive for clicking relative to a visible editor element
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
- `insert-widget` is now producing real page mutations on validated visible targets, but broader target coverage still needs more hardening across alternate page layouts and scroll states.
- `set-dialog-field` is present but still experimental; it needs more validation across a wider range of Studio Pro property dialogs.
- the current real `insert-action` gesture is still not inserting a visible activity on `ClinicalDocument_ShowPage`; current instrumentation shows it can open the parent microflow properties dialog instead.
- the current `Add > Activity` menu path is stable on `ClinicalDocument_ShowPage`, but it still does not produce a confirmed visible activity mutation by itself or after the current placement-click experiment.
- current runtime-id probing can reliably identify parameter and activity context menus, but connector insertion surfaces still do not expose a stable add-action menu path.
- run and responsive-browser commands now surface blocking Studio Pro dialogs, but they still do not verify a healthy Mendix runtime or browser content.
- `open-properties` is currently strongest on the page designer and `pageExplorer`; other scopes may still need more hardening.
- unopened documents that Studio Pro does not resolve through `Go to` now fail explicitly instead of returning misleading editor results.
