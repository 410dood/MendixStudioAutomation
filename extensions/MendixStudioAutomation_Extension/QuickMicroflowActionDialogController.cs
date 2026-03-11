using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Model.Microflows;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;

namespace MendixStudioAutomation.Extension;

[Export(typeof(QuickMicroflowActionDialogController))]
[method: ImportingConstructor]
public sealed class QuickMicroflowActionDialogController(
    IDialogService dialogService,
    IMessageBoxService messageBoxService,
    ILogService logService,
    IMicroflowService microflowService,
    IMicroflowActivitiesService microflowActivitiesService)
{
    private readonly IDialogService _dialogService = dialogService;
    private readonly IMessageBoxService _messageBoxService = messageBoxService;
    private readonly ILogService _logService = logService;
    private readonly IMicroflowService _microflowService = microflowService;
    private readonly IMicroflowActivitiesService _microflowActivitiesService = microflowActivitiesService;

    public void ShowDialog(
        IModel currentApp,
        Uri webServerBaseUrl,
        string? initialMicroflowName,
        string? initialModuleName,
        string? initialEntityName,
        string? initialOutputVariableName)
    {
        var modalViewModel = new QuickMicroflowActionDialogViewModel(
            title: "Quick Create Object",
            currentApp: currentApp,
            dialogService: _dialogService,
            messageBoxService: _messageBoxService,
            logService: _logService,
            microflowService: _microflowService,
            microflowActivitiesService: _microflowActivitiesService,
            webServerBaseUrl: webServerBaseUrl,
            initialMicroflowName: initialMicroflowName,
            initialModuleName: initialModuleName,
            initialEntityName: initialEntityName,
            initialOutputVariableName: initialOutputVariableName)
        {
            Height = 360,
            Width = 620
        };

        _dialogService.ShowDialog(modalViewModel);
    }

    public static string? ResolveModuleName(IAbstractUnit? unit)
    {
        var current = unit;
        while (current is not null)
        {
            if (current is IModule module)
            {
                return module.Name;
            }

            current = current.Container;
        }

        return null;
    }

    public static (IMicroflow Microflow, string Name, string ModuleName)[] ResolveMicroflows(
        IProject project,
        IModule? moduleHint,
        string microflowName,
        bool allowAllModules)
    {
        var name = microflowName.Trim();
        var candidates = new List<(IMicroflow Microflow, string Name, string ModuleName)>();
        var modules = moduleHint is null || !allowAllModules
            ? (moduleHint is null ? project.GetModules() : [moduleHint])
            : project.GetModules();

        foreach (var candidateModule in modules.Distinct())
        {
            var moduleDocuments = project.GetModuleDocuments(candidateModule, typeof(IMicroflow));
            foreach (var document in moduleDocuments)
            {
                if (!string.Equals(document.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (document is IMicroflow microflow)
                {
                    candidates.Add((microflow, document.Name, candidateModule.Name));
                }
            }
        }

        return [.. candidates];
    }
}
