using System.ComponentModel.Composition;
using System.Net;
using System.Text;
using System.Text.Json;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.Events;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.WebServer;
using Mendix.StudioPro.ExtensionsAPI.VersionControl;
using MendixStudioAutomation.Extension.Infrastructure;
using MendixStudioAutomation.Extension.Models;

namespace MendixStudioAutomation.Extension;

[method: ImportingConstructor]
[Export(typeof(WebServerExtension))]
public sealed class MendixStudioAutomationWebServerExtension(
    ILogService logService,
    IExtensionFileService extensionFileService,
    IVersionControlService versionControlService) : WebServerExtension
{
    private const string RoutePrefix = "/mendix-studio-automation";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly ExtensionStateStore _stateStore = SharedExtensionStateStore.Instance;
    private readonly ILogService _logService = logService;
    private readonly IExtensionFileService _extensionFileService = extensionFileService;
    private readonly IVersionControlService _versionControlService = versionControlService;
    private IEventSubscription? _activeDocumentSubscription;

    public override void InitializeWebServer(IWebServer webServer)
    {
        _activeDocumentSubscription ??= Subscribe<ActiveDocumentChanged>(OnActiveDocumentChanged);

        webServer.AddRoute($"{RoutePrefix}/health", HandleHealthAsync);
        webServer.AddRoute($"{RoutePrefix}/context", HandleContextAsync);
        webServer.AddRoute($"{RoutePrefix}/capabilities", HandleCapabilitiesAsync);

        PersistRuntimeEndpoint();
        _logService.Info($"Mendix Studio Automation extension routes registered at {WebServerBaseUrl}{RoutePrefix}");
    }

    private Task HandleHealthAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        DateTimeOffset? lastDocumentChangeUtc = _stateStore.LastDocumentChangeUtc == DateTimeOffset.MinValue
            ? null
            : _stateStore.LastDocumentChangeUtc;

        var payload = new
        {
            ok = true,
            extensionName = "Mendix Studio Automation",
            generatedAtUtc = DateTimeOffset.UtcNow,
            routePrefix = RoutePrefix,
            baseUrl = WebServerBaseUrl?.ToString(),
            lastDocumentChangeUtc
        };

        return WriteJsonAsync(response, payload, HttpStatusCode.OK, cancellationToken);
    }

    private Task HandleCapabilitiesAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var payload = new
        {
            ok = true,
            capabilities = GetCapabilities()
        };

        return WriteJsonAsync(response, payload, HttpStatusCode.OK, cancellationToken);
    }

    private Task HandleContextAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var payload = BuildContextSnapshot();
        return WriteJsonAsync(response, payload, HttpStatusCode.OK, cancellationToken);
    }

    private StudioContextSnapshot BuildContextSnapshot()
    {
        var currentApp = CurrentApp;
        var activeDocument = _stateStore.GetActiveDocumentSnapshot();
        var branchName = TryGetBranchName(currentApp);

        return new StudioContextSnapshot(
            ExtensionName: "Mendix Studio Automation",
            ExtensionVersion: typeof(MendixStudioAutomationWebServerExtension).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            StudioWebServerBaseUrl: WebServerBaseUrl?.ToString(),
            AppName: currentApp?.Root?.Name,
            AppDirectory: currentApp?.Root?.DirectoryPath,
            BranchName: branchName,
            IsVersionControlled: currentApp is not null && _versionControlService.IsProjectVersionControlled(currentApp),
            ErrorCount: null,
            ActiveDocument: activeDocument,
            Capabilities: GetCapabilities());
    }

    private IReadOnlyList<string> GetCapabilities()
    {
        return
        [
            "context.read",
            "document.active.read",
            "branch.read",
            "webserver.health",
            "webserver.context"
        ];
    }

    private void OnActiveDocumentChanged(ActiveDocumentChanged change)
    {
        try
        {
            var document = CurrentApp?.Root is IProject project ? change.GetDocument(project) : null;
            var moduleName = ResolveModuleName(document);

            _stateStore.UpdateActiveDocument(
                document?.Id,
                change.DocumentName,
                change.DocumentType,
                moduleName);

            _logService.Debug($"Active document changed: {change.DocumentName} ({change.DocumentType})");
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to process ActiveDocumentChanged event.", ex);
        }
    }

    private string? TryGetBranchName(IModel? currentApp)
    {
        if (currentApp is null)
        {
            return null;
        }

        try
        {
            if (!_versionControlService.IsProjectVersionControlled(currentApp))
            {
                return null;
            }

            var branch = _versionControlService.GetCurrentBranch(currentApp);
            return branch?.Name;
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to read current branch information.", ex);
            return null;
        }
    }

    private void PersistRuntimeEndpoint()
    {
        try
        {
            var endpointFilePath = _extensionFileService.ResolvePath("runtime", "endpoint.json");
            var endpointDirectory = Path.GetDirectoryName(endpointFilePath);
            if (!string.IsNullOrWhiteSpace(endpointDirectory))
            {
                Directory.CreateDirectory(endpointDirectory);
            }

            var payload = new
            {
                extensionName = "Mendix Studio Automation",
                generatedAtUtc = DateTimeOffset.UtcNow,
                baseUrl = WebServerBaseUrl?.ToString(),
                routePrefix = RoutePrefix,
                contextUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/context"),
                healthUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/health")
            };

            File.WriteAllText(endpointFilePath, JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to persist the extension runtime endpoint file.", ex);
        }
    }

    private static string? ResolveModuleName(IAbstractUnit? unit)
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

    private static string? CombineUrl(Uri? baseUrl, string path)
    {
        if (baseUrl is null)
        {
            return null;
        }

        return new Uri(baseUrl, path.TrimStart('/')).ToString();
    }

    private static async Task WriteJsonAsync(
        HttpListenerResponse response,
        object payload,
        HttpStatusCode statusCode,
        CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = body.Length;
        await response.OutputStream.WriteAsync(body, cancellationToken);
        response.OutputStream.Close();
    }

    private static class SharedExtensionStateStore
    {
        internal static readonly ExtensionStateStore Instance = new();
    }
}
