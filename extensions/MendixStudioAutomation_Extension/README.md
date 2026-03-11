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
  - `/mendix-studio-automation/ui/quick-create-object`
  - `/mendix-studio-automation/ui/quick-create-object/open`
  - `/mendix-studio-automation/documents/search`
  - `/mendix-studio-automation/documents/open`
  - `/mendix-studio-automation/microflows/list-activities`
  - `/mendix-studio-automation/navigation/populate`
  - `/mendix-studio-automation/microflows/create-object`
  - `/mendix-studio-automation/microflows/create-list`
  - `/mendix-studio-automation/microflows/call-microflow`
  - `/mendix-studio-automation/microflows/retrieve-database`
  - `/mendix-studio-automation/microflows/retrieve-association`
  - `/mendix-studio-automation/microflows/filter-by-association`
  - `/mendix-studio-automation/microflows/find-by-association`
  - `/mendix-studio-automation/microflows/filter-by-attribute`
  - `/mendix-studio-automation/microflows/find-by-attribute`
  - `/mendix-studio-automation/microflows/find-by-expression`
  - `/mendix-studio-automation/microflows/aggregate-list`
  - `/mendix-studio-automation/microflows/aggregate-by-attribute`
  - `/mendix-studio-automation/microflows/aggregate-by-expression`
  - `/mendix-studio-automation/microflows/change-list`
  - `/mendix-studio-automation/microflows/sort-list`
  - `/mendix-studio-automation/microflows/reduce-aggregate`
  - `/mendix-studio-automation/microflows/list-head`
  - `/mendix-studio-automation/microflows/list-tail`
  - `/mendix-studio-automation/microflows/list-contains`
  - `/mendix-studio-automation/microflows/list-union`
  - `/mendix-studio-automation/microflows/list-intersect`
  - `/mendix-studio-automation/microflows/list-subtract`
  - `/mendix-studio-automation/microflows/list-equals`
  - `/mendix-studio-automation/microflows/delete-object`
  - `/mendix-studio-automation/microflows/commit-object`
  - `/mendix-studio-automation/microflows/rollback-object`
  - `/mendix-studio-automation/microflows/change-attribute`
  - `/mendix-studio-automation/microflows/change-association`
- tracks the active Studio Pro document through `ActiveDocumentChanged`
- writes a runtime discovery file to `runtime/endpoint.json`
- adds a small `Mendix Studio Automation` menu in Studio Pro for manual verification
- adds a microflow `ContextMenuExtension<IDocument>` entry for quick create-object workflows
- includes a modal webview quick-create-object dialog pattern based on Mendix sample guidance

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
  - launch an in-Studio quick create-object dialog through menu/context menu/web route
  - create a `Create object` activity in a target microflow (optional insert-before targeting by caption/type or index)
  - create a `Create list` activity in a target microflow (optional insert-before targeting by caption/type or index)
  - add a `Call microflow` activity in a target microflow (optional insert-before targeting by caption/type or index)
  - add a `Retrieve from database` activity in a target microflow (with optional XPath, sort, range expressions, and insert-before targeting by caption/type or index)
  - add a `Retrieve by association` activity in a target microflow (optional insert-before targeting by caption/type or index)
  - add a `Filter by association` activity in a target microflow
  - add a `Find by association` activity in a target microflow
  - add a `Filter by attribute` activity in a target microflow
  - add a `Find by attribute` activity in a target microflow
  - add a `Find by expression` activity in a target microflow
  - add an `Aggregate list` activity in a target microflow
  - add an `Aggregate by attribute` activity in a target microflow
  - add an `Aggregate by expression` activity in a target microflow
  - add a `Change list` activity in a target microflow
  - add a `Sort list` activity in a target microflow
  - add a `Reduce aggregate` activity in a target microflow
  - add a `List head` activity in a target microflow
  - add a `List tail` activity in a target microflow
  - add a `List contains` activity in a target microflow
  - add a `List union` activity in a target microflow
  - add a `List intersect` activity in a target microflow
  - add a `List subtract` activity in a target microflow
  - add a `List equals` activity in a target microflow
  - add a `Delete object` activity in a target microflow (optional insert-before targeting by caption/type or index)
  - add a `Commit object` activity in a target microflow (optional insert-before targeting by caption/type or index)
  - add a `Rollback object` activity in a target microflow (optional insert-before targeting by caption/type or index)
  - add a `Change attribute` activity in a target microflow (optional insert-before targeting by caption/type or index)
  - add a `Change association` activity in a target microflow (optional insert-before targeting by caption/type or index)

Remaining write operations are intentionally not implemented yet.

This is intentionally the smallest supported in-Studio slice: stable context and transport first, model mutation second.
