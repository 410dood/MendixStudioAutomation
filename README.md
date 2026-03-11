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
- send arbitrary key chords to Studio Pro or the active editor context
- trigger first-pass local run and responsive web shortcuts from Studio Pro, including URL readiness verification
- create pages through Studio Pro's native `New Document` and `Create Page` wizards
- query an in-Studio hybrid extension over a supported local webserver route
- list activity metadata for a selected microflow through the hybrid extension route
- open an in-Studio quick create-object modal dialog (menu, context menu, or CLI trigger)
- track and summarize automation knowledge gaps locally for prioritization
- add opened pages to the web navigation profile via the hybrid extension route
- insert selected microflow activities through hybrid extension routes (`Create object`, `Create list`, `Call microflow`, `Retrieve from database`, `Retrieve by association`, `Filter by association`, `Find by association`, `Filter by attribute`, `Find by attribute`, `Find by expression`, `Aggregate list`, `Aggregate by attribute`, `Aggregate by expression`, `Change list`, `Sort list`, `Reduce aggregate`, `List head`, `List tail`, `List contains`, `List union`, `List intersect`, `List subtract`, `List equals`, `Delete object`, `Commit object`, `Rollback object`, `Change attribute`, `Change association`)
- open native Studio Pro properties dialogs from selected editor targets
- inspect and wait for Studio Pro popups to clear
- list open Studio Pro dialogs, inspect dialog controls, and invoke dialog controls
- set native Studio Pro dialog fields by visible label on an experimental basis
- list and invoke nested editor context-menu paths
- inspect scoped editor surfaces and invoke raw runtime-id elements for microflow probing
- fall back from `Shift+F10` to native right-click when selected editor labels open properties instead of a menu
- click a point relative to a visible editor element for placement and hotspot testing
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
- execute native widget insertion through `Page Explorer -> Add widget... -> Select Widget`
- detect real page-designer controls on `Client_ClinicalDocument_V3`, including `Structure mode`, `Parameters (8)`, `Olari_Popup_Default`, and real Page Explorer rows such as `container34`
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
npm run send-keys -- --page "Client_ClinicalDocument_V3" --scope editor --keys "^,"
npm run run-local
npm run run-local-verify -- --url "http://localhost:8080" --verify-timeout-ms 120000 --verify-poll-ms 2000
npm run run-local-verify -- --url "http://localhost:8080" --verify-status 200 --verify-text "Mendix"
npm run run-local-verify -- --url "http://localhost:8080" --verify-status 200 --verify-content-type "text/html" --verify-title "Olari"
npm run run-local-verify -- --url "http://localhost:8080" --verify-only true --verify-header "set-cookie=mendix;;content-type=text/html"
npm run run-local-verify -- --url "http://localhost:8080" --verify-follow-redirects true --verify-status 200 --verify-title "Olari"
npm run run-local-verify -- --url "http://localhost:8080" --verify-follow-redirects true --verify-final-url "/index.html" --verify-status 200
npm run run-local-verify -- --url "http://localhost:8080" --verify-only true --verify-status 200
npm run show-responsive-web
npm run create-page -- --module "Az_ClientManagement" --page-name "Clients_Auto3"
npm run create-clients-page -- --module "Az_ClientManagement" --page-name "Clients" --widget "Data Grid 2"
npm run open-properties -- --page "Client_ClinicalDocument_V3" --item "Structure mode" --scope editor
npm run open-item -- --item "Client_ClinicalDocument_V4"
npm run select-widget -- --page "Client_ClinicalDocument_V3" --widget "Olari_Popup_Default"
npm run select-widget -- --page "Client_ClinicalDocument_V3" --widget "Structure mode"
npm run popup-status
npm run wait-ready -- --timeout-ms 60000
npm run list-open-tabs
npm run list-open-tabs -- --kind microflow
npm run list-open-tabs -- --module Az_ClientManagement
npm run active-tab
npm run active-context
npm run extension-status
npm run extension-context
npm run extension-capabilities
npm run extension-search-documents -- --query ClinicalDocument --module Az_ClientManagement --limit 10
npm run extension-open-document -- --name "ClinicalDocument_ShowPage" --module Az_ClientManagement
npm run extension-open-document -- --document-id "00000000-0000-0000-0000-000000000000"
npm run list-microflow-activities -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement"
npm run find-microflow-activities -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --query "retrieve" --action-type "RetrieveByAssociation"
npm run open-quick-create-object-dialog -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "CreatedObject"
npm run record-knowledge-gap -- --requested-capability "page.insertWidget" --observed-issue "Intermittent selector drift in Select Widget dialog" --impact "Blocks repeatable page automation" --context "Client_ClinicalDocument_V3"
npm run list-knowledge-gaps -- --status open --limit 20
npm run summarize-knowledge-gaps
npm run rag-search -- --query "insert-before-index create-object" --scope "README.md,docs,src/lib"
npm run add-navigation-shortcut -- --page "Client_ClinicalDocument_V3" --caption "Clinical Document" --module Az_ClientManagement
npm run hybrid-context
npm run list-scope-elements -- --microflow "ClinicalDocument_ShowPage" --scope editor --near-name "DS_AppConfig" --radius 320
npm run invoke-scope-element-action -- --microflow "ClinicalDocument_ShowPage" --scope editor --runtime-id "42.1050910.4.15.1.1135" --action rightClick
npm run select-tab -- --tab "Client_ClinicalDocument_V3" --module "Az_ClientManagement"
npm run close-tab -- --tab "Client_ClinicalDocument_V3 [Az_ClientManagement]" --dry-run
npm run close-tab -- --dry-run
npm run select-app-explorer-item -- --item "Client_ClinicalDocument_V3"
npm run select-explorer-item -- --page "Client_ClinicalDocument_V3" --item "container34"
npm run select-toolbox-item -- --item "Text"
npm run list-dialogs
npm run list-dialog-items -- --dialog "Select Widget" --limit 40
npm run list-dialog-fields -- --dialog "Edit Data grid 2 'dataGrid21'" --control-type CheckBox
npm run invoke-dialog-control -- --dialog "Select Widget" --control "Select" --control-type Button
npm run get-dialog-field -- --dialog "Edit Data grid 2 'dataGrid21'" --label "Show search bar" --control-type CheckBox
npm run get-dialog-field -- --dialog "Edit Container 'container39'" --label "Name" --control-type Edit --verify-value-contains "container"
npm run set-dialog-fields -- --dialog "Edit Data grid 2 'dataGrid21'" --fields-json "{\"Name\":{\"value\":\"ClientGrid\",\"verifyValue\":\"ClientGrid\"},\"Show search bar\":{\"value\":true,\"controlType\":\"CheckBox\",\"verifyToggleState\":\"On\"}}"
npm run set-dialog-field -- --dialog "Edit Container 'container39'" --label "Name" --value "container39_test" --control-type Edit
npm run set-dialog-field -- --dialog "Edit Data grid 2 'dataGrid21'" --label "Show search bar" --value true --control-type CheckBox --verify-toggle-state On
npm run list-editor-menu-items -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType"
npm run invoke-editor-menu-path -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType" --menu-path "Add>Activity"
npm run invoke-editor-menu-path -- --microflow "ClinicalDocument_ShowPage" --menu-path "Add>Activity" --dry-run
npm run click-editor-offset -- --microflow "ClinicalDocument_ShowPage" --element "DocumentType" --offset-x 220 --offset-y 0
npm run list-app-explorer-items
npm run list-page-explorer-items -- --page "Client_ClinicalDocument_V3"
npm run list-toolbox-items -- --page "Client_ClinicalDocument_V3"
npm run list-editor-labels -- --page "Client_ClinicalDocument_V3"
npm run insert-widget -- --page "Client_ClinicalDocument_V3" --target "container34" --widget "Text" --dry-run
npm run create-clients-page -- --module "Az_ClientManagement" --page-name "Clients" --widget "Data Grid 2"
npm run select-microflow-node -- --microflow "ClinicalDocument_ShowPage" --node "DocumentType"
npm run insert-action -- --microflow "ClinicalDocument_ShowPage" --target "DocumentType" --action-name "Create object" --dry-run
npm run add-microflow-create-object -- --microflow "ClinicalDocument_ShowPage" --module "Document" --entity "ClientDocument" --commit "YesWithoutEvents" --refresh-in-client false
npm run add-microflow-create-object -- --microflow "ClinicalDocument_ShowPage" --module "Document" --entity "ClientDocument" --commit "No" --insert-before-activity "Retrieve"
npm run add-microflow-create-object -- --microflow "ClinicalDocument_ShowPage" --module "Document" --entity "ClientDocument" --insert-before-index 5
npm run add-microflow-create-list -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "ClientDocumentList"
npm run add-microflow-create-list -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "ClientDocumentList" --insert-before-activity "Create object"
npm run add-microflow-create-list -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "ClientDocumentList" --insert-before-index 6
npm run add-microflow-call -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --called-microflow "General.ACT_GetCurrentAccount" --output-variable-name "CurrentAccount" --parameter-mappings "{}"
npm run add-microflow-call -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --called-microflow "General.ACT_GetCurrentAccount" --output-variable-name "CurrentAccount" --insert-before-activity "Retrieve"
npm run add-microflow-call -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --called-microflow "General.ACT_GetCurrentAccount" --output-variable-name "CurrentAccount" --insert-before-index 7
npm run add-microflow-retrieve-database -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "ClientDocumentList" --x-path-constraint "[Status='Draft']" --retrieve-first false
npm run add-microflow-retrieve-database -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --output-variable-name "ClientDocumentWindow" --x-path-constraint "[Status='Draft']" --sort-attribute "Status" --sort-descending false --range-offset-expression "0" --range-amount-expression "25"
npm run add-microflow-retrieve-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --entity-variable "ClientDocumentObj" --output-variable-name "ClientObj"
npm run add-microflow-retrieve-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --entity-variable "ClientDocumentObj" --output-variable-name "ClientObj" --insert-before-activity "Create object"
npm run add-microflow-retrieve-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --entity-variable "ClientDocumentObj" --output-variable-name "ClientObj" --insert-before-index 8
npm run add-microflow-filter-by-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --list-variable "ClientDocumentList" --output-variable-name "FilteredClientDocumentList" --filter-expression "$ClientObj"
npm run add-microflow-find-by-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --list-variable "ClientDocumentList" --output-variable-name "FoundClientDocument" --find-expression "$ClientObj"
npm run add-microflow-filter-by-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "Status" --list-variable "ClientDocumentList" --output-variable-name "DraftClientDocuments" --filter-expression "Draft"
npm run add-microflow-find-by-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "Status" --list-variable "ClientDocumentList" --output-variable-name "FirstDraftClientDocument" --find-expression "Draft"
npm run add-microflow-find-by-expression -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "FirstMatchingDocument" --find-expression '$currentObject/Status = ''Draft'''
npm run add-microflow-filter-by-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --list-variable "ClientDocumentList" --output-variable-name "FilteredClientDocumentList" --filter-expression "$ClientObj" --insert-before-index 14
npm run add-microflow-find-by-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --list-variable "ClientDocumentList" --output-variable-name "FoundClientDocument" --find-expression "$ClientObj" --insert-before-activity "Retrieve"
npm run add-microflow-filter-by-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "Status" --list-variable "ClientDocumentList" --output-variable-name "DraftClientDocuments" --filter-expression "Draft" --insert-before-activity "Create object"
npm run add-microflow-find-by-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "Status" --list-variable "ClientDocumentList" --output-variable-name "FirstDraftClientDocument" --find-expression "Draft" --insert-before-index 15
npm run add-microflow-find-by-expression -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "FirstMatchingDocument" --find-expression '$currentObject/Status = ''Draft''' --insert-before-activity "Retrieve"
npm run add-microflow-aggregate-list -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "ClientDocumentCount" --aggregate-function Count
npm run add-microflow-aggregate-by-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "VersionNumber" --list-variable "ClientDocumentList" --output-variable-name "TotalVersionNumber" --aggregate-function Sum
npm run add-microflow-aggregate-by-expression -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "WeightedTotal" --aggregate-expression '$currentObject/VersionNumber * 1' --aggregate-function Sum
npm run add-microflow-aggregate-list -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "ClientDocumentCount" --aggregate-function Count --insert-before-index 16
npm run add-microflow-aggregate-by-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "VersionNumber" --list-variable "ClientDocumentList" --output-variable-name "TotalVersionNumber" --aggregate-function Sum --insert-before-activity "Retrieve"
npm run add-microflow-aggregate-by-expression -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "WeightedTotal" --aggregate-expression '$currentObject/VersionNumber * 1' --aggregate-function Sum --insert-before-activity "Create object"
npm run add-microflow-change-list -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --change-list-operation Add --value '$ClientDocumentObj' --insert-before-index 17
npm run add-microflow-sort-list -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "Status" --list-variable "ClientDocumentList" --output-variable-name "SortedClientDocumentList" --sort-descending false --insert-before-activity "Retrieve"
npm run add-microflow-reduce-aggregate -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "ReducedVersionTotal" --aggregate-expression '$currentValue + $currentObject/VersionNumber' --initial-expression "0" --reduce-type Decimal --insert-before-activity "Create object"
npm run add-microflow-list-head -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "FirstClientDocument"
npm run add-microflow-list-tail -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "ClientDocumentListTail"
npm run add-microflow-list-contains -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --object-variable "ClientDocumentObj" --output-variable-name "HasClientDocument"
npm run add-microflow-list-head -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "FirstClientDocument" --insert-before-activity "Retrieve"
npm run add-microflow-list-tail -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --output-variable-name "ClientDocumentListTail" --insert-before-index 13
npm run add-microflow-list-contains -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --object-variable "ClientDocumentObj" --output-variable-name "HasClientDocument" --insert-before-activity "Create object"
npm run add-microflow-list-union -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --other-list-variable "DraftClientDocumentList" --output-variable-name "CombinedClientDocumentList"
npm run add-microflow-list-intersect -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --other-list-variable "DraftClientDocumentList" --output-variable-name "SharedClientDocumentList"
npm run add-microflow-list-subtract -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --other-list-variable "DraftClientDocumentList" --output-variable-name "RemainingClientDocumentList"
npm run add-microflow-list-equals -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --other-list-variable "DraftClientDocumentList" --output-variable-name "ClientDocumentListsMatch"
npm run add-microflow-list-union -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --other-list-variable "DraftClientDocumentList" --output-variable-name "CombinedClientDocumentList" --insert-before-index 11
npm run add-microflow-list-intersect -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --other-list-variable "DraftClientDocumentList" --output-variable-name "SharedClientDocumentList" --insert-before-activity "Retrieve"
npm run add-microflow-list-subtract -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --other-list-variable "DraftClientDocumentList" --output-variable-name "RemainingClientDocumentList" --insert-before-activity "Create object"
npm run add-microflow-list-equals -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --list-variable "ClientDocumentList" --other-list-variable "DraftClientDocumentList" --output-variable-name "ClientDocumentListsMatch" --insert-before-index 12
npm run add-microflow-delete-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj"
npm run add-microflow-commit-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj" --with-events false --refresh-in-client false
npm run add-microflow-rollback-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj" --refresh-in-client false
npm run add-microflow-change-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "Status" --variable "ClientDocumentObj" --value "Draft" --change-type Set --commit No
npm run add-microflow-change-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --variable "ClientDocumentObj" --value "$ClientObj" --change-type Set --commit No
npm run add-microflow-delete-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj" --insert-before-index 9
npm run add-microflow-commit-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj" --insert-before-activity "Retrieve"
npm run add-microflow-rollback-object -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --variable "ClientDocumentObj" --insert-before-activity "Create object"
npm run add-microflow-change-attribute -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --entity "Document.ClientDocument" --attribute "Status" --variable "ClientDocumentObj" --value "Draft" --change-type Set --commit No --insert-before-index 10
npm run add-microflow-change-association -- --microflow "ClinicalDocument_ShowPage" --module "Az_ClientManagement" --association "ClientDocument_Client" --variable "ClientDocumentObj" --value "$ClientObj" --change-type Set --commit No --insert-before-activity "Commit"
```

### Clients Page Scaffold

- `create-clients-page` now bundles:
  - create a page (defaults to module `Az_ClientManagement`, name `Clients`)
  - discover a likely insertion target in the new page’s Page Explorer
  - insert a `Data Grid 2` scaffold widget
- automatically wire the new page into Web navigation when extension context is available with `--add-navigation`
- If the target selector drifts, use `--target` to point to a specific Page Explorer row.
- If extension route is unavailable, `--add-navigation` reports a warning and still creates the page.

Example:

```powershell
npm run create-clients-page -- --module Az_ClientManagement --page-name Clients --add-navigation --navigation-caption "Clients"
```

You can also target the active Studio Pro window by title:

```powershell
node src/cli.mjs snapshot --title "Olari"
```

## Documentation

- [Setup](docs/SETUP.md)
- [User Manual](docs/USER_MANUAL.md)
- [Hybrid Architecture](docs/HYBRID_ARCHITECTURE.md)
- [External References Review](docs/EXTERNAL_REFERENCES.md)
- [Release Notes](RELEASE_NOTES.md)

## Hybrid Extension

The repo now includes a supported C# Studio Pro extension project at:

- `extensions/MendixStudioAutomation_Extension`

It targets the exact package version that matches the current Olari Studio Pro build:

- `Mendix.StudioPro.ExtensionsAPI 10.24.14-build.90436`

Build it with:

```powershell
dotnet build .\extensions\MendixStudioAutomation_Extension\MendixStudioAutomation_Extension.csproj
```

Install it into a Mendix app with:

```powershell
pwsh .\scripts\Install-MendixStudioAutomationExtension.ps1 -AppDirectory C:\Users\willi\Mendix\Olari-main -Build
```

Once Studio Pro loads the extension, the Node layer can query it with:

```powershell
npm run extension-status
npm run extension-context
npm run hybrid-context
```

## Roadmap

Phase 1:

- reliable attach, snapshot, search, click, focus, invoke
- stable selectors for Studio Pro panes and editors

Phase 2:

- higher-level operations: open page, open microflow, expand module, open toolbox category
- current `open-item`, `select-widget`, `select-explorer-item`, `insert-widget`, `select-microflow-node`, and `insert-action` now prefer an already open matching editor tab before falling back to `Go to`
- current `create-page` drives the native `New Document` and `Create Page` dialogs and is validated for creating pages like `Clients`, `Clients_Auto2`, and `Clients_Auto3` in `Az_ClientManagement`
- current `create-page` selects the first visible right-hand page template card by default and is validated against the default `Dashboard Action Center` template flow
- current `active-tab` uses the true UI Automation selection state when available, and otherwise falls back to the last tab explicitly activated by this automation
- current `extension-status`, `extension-context`, and `hybrid-context` can discover and query the in-Studio hybrid extension when its `runtime/endpoint.json` file exists
- current `extension-open-document` now verifies the requested editor tab appears and updates remembered active-tab state for follow-up commands
- current `extension-open-document` now also falls back to extension-backed search, returns structured ambiguity details, and verifies the opened tab
- current `open-item` now falls back to extension-backed document search and opens a unique or exact search match when direct extension open by name fails
- current `open-item` now returns structured ambiguity details from the extension search path instead of silently dropping to generic UI automation
- current extension search-based document opens now reopen by stable document ID when available instead of relying on the name a second time
- current `extension-open-document` and `open-item` both accept a direct document id for deterministic extension-backed open when that id is already known
- current `active-context` now reports whether extension metadata actually contributed to the resolved context instead of assuming any extension response improved it
- current `list-microflow-activities` returns activity/action metadata for a target microflow, including available variable names
- current `find-microflow-activities` filters microflow activity metadata by query text, action type, and variable name
- current hybrid extension project is a real Mendix `WebServerExtension` plus `MenuExtension`, not a placeholder stub
- current hybrid extension also provides a `ContextMenuExtension<IDocument>` for microflow document actions
- current `open-quick-create-object-dialog` opens a modal webview dialog to stage/create a `Create object` action with prefilled context
- current `select-widget` can target both the editor surface and a named alternate surface like `pageExplorer`
- current `select-widget` is validated against real page-designer labels on `Client_ClinicalDocument_V3`
- current `select-explorer-item` targets exact visible Page Explorer rows from the Page Explorer dock container and is validated against `container34`
- current `select-toolbox-item` targets exact visible Toolbox items from the Toolbox dock container and is validated for both page and microflow toolbox content
- current `insert-widget` supports a dry-run verification mode and is validated end-to-end for resolving visible container targets plus `Text`
- current `insert-widget` can open and traverse the native `Select Widget` dialog, including disambiguating duplicate widget names like `Text`
- current `insert-widget` records native accept attempts (`Select` button, `Enter`, double-click) plus before/after Page Explorer snapshots for debugging
- current `insert-widget` is now producing confirmed Page Explorer mutations on `Client_ClinicalDocument_V3`, including live insertions under `container39` and `container38`
- current dialog inspection is sufficient to read local-run blockers such as Studio Pro `Information` dialogs
- current `list-dialog-fields` can enumerate dialog labels as resolved field/value pairs instead of raw control dumps
- current `invoke-dialog-control` now reports whether the dialog window actually closed after the control invocation
- current `get-dialog-field` can inspect native dialog field values and toggle state by visible label
- current `set-dialog-fields` can apply multiple dialog property changes from one JSON payload
- current dialog/listed automation elements now expose `textValue` when a native control supports `ValuePattern`
- current `set-dialog-field` exists as an experimental label-based native dialog field writer for `Edit`, `ComboBox`, `CheckBox`, and `ToggleButton` controls
- current `set-dialog-field` now reports the observed post-write text/toggle state so dialog mutations can be verified directly
- current `set-dialog-field` now supports `--verify-value` and `--verify-toggle-state` to fail fast when the observed dialog state does not match the expected result
- current `get-dialog-field` and `set-dialog-field` now support `--verify-value-contains` for substring-based text verification
- current `select-microflow-node` uses the active microflow editor container instead of the whole window
- current `insert-action` follows the same pattern for microflow actions and is verified in `--dry-run` mode
- current `insert-action` now records before/after microflow-editor snapshots and any post-action dialog so failed action-insert gestures are diagnosable instead of opaque
- current `add-microflow-create-object` now inserts SDK-backed `Create object` activities through the hybrid extension route
- current `add-microflow-create-list` now inserts SDK-backed `Create list` activities through the hybrid extension route
- current `add-microflow-create-object` and `add-microflow-create-list` now support optional insert-before targeting by activity caption or action type
- current `add-microflow-call` now inserts SDK-backed `Call microflow` activities through the hybrid extension route
- current `add-microflow-retrieve-database` now inserts SDK-backed `Retrieve from database` activities through the hybrid extension route
- current `add-microflow-retrieve-database` now also supports optional sort attributes and ranged retrieval expressions
- current `add-microflow-retrieve-association` now inserts SDK-backed `Retrieve by association` activities through the hybrid extension route
- current `add-microflow-call`, `add-microflow-retrieve-database`, and `add-microflow-retrieve-association` now support optional insert-before targeting by activity caption or action type
- current insert-before targeting also supports deterministic activity index selection via `--insert-before-index` (from `list-microflow-activities`)
- current `add-microflow-filter-by-association` now inserts SDK-backed `Filter by association` activities through the hybrid extension route
- current `add-microflow-find-by-association` now inserts SDK-backed `Find by association` activities through the hybrid extension route
- current `add-microflow-filter-by-attribute` now inserts SDK-backed `Filter by attribute` activities through the hybrid extension route
- current `add-microflow-find-by-attribute` now inserts SDK-backed `Find by attribute` activities through the hybrid extension route
- current `add-microflow-find-by-expression` now inserts SDK-backed `Find by expression` activities through the hybrid extension route
- current filter/find actions (`by-association`, `by-attribute`, `by-expression`) now also support optional `--insert-before-activity` and `--insert-before-index`
- current `add-microflow-aggregate-list` now inserts SDK-backed `Aggregate list` activities through the hybrid extension route
- current `add-microflow-aggregate-by-attribute` now inserts SDK-backed `Aggregate by attribute` activities through the hybrid extension route
- current `add-microflow-aggregate-by-expression` now inserts SDK-backed `Aggregate by expression` activities through the hybrid extension route
- current aggregate actions (`aggregate-list`, `aggregate-by-attribute`, `aggregate-by-expression`) now also support optional `--insert-before-activity` and `--insert-before-index`
- current `add-microflow-change-list` now inserts SDK-backed `Change list` activities through the hybrid extension route
- current `add-microflow-sort-list` now inserts SDK-backed `Sort list` activities through the hybrid extension route
- current `add-microflow-reduce-aggregate` now inserts SDK-backed `Reduce aggregate` activities through the hybrid extension route
- current change/sort/reduce actions (`change-list`, `sort-list`, `reduce-aggregate`) now also support optional `--insert-before-activity` and `--insert-before-index`
- current `add-microflow-list-head` now inserts SDK-backed `List head` activities through the hybrid extension route
- current `add-microflow-list-tail` now inserts SDK-backed `List tail` activities through the hybrid extension route
- current `add-microflow-list-contains` now inserts SDK-backed `List contains` activities through the hybrid extension route
- current list unary/binary actions (`list-head`, `list-tail`, `list-contains`) now also support optional `--insert-before-activity` and `--insert-before-index`
- current `add-microflow-list-union` now inserts SDK-backed `List union` activities through the hybrid extension route
- current `add-microflow-list-intersect` now inserts SDK-backed `List intersect` activities through the hybrid extension route
- current `add-microflow-list-subtract` now inserts SDK-backed `List subtract` activities through the hybrid extension route
- current `add-microflow-list-equals` now inserts SDK-backed `List equals` activities through the hybrid extension route
- current binary list actions (`list-union`, `list-intersect`, `list-subtract`, `list-equals`) now also support optional `--insert-before-activity` and `--insert-before-index`
- current `add-microflow-delete-object` now inserts SDK-backed `Delete object` activities through the hybrid extension route
- current `add-microflow-commit-object` now inserts SDK-backed `Commit object` activities through the hybrid extension route
- current `add-microflow-rollback-object` now inserts SDK-backed `Rollback object` activities through the hybrid extension route
- current `add-microflow-change-attribute` now inserts SDK-backed `Change attribute` activities through the hybrid extension route
- current `add-microflow-change-association` now inserts SDK-backed `Change association` activities through the hybrid extension route
- current object mutation actions (`delete`, `commit`, `rollback`, `change-attribute`, `change-association`) now also support optional `--insert-before-activity` and `--insert-before-index`
- current `rag-search` provides local RAG-style ranked retrieval across automation docs/source for faster MCP prompt grounding
- current editor context-menu commands can traverse nested menu paths such as `Add > Activity`
- current node-level editor context menus fall back to native right-click when `Shift+F10` opens the microflow properties dialog instead of a menu
- current `click-editor-offset` provides a placement primitive for clicking relative to a visible page or microflow element
- current dialog commands are validated against native Studio Pro dialogs such as `Select Widget` and `Edit Template Grid 'templateGrid1'`
- current run/browser commands drive Studio Pro shortcuts for `F5`, `Shift+F5`, and `F9`
- current `open-properties` is validated from editor-surface targets like `Structure mode` and `Parameters (8)` on `Client_ClinicalDocument_V3`
- current `open-properties` is also validated from `pageExplorer` targets like `container34` on `Client_ClinicalDocument_V3`
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
- `create-page` is currently strongest when using the default visible template cards on the right side of the Create Page wizard. Left-pane template-category switching still needs more hardening.
- The first goal is dependable control primitives, not yet full end-to-end authoring.
