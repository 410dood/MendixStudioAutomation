# Setup

## Prerequisites

- Windows desktop session
- Mendix Studio Pro running locally
- Node.js 20+ available on `PATH`
- PowerShell with access to Windows UI Automation assemblies

Optional but available on this machine:

- `.NET SDK 10.0.200`

The current implementation does not require a compiled .NET helper, but the SDK is useful for future hardening.

## Repository Setup

From the repo root:

```powershell
cd C:\Users\willi\Mendix\MyPluggableWidgets\MendixStudioAutomation
```

There are no npm dependencies yet, so no install step is required.

## Studio Pro Setup

- Open the Mendix app in Studio Pro.
- Keep the app in the foreground desktop session where PowerShell UI Automation can interact with it.
- Prefer working on a branch line when testing automation that can modify pages or microflows.

## Basic Verification

Use these commands to confirm the agent can see Studio Pro:

```powershell
npm run status
npm run popup-status
npm run wait-ready -- --timeout-ms 5000
```

If those work, try a safe selection-only command:

```powershell
npm run select-toolbox-item -- --item "Create object"
npm run list-toolbox-items -- --microflow "ClinicalDocument_ShowPage" --limit 8
```

You can also confirm that open document tabs are discoverable:

```powershell
npm run list-open-tabs
npm run list-open-tabs -- --kind microflow
npm run list-open-tabs -- --module Az_ClientManagement
npm run active-tab
npm run active-context
npm run close-tab -- --tab "Client_ClinicalDocument_V3 [Az_ClientManagement]" --dry-run
npm run close-tab -- --dry-run
```

If a page or microflow is already open, `open-item` will now reuse that tab before falling back to Studio Pro's `Go to` dialog.
If a page or microflow command cannot confirm that Studio Pro actually opened the requested document, it now fails explicitly instead of scraping the wrong window state.

## Safety Model

- The repo intentionally excludes Mendix project commit and merge operations.
- Prefer `--dry-run` on insertion-style commands first.
- Review Studio Pro visually after any command that changes selection or editor state.
