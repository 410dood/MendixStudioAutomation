using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.Menu;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;

namespace MendixStudioAutomation.ProbeExtension;

[method: ImportingConstructor]
[Export(typeof(MenuExtension))]
public sealed class ProbeMenuExtension(IMessageBoxService messageBoxService, ILogService logService) : MenuExtension
{
    public override IEnumerable<MenuViewModel> GetMenus()
    {
        logService.Info("[MendixStudioAutomation.Probe] GetMenus invoked.");
        return
        [
            new MenuViewModel(
                "Probe hello",
                () =>
                {
                    logService.Info("[MendixStudioAutomation.Probe] Probe hello clicked.");
                    messageBoxService.ShowInformation("Probe extension loaded.");
                })
        ];
    }
}
