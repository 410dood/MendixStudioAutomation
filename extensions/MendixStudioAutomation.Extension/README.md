# MendixStudioAutomation.Extension

This folder contains the first in-Studio part of the hybrid architecture for `Mendix Studio Automation`.

It uses the official `Mendix.StudioPro.ExtensionsAPI` package pinned to the exact Studio Pro build currently used for Olari:

- `10.24.14-build.90436`

## What it does

- registers a supported `WebServerExtension`
- exposes:
  - `/mendix-studio-automation/health`
  - `/mendix-studio-automation/context`
  - `/mendix-studio-automation/capabilities`
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
dist/MendixStudioAutomation.Extension.dll
```

## Install in Studio Pro

Use this folder as the extension root in Studio Pro development mode, or copy it under your Mendix app directory as:

```text
<your app>\extensions\MendixStudioAutomation.Extension
```

The required root-level file is:

- `manifest.json`

The project also includes an install helper:

```powershell
pwsh .\scripts\Install-MendixStudioAutomationExtension.ps1 -AppDirectory C:\Users\willi\Mendix\Olari-main -Build
```

That keeps the extension root in the Mendix-app-supported shape:

- `manifest.json` stays at the extension root
- `dist/` contains the built assembly and dependencies
- `runtime/endpoint.json` is written beside the manifest for discovery by the Node automation layer

## Current limits

- selected element inspection is not implemented yet
- error-count reporting is not implemented yet
- write operations are not implemented yet

This is intentionally the smallest supported in-Studio slice: stable context and transport first, model mutation second.
