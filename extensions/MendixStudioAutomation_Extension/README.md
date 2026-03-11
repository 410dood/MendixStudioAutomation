# MendixStudioAutomation_Extension

This folder contains the first in-Studio part of the hybrid architecture for `Mendix Studio Automation`.

It uses the official `Mendix.StudioPro.ExtensionsAPI` package pinned to the exact Studio Pro build currently used for Olari:

- `10.24.14-build.90436`

## What it does

- registers a supported `WebServerExtension`
- exposes:
  - `/mendix-studio-automation/health`
  - `/mendix-studio-automation/context`
  - `/mendix-studio-automation/capabilities`
  - `/mendix-studio-automation/documents/search`
  - `/mendix-studio-automation/documents/open`
  - `/mendix-studio-automation/navigation/populate`
  - `/mendix-studio-automation/microflows/create-object`
  - `/mendix-studio-automation/microflows/create-list`
  - `/mendix-studio-automation/microflows/retrieve-database`
  - `/mendix-studio-automation/microflows/delete-object`
  - `/mendix-studio-automation/microflows/commit-object`
  - `/mendix-studio-automation/microflows/rollback-object`
  - `/mendix-studio-automation/microflows/change-attribute`
- tracks the active Studio Pro document through `ActiveDocumentChanged`
- writes a runtime discovery file to `runtime/endpoint.json`
- adds a small `Mendix Studio Automation` menu in Studio Pro for manual verification

## Build

From this folder:

```powershell
dotnet build
```

The build copies all extension artifacts into `dist/`, and `mxextensions.json` points Studio Pro at:

```text
bin\Debug\net8.0-windows\MendixStudioAutomation_Extension.dll
```

## Install in Studio Pro

Copy the build output under your Mendix app directory as:

```text
<your app>\extensions\MendixStudioAutomation_Extension
```

The project also includes an install helper:

```powershell
pwsh .\scripts\Install-MendixStudioAutomationExtension.ps1 -AppDirectory C:\Users\willi\Mendix\Olari-main -Build
```

That keeps the extension root in the Mendix-documented shape:

- `manifest.json` stays at the extension root
- the DLL and dependencies sit beside `manifest.json`
- `runtime/endpoint.json` is written beside the manifest for discovery by the Node automation layer

## Current limits

- selected element inspection is not implemented yet
- error-count reporting is not implemented yet
- write operations now include a limited, targeted microflow mutation path:
  - create a `Create object` activity in a target microflow
  - create a `Create list` activity in a target microflow
  - add a `Retrieve from database` activity in a target microflow
  - add a `Delete object` activity in a target microflow
  - add a `Commit object` activity in a target microflow
  - add a `Rollback object` activity in a target microflow
  - add a `Change attribute` activity in a target microflow

Remaining write operations are intentionally not implemented yet.

This is intentionally the smallest supported in-Studio slice: stable context and transport first, model mutation second.
