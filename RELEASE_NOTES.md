# Release Notes

## 0.2.6

Hybrid write-path expansion for microflow attribute mutation.

- Added new extension route:
  - `/mendix-studio-automation/microflows/change-attribute`
- Added Studio Pro client support for:
  - inserting a `Change attribute` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm script:
  - `add-microflow-change-attribute`
  - `npm run add-microflow-change-attribute`
- Added options to support `Change attribute` insertion:
  - `--attribute`
  - `--value`
  - `--change-type`
  - `--entity`
  - `--variable`
  - `--commit`
- Updated operation catalog with:
  - `microflow.changeAttribute`
- Updated documentation:
  - README
  - User Manual
  - Extension README
- Kept commit-related behavior unchanged (review-first workflow; no version-control commits).

## 0.2.5

Hybrid write-path expansion for object lifecycle microflow activities.

- Added new extension routes:
  - `/mendix-studio-automation/microflows/delete-object`
  - `/mendix-studio-automation/microflows/commit-object`
- Added Studio Pro client support for:
  - inserting a `Delete object` action into a selected microflow via extension service APIs
  - inserting a `Commit object` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm scripts:
  - `add-microflow-delete-object`
  - `add-microflow-commit-object`
  - `npm run add-microflow-delete-object`
  - `npm run add-microflow-commit-object`
- Added options to support new microflow action insertion:
  - `--variable`
  - `--with-events`
  - `--refresh-in-client`
- Updated operation catalog with:
  - `microflow.deleteObject`
  - `microflow.commitObject`
- Updated documentation:
  - README
  - User Manual
  - Extension README
- Kept commit-related behavior unchanged (review-first workflow; no version-control commits).

## 0.2.4

Hybrid write-path expansion for microflow model creation.

- Added new extension route:
  - `/mendix-studio-automation/microflows/create-object`
- Added Studio Pro client support for:
  - inserting a `Create object` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm script:
  - `add-microflow-create-object`
  - `npm run add-microflow-create-object`
- Added options to support microflow action insertion:
  - `--microflow`
  - `--module`
  - `--entity`
  - `--output-variable-name`
  - `--commit`
  - `--refresh-in-client`
  - `--initial-values`
- Updated operation catalog with `microflow.createObject`.
- Updated documentation:
  - README
  - User Manual
  - Extension README
- Kept commit-related behavior unchanged (review-first workflow; no version-control commits).

## 0.2.3

Top-level navigation shortcut automation milestone.

- Added an in-process extension route to add pages (or other document types) to the app’s Web navigation:
  - new route: `/mendix-studio-automation/navigation/populate`
  - persisted as `navigationPopulateUrl` in `runtime/endpoint.json`
- Added hybrid client support for the new route:
  - `extensionClient.addNavigationShortcut(...)`
  - `StudioProClient.addNavigationShortcut(...)`
  - new CLI command `add-navigation-shortcut`
- Extended `create-clients-page` to optionally wire the newly created page to Web navigation via:
  - `--add-navigation`
  - `--navigation-caption`
- Updated operation catalog with `studio.addNavigationShortcut`.
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`
  - `RELEASE_NOTES.md`

## 0.2.2

Higher-level page scaffolding milestone.

- Added `create-clients-page` CLI workflow:
  - creates a page via existing `Create-StudioProPage` flow
  - discovers the new page’s Page Explorer target
  - inserts a default `Data Grid 2` widget into the selected target
- Added `create-clients-page` and supporting options to CLI (`node src/cli.mjs` and npm script wrapper).
- Added `studio.createClientsPage` operation in the operation catalog.
- Added README command examples and notes for the new workflow.

- `create-clients-page` currently defaults to module `Az_ClientManagement`, page `Clients`, and widget `Data Grid 2`.

## 0.2.1

Hybrid reliability release.

- Added extension-capability discovery from the hybrid extension route and CLI.
- Added CLI helpers for:
  - `extension-capabilities`
  - `extension-search-documents`
  - `extension-open-document`
- Persisted `capabilitiesUrl` in the extension `endpoint.json` payload to keep discovery deterministic.
- Hardened `Install-MendixStudioAutomationExtension.ps1` by removing legacy invalid extension folders (`MendixStudioAutomation.Extension`, `MendixStudioAutomation.ProbeExtension`, `MendixStudioAutomation_ProbeExtension`) before install.
- Updated extension install metadata cleanup and scripts so hybrid restarts are less brittle.

## 0.2.0

Hybrid extension foundation release.

Included in this release:

- real Mendix C# extension project at `extensions/MendixStudioAutomation_Extension`
- exact package pin to `Mendix.StudioPro.ExtensionsAPI 10.24.14-build.90436`
- supported `WebServerExtension` with:
  - `/mendix-studio-automation/health`
  - `/mendix-studio-automation/context`
  - `/mendix-studio-automation/capabilities`
- supported `MenuExtension` for manual verification inside Studio Pro
- extension runtime discovery file written to `runtime/endpoint.json`
- Node hybrid client for extension discovery and HTTP calls
- new commands:
  - `extension-status`
  - `extension-context`
  - `hybrid-context`
- app-local install helper script:
  - `scripts/Install-MendixStudioAutomationExtension.ps1`
- `hybrid-context` now falls back cleanly to UI Automation when the extension is not active

Not included in this release:

- automatic Studio Pro extension installation through the Node CLI
- selected widget or microflow node identity from the extension
- extension-backed Mendix error count or consistency-check reporting
- extension-backed write operations

## 0.1.0

Initial public checkpoint for the Mendix Studio Pro automation repo.

Included in this release:

- Node-based CLI for driving the automation layer
- PowerShell UI Automation helpers for Studio Pro window discovery and interaction
- Studio Pro attach, snapshot, search, click, and focus support
- popup inspection and wait-until-ready commands
- generic Studio Pro key-chord sending
- first-pass shortcuts for local run, stop, and responsive web
- first-pass native page creation through Studio Pro `New Document` and `Create Page`
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
- `create-page` is now validated against the default right-hand page-template flow and can create pages such as `Clients`, `Clients_Auto2`, and `Clients_Auto3` in `Az_ClientManagement`
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
- `create-page` is currently strongest when the desired template is already visible in the right-hand template panel. Left-side template-category switching in the wizard still needs more hardening.
- `set-dialog-field` is present but still experimental; it needs more validation across a wider range of Studio Pro property dialogs.
- the current real `insert-action` gesture is still not inserting a visible activity on `ClinicalDocument_ShowPage`; current instrumentation shows it can open the parent microflow properties dialog instead.
- the current `Add > Activity` menu path is stable on `ClinicalDocument_ShowPage`, but it still does not produce a confirmed visible activity mutation by itself or after the current placement-click experiment.
- current runtime-id probing can reliably identify parameter and activity context menus, but connector insertion surfaces still do not expose a stable add-action menu path.
- run and responsive-browser commands now surface blocking Studio Pro dialogs, but they still do not verify a healthy Mendix runtime or browser content.
- `open-properties` is currently strongest on the page designer and `pageExplorer`; other scopes may still need more hardening.
- unopened documents that Studio Pro does not resolve through `Go to` now fail explicitly instead of returning misleading editor results.
