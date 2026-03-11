using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.Menu;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;

namespace MendixStudioAutomation.Extension;

[Export(typeof(MenuExtension))]
public sealed class MendixStudioAutomationMenuExtension : MenuExtension
{
    private readonly IMessageBoxService _messageBoxService;
    private readonly IExtensionFileService _extensionFileService;
    private readonly QuickMicroflowActionDialogController _quickMicroflowActionDialogController;
    private readonly ILogService _logService;

    [ImportingConstructor]
    public MendixStudioAutomationMenuExtension(
        IMessageBoxService messageBoxService,
        IExtensionFileService extensionFileService,
        QuickMicroflowActionDialogController quickMicroflowActionDialogController,
        ILogService logService)
    {
        _messageBoxService = messageBoxService;
        _extensionFileService = extensionFileService;
        _quickMicroflowActionDialogController = quickMicroflowActionDialogController;
        _logService = logService;
        _logService.Info("[MendixStudioAutomation] MenuExtension constructed.");
    }

    public override IEnumerable<MenuViewModel> GetMenus()
    {
        _logService.Info("[MendixStudioAutomation] GetMenus invoked.");
        return
        [
            new MenuViewModel(
                "Mendix Studio Automation",
                [
                    new MenuViewModel("Show Hybrid Endpoint", ShowHybridEndpoint),
                    new MenuViewModel("Quick Create Object Dialog", ShowQuickCreateObjectDialog)
                ])
        ];
    }

    private void ShowHybridEndpoint()
    {
        var endpointFile = _extensionFileService.ResolvePath("runtime", "endpoint.json");
        var details = File.Exists(endpointFile)
            ? File.ReadAllText(endpointFile)
            : $"Runtime endpoint file not found at {endpointFile}. Load the web server extension once and try again.";

        _messageBoxService.ShowInformation(
            "Mendix Studio Automation hybrid extension status",
            details,
            null,
            null);
    }

    private void ShowQuickCreateObjectDialog()
    {
        if (CurrentApp is null || WebServerBaseUrl is null)
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                "No active app or webserver context is available.",
                null,
                null);
            return;
        }

        _quickMicroflowActionDialogController.ShowDialog(
            CurrentApp,
            WebServerBaseUrl,
            initialMicroflowName: null,
            initialModuleName: null,
            initialEntityName: "Document.ClientDocument",
            initialOutputVariableName: "CreatedObject");
    }
}
