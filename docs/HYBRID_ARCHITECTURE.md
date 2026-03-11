# Hybrid Architecture

## Goal

Combine three layers instead of forcing one tool to do everything:

1. `Studio Pro extension` for deterministic in-process access to the Mendix model and Studio Pro services
2. `Current UI automation layer` for full-fidelity editor gestures where the extension surface is not enough
3. `Platform/Model SDK` for out-of-process app metadata, working-copy, and repository workflows

This is the most realistic path to senior-developer-level Mendix automation.

## Version fit

For the current Olari setup on `Mendix Studio Pro 10.24.14`:

- `C# Studio Pro extension`: viable
- `Web extensibility API`: not the primary path for this app version

This matters because Mendix documents the current web extensibility getting-started flow for `Studio Pro 11.2.0 or higher`, while the C# extensibility and Model API are available on the Studio Pro extension surface for the 10.x line as well.

Official references:

- APIs and SDK overview: https://docs.mendix.com/apidocs-mxsdk/
- C# extension points: https://docs.mendix.com/apidocs-mxsdk/apidocs/csharp-extensibility-api-11/extension-points/
- C# model API: https://docs.mendix.com/apidocs-mxsdk/apidocs/interact-with-model-api-11/
- C# services: https://docs.mendix.com/apidocs-mxsdk/apidocs/csharp-extensibility-api-10/services/
- Platform SDK usage: https://docs.mendix.com/apidocs-mxsdk/mxsdk/using-platform-sdk/
- Web extensibility getting started: https://docs.mendix.com/apidocs-mxsdk/apidocs/web-extensibility-api-11/getting-started/

## Why hybrid is better than automation-only

The current UI automation repo is already good at:

- opening documents
- selecting page and microflow elements
- driving native dialogs
- triggering real page mutations
- running Studio Pro and surfacing runtime blockers

But UI automation is still weak for:

- reliable model introspection
- deterministic property read/write
- stable microflow mutation
- understanding Studio Pro state without scraping visible UI

A Studio Pro extension can cover those weak points.

## Recommended split

### Layer 1: C# Studio Pro extension

Use a local extension inside Studio Pro for operations that need model truth instead of screen scraping.

Primary responsibilities:

- expose current app/document/editor context
- expose selected model element identity
- read and write model data through `CurrentApp` / `IModel`
- wrap transactions safely
- use `IMicroflowService` and related services where available
- surface consistency-check/build/error information
- expose stable commands for:
  - create page
  - inspect page structure
  - inspect widget properties
  - inspect microflow graph
  - create/update page elements where supported by the model/service layer

Suggested transport:

- local named pipe first
- localhost HTTP only if named pipes become impractical

Suggested shape:

- extension DLL loaded by Studio Pro
- small command host inside the extension
- command schema with request/response JSON

### Layer 2: UI automation fallback

Keep this repo as the full-fidelity fallback when Studio Pro model/services do not directly expose the operation.

Use automation for:

- page designer gestures
- Page Explorer context menus
- widget picker flows
- microflow canvas placement clicks
- runtime/browser launch and browser verification
- any third-party widget/editor surface that is only available visually

Rule:

- prefer extension command first
- fall back to UI automation only when the extension reports `unsupported` or `insufficient`

### Layer 3: Platform SDK / Model SDK

Use SDKs outside Studio Pro for platform-level tasks that do not require the live editor surface.

Use them for:

- app metadata
- repository/working-copy operations
- app open/bootstrap workflows
- offline model inspection where it is sufficient

Do not rely on SDKs for full page fidelity.

## Concrete command routing

### Prefer extension

- `studio.getCurrentContext`
- `page.getStructure`
- `page.getSelectedElement`
- `page.getProperties`
- `page.setProperties`
- `microflow.getGraph`
- `microflow.getSelectedNode`
- `microflow.insertActionDeterministic`
- `app.getErrors`
- `app.runConsistencyCheck`

### Prefer UI automation

- `page.insertWidgetVisual`
- `page.dragWidget`
- `page.openWidgetPicker`
- `page.placeWidgetOnCanvas`
- `microflow.placeActionVisual`
- `browser.openResponsive`
- `browser.verifyRenderedPage`

### Prefer Platform/Model SDK

- `platform.openWorkingCopy`
- `platform.getRepoInfo`
- `platform.listBranches`
- `platform.getAppInfo`

## Immediate build plan

### Phase 1

Add a minimal C# extension that returns:

- app name
- active document
- active module
- selected element if available
- Studio Pro error count if available

This should prove:

- extension packaging/loading
- IPC from Studio Pro to the local automation repo
- reliable context handoff

### Phase 2

Add model-backed inspection:

- page structure dump
- widget property read
- microflow graph dump

### Phase 3

Add write operations with transactions:

- page property updates
- selected element property updates
- deterministic microflow insertions where supported

### Phase 4

Add orchestrator routing in this repo:

- try extension
- if unsupported, route to current PowerShell/UIA flow
- unify results under one CLI surface

## What I would build next

The next highest-value step is:

1. create a `StudioProExtensionHost` C# project
2. implement `get-context`
3. expose it over a local pipe
4. call it from this repo before any page/microflow mutation

That gives us a real hybrid foundation without abandoning the automation work already proven.
