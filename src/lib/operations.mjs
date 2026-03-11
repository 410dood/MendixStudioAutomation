export const operationCatalog = {
    version: 1,
    operations: [
        {
            id: "studio.attach",
            description: "Attach to the running Mendix Studio Pro main window."
        },
        {
            id: "explorer.openItem",
            description: "Open an App Explorer item such as a module, page, or microflow.",
            status: "implemented-via-go-to"
        },
        {
            id: "page.selectWidget",
            description: "Select a visible widget on the active page canvas.",
            status: "implemented-first-pass"
        },
        {
            id: "page.insertWidget",
            description: "Insert a toolbox widget into the active page.",
            status: "implemented-first-pass"
        },
        {
            id: "toolbox.selectItem",
            description: "Select an exact visible Toolbox item.",
            status: "implemented-first-pass"
        },
        {
            id: "studio.listVisibleLabels",
            description: "List visible labels from key Studio Pro panes and editor surfaces.",
            status: "implemented-first-pass"
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
            id: "studio.selectTab",
            description: "Activate an already open page or microflow editor tab.",
            status: "implemented"
        },
        {
            id: "page.selectExplorerItem",
            description: "Select an exact visible Page Explorer row for the active page.",
            status: "implemented-first-pass"
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
            status: "implemented-first-pass"
        },
        {
            id: "microflow.connectNodes",
            description: "Connect two microflow nodes on the designer surface.",
            status: "planned"
        },
        {
            id: "microflow.selectNode",
            description: "Select a visible microflow node or activity label on the editor surface.",
            status: "implemented-first-pass"
        }
    ]
};
