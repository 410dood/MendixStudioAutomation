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
    private readonly ILogService _logService;

    [ImportingConstructor]
    public MendixStudioAutomationMenuExtension(
        IMessageBoxService messageBoxService,
        IExtensionFileService extensionFileService,
        ILogService logService)
    {
        _messageBoxService = messageBoxService;
        _extensionFileService = extensionFileService;
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
                    new MenuViewModel("Show Hybrid Endpoint", ShowHybridEndpoint)
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
}
