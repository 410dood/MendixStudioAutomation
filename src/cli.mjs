#!/usr/bin/env node

import { formatOutput, parseArgs } from "./lib/cli-helpers.mjs";
import { StudioProClient } from "./lib/studio-pro-client.mjs";
import { operationCatalog } from "./lib/operations.mjs";

async function main() {
    const { command, options } = parseArgs(process.argv.slice(2));
    const client = new StudioProClient();

    switch (command) {
        case "status":
        case "snapshot": {
            const snapshot = await client.snapshot(options);
            formatOutput(snapshot);
            return;
        }
        case "find": {
            const result = await client.findElement(options);
            formatOutput(result);
            return;
        }
        case "click": {
            const result = await client.clickElement(options);
            formatOutput(result);
            return;
        }
        case "send-keys": {
            const result = await client.sendKeys(options);
            formatOutput(result);
            return;
        }
        case "run-local": {
            const result = await client.runLocalApp(options);
            formatOutput(result);
            return;
        }
        case "run-local-verify": {
            const result = await client.runLocalAndVerify(options);
            formatOutput(result);
            return;
        }
        case "stop-local": {
            const result = await client.stopLocalApp(options);
            formatOutput(result);
            return;
        }
        case "show-responsive-web": {
            const result = await client.showResponsiveWeb(options);
            formatOutput(result);
            return;
        }
        case "create-page": {
            const result = await client.createPage(options);
            formatOutput(result);
            return;
        }
        case "create-page-with-widget": {
            const result = await client.createPageWithWidget(options);
            formatOutput(result);
            return;
        }
        case "export-page-widget-properties": {
            const result = await client.exportPageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "open-page-widget-properties": {
            const result = await client.openPageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "get-page-widget-property": {
            const result = await client.getPageWidgetProperty(options);
            formatOutput(result);
            return;
        }
        case "set-page-widget-property": {
            const result = await client.setPageWidgetProperty(options);
            formatOutput(result);
            return;
        }
        case "set-page-widget-properties": {
            const result = await client.setPageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "invoke-page-widget-property-control": {
            const result = await client.invokePageWidgetPropertyControl(options);
            formatOutput(result);
            return;
        }
        case "compare-page-widget-properties": {
            const result = await client.comparePageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "export-compare-page-widget-properties": {
            const result = await client.exportComparePageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "sync-page-widget-properties": {
            const result = await client.syncPageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "export-sync-page-widget-properties": {
            const result = await client.exportSyncPageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "inspect-page-widget-properties": {
            const result = await client.inspectPageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "list-page-widget-property-fields": {
            const result = await client.listPageWidgetPropertyFields(options);
            formatOutput(result);
            return;
        }
        case "list-page-widget-property-items": {
            const result = await client.listPageWidgetPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "export-page-widget-property-items": {
            const result = await client.exportPageWidgetPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "compare-page-widget-property-items": {
            const result = await client.comparePageWidgetPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "sync-page-widget-property-items": {
            const result = await client.syncPageWidgetPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "export-sync-page-widget-property-items": {
            const result = await client.exportSyncPageWidgetPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "export-inspect-page-widget-properties": {
            const result = await client.exportInspectPageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "export-review-page-widget-properties": {
            const result = await client.exportReviewPageWidgetProperties(options);
            formatOutput(result);
            return;
        }
        case "export-page-explorer-item-properties": {
            const result = await client.exportPageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "open-page-explorer-item-properties": {
            const result = await client.openPageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "get-page-explorer-item-property": {
            const result = await client.getPageExplorerItemProperty(options);
            formatOutput(result);
            return;
        }
        case "set-page-explorer-item-property": {
            const result = await client.setPageExplorerItemProperty(options);
            formatOutput(result);
            return;
        }
        case "set-page-explorer-item-properties": {
            const result = await client.setPageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "invoke-page-explorer-item-property-control": {
            const result = await client.invokePageExplorerItemPropertyControl(options);
            formatOutput(result);
            return;
        }
        case "compare-page-explorer-item-properties": {
            const result = await client.comparePageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "export-compare-page-explorer-item-properties": {
            const result = await client.exportComparePageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "sync-page-explorer-item-properties": {
            const result = await client.syncPageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "export-sync-page-explorer-item-properties": {
            const result = await client.exportSyncPageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "inspect-page-explorer-item-properties": {
            const result = await client.inspectPageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "list-page-explorer-item-property-fields": {
            const result = await client.listPageExplorerItemPropertyFields(options);
            formatOutput(result);
            return;
        }
        case "list-page-explorer-item-property-items": {
            const result = await client.listPageExplorerItemPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "export-page-explorer-item-property-items": {
            const result = await client.exportPageExplorerItemPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "compare-page-explorer-item-property-items": {
            const result = await client.comparePageExplorerItemPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "sync-page-explorer-item-property-items": {
            const result = await client.syncPageExplorerItemPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "export-sync-page-explorer-item-property-items": {
            const result = await client.exportSyncPageExplorerItemPropertyItems(options);
            formatOutput(result);
            return;
        }
        case "export-inspect-page-explorer-item-properties": {
            const result = await client.exportInspectPageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "export-review-page-explorer-item-properties": {
            const result = await client.exportReviewPageExplorerItemProperties(options);
            formatOutput(result);
            return;
        }
        case "open-properties": {
            const result = await client.openProperties(options);
            formatOutput(result);
            return;
        }
        case "export-properties-dialog": {
            const result = await client.exportPropertiesDialog(options);
            formatOutput(result);
            return;
        }
        case "compare-properties-dialog": {
            const result = await client.comparePropertiesDialog(options);
            formatOutput(result);
            return;
        }
        case "export-compare-properties-dialog": {
            const result = await client.exportComparePropertiesDialog(options);
            formatOutput(result);
            return;
        }
        case "list-properties-dialog-fields": {
            const result = await client.listPropertiesDialogFields(options);
            formatOutput(result);
            return;
        }
        case "list-properties-dialog-items": {
            const result = await client.listPropertiesDialogItems(options);
            formatOutput(result);
            return;
        }
        case "export-properties-dialog-items": {
            const result = await client.exportPropertiesDialogItems(options);
            formatOutput(result);
            return;
        }
        case "compare-properties-dialog-items": {
            const result = await client.comparePropertiesDialogItems(options);
            formatOutput(result);
            return;
        }
        case "sync-properties-dialog-items": {
            const result = await client.syncPropertiesDialogItems(options);
            formatOutput(result);
            return;
        }
        case "export-sync-properties-dialog-items": {
            const result = await client.exportSyncPropertiesDialogItems(options);
            formatOutput(result);
            return;
        }
        case "invoke-properties-dialog-control": {
            const result = await client.invokePropertiesDialogControl(options);
            formatOutput(result);
            return;
        }
        case "get-properties-dialog-field": {
            const result = await client.getPropertiesDialogField(options);
            formatOutput(result);
            return;
        }
        case "set-properties-dialog-fields": {
            const result = await client.setPropertiesDialogFields(options);
            formatOutput(result);
            return;
        }
        case "set-properties-dialog-field": {
            const result = await client.setPropertiesDialogField(options);
            formatOutput(result);
            return;
        }
        case "sync-properties-dialog": {
            const result = await client.syncPropertiesDialog(options);
            formatOutput(result);
            return;
        }
        case "export-sync-properties-dialog": {
            const result = await client.exportSyncPropertiesDialog(options);
            formatOutput(result);
            return;
        }
        case "open-item": {
            const result = await client.openItem(options);
            formatOutput(result);
            return;
        }
        case "select-widget": {
            const result = await client.selectWidget(options);
            formatOutput(result);
            return;
        }
        case "popup-status": {
            const result = await client.getPopupStatus(options);
            formatOutput(result);
            return;
        }
        case "wait-ready": {
            const result = await client.waitUntilReady(options);
            formatOutput(result);
            return;
        }
        case "select-app-explorer-item": {
            const result = await client.selectAppExplorerItem(options);
            formatOutput(result);
            return;
        }
        case "select-explorer-item": {
            const result = await client.selectExplorerItem(options);
            formatOutput(result);
            return;
        }
        case "select-toolbox-item": {
            const result = await client.selectToolboxItem(options);
            formatOutput(result);
            return;
        }
        case "list-dialogs": {
            const result = await client.listDialogs(options);
            formatOutput(result);
            return;
        }
        case "list-scope-elements": {
            const result = await client.listScopeElements(options);
            formatOutput(result);
            return;
        }
        case "invoke-scope-element-action": {
            const result = await client.invokeScopeElementAction(options);
            formatOutput(result);
            return;
        }
        case "list-dialog-items": {
            const result = await client.listDialogItems(options);
            formatOutput(result);
            return;
        }
        case "export-dialog-items": {
            const result = await client.exportDialogItems(options);
            formatOutput(result);
            return;
        }
        case "compare-dialog-items": {
            const result = await client.compareDialogItems(options);
            formatOutput(result);
            return;
        }
        case "sync-dialog-items": {
            const result = await client.syncDialogItems(options);
            formatOutput(result);
            return;
        }
        case "export-sync-dialog-items": {
            const result = await client.exportSyncDialogItems(options);
            formatOutput(result);
            return;
        }
        case "list-dialog-fields": {
            const result = await client.listDialogFields(options);
            formatOutput(result);
            return;
        }
        case "export-dialog-fields": {
            const result = await client.exportDialogFields(options);
            formatOutput(result);
            return;
        }
        case "compare-dialog-fields": {
            const result = await client.compareDialogFields(options);
            formatOutput(result);
            return;
        }
        case "sync-dialog-fields": {
            const result = await client.syncDialogFields(options);
            formatOutput(result);
            return;
        }
        case "invoke-dialog-control": {
            const result = await client.invokeDialogControl(options);
            formatOutput(result);
            return;
        }
        case "get-dialog-field": {
            const result = await client.getDialogField(options);
            formatOutput(result);
            return;
        }
        case "set-dialog-fields": {
            const result = await client.setDialogFields(options);
            formatOutput(result);
            return;
        }
        case "set-dialog-field": {
            const result = await client.setDialogField(options);
            formatOutput(result);
            return;
        }
        case "list-editor-menu-items": {
            const result = await client.listEditorMenuItems(options);
            formatOutput(result);
            return;
        }
        case "invoke-editor-menu-item": {
            const result = await client.invokeEditorMenuItem(options);
            formatOutput(result);
            return;
        }
        case "invoke-editor-menu-path": {
            const result = await client.invokeEditorMenuPath(options);
            formatOutput(result);
            return;
        }
        case "click-editor-offset": {
            const result = await client.clickEditorOffset(options);
            formatOutput(result);
            return;
        }
        case "list-app-explorer-items": {
            const result = await client.listAppExplorerItems(options);
            formatOutput(result);
            return;
        }
        case "list-page-explorer-items": {
            const result = await client.listPageExplorerItems(options);
            formatOutput(result);
            return;
        }
        case "list-toolbox-items": {
            const result = await client.listToolboxItems(options);
            formatOutput(result);
            return;
        }
        case "list-editor-labels": {
            const result = await client.listEditorLabels(options);
            formatOutput(result);
            return;
        }
        case "list-open-tabs": {
            const result = await client.listOpenTabs(options);
            formatOutput(result);
            return;
        }
        case "active-tab": {
            const result = await client.getActiveTab(options);
            formatOutput(result);
            return;
        }
        case "active-context": {
            const result = await client.getActiveContext(options);
            formatOutput(result);
            return;
        }
        case "extension-status": {
            const result = await client.getExtensionStatus(options);
            formatOutput(result);
            return;
        }
        case "extension-context": {
            const result = await client.getExtensionContext(options);
            formatOutput(result);
            return;
        }
        case "extension-capabilities": {
            const result = await client.getExtensionCapabilities(options);
            formatOutput(result);
            return;
        }
        case "extension-search-documents": {
            const result = await client.searchExtensionDocuments(options);
            formatOutput(result);
            return;
        }
        case "extension-open-document": {
            const result = await client.openExtensionDocument(options);
            formatOutput(result);
            return;
        }
        case "list-microflow-activities": {
            const result = await client.listMicroflowActivities(options);
            formatOutput(result);
            return;
        }
        case "find-microflow-activities": {
            const result = await client.findMicroflowActivities(options);
            formatOutput(result);
            return;
        }
        case "open-quick-create-object-dialog": {
            const result = await client.openQuickCreateObjectDialog(options);
            formatOutput(result);
            return;
        }
        case "add-navigation-shortcut": {
            const result = await client.addNavigationShortcut(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-create-object": {
            const result = await client.addMicroflowCreateObject(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-create-list": {
            const result = await client.addMicroflowCreateList(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-call": {
            const result = await client.addMicroflowCallMicroflow(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-retrieve-database": {
            const result = await client.addMicroflowRetrieveDatabase(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-retrieve-association": {
            const result = await client.addMicroflowRetrieveAssociation(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-filter-by-association": {
            const result = await client.addMicroflowFilterByAssociation(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-find-by-association": {
            const result = await client.addMicroflowFindByAssociation(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-filter-by-attribute": {
            const result = await client.addMicroflowFilterByAttribute(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-find-by-attribute": {
            const result = await client.addMicroflowFindByAttribute(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-find-by-expression": {
            const result = await client.addMicroflowFindByExpression(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-aggregate-list": {
            const result = await client.addMicroflowAggregateList(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-aggregate-by-attribute": {
            const result = await client.addMicroflowAggregateByAttribute(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-aggregate-by-expression": {
            const result = await client.addMicroflowAggregateByExpression(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-change-list": {
            const result = await client.addMicroflowChangeList(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-sort-list": {
            const result = await client.addMicroflowSortList(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-reduce-aggregate": {
            const result = await client.addMicroflowReduceAggregate(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-list-head": {
            const result = await client.addMicroflowListHead(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-list-tail": {
            const result = await client.addMicroflowListTail(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-list-contains": {
            const result = await client.addMicroflowListContains(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-list-union": {
            const result = await client.addMicroflowListUnion(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-list-intersect": {
            const result = await client.addMicroflowListIntersect(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-list-subtract": {
            const result = await client.addMicroflowListSubtract(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-list-equals": {
            const result = await client.addMicroflowListEquals(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-delete-object": {
            const result = await client.addMicroflowDeleteObject(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-commit-object": {
            const result = await client.addMicroflowCommitObject(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-rollback-object": {
            const result = await client.addMicroflowRollbackObject(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-change-attribute": {
            const result = await client.addMicroflowChangeAttribute(options);
            formatOutput(result);
            return;
        }
        case "add-microflow-change-association": {
            const result = await client.addMicroflowChangeAssociation(options);
            formatOutput(result);
            return;
        }
        case "create-clients-page": {
            const result = await client.createClientsPage(options);
            formatOutput(result);
            return;
        }
        case "hybrid-context": {
            const result = await client.getHybridContext(options);
            formatOutput(result);
            return;
        }
        case "record-knowledge-gap": {
            const result = await client.recordKnowledgeGap(options);
            formatOutput(result);
            return;
        }
        case "list-knowledge-gaps": {
            const result = await client.listKnowledgeGaps(options);
            formatOutput(result);
            return;
        }
        case "summarize-knowledge-gaps": {
            const result = await client.summarizeKnowledgeGaps(options);
            formatOutput(result);
            return;
        }
        case "rag-search": {
            const result = await client.ragSearch(options);
            formatOutput(result);
            return;
        }
        case "select-tab": {
            const result = await client.selectTab(options);
            formatOutput(result);
            return;
        }
        case "close-tab": {
            const result = await client.closeTab(options);
            formatOutput(result);
            return;
        }
        case "insert-widget": {
            const result = await client.insertWidget(options);
            formatOutput(result);
            return;
        }
        case "select-microflow-node": {
            const result = await client.selectMicroflowNode(options);
            formatOutput(result);
            return;
        }
        case "insert-action": {
            const result = await client.insertAction(options);
            formatOutput(result);
            return;
        }
        case "operations": {
            formatOutput(operationCatalog);
            return;
        }
        case "help":
        default:
            printHelp();
    }
}

function printHelp() {
    console.log(`Mendix Studio Automation

Usage:
  node src/cli.mjs <command> [options]

Commands:
  status                      Snapshot the attached Studio Pro window
  snapshot                    Alias for status
  find                        Search for an element in Studio Pro
  click                       Invoke or click a matched element
  send-keys                   Send a Studio Pro key chord to the foreground editor
  run-local                   Run the current app locally in Studio Pro
  run-local-verify            Run the app locally and wait until a local URL responds
  stop-local                  Stop a locally running app in Studio Pro
  show-responsive-web         Open the app in Studio Pro's responsive browser view
  create-page                 Create a Mendix page through the native Studio Pro wizard
  create-page-with-widget     Create a page and insert an initial widget
  export-page-widget-properties Export a page widget property plan to JSON
  open-page-widget-properties Open the native properties dialog for a page widget
  get-page-widget-property    Read one property field for a page widget
  set-page-widget-property    Set one property field for a page widget
  set-page-widget-properties  Apply batch field edits for a page widget from JSON input
  invoke-page-widget-property-control Invoke a named control in a page widget properties dialog
  compare-page-widget-properties Compare a page widget against a saved property plan
  export-compare-page-widget-properties Export a field diff for a page widget property plan
  sync-page-widget-properties Apply or preview a page widget property plan
  export-sync-page-widget-properties Export a dry-run field sync plan for a page widget
  inspect-page-widget-properties Inspect both fields and raw controls for a page widget
  list-page-widget-property-fields List resolved field/value pairs for a page widget properties dialog
  list-page-widget-property-items List raw visible controls for a page widget properties dialog
  export-page-widget-property-items Export raw visible controls for a page widget properties dialog
  compare-page-widget-property-items Compare raw visible controls for a page widget properties dialog
  sync-page-widget-property-items Build a raw-control sync plan for a page widget properties dialog
  export-sync-page-widget-property-items Export a raw-control sync plan for a page widget properties dialog
  export-inspect-page-widget-properties Export the combined widget inspection payload to JSON
  export-review-page-widget-properties Export a combined review bundle for a page widget
  export-page-explorer-item-properties Export a page explorer item property plan to JSON
  open-page-explorer-item-properties Open the native properties dialog for a page explorer item
  get-page-explorer-item-property Read one property field for a page explorer item
  set-page-explorer-item-property Set one property field for a page explorer item
  set-page-explorer-item-properties Apply batch field edits for a page explorer item from JSON input
  invoke-page-explorer-item-property-control Invoke a named control in a page explorer item properties dialog
  compare-page-explorer-item-properties Compare a page explorer item against a saved property plan
  export-compare-page-explorer-item-properties Export a field diff for a page explorer item property plan
  sync-page-explorer-item-properties Apply or preview a page explorer item property plan
  export-sync-page-explorer-item-properties Export a dry-run field sync plan for a page explorer item
  inspect-page-explorer-item-properties Inspect both fields and raw controls for a page explorer item
  list-page-explorer-item-property-fields List resolved field/value pairs for a page explorer item properties dialog
  list-page-explorer-item-property-items List raw visible controls for a page explorer item properties dialog
  export-page-explorer-item-property-items Export raw visible controls for a page explorer item properties dialog
  compare-page-explorer-item-property-items Compare raw visible controls for a page explorer item properties dialog
  sync-page-explorer-item-property-items Build a raw-control sync plan for a page explorer item properties dialog
  export-sync-page-explorer-item-property-items Export a raw-control sync plan for a page explorer item properties dialog
  export-inspect-page-explorer-item-properties Export the combined explorer-item inspection payload to JSON
  export-review-page-explorer-item-properties Export a combined review bundle for a page explorer item
  open-properties             Open the properties dialog for a selected Studio Pro item
  export-properties-dialog    Open a properties dialog and export its field plan to JSON
  compare-properties-dialog   Open a properties dialog and compare it to a JSON field plan
  export-compare-properties-dialog Open a properties dialog and export a field diff to JSON
  list-properties-dialog-fields Open a properties dialog and list resolved field/value pairs
  list-properties-dialog-items Open a properties dialog and list raw visible controls
  export-properties-dialog-items Open a properties dialog and export raw visible controls to JSON
  compare-properties-dialog-items Open a properties dialog and compare raw visible controls to JSON
  sync-properties-dialog-items Open a properties dialog and build a raw-control sync plan
  export-sync-properties-dialog-items Open a properties dialog and export a raw-control sync plan to JSON
  export-sync-properties-dialog Open a properties dialog and export a field sync plan to JSON
  invoke-properties-dialog-control Open a properties dialog and invoke a named control inside it
  get-properties-dialog-field Open a properties dialog and read one labeled field
  set-properties-dialog-fields Open a properties dialog and apply batch field edits
  set-properties-dialog-field Open a properties dialog and set one labeled field
  sync-properties-dialog      Open a properties dialog and sync it from a JSON field plan
  open-item                   Open a Studio Pro document by name with Ctrl+G
  select-widget               Select a visible named widget or page element
  popup-status                Inspect current Studio Pro popup windows
  wait-ready                  Wait until Studio Pro popups are cleared
  select-app-explorer-item   Select an App Explorer row by exact name
  select-explorer-item        Select a Page Explorer row by exact name
  select-toolbox-item         Select a Toolbox item by exact name
  list-dialogs                List open Studio Pro modal/editor dialogs
  list-scope-elements         List visible elements inside a scoped Studio Pro surface
  invoke-scope-element-action Click or invoke a scoped Studio Pro element by runtime id
  list-dialog-items           List visible named controls inside a Studio Pro dialog
  export-dialog-items         Export raw visible dialog controls to JSON
  compare-dialog-items        Compare raw visible dialog controls to JSON
  sync-dialog-items           Build a raw-control sync plan from dialog comparison
  export-sync-dialog-items    Export a raw-control sync plan to JSON
  list-dialog-fields          List visible dialog labels that resolve to field/value pairs
  export-dialog-fields        Export dialog field/value pairs to a JSON file for reuse
  compare-dialog-fields       Compare a live dialog against a JSON field plan
  sync-dialog-fields          Apply only the changed fields from a JSON dialog plan
  invoke-dialog-control       Click/select a visible named control in a Studio Pro dialog
  get-dialog-field            Read a native Studio Pro dialog field by its visible label
  set-dialog-fields           Set multiple native Studio Pro dialog fields from JSON input
  set-dialog-field            Set a native Studio Pro dialog field by its visible label
  list-editor-menu-items      Open an editor element context menu and list its items
  invoke-editor-menu-item     Invoke a named editor element context-menu item
  invoke-editor-menu-path     Invoke a multi-step editor context-menu path
  click-editor-offset         Click a point relative to a visible editor element
  list-app-explorer-items     List visible App Explorer labels
  list-page-explorer-items    List visible Page Explorer labels
  list-toolbox-items          List visible Toolbox labels
  list-editor-labels          List visible editor labels
  list-open-tabs              List open page and microflow editor tabs
  active-tab                  Get the currently active open editor tab
  active-context              Parse the active tab into document/module context
  extension-status            Check whether the in-Studio hybrid extension is available
  extension-context           Read the context exposed by the in-Studio hybrid extension
  extension-capabilities      Read the feature capabilities exposed by the in-Studio hybrid extension
  extension-search-documents  Search project documents through the in-Studio hybrid extension
  extension-open-document     Open a project document through the in-Studio hybrid extension
  list-microflow-activities   List activity metadata for a microflow through the extension
  find-microflow-activities   Filter activity metadata for a microflow through the extension
  open-quick-create-object-dialog Open the in-Studio quick create-object modal dialog
  add-navigation-shortcut     Add a document to the web navigation profile through the extension
  add-microflow-create-object Add a microflow Create object activity through the extension
  add-microflow-create-list   Add a microflow Create list activity through the extension
  add-microflow-call          Add a microflow Call microflow activity through the extension
  add-microflow-retrieve-database Add a microflow Retrieve from database activity through the extension
  add-microflow-retrieve-association Add a microflow Retrieve by association activity through the extension
  add-microflow-filter-by-association Add a microflow Filter by association activity through the extension
  add-microflow-find-by-association Add a microflow Find by association activity through the extension
  add-microflow-filter-by-attribute Add a microflow Filter by attribute activity through the extension
  add-microflow-find-by-attribute Add a microflow Find by attribute activity through the extension
  add-microflow-find-by-expression Add a microflow Find by expression activity through the extension
  add-microflow-aggregate-list Add a microflow Aggregate list activity through the extension
  add-microflow-aggregate-by-attribute Add a microflow Aggregate by attribute activity through the extension
  add-microflow-aggregate-by-expression Add a microflow Aggregate by expression activity through the extension
  add-microflow-change-list   Add a microflow Change list activity through the extension
  add-microflow-sort-list     Add a microflow Sort list activity through the extension
  add-microflow-reduce-aggregate Add a microflow Reduce aggregate activity through the extension
  add-microflow-list-head     Add a microflow List head activity through the extension
  add-microflow-list-tail     Add a microflow List tail activity through the extension
  add-microflow-list-contains Add a microflow List contains activity through the extension
  add-microflow-list-union    Add a microflow List union activity through the extension
  add-microflow-list-intersect Add a microflow List intersect activity through the extension
  add-microflow-list-subtract Add a microflow List subtract activity through the extension
  add-microflow-list-equals   Add a microflow List equals activity through the extension
  add-microflow-delete-object Add a microflow Delete object activity through the extension
  add-microflow-commit-object Add a microflow Commit object activity through the extension
  add-microflow-rollback-object Add a microflow Rollback object activity through the extension
  add-microflow-change-attribute Add a microflow Change attribute activity through the extension
  add-microflow-change-association Add a microflow Change association activity through the extension
  create-clients-page         Create a Clients page and insert a default DataGrid widget
  hybrid-context              Prefer the extension context and fall back to UI automation
  record-knowledge-gap        Record an automation capability gap for later hardening
  list-knowledge-gaps         List recorded automation capability gaps
  summarize-knowledge-gaps    Summarize gap counts by status and capability
  rag-search                  Local RAG search across automation docs and source files
  select-tab                  Activate an open Studio Pro editor tab
  close-tab                   Close a specific open Studio Pro editor tab
  insert-widget              Prepare or execute first-pass widget insertion
  select-microflow-node      Select a visible microflow node label
  insert-action              Prepare or execute first-pass microflow action insertion
  operations                  List planned high-level operations

Options:
  --title <text>              Match Studio Pro main window title
  --process-id <id>           Target a specific Studio Pro process
  --depth <n>                 UI tree traversal depth
  --max-children <n>          Max children emitted per node
  --name <text>               Element name filter
  --automation-id <text>      AutomationId filter
  --class-name <text>         Class name filter
  --control-type <text>       Control type filter, e.g. TreeItem
  --runtime-id <a.b.c>        Runtime id returned by find/snapshot
  --max-results <n>           Max matches returned by find
  --keys <value>              Studio Pro key chord for send-keys, e.g. "{F5}" or "^,"
  --url <url>                 URL used by run-local-verify (default: http://localhost:8080)
  --verify-timeout-ms <n>     Max wait for run-local-verify URL readiness (default: 120000)
  --verify-poll-ms <n>        Poll interval for run-local-verify URL readiness (default: 2000)
  --verify-status <codes>     Comma-separated expected HTTP status codes for run-local-verify
  --verify-text <text>        Required substring in HTTP response body for run-local-verify
  --verify-location <text>    Required substring in HTTP Location header for run-local-verify
  --verify-title <text>       Required substring in HTML <title> for run-local-verify
  --verify-content-type <t>   Required substring in HTTP Content-Type for run-local-verify
  --verify-header <rule>      Header assertion for run-local-verify in Name=substring form; separate multiple with ';;'
  --verify-follow-redirects <bool> Follow redirects before evaluating run-local-verify assertions
  --verify-final-url <text>   Required substring in the final resolved URL for run-local-verify
  --verify-only <bool>        Skip F5 and only perform run-local-verify HTTP checks
  --page-name <name>          Page name for create-page
  --scope <name>              UI scope (editor/pageExplorer/toolbox) or comma-separated rag-search sources
  --item <name>               Document, page, snippet, microflow, or entity name to open
  --document-id <id>          Stable Mendix document id for extension-backed open commands
  --template <name>           Visible template name for create-page
  --page <name>               Page to open before selecting a widget
  --widget <name>             Visible widget or element name to select
  --surface <name>            editor or any (default: editor)
  --timeout-ms <n>            Wait timeout in milliseconds
  --poll-ms <n>               Wait poll interval in milliseconds
  --dialog <name>             Open Studio Pro dialog window name
  --label-contains <text>     Filter list-dialog-fields results to labels containing text
  --output-file <path>        Output file path for export-dialog-fields
  --items-json <json>         JSON array for compare-dialog-items item comparison
  --items-file <path>         JSON file for compare-dialog-items item comparison
  --format <name>             Output format for export-dialog-fields: object|array
  --near-name <name>          Sort/filter scope elements around a visible named element
  --radius <n>                Optional max pixel distance from --near-name
  --control <name>            Visible control name inside a Studio Pro dialog
  --label <name>              Visible field label inside a Studio Pro dialog
  --value <text>              Value to set into a native Studio Pro dialog field
  --fields-json <json>        JSON object/array for set-dialog-fields batch updates
  --fields-file <path>        JSON file for set-dialog-fields batch updates
  --verify-value <text>       Require set-dialog-field to observe an exact post-write text value
  --verify-value-contains <t> Require get/set-dialog-field to observe a post-read/write text substring
  --verify-toggle-state <t>   Require set-dialog-field to observe toggle state On|Off|Indeterminate (also accepts true/false)
  --continue-on-error <bool>  Continue batch dialog field updates after a field-level failure
  --dry-run <bool>            Preview planned sync updates without mutating Studio Pro
  --finalize-dialog <name>    Click a dialog button like OK, Apply, Cancel, or Close after a properties command
  --element <name>            Visible editor element name to target for a context menu
  --menu-item <name>          Editor context-menu item to invoke
  --menu-path <a>b>           Editor context-menu path, e.g. "Add>Activity"
  --offset-x <n>              Horizontal pixel offset from the target element center
  --offset-y <n>              Vertical pixel offset from the target element center
  --target <name>             Page Explorer target container/row for insertion
  --dry-run                   Resolve the insertion path without executing it
  --microflow <name>          Microflow to open before node/action operations
  --action-name <name>        Toolbox action name for microflow insertion
  --node <name>               Visible microflow node label to select
  --tab <name>                Open Studio Pro editor tab name to activate or close
  --kind <name>               Filter open tabs by kind, e.g. microflow or page-or-document
  --module <name>             Filter or disambiguate by Mendix module name, or target module for create-page
  --endpoint-file <path>      Hybrid extension endpoint discovery file
  --endpoint-url <url>        Explicit hybrid extension base URL
  --query <text>              Extension document search text
  --action-type <name>        Activity/action type filter for find-microflow-activities
  --type <name>               Extension document type, e.g. Microflow or Page
  --control-type <name>       Optional dialog field type filter such as Edit or ComboBox
  --widget <name>             Widget name for create-page-with-widget or create-clients-page
  --target <name>             Optional explicit Page Explorer target for create-page-with-widget or create-clients-page
  --template <name>           Optional page template to use for create-page-with-widget or create-clients-page
  --page-explorer-limit <n>   Max Page Explorer rows considered as insert targets
  --timeout-ms <n>            Wait timeout in milliseconds
  --add-navigation            Attempt to add the created/opened page to web navigation via extension
  --navigation-caption <text>  Optional caption for the generated navigation item
  --entity <name>             Module-qualified or local entity to instantiate, e.g. Document.ClientDocument
  --output-variable-name <text> Output variable name for create/call/retrieve/filter/find actions
  --insert-before-activity <text> Optional activity caption/action type to insert before for selected microflow mutation commands
  --insert-before-index <n>   Optional activity index from list-microflow-activities to insert before
  --commit <name>             Commit mode for create/change actions: Yes|YesWithoutEvents|No
  --refresh-in-client <true|false> Refresh client after create
  --initial-values <json>     JSON object of initial attribute values, e.g. {"Name":"John","Amount":1}
  --called-microflow <name>   Called microflow for add-microflow-call; supports Module.Microflow
  --called-module <name>      Optional module hint for called microflow resolution
  --parameter-mappings <json> JSON object of call parameter expressions, e.g. {"P_Name":"$Name","P_Count":"1"}
  --variable <name>           Existing scope variable name for delete/commit/rollback/change-attribute/change-association actions
  --entity-variable <name>    Source object variable for retrieve-association actions
  --list-variable <name>      Source list variable for filter/find-by-association actions
  --list <name>               Alias for --list-variable
  --with-events <true|false>  Include events when committing objects
  --attribute <name>          Attribute to mutate: Attribute, Entity.Attribute, or Module.Entity.Attribute
  --association <name>        Association to mutate or retrieve by: Association, Entity.Association, or Module.Entity.Association
  --value <text>              New value/expression for change actions; alias for association filter/find expressions
  --change-type <name>        Change type for change-attribute/change-association actions: Set|Add|Remove
  --change-list-operation <name> Change-list operation: Set|Add|Remove|Clear
  --filter-expression <text>  Expression for filter-by-association actions
  --find-expression <text>    Expression for find-by-association actions
  --aggregate-expression <text> Expression for aggregate-by-expression actions
  --aggregate-function <text> Aggregate function: Sum|Average|Count|Minimum|Maximum|All|Any|Reduce
  --initial-expression <text> Initial expression for reduce-aggregate actions
  --reduce-type <name>      Result type for reduce-aggregate: String|Integer|Decimal|Float|Boolean|DateTime
  --object-variable <name>  Object/list variable for list-contains actions
  --other-list-variable <name> Second list variable for list-union/intersect/subtract/equals actions
  --second-list-variable <name> Alias for --other-list-variable
  --sort-descending <true|false> Sort descending for sort-list actions (default: false)
  --x-path-constraint <text>  Optional XPath constraint for retrieve-database action
  --retrieve-first <true|false> Retrieve first object instead of a list
  --sort-attribute <name>     Optional sort attribute for retrieve-database actions
  --range-offset-expression <text> Optional start expression for ranged retrieve-database actions
  --range-amount-expression <text> Optional amount expression for ranged retrieve-database actions
  --requested-capability <text> Capability requested when recording a knowledge gap
  --observed-issue <text>     What failed or is missing
  --impact <text>             Why the gap matters
  --context <text>            Optional context such as page/microflow/environment
  --per-file-limit <n>        Max RAG matches per file before global ranking
  --source <text>             Source of the gap report (default: manual)
  --status <text>             Gap status: open|in_progress|resolved|blocked
  --limit <n>                 Limit records returned by list-knowledge-gaps
  --caption <text>            Optional explicit caption for add-navigation-shortcut
`);
}

main().catch(error => {
    const payload = {
        ok: false,
        error: error instanceof Error ? error.message : String(error)
    };
    console.error(JSON.stringify(payload, null, 2));
    process.exitCode = 1;
});
