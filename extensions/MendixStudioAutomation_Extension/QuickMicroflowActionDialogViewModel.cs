using System.ComponentModel;
using System.Net;
using System.Text.Json;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Model.Microflows.Actions;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.Dialogs;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.WebView;

namespace MendixStudioAutomation.Extension;

public sealed class QuickMicroflowActionDialogViewModel : WebViewModalDialogViewModel
{
    private readonly IModel _currentApp;
    private readonly IDialogService _dialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ILogService _logService;
    private readonly IMicroflowService _microflowService;
    private readonly IMicroflowActivitiesService _microflowActivitiesService;
    private readonly Uri _webServerBaseUrl;
    private readonly string? _initialMicroflowName;
    private readonly string? _initialModuleName;
    private readonly string? _initialEntityName;
    private readonly string? _initialOutputVariableName;

    public QuickMicroflowActionDialogViewModel(
        string title,
        IModel currentApp,
        IDialogService dialogService,
        IMessageBoxService messageBoxService,
        ILogService logService,
        IMicroflowService microflowService,
        IMicroflowActivitiesService microflowActivitiesService,
        Uri webServerBaseUrl,
        string? initialMicroflowName,
        string? initialModuleName,
        string? initialEntityName,
        string? initialOutputVariableName)
        : base(title)
    {
        _currentApp = currentApp;
        _dialogService = dialogService;
        _messageBoxService = messageBoxService;
        _logService = logService;
        _microflowService = microflowService;
        _microflowActivitiesService = microflowActivitiesService;
        _webServerBaseUrl = webServerBaseUrl;
        _initialMicroflowName = initialMicroflowName;
        _initialModuleName = initialModuleName;
        _initialEntityName = initialEntityName;
        _initialOutputVariableName = initialOutputVariableName;
    }

    public override void InitWebView(IWebView webView)
    {
        webView.MessageReceived += WebViewOnMessageReceived;
        OnClosing = HandleOnClosing;

        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_initialMicroflowName))
        {
            queryParts.Add($"microflow={WebUtility.UrlEncode(_initialMicroflowName)}");
        }

        if (!string.IsNullOrWhiteSpace(_initialModuleName))
        {
            queryParts.Add($"module={WebUtility.UrlEncode(_initialModuleName)}");
        }

        if (!string.IsNullOrWhiteSpace(_initialEntityName))
        {
            queryParts.Add($"entity={WebUtility.UrlEncode(_initialEntityName)}");
        }

        if (!string.IsNullOrWhiteSpace(_initialOutputVariableName))
        {
            queryParts.Add($"outputVariableName={WebUtility.UrlEncode(_initialOutputVariableName)}");
        }

        var relativePath = "mendix-studio-automation/ui/quick-create-object";
        if (queryParts.Count > 0)
        {
            relativePath += "?" + string.Join("&", queryParts);
        }

        webView.Address = new Uri(_webServerBaseUrl, relativePath);
    }

    private void HandleOnClosing(CancelEventArgs cancelEventArgs)
    {
        // No-op on close.
    }

    private void WebViewOnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<QuickCreateObjectDialogMessage>(e.Message);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Action))
            {
                return;
            }

            switch (payload.Action)
            {
                case "cancel":
                    _dialogService.CloseDialog(this);
                    break;
                case "createObject":
                    HandleCreateObject(payload);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logService.Error("Quick create object dialog failed to process message.", ex);
            _messageBoxService.ShowError(
                "Mendix Studio Automation",
                $"Quick create object dialog failed: {ex.Message}",
                null,
                null);
        }
    }

    private void HandleCreateObject(QuickCreateObjectDialogMessage payload)
    {
        if (_currentApp.Root is not IProject project)
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                "No active Mendix app model is available.",
                null,
                null);
            return;
        }

        var microflowName = payload.Microflow?.Trim();
        var moduleName = payload.Module?.Trim();
        var entityName = payload.Entity?.Trim();
        var outputVariableName = payload.OutputVariableName?.Trim();
        var commitText = payload.Commit?.Trim();
        var refreshInClient = payload.RefreshInClient ?? false;

        if (string.IsNullOrWhiteSpace(microflowName)
            || string.IsNullOrWhiteSpace(entityName)
            || string.IsNullOrWhiteSpace(outputVariableName))
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                "Microflow, Entity, and Output variable are required.",
                null,
                null);
            return;
        }

        var module = string.IsNullOrWhiteSpace(moduleName)
            ? null
            : project.GetModules().FirstOrDefault(candidate =>
                string.Equals(candidate.Name, moduleName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(moduleName) && module is null)
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                $"No module named '{moduleName}' was found.",
                null,
                null);
            return;
        }

        var microflowMatches = QuickMicroflowActionDialogController.ResolveMicroflows(project, module, microflowName, allowAllModules: true);
        if (microflowMatches.Length == 0)
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                $"No microflow named '{microflowName}' was found.",
                null,
                null);
            return;
        }

        if (microflowMatches.Length > 1)
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                $"Microflow '{microflowName}' is ambiguous. Specify a module.",
                null,
                null);
            return;
        }

        var targetMicroflow = microflowMatches[0].Microflow;
        var targetModuleName = microflowMatches[0].ModuleName;

        var entityParts = entityName.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        var entityModuleName = entityParts.Length == 2 ? entityParts[0].Trim() : moduleName;
        var targetEntityName = entityParts.Length == 2 ? entityParts[1].Trim() : entityName;
        var modulesToSearch = string.IsNullOrWhiteSpace(entityModuleName)
            ? project.GetModules()
            : project.GetModules().Where(candidate =>
                string.Equals(candidate.Name, entityModuleName, StringComparison.OrdinalIgnoreCase));

        var targetEntity = modulesToSearch
            .Where(candidate => candidate.DomainModel is not null)
            .SelectMany(candidate => candidate.DomainModel!.GetEntities())
            .FirstOrDefault(candidate => string.Equals(candidate.Name, targetEntityName, StringComparison.OrdinalIgnoreCase));

        if (targetEntity is null)
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                $"Entity '{entityName}' was not found.",
                null,
                null);
            return;
        }

        if (!Enum.TryParse<CommitEnum>(string.IsNullOrWhiteSpace(commitText) ? "No" : commitText, ignoreCase: true, out var commitMode))
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                "Commit must be one of Yes, YesWithoutEvents, or No.",
                null,
                null);
            return;
        }

        using var tx = _currentApp.StartTransaction($"Quick create object in {targetMicroflow}");
        var activity = _microflowActivitiesService.CreateCreateObjectActivity(
            _currentApp,
            targetEntity,
            outputVariableName,
            commitMode,
            refreshInClient,
            []);

        var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
        if (!inserted)
        {
            _messageBoxService.ShowWarning(
                "Mendix Studio Automation",
                "Could not insert the Create object activity at the start of the microflow.",
                null,
                null);
            return;
        }

        tx.Commit();

        _messageBoxService.ShowInformation(
            "Mendix Studio Automation",
            $"Create object inserted into {targetModuleName}.{microflowName}.",
            null,
            null);

        _dialogService.CloseDialog(this);
    }

    private sealed class QuickCreateObjectDialogMessage
    {
        public string? Action { get; set; }
        public string? Microflow { get; set; }
        public string? Module { get; set; }
        public string? Entity { get; set; }
        public string? OutputVariableName { get; set; }
        public string? Commit { get; set; }
        public bool? RefreshInClient { get; set; }
    }
}
