export const operationCatalog = {
    version: 1,
    operations: [
        {
            id: "studio.attach",
            description: "Attach to the running Mendix Studio Pro main window."
        },
        {
            id: "studio.sendKeys",
            description: "Send a key chord to the active Studio Pro window or editor context.",
            status: "implemented"
        },
        {
            id: "studio.runLocal",
            description: "Run the current app locally from Studio Pro.",
            status: "implemented-first-pass"
        },
        {
            id: "studio.stopLocal",
            description: "Stop the currently running local app from Studio Pro.",
            status: "implemented-first-pass"
        },
        {
            id: "studio.showResponsiveWeb",
            description: "Open the current app in Studio Pro's responsive browser view.",
            status: "implemented-first-pass"
        },
        {
            id: "page.create",
            description: "Create a Mendix page through the native New Document and Create Page wizards.",
            status: "implemented-first-pass"
        },
        {
            id: "studio.openProperties",
            description: "Open the native Studio Pro properties dialog for a selected item.",
            status: "implemented-first-pass"
        },
        {
            id: "explorer.openItem",
            description: "Open an App Explorer item such as a module, page, or microflow.",
            status: "implemented-via-go-to"
        },
        {
            id: "page.selectWidget",
            description: "Select a visible widget on the active page canvas.",
            status: "implemented-validated"
        },
        {
            id: "page.insertWidget",
            description: "Insert a toolbox widget into the active page.",
            status: "implemented-validated"
        },
        {
            id: "toolbox.selectItem",
            description: "Select an exact visible Toolbox item.",
            status: "implemented-validated"
        },
        {
            id: "studio.listDialogs",
            description: "List open Studio Pro modal and editor dialogs.",
            status: "implemented"
        },
        {
            id: "dialog.listItems",
            description: "List visible named controls inside an open Studio Pro dialog.",
            status: "implemented"
        },
        {
            id: "dialog.invokeControl",
            description: "Click or select a visible named control inside an open Studio Pro dialog.",
            status: "implemented"
        },
        {
            id: "dialog.setField",
            description: "Set a native Studio Pro dialog field by visible label.",
            status: "implemented-experimental"
        },
        {
            id: "studio.listVisibleLabels",
            description: "List visible labels from key Studio Pro panes and editor surfaces.",
            status: "implemented-validated"
        },
        {
            id: "studio.listOpenTabs",
            description: "List the open page and microflow editor tabs in the top document area.",
            status: "implemented"
        },
        {
            id: "studio.getActiveTab",
            description: "Report the currently active open page or microflow editor tab.",
            status: "implemented"
        },
        {
            id: "studio.getActiveContext",
            description: "Parse the active editor tab into a best-effort document and module context.",
            status: "implemented"
        },
        {
            id: "extension.getStatus",
            description: "Read the local Mendix Studio Pro hybrid extension health endpoint.",
            status: "implemented-first-pass"
        },
        {
            id: "extension.getContext",
            description: "Read active app and document context from the in-Studio Mendix extension.",
            status: "implemented-first-pass"
        },
        {
            id: "extension.getCapabilities",
            description: "Read extension feature capabilities from the in-Studio Mendix extension.",
            status: "implemented-first-pass"
        },
        {
            id: "extension.searchDocuments",
            description: "Search project documents from the in-Studio Mendix extension.",
            status: "implemented-first-pass"
        },
        {
            id: "extension.openDocument",
            description: "Open a project document through the in-Studio Mendix extension.",
            status: "implemented-first-pass"
        },
        {
            id: "ui.quickCreateObjectDialog",
            description: "Open an in-Studio modal dialog to insert a Create object action into a microflow.",
            status: "implemented-first-pass"
        },
        {
            id: "hybrid.getContext",
            description: "Prefer the in-Studio extension context and fall back to UI automation context.",
            status: "implemented-first-pass"
        },
        {
            id: "knowledge.recordGap",
            description: "Record an automation capability gap for prioritization and hardening.",
            status: "implemented-first-pass"
        },
        {
            id: "knowledge.listGaps",
            description: "List recorded automation capability gaps.",
            status: "implemented-first-pass"
        },
        {
            id: "knowledge.summarizeGaps",
            description: "Summarize gap counts by status and requested capability.",
            status: "implemented-first-pass"
        },
        {
            id: "studio.createClientsPage",
            description: "Create a Clients scaffold page and optionally add it to Web navigation.",
            status: "implemented-first-pass"
        },
        {
            id: "studio.addNavigationShortcut",
            description: "Add an open page to the web navigation profile through the in-Studio extension.",
            status: "implemented-first-pass"
        },
        {
            id: "studio.selectTab",
            description: "Activate an already open page or microflow editor tab.",
            status: "implemented"
        },
        {
            id: "studio.closeTab",
            description: "Close a specific open page or microflow editor tab.",
            status: "implemented"
        },
        {
            id: "page.selectExplorerItem",
            description: "Select an exact visible Page Explorer row for the active page.",
            status: "implemented-validated"
        },
        {
            id: "appExplorer.selectItem",
            description: "Select an exact visible App Explorer row.",
            status: "implemented-first-pass"
        },
        {
            id: "studio.waitReady",
            description: "Wait until transient Studio Pro popup windows are gone.",
            status: "implemented"
        },
        {
            id: "microflow.insertAction",
            description: "Insert an activity on the active microflow canvas.",
            status: "implemented-dry-run-validated"
        },
        {
            id: "microflow.connectNodes",
            description: "Connect two microflow nodes on the designer surface.",
            status: "planned"
        },
        {
            id: "microflow.createObject",
            description: "Insert a Create object activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.createList",
            description: "Insert a Create list activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.callMicroflow",
            description: "Insert a Call microflow activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.retrieveDatabase",
            description: "Insert a Retrieve from database activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.retrieveAssociation",
            description: "Insert a Retrieve by association activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.filterByAssociation",
            description: "Insert a Filter by association activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.findByAssociation",
            description: "Insert a Find by association activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.filterByAttribute",
            description: "Insert a Filter by attribute activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.findByAttribute",
            description: "Insert a Find by attribute activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.findByExpression",
            description: "Insert a Find by expression activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.aggregateList",
            description: "Insert an Aggregate list activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.aggregateByAttribute",
            description: "Insert an Aggregate by attribute activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.aggregateByExpression",
            description: "Insert an Aggregate by expression activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.changeList",
            description: "Insert a Change list activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.deleteObject",
            description: "Insert a Delete object activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.commitObject",
            description: "Insert a Commit object activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.rollbackObject",
            description: "Insert a Rollback object activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.changeAttribute",
            description: "Insert a Change attribute activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.changeAssociation",
            description: "Insert a Change association activity into a microflow through the extension API.",
            status: "implemented-using-hybrid-endpoint"
        },
        {
            id: "microflow.selectNode",
            description: "Select a visible microflow node or activity label on the editor surface.",
            status: "implemented-validated"
        }
    ]
};
