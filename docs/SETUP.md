# Setup

## Prerequisites

- Windows desktop session
- Mendix Studio Pro running locally
- Node.js 20+ available on `PATH`
- PowerShell with access to Windows UI Automation assemblies

Optional but available on this machine:

- `.NET SDK 10.0.200`

The current implementation now includes a real Mendix Studio Pro extension project, so the .NET SDK is useful immediately.

## Repository Setup

From the repo root:

```powershell
cd C:\Users\willi\Mendix\MyPluggableWidgets\MendixStudioAutomation
```

There are no npm dependencies yet, so no install step is required.

## Hybrid Extension Setup

The repo includes a C# Studio Pro extension in:

```text
extensions\MendixStudioAutomation_Extension
```

Build it with:

```powershell
dotnet build .\extensions\MendixStudioAutomation_Extension\MendixStudioAutomation_Extension.csproj
```

Install it into the Mendix app folder with:

```powershell
pwsh .\scripts\Install-MendixStudioAutomationExtension.ps1 -AppDirectory C:\Users\willi\Mendix\Olari-main -Build
```

This copies the extension into the Mendix-supported app-local location:

```text
C:\Users\willi\Mendix\Olari-main\extensions\MendixStudioAutomation_Extension
```

The extension root contains:

- `manifest.json`
- `dist\`
- `runtime\endpoint.json` once Studio Pro loads the extension

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
npm run create-page -- --module "Az_ClientManagement" --page-name "Clients_Auto3"
```

If those work, try a safe selection-only command:

```powershell
npm run select-widget -- --page "Client_ClinicalDocument_V3" --widget "Olari_Popup_Default"
npm run list-page-explorer-items -- --page "Client_ClinicalDocument_V3" --limit 12
npm run list-toolbox-items -- --page "Client_ClinicalDocument_V3" --limit 12
npm run list-dialogs
npm run send-keys -- --keys "{ESC}"
```

You can also confirm that open document tabs are discoverable:

```powershell
npm run list-open-tabs
npm run list-open-tabs -- --kind microflow
npm run list-open-tabs -- --module Az_ClientManagement
npm run active-tab
npm run active-context
npm run extension-status
npm run hybrid-context
npm run close-tab -- --tab "Client_ClinicalDocument_V3 [Az_ClientManagement]" --dry-run
npm run close-tab -- --dry-run
```

If a page or microflow is already open, `open-item` will now reuse that tab before falling back to Studio Pro's `Go to` dialog.
If a page or microflow command cannot confirm that Studio Pro actually opened the requested document, it now fails explicitly instead of scraping the wrong window state.
Dialog-oriented commands can be used against native Studio Pro windows like `Select Widget` once they are visible.
`create-page` is now validated for native page creation when the desired template is already visible in the right-hand panel of the `Create Page` wizard.
`extension-status` reports whether Studio Pro has loaded the hybrid extension and written its runtime discovery file.

## Safety Model

- The repo intentionally excludes Mendix project commit and merge operations.
- Prefer `--dry-run` on insertion-style commands first.
- Review Studio Pro visually after any command that changes selection or editor state.
