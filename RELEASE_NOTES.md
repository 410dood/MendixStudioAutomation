# Release Notes

## 0.1.0

Initial public checkpoint for the Mendix Studio Pro automation repo.

Included in this release:

- Node-based CLI for driving the automation layer
- PowerShell UI Automation helpers for Studio Pro window discovery and interaction
- Studio Pro attach, snapshot, search, click, and focus support
- popup inspection and wait-until-ready commands
- document opening through Studio Pro `Go to`
- open-editor tab listing and direct tab activation
- best-known active editor tab reporting with last-known fallback
- active editor context parsing from the current tab title
- first-pass editor selection for page widgets
- first-pass selection for:
  - App Explorer items
  - Page Explorer items
  - Toolbox items
- first-pass `insert-widget` flow with `--dry-run`
- first-pass microflow commands:
  - `select-microflow-node`
  - `insert-action` with `--dry-run`

Not included in this release:

- Mendix project commit support
- Mendix branch merge support
- guaranteed insertion reliability across every Studio Pro pane/layout state
- stable automation for all unopened microflows/documents

Known limitations:

- Studio Pro UI Automation structure changes depending on active panes, popups, and editor type.
- `open-item` is more reliable for already-known or already-open documents than for every unopened asset.
- `select-app-explorer-item` still needs more hardening against alternate left-pane states.
- page and toolbox pane listing are available, but still depend on active Studio Pro layout and tab state.
- microflow targeting is scaffolded, but needs more live verification on canvas nodes and toolbox categories.
