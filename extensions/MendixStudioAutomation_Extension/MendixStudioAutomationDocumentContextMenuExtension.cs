using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Model.Microflows;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.UI.Menu;

namespace MendixStudioAutomation.Extension;

[Export(typeof(ContextMenuExtension<>))]
[method: ImportingConstructor]
public sealed class MendixStudioAutomationDocumentContextMenuExtension(
    QuickMicroflowActionDialogController quickMicroflowActionDialogController) : ContextMenuExtension<IDocument>
{
    private readonly QuickMicroflowActionDialogController _quickMicroflowActionDialogController = quickMicroflowActionDialogController;

    public override IEnumerable<MenuViewModel> GetContextMenus(IDocument document)
    {
        if (CurrentApp is null || WebServerBaseUrl is null)
        {
            yield break;
        }

        if (document is not IMicroflow microflow)
        {
            yield break;
        }

        var moduleName = QuickMicroflowActionDialogController.ResolveModuleName(document);
        yield return new MenuViewModel(
            "Mendix Studio Automation",
            [
                new MenuViewModel(
                    "Quick Create Object Dialog",
                    () => _quickMicroflowActionDialogController.ShowDialog(
                        CurrentApp,
                        WebServerBaseUrl,
                        initialMicroflowName: microflow.Name,
                        initialModuleName: moduleName,
                        initialEntityName: "Document.ClientDocument",
                        initialOutputVariableName: "CreatedObject"))
            ]);
    }
}
