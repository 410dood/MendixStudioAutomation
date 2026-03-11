using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.Menu;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;

namespace MendixStudioAutomation.Extension;

[method: ImportingConstructor]
[Export(typeof(MenuExtension))]
public sealed class MendixStudioAutomationMenuExtension(
    IMessageBoxService messageBoxService,
    IExtensionFileService extensionFileService) : MenuExtension
{
    private readonly IMessageBoxService _messageBoxService = messageBoxService;
    private readonly IExtensionFileService _extensionFileService = extensionFileService;

    public override IEnumerable<MenuViewModel> GetMenus()
    {
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
