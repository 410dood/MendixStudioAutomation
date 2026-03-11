# Release Notes

## 0.2.57

Raw dialog comparison for generic and target-first workflows.

- Added `compare-dialog-items` to diff a live dialog's raw visible controls against a saved JSON inventory.
- Added `compare-properties-dialog-items` to perform the same comparison from a real page or microflow properties target.
- Raw dialog comparison now reports state drift on matched controls as `changed`, not just `missing` and `extra`.
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.56

Raw dialog export for generic and target-first workflows.

- Added `export-dialog-items` to write the raw visible control inventory of any open Studio Pro dialog to JSON.
- Added `export-properties-dialog-items` to do the same from a real page or microflow properties target.
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.55

Target-first raw properties dialog inspection.

- Added `list-properties-dialog-items` to open a Studio Pro properties dialog from a page or microflow target and enumerate raw visible controls.
- This complements `list-properties-dialog-fields` when label-to-field resolution is not sufficient for reverse-engineering a dialog.
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.54

Generic target-first properties control invocation.

- Added `invoke-properties-dialog-control` to open a Studio Pro properties dialog from a page or microflow target and invoke a named button or control inside it.
- This complements `--finalize-dialog` when a workflow needs a nonstandard button like `Apply` or another custom dialog action.
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.53

Properties dialog finalization support.

- Added `finalizeDialog` / `--finalize-dialog` support to target-first properties commands so they can click `OK`, `Apply`, `Cancel`, or `Close` after read, compare, export, set, or sync operations.
- This closes an important gap for property editing because set and sync flows can now explicitly persist dialog changes with `OK`.
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.52

Dry-run planning for dialog sync commands.

- Added `dryRun` / `--dry-run` support to `sync-dialog-fields`, returning `plannedFields` without mutating the live dialog.
- `sync-properties-dialog` now forwards the same dry-run behavior for target-first property synchronization.
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.51

Target-first property discovery and batch editing.

- Added `list-properties-dialog-fields` to open a Studio Pro properties dialog from a page or microflow target and enumerate resolved field/value pairs.
- Added `set-properties-dialog-fields` to open a Studio Pro properties dialog from a page or microflow target and apply batch field edits with the existing JSON plan format.
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.50

Direct single-field properties editing workflows.

- Added `get-properties-dialog-field` to open a Studio Pro properties dialog from a page or microflow target and read one labeled field directly.
- Added `set-properties-dialog-field` to open a Studio Pro properties dialog from a page or microflow target and apply one labeled field edit with the existing verification options.
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.49

Review-first properties dialog workflows.

- Added `export-properties-dialog` to open a Studio Pro properties dialog from a page or microflow target and export its field/value plan directly to JSON.
- Added `compare-properties-dialog` to open a Studio Pro properties dialog from a page or microflow target and diff it against a saved JSON field plan before mutating anything.
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.48

Direct document-id support in public open commands.

- `extension-open-document` now accepts:
  - `documentId` / `id`
- `open-item` now also accepts:
  - `documentId` / `id`
- This exposes the deterministic extension-backed open-by-id path directly to CLI callers instead of requiring a prior search round trip.
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.47

Deterministic ID-based document open after extension search.

- Added extension route:
  - `/mendix-studio-automation/documents/open-by-id`
- Added extension capability:
  - `documents.openById`
- Search-based document open flows now prefer the stable document ID returned by extension search results instead of reopening by name.
- Updated runtime endpoint metadata and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.46

Search fallback and ambiguity reporting for direct extension document open.

- Enhanced `extension-open-document` so it now:
  - falls back to extension document search when direct open by name fails
  - applies the same exact-match selection rules as `open-item`
  - returns structured candidate matches when the search is ambiguous
  - verifies the editor tab and remembers it on success
- Refactored shared extension search/open verification logic in the Node client
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.45

Final-destination URL assertions for runtime verification.

- Enhanced `run-local-verify` with:
  - `verifyFinalUrl` / `expectedFinalUrl`
- This allows assertions against the final resolved URL after redirects when `verifyFollowRedirects` is enabled.
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.44

Structured ambiguity reporting for extension-backed document open.

- Enhanced `open-item` extension fallback so it now:
  - prefers exact name + module + type matches from extension search results
  - returns structured candidate matches when the search is ambiguous
  - returns a targeted error when a selected extension search match still cannot be opened
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.43

Optional redirect-following for local runtime verification.

- Enhanced `run-local-verify` with:
  - `verifyFollowRedirects` / `followRedirects`
  - `finalUrl` in verification output
- This allows local validation against the destination page after redirects instead of only the initial response.
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.42

More accurate active-context source reporting.

- Enhanced `active-context` to distinguish:
  - when extension metadata actually contributed to the resolved document context
  - when the result is effectively still UI-automation-only
- Added `extensionContributed` to active-context output
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.41

Extension-backed search fallback for document opening.

- Enhanced `open-item` so the extension path now:
  - first tries direct document open by requested name
  - falls back to extension document search when direct open fails
  - opens a unique or exact search match and verifies the editor tab
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.40

Verified extension-backed document opening.

- Enhanced `extension-open-document` so it now:
  - waits for a matching editor tab after the extension opens the document
  - updates remembered active-tab state for follow-up page and microflow commands
  - returns `verifiedOpen`, `attempts`, and `tab` metadata
- Updated operation catalog wording for hybrid active-context resolution
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.39

Generic response-header assertions for local runtime validation.

- Enhanced `run-local-verify` with optional header assertions:
  - `verifyHeader` / `expectedHeader` in `Name=substring` form
  - multiple header rules can be supplied in one argument separated by `;;`
- Verification output now includes:
  - observed response `headers`
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.38

Richer page-level HTTP validation for local runtime checks.

- Enhanced `run-local-verify` with optional assertions for:
  - HTML title via `verifyTitle` / `expectedTitle`
  - response content type via `verifyContentType` / `expectedContentType`
- Verification output now includes:
  - observed `contentType`
  - parsed HTML `title`
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.37

Run-local verification without forced rerun.

- Added `verifyOnly` / `skipRun` support to `run-local-verify`:
  - skip sending `F5`
  - perform only HTTP verification assertions against the target URL
- Added CLI flag:
  - `--verify-only <bool>`
- Updated docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.36

Richer local runtime verification assertions.

- Enhanced `run-local-verify` to support optional HTTP assertions:
  - `verifyStatus` / `expectedStatus` for specific status-code validation
  - `verifyText` / `expectedText` for response-body substring validation
  - `verifyLocation` / `expectedLocation` for redirect Location header validation
- Added result diagnostics:
  - last observed `location`
  - short `responseSnippet` on failure
  - echoed expected assertion inputs in verification output
- Updated CLI help and docs:
  - `README.md`
  - `docs/USER_MANUAL.md`

## 0.2.35

Insert-before targeting for change/sort/reduce microflow operations.

- Enhanced extension routes:
  - `/mendix-studio-automation/microflows/change-list`
  - `/mendix-studio-automation/microflows/sort-list`
  - `/mendix-studio-automation/microflows/reduce-aggregate`
- Added optional query argument support:
  - `insertBeforeActivity` (aliases: `insertBefore`, `beforeActivity`, `beforeCaption`)
  - `insertBeforeIndex` (alias: `beforeIndex`)
- Added Node/CLI propagation for:
  - `add-microflow-change-list`
  - `add-microflow-sort-list`
  - `add-microflow-reduce-aggregate`
- Insertion behavior:
  - default remains insert after microflow start
  - when `insertBeforeActivity` is supplied, insertion matches by caption/action type
  - when `insertBeforeIndex` is supplied, insertion is index-based from `list-microflow-activities`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.34

Insert-before targeting for aggregate microflow operations.

- Enhanced extension routes:
  - `/mendix-studio-automation/microflows/aggregate-list`
  - `/mendix-studio-automation/microflows/aggregate-by-attribute`
  - `/mendix-studio-automation/microflows/aggregate-by-expression`
- Added optional query argument support:
  - `insertBeforeActivity` (aliases: `insertBefore`, `beforeActivity`, `beforeCaption`)
  - `insertBeforeIndex` (alias: `beforeIndex`)
- Added Node/CLI propagation for:
  - `add-microflow-aggregate-list`
  - `add-microflow-aggregate-by-attribute`
  - `add-microflow-aggregate-by-expression`
- Insertion behavior:
  - default remains insert after microflow start
  - when `insertBeforeActivity` is supplied, insertion matches by caption/action type
  - when `insertBeforeIndex` is supplied, insertion is index-based from `list-microflow-activities`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.33

Insert-before targeting for filter/find microflow operations.

- Enhanced extension routes:
  - `/mendix-studio-automation/microflows/filter-by-association`
  - `/mendix-studio-automation/microflows/find-by-association`
  - `/mendix-studio-automation/microflows/filter-by-attribute`
  - `/mendix-studio-automation/microflows/find-by-attribute`
  - `/mendix-studio-automation/microflows/find-by-expression`
- Added optional query argument support:
  - `insertBeforeActivity` (aliases: `insertBefore`, `beforeActivity`, `beforeCaption`)
  - `insertBeforeIndex` (alias: `beforeIndex`)
- Added Node/CLI propagation for:
  - `add-microflow-filter-by-association`
  - `add-microflow-find-by-association`
  - `add-microflow-filter-by-attribute`
  - `add-microflow-find-by-attribute`
  - `add-microflow-find-by-expression`
- Insertion behavior:
  - default remains insert after microflow start
  - when `insertBeforeActivity` is supplied, insertion matches by caption/action type
  - when `insertBeforeIndex` is supplied, insertion is index-based from `list-microflow-activities`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.32

Insert-before targeting for list head/tail/contains microflow operations.

- Enhanced extension routes:
  - `/mendix-studio-automation/microflows/list-head`
  - `/mendix-studio-automation/microflows/list-tail`
  - `/mendix-studio-automation/microflows/list-contains`
- Added optional query argument support:
  - `insertBeforeActivity` (aliases: `insertBefore`, `beforeActivity`, `beforeCaption`)
  - `insertBeforeIndex` (alias: `beforeIndex`)
- Added Node/CLI propagation for:
  - `add-microflow-list-head`
  - `add-microflow-list-tail`
  - `add-microflow-list-contains`
- Insertion behavior:
  - default remains insert after microflow start
  - when `insertBeforeActivity` is supplied, insertion matches by caption/action type
  - when `insertBeforeIndex` is supplied, insertion is index-based from `list-microflow-activities`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.31

Insert-before targeting for binary list microflow operations.

- Enhanced extension route family:
  - `/mendix-studio-automation/microflows/list-union`
  - `/mendix-studio-automation/microflows/list-intersect`
  - `/mendix-studio-automation/microflows/list-subtract`
  - `/mendix-studio-automation/microflows/list-equals`
- Added optional query argument support:
  - `insertBeforeActivity` (aliases: `insertBefore`, `beforeActivity`, `beforeCaption`)
  - `insertBeforeIndex` (alias: `beforeIndex`)
- Added Node/CLI propagation for:
  - `add-microflow-list-union`
  - `add-microflow-list-intersect`
  - `add-microflow-list-subtract`
  - `add-microflow-list-equals`
- Insertion behavior:
  - default remains insert after microflow start
  - when `insertBeforeActivity` is supplied, insertion matches by caption/action type
  - when `insertBeforeIndex` is supplied, insertion is index-based from `list-microflow-activities`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.30

Local RAG-style retrieval for automation procedures and capabilities.

- Added local knowledge search command:
  - `rag-search`
  - `npm run rag-search`
- Added ranked retrieval over local files:
  - `README.md`
  - `RELEASE_NOTES.md`
  - `docs/`
  - `extensions/MendixStudioAutomation_Extension/README.md`
  - `src/lib/operations.mjs`
  - `src/cli.mjs`
- Added search filters/options:
  - `--query` required
  - `--scope` optional comma-separated source paths
  - `--limit` global match limit (default `20`)
  - `--per-file-limit` per-file pre-rank limit (default `8`)
- Added operation catalog entry:
  - `knowledge.ragSearch`
- Updated documentation:
  - README
  - User Manual

## 0.2.29

Insert-before targeting expansion for object mutation microflow actions.

- Enhanced extension routes:
  - `/mendix-studio-automation/microflows/delete-object`
  - `/mendix-studio-automation/microflows/commit-object`
  - `/mendix-studio-automation/microflows/rollback-object`
  - `/mendix-studio-automation/microflows/change-attribute`
  - `/mendix-studio-automation/microflows/change-association`
- Added optional query argument support:
  - `insertBeforeActivity` (aliases: `insertBefore`, `beforeActivity`, `beforeCaption`)
  - `insertBeforeIndex` (alias: `beforeIndex`)
- Added Node/CLI propagation for:
  - `add-microflow-delete-object`
  - `add-microflow-commit-object`
  - `add-microflow-rollback-object`
  - `add-microflow-change-attribute`
  - `add-microflow-change-association`
- Insertion behavior:
  - default remains insert after microflow start
  - when `insertBeforeActivity` is supplied, insertion matches by caption/action type
  - when `insertBeforeIndex` is supplied, insertion is index-based from `list-microflow-activities`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.28

Filtered microflow activity finder command for faster index targeting and debugging.

- Added Studio Pro client support for:
  - `find-microflow-activities`
- Added CLI/npm support for:
  - `find-microflow-activities`
  - `npm run find-microflow-activities`
- Added activity finder filters:
  - `--query` text match across caption/action/activity/list-operation/variables
  - `--action-type` exact match against action/activity/list-operation type
  - `--variable` exact-or-contains match against activity variable names
  - bounded `--limit` (defaults to `200`, resets to `200` when non-positive)
- Finder responses now include filtered `payload.items` plus:
  - `totalActivities`
  - `matchedCount`
  - `count`
  - `limit`
- Updated operation catalog with:
  - `microflow.findActivities`
- Updated documentation:
  - README
  - User Manual

## 0.2.27

Deterministic insert-before index targeting for selected hybrid microflow mutations.

- Enhanced extension routes:
  - `/mendix-studio-automation/microflows/create-object`
  - `/mendix-studio-automation/microflows/create-list`
  - `/mendix-studio-automation/microflows/call-microflow`
  - `/mendix-studio-automation/microflows/retrieve-database`
  - `/mendix-studio-automation/microflows/retrieve-association`
- Added optional query argument support:
  - `insertBeforeIndex` (alias: `beforeIndex`)
- Insertion behavior:
  - if `insertBeforeIndex` is provided, the extension inserts before that activity index from `list-microflow-activities`
  - if `insertBeforeIndex` is not provided, existing insert-before-caption/type and insert-after-start behavior remains
- Added Node/CLI support for:
  - `--insert-before-index`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.26

Targeted insertion expansion for call and retrieve hybrid mutations.

- Enhanced extension routes:
  - `/mendix-studio-automation/microflows/call-microflow`
  - `/mendix-studio-automation/microflows/retrieve-database`
  - `/mendix-studio-automation/microflows/retrieve-association`
- Added optional query argument support:
  - `insertBeforeActivity` (aliases: `insertBefore`, `beforeActivity`, `beforeCaption`)
- Insertion behavior:
  - default remains insert after microflow start
  - if `insertBeforeActivity` is provided, the extension attempts insert-before by matching activity caption or action type
  - conflict responses include insertion details on failure
- Added Node/CLI support for:
  - `--insert-before-activity` on `add-microflow-call`, `add-microflow-retrieve-database`, and `add-microflow-retrieve-association`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.25

Targeted insertion support for create-object/create-list hybrid mutations.

- Enhanced extension routes:
  - `/mendix-studio-automation/microflows/create-object`
  - `/mendix-studio-automation/microflows/create-list`
- Added optional query argument support:
  - `insertBeforeActivity` (aliases: `insertBefore`, `beforeActivity`, `beforeCaption`)
- Insertion behavior:
  - default remains insert after microflow start
  - if `insertBeforeActivity` is provided, the extension attempts insert-before by matching activity caption or action type
  - conflict responses now include insertion details on failure
- Added Node/CLI support for:
  - `--insert-before-activity`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.24

Hybrid microflow activity inspection route.

- Added new extension route:
  - `/mendix-studio-automation/microflows/list-activities`
- Added extension capability:
  - `microflow.listActivities`
- Added Studio Pro client and CLI support:
  - `list-microflow-activities`
  - `npm run list-microflow-activities`
- Added operation catalog entry:
  - `microflow.listActivities`
- Payload includes activity-level metadata for each microflow activity:
  - activity/action type
  - caption and disabled state (when available)
  - variable-oriented hints (output/input variables, list-operation second variable, etc.)
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.23

Hybrid retrieve-database expansion with sorting and range expressions.

- Enhanced extension route:
  - `/mendix-studio-automation/microflows/retrieve-database`
- Added retrieve-database options:
  - optional `sortAttribute` and `sortDescending`
  - optional `rangeOffsetExpression` and `rangeAmountExpression`
  - validation to prevent invalid combinations (partial range args or range + `retrieveFirst`)
- Added Node/CLI support for:
  - `--sort-attribute`
  - `--range-offset-expression`
  - `--range-amount-expression`
  - existing `--sort-descending` now also applies to retrieve-database sort
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.22

Hybrid write-path expansion for binary list microflow operations.

- Added new extension routes:
  - `/mendix-studio-automation/microflows/list-union`
  - `/mendix-studio-automation/microflows/list-intersect`
  - `/mendix-studio-automation/microflows/list-subtract`
  - `/mendix-studio-automation/microflows/list-equals`
- Added extension capabilities:
  - `microflow.listUnion`
  - `microflow.listIntersect`
  - `microflow.listSubtract`
  - `microflow.listEquals`
- Added Studio Pro client support for:
  - inserting a `List union` action into a selected microflow via extension service APIs
  - inserting a `List intersect` action into a selected microflow via extension service APIs
  - inserting a `List subtract` action into a selected microflow via extension service APIs
  - inserting a `List equals` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm scripts:
  - `add-microflow-list-union`
  - `add-microflow-list-intersect`
  - `add-microflow-list-subtract`
  - `add-microflow-list-equals`
  - `npm run add-microflow-list-union`
  - `npm run add-microflow-list-intersect`
  - `npm run add-microflow-list-subtract`
  - `npm run add-microflow-list-equals`
- Added CLI options:
  - `--other-list-variable`
  - `--second-list-variable`
- Updated operation catalog with:
  - `microflow.listUnion`
  - `microflow.listIntersect`
  - `microflow.listSubtract`
  - `microflow.listEquals`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.21

Hybrid write-path expansion for microflow list-operation endpoints.

- Added new extension routes:
  - `/mendix-studio-automation/microflows/list-head`
  - `/mendix-studio-automation/microflows/list-tail`
  - `/mendix-studio-automation/microflows/list-contains`
- Added extension capabilities:
  - `microflow.listHead`
  - `microflow.listTail`
  - `microflow.listContains`
- Added Studio Pro client support for:
  - inserting a `List head` action into a selected microflow via extension service APIs
  - inserting a `List tail` action into a selected microflow via extension service APIs
  - inserting a `List contains` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm scripts:
  - `add-microflow-list-head`
  - `add-microflow-list-tail`
  - `add-microflow-list-contains`
  - `npm run add-microflow-list-head`
  - `npm run add-microflow-list-tail`
  - `npm run add-microflow-list-contains`
- Added CLI option:
  - `--object-variable`
- Updated operation catalog with:
  - `microflow.listHead`
  - `microflow.listTail`
  - `microflow.listContains`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.20

Hybrid write-path expansion for microflow reduce-aggregate operations.

- Added new extension route:
  - `/mendix-studio-automation/microflows/reduce-aggregate`
- Added extension capability:
  - `microflow.reduceAggregate`
- Added Studio Pro client support for:
  - inserting a `Reduce aggregate` action into a selected microflow via extension service APIs
  - configurable reduce result type and initial expression
- Added CLI plumbing and npm script:
  - `add-microflow-reduce-aggregate`
  - `npm run add-microflow-reduce-aggregate`
- Added CLI options:
  - `--initial-expression`
  - `--reduce-type`
- Updated operation catalog with:
  - `microflow.reduceAggregate`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.19

Runtime verification expansion for local runs.

- Added new CLI command:
  - `run-local-verify`
  - `npm run run-local-verify`
- Added Studio Pro client support for:
  - triggering local run via `F5`
  - polling a configured URL until it responds (basic readiness check)
- Added CLI options:
  - `--url`
  - `--verify-timeout-ms`
  - `--verify-poll-ms`
- Updated operation catalog with:
  - `studio.runLocalVerify`
- Updated documentation:
  - README
  - User Manual

## 0.2.18

Hybrid write-path expansion for microflow sort-list operations.

- Added new extension route:
  - `/mendix-studio-automation/microflows/sort-list`
- Added extension capability:
  - `microflow.sortList`
- Added Studio Pro client support for:
  - inserting a `Sort list` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm script:
  - `add-microflow-sort-list`
  - `npm run add-microflow-sort-list`
- Added CLI option:
  - `--sort-descending`
- Updated operation catalog with:
  - `microflow.sortList`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.17

Hybrid write-path expansion for microflow change-list operations.

- Added new extension route:
  - `/mendix-studio-automation/microflows/change-list`
- Added extension capability:
  - `microflow.changeList`
- Added Studio Pro client support for:
  - inserting a `Change list` action into a selected microflow via extension service APIs
  - `Set`, `Add`, `Remove`, and `Clear` list operations
- Added CLI plumbing and npm script:
  - `add-microflow-change-list`
  - `npm run add-microflow-change-list`
- Added CLI option:
  - `--change-list-operation`
- Updated operation catalog with:
  - `microflow.changeList`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.16

Hybrid write-path expansion for microflow aggregate operations.

- Added new extension routes:
  - `/mendix-studio-automation/microflows/aggregate-list`
  - `/mendix-studio-automation/microflows/aggregate-by-attribute`
  - `/mendix-studio-automation/microflows/aggregate-by-expression`
- Added extension capabilities:
  - `microflow.aggregateList`
  - `microflow.aggregateByAttribute`
  - `microflow.aggregateByExpression`
- Added Studio Pro client support for:
  - inserting an `Aggregate list` action into a selected microflow via extension service APIs
  - inserting an `Aggregate by attribute` action into a selected microflow via extension service APIs
  - inserting an `Aggregate by expression` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm scripts:
  - `add-microflow-aggregate-list`
  - `add-microflow-aggregate-by-attribute`
  - `add-microflow-aggregate-by-expression`
  - `npm run add-microflow-aggregate-list`
  - `npm run add-microflow-aggregate-by-attribute`
  - `npm run add-microflow-aggregate-by-expression`
- Added CLI options:
  - `--aggregate-function`
  - `--aggregate-expression`
- Updated operation catalog with:
  - `microflow.aggregateList`
  - `microflow.aggregateByAttribute`
  - `microflow.aggregateByExpression`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.15

Hybrid write-path expansion for microflow attribute/expression list operations.

- Added new extension routes:
  - `/mendix-studio-automation/microflows/filter-by-attribute`
  - `/mendix-studio-automation/microflows/find-by-attribute`
  - `/mendix-studio-automation/microflows/find-by-expression`
- Added extension capabilities:
  - `microflow.filterByAttribute`
  - `microflow.findByAttribute`
  - `microflow.findByExpression`
- Added Studio Pro client support for:
  - inserting a `Filter by attribute` action into a selected microflow via extension service APIs
  - inserting a `Find by attribute` action into a selected microflow via extension service APIs
  - inserting a `Find by expression` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm scripts:
  - `add-microflow-filter-by-attribute`
  - `add-microflow-find-by-attribute`
  - `add-microflow-find-by-expression`
  - `npm run add-microflow-filter-by-attribute`
  - `npm run add-microflow-find-by-attribute`
  - `npm run add-microflow-find-by-expression`
- Updated operation catalog with:
  - `microflow.filterByAttribute`
  - `microflow.findByAttribute`
  - `microflow.findByExpression`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.14

Hybrid write-path expansion for microflow call activities.

- Added new extension route:
  - `/mendix-studio-automation/microflows/call-microflow`
- Added extension capability:
  - `microflow.callMicroflow`
- Added Studio Pro client support for:
  - inserting a `Call microflow` action into a selected microflow via extension service APIs
  - optional parameter mapping through JSON expressions
- Added CLI plumbing and npm script:
  - `add-microflow-call`
  - `npm run add-microflow-call`
- Added options for call insertion:
  - `--called-microflow`
  - `--called-module`
  - `--parameter-mappings`
- Updated operation catalog with:
  - `microflow.callMicroflow`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.13

Sample-pattern integration milestone (`ExtensionAPI-Samples` aligned to Studio Pro `10.24.14`).

- Added menu/context-menu and modal webview patterns to the extension:
  - new `ContextMenuExtension<IDocument>` entry on microflow documents for quick create-object workflows
  - new modal webview quick action dialog for create-object insertion
- Added new extension routes:
  - `/mendix-studio-automation/ui/quick-create-object`
  - `/mendix-studio-automation/ui/quick-create-object/open`
- Added extension capability:
  - `ui.quickCreateObjectDialog`
- Added Node/CLI support:
  - `open-quick-create-object-dialog`
  - `npm run open-quick-create-object-dialog`
- Updated operation catalog with:
  - `ui.quickCreateObjectDialog`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.12

Hybrid write-path expansion for association list operations.

- Added new extension routes:
  - `/mendix-studio-automation/microflows/filter-by-association`
  - `/mendix-studio-automation/microflows/find-by-association`
- Added Studio Pro client support for:
  - inserting a `Filter by association` action into a selected microflow via extension service APIs
  - inserting a `Find by association` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm scripts:
  - `add-microflow-filter-by-association`
  - `add-microflow-find-by-association`
  - `npm run add-microflow-filter-by-association`
  - `npm run add-microflow-find-by-association`
- Added options to support association list operations:
  - `--list-variable` (alias `--list`)
  - `--filter-expression`
  - `--find-expression`
- Updated operation catalog with:
  - `microflow.filterByAssociation`
  - `microflow.findByAssociation`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.11

Hybrid write-path expansion for association-level microflow actions.

- Added new extension routes:
  - `/mendix-studio-automation/microflows/retrieve-association`
  - `/mendix-studio-automation/microflows/change-association`
- Added Studio Pro client support for:
  - inserting a `Retrieve by association` action into a selected microflow via extension service APIs
  - inserting a `Change association` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm scripts:
  - `add-microflow-retrieve-association`
  - `add-microflow-change-association`
  - `npm run add-microflow-retrieve-association`
  - `npm run add-microflow-change-association`
- Added options to support association actions:
  - `--association`
  - `--entity-variable`
  - existing `--change-type`, `--commit`, and `--value` for association changes
- Updated operation catalog with:
  - `microflow.retrieveAssociation`
  - `microflow.changeAssociation`
- Updated documentation:
  - README
  - User Manual
  - Extension README

## 0.2.10

External-reference adoption milestone.

- Reviewed and incorporated useful patterns from:
  - `@jordnlvr/mendix-mcp-server`
  - `ruvnet` MCP gist
- Added local knowledge-gap tracking to prioritize automation hardening:
  - new store: `.automation-state/knowledge-gaps.json`
  - new CLI commands:
    - `record-knowledge-gap`
    - `list-knowledge-gaps`
    - `summarize-knowledge-gaps`
  - new operation catalog entries:
    - `knowledge.recordGap`
    - `knowledge.listGaps`
    - `knowledge.summarizeGaps`
- Added external-reference review document:
  - `docs/EXTERNAL_REFERENCES.md`

## 0.2.9

Hybrid write-path expansion for microflow rollback actions.

- Added new extension route:
  - `/mendix-studio-automation/microflows/rollback-object`
- Added Studio Pro client support for:
  - inserting a `Rollback object` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm script:
  - `add-microflow-rollback-object`
  - `npm run add-microflow-rollback-object`
- Added options to support rollback insertion:
  - `--variable`
  - `--refresh-in-client`
  - `--microflow`
  - `--module`
- Updated operation catalog with:
  - `microflow.rollbackObject`
- Updated documentation:
  - README
  - User Manual
  - Extension README
- Kept commit-related behavior unchanged (review-first workflow; no version-control commits).

## 0.2.8

Hybrid write-path expansion for microflow database retrieval.

- Added new extension route:
  - `/mendix-studio-automation/microflows/retrieve-database`
- Added Studio Pro client support for:
  - inserting a `Retrieve from database` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm script:
  - `add-microflow-retrieve-database`
  - `npm run add-microflow-retrieve-database`
- Added options to support database retrieval insertion:
  - `--entity`
  - `--output-variable-name`
  - `--x-path-constraint`
  - `--retrieve-first`
  - `--microflow`
  - `--module`
- Updated operation catalog with:
  - `microflow.retrieveDatabase`
- Updated documentation:
  - README
  - User Manual
  - Extension README
- Kept commit-related behavior unchanged (review-first workflow; no version-control commits).

## 0.2.7

Hybrid write-path expansion for microflow list creation.

- Added new extension route:
  - `/mendix-studio-automation/microflows/create-list`
- Added Studio Pro client support for:
  - inserting a `Create list` action into a selected microflow via extension service APIs
- Added CLI plumbing and npm script:
  - `add-microflow-create-list`
  - `npm run add-microflow-create-list`
- Added/updated options for list creation:
  - `--entity`
  - `--output-variable-name`
  - `--microflow`
  - `--module`
- Updated operation catalog with:
  - `microflow.createList`
- Updated documentation:
  - README
  - User Manual
  - Extension README
- Kept commit-related behavior unchanged (review-first workflow; no version-control commits).

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
- Added `sync-properties-dialog` to open a Studio Pro properties dialog from a page/microflow target and sync only the changed fields from a saved plan.
- Added `sync-dialog-fields` to compare a live dialog with a saved plan and apply only the changed fields.
- Added `compare-dialog-fields` to diff a live Studio Pro dialog against a saved JSON field plan.
- Added `export-dialog-fields` to write live dialog field/value pairs into reusable JSON files.
- `set-dialog-fields` now also supports `--fields-file`, which avoids shell-escaping issues for larger dialog property batches.
- Added `set-dialog-fields` for batch dialog property updates from JSON payloads, with optional per-field verification rules.
- Added `list-dialog-fields` to enumerate native Studio Pro dialog labels as resolved field/value pairs.
- `get-dialog-field` and `set-dialog-field` now support `--verify-value-contains` for substring-based text verification.
- Native automation element snapshots now include `textValue` whenever the control exposes a `ValuePattern`, improving `list-dialog-items` and related inspection output.
- Added `get-dialog-field` to inspect native Studio Pro dialog fields by visible label, including observed text and toggle state.
- `set-dialog-field` now supports `--verify-value` and `--verify-toggle-state`, so callers can require an exact observed post-write text or toggle state.
- `set-dialog-field` now reports observed post-write text/toggle state in its response payload so dialog mutations can be verified programmatically.
- `set-dialog-field` now supports `CheckBox` and `ToggleButton` controls in native Studio Pro dialogs, including boolean value normalization for `true/false`, `yes/no`, `on/off`, and `1/0`.
- `set-dialog-field` is present but still experimental; it needs more validation across a wider range of Studio Pro property dialogs.
- the current real `insert-action` gesture is still not inserting a visible activity on `ClinicalDocument_ShowPage`; current instrumentation shows it can open the parent microflow properties dialog instead.
- the current `Add > Activity` menu path is stable on `ClinicalDocument_ShowPage`, but it still does not produce a confirmed visible activity mutation by itself or after the current placement-click experiment.
- current runtime-id probing can reliably identify parameter and activity context menus, but connector insertion surfaces still do not expose a stable add-action menu path.
- run and responsive-browser commands now surface blocking Studio Pro dialogs, but they still do not verify a healthy Mendix runtime or browser content.
- `open-properties` is currently strongest on the page designer and `pageExplorer`; other scopes may still need more hardening.
- unopened documents that Studio Pro does not resolve through `Go to` now fail explicitly instead of returning misleading editor results.
