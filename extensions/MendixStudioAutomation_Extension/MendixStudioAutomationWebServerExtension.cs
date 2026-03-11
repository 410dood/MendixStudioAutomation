using System.ComponentModel.Composition;
using System.Net;
using System.Text;
using System.Text.Json;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Model.Projects;
using Mendix.StudioPro.ExtensionsAPI.Model.Pages;
using Mendix.StudioPro.ExtensionsAPI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.Events;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.WebServer;
using Mendix.StudioPro.ExtensionsAPI.VersionControl;
using MendixStudioAutomation.Extension.Infrastructure;
using MendixStudioAutomation.Extension.Models;

namespace MendixStudioAutomation.Extension;

[Export(typeof(WebServerExtension))]
public sealed class MendixStudioAutomationWebServerExtension : WebServerExtension
{
    private const string RoutePrefix = "/mendix-studio-automation";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly ExtensionStateStore _stateStore = SharedExtensionStateStore.Instance;
    private readonly ILogService _logService;
    private readonly IExtensionFileService _extensionFileService;
    private readonly IDockingWindowService _dockingWindowService;
    private readonly IVersionControlService _versionControlService;
    private readonly INavigationManagerService _navigationManagerService;
    private IEventSubscription? _activeDocumentSubscription;

    [ImportingConstructor]
    public MendixStudioAutomationWebServerExtension(
        ILogService logService,
        IExtensionFileService extensionFileService,
        IDockingWindowService dockingWindowService,
        IVersionControlService versionControlService,
        INavigationManagerService navigationManagerService)
    {
        _logService = logService;
        _extensionFileService = extensionFileService;
        _dockingWindowService = dockingWindowService;
        _versionControlService = versionControlService;
        _navigationManagerService = navigationManagerService;
        _logService.Info("[MendixStudioAutomation] WebServerExtension constructed.");
    }

    public override void InitializeWebServer(IWebServer webServer)
    {
        _logService.Info("[MendixStudioAutomation] InitializeWebServer invoked.");
        _activeDocumentSubscription ??= Subscribe<ActiveDocumentChanged>(OnActiveDocumentChanged);

        webServer.AddRoute($"{RoutePrefix}/health", HandleHealthAsync);
        webServer.AddRoute($"{RoutePrefix}/context", HandleContextAsync);
        webServer.AddRoute($"{RoutePrefix}/capabilities", HandleCapabilitiesAsync);
        webServer.AddRoute($"{RoutePrefix}/documents/search", HandleSearchDocumentsAsync);
        webServer.AddRoute($"{RoutePrefix}/documents/open", HandleOpenDocumentAsync);
        webServer.AddRoute($"{RoutePrefix}/navigation/populate", HandlePopulateNavigationAsync);

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
        RefreshActiveEditorSnapshot(currentApp);
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
            "documents.search",
            "documents.open",
            "navigation.populate",
            "branch.read",
            "webserver.health",
            "webserver.context"
        ];
    }

    private Task HandleSearchDocumentsAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var query = request.QueryString["query"] ?? request.QueryString["q"];
        var moduleName = request.QueryString["module"];
        var type = request.QueryString["type"];
        var limit = TryParseLimit(request.QueryString["limit"]);

        var documents = SearchDocuments(query, moduleName, type, limit);
        var payload = new
        {
            ok = true,
            query,
            module = moduleName,
            type,
            count = documents.Count,
            items = documents
        };

        return WriteJsonAsync(response, payload, HttpStatusCode.OK, cancellationToken);
    }

    private Task HandleOpenDocumentAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var name = request.QueryString["name"];
        var moduleName = request.QueryString["module"];
        var type = request.QueryString["type"];

        if (string.IsNullOrWhiteSpace(name))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'name' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        var matches = SearchDocuments(name, moduleName, type, limit: 10)
            .Where(document => string.Equals(document.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"No document matched '{name}'.",
                name,
                module = moduleName,
                type
            }, HttpStatusCode.NotFound, cancellationToken);
        }

        if (matches.Count > 1)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"Multiple documents matched '{name}'. Provide a module or type to disambiguate.",
                name,
                module = moduleName,
                type,
                matches
            }, HttpStatusCode.Conflict, cancellationToken);
        }

        var document = ResolveDocument(matches[0].Id);
        if (document is null)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"The document '{name}' could not be resolved from the project model anymore.",
                name,
                module = moduleName,
                type,
                match = matches[0]
            }, HttpStatusCode.Gone, cancellationToken);
        }

        var opened = _dockingWindowService.TryOpenEditor(document, null!);
        if (opened)
        {
            UpdateStateFromUnit(
                document,
                fallbackDocumentName: matches[0].Name,
                fallbackDocumentType: matches[0].Type,
                selectionSource: "extension-open-document");
        }

        return WriteJsonAsync(response, new
        {
            ok = opened,
            name,
            module = moduleName,
            type,
            opened,
            match = matches[0]
        }, opened ? HttpStatusCode.OK : HttpStatusCode.InternalServerError, cancellationToken);
    }

    private Task HandlePopulateNavigationAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var pageName = request.QueryString["pageName"] ?? request.QueryString["page"];
        var navigationCaption = request.QueryString["caption"] ?? pageName;
        var moduleName = request.QueryString["module"];
        var type = request.QueryString["type"] ?? "Page";

        if (string.IsNullOrWhiteSpace(pageName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'page' (or 'pageName') query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(navigationCaption))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The navigation caption is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (CurrentApp?.Root is null)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "No active Mendix app model is available."
            }, HttpStatusCode.ServiceUnavailable, cancellationToken);
        }

        var matches = SearchDocuments(pageName, moduleName, type, limit: 10)
            .Where(document => string.Equals(document.Name, pageName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"No page matched '{pageName}'.",
                pageName,
                module = moduleName,
                type
            }, HttpStatusCode.NotFound, cancellationToken);
        }

        if (matches.Count > 1)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"Multiple pages matched '{pageName}'. Provide a module to disambiguate.",
                pageName,
                module = moduleName,
                type,
                matches
            }, HttpStatusCode.Conflict, cancellationToken);
        }

        var match = matches[0];
        var document = ResolveDocument(match.Id);
        if (document is null)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"The page '{pageName}' could not be resolved from the project model anymore.",
                pageName,
                module = moduleName,
                type,
                match
            }, HttpStatusCode.Gone, cancellationToken);
        }

        if (document is not IPage page)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"The resolved document '{pageName}' is not a page.",
                pageName,
                type = match.Type,
                match
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        try
        {
            var caption = navigationCaption.Trim();
            using var tx = CurrentApp.StartTransaction($"Add web navigation item '{caption}'");
            _navigationManagerService.PopulateWebNavigationWith(CurrentApp, [(caption, page)]);
            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                caption = caption,
                pageName = match.Name,
                module = match.ModuleName,
                profile = "web",
                match
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to populate web navigation profile.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message,
                pageName,
                module = moduleName,
                type,
                match
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private void OnActiveDocumentChanged(ActiveDocumentChanged change)
    {
        try
        {
            var document = CurrentApp?.Root is IProject project ? change.GetDocument(project) : null;
            UpdateStateFromUnit(
                document,
                fallbackDocumentName: change.DocumentName,
                fallbackDocumentType: change.DocumentType,
                selectionSource: "active-document-changed");

            _logService.Debug($"Active document changed: {change.DocumentName} ({change.DocumentType})");
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to process ActiveDocumentChanged event.", ex);
        }
    }

    private void RefreshActiveEditorSnapshot(IModel? currentApp)
    {
        if (currentApp is null)
        {
            return;
        }

        try
        {
            if (_dockingWindowService.TryGetActiveEditor(currentApp, out var unit))
            {
                UpdateStateFromUnit(
                    unit,
                    fallbackDocumentName: null,
                    fallbackDocumentType: null,
                    selectionSource: "docking-window-service");
            }
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to query the active editor through IDockingWindowService.", ex);
        }
    }

    private void UpdateStateFromUnit(
        IAbstractUnit? unit,
        string? fallbackDocumentName,
        string? fallbackDocumentType,
        string selectionSource)
    {
        var documentName = ResolveDocumentName(unit) ?? fallbackDocumentName;
        var documentType = ResolveDocumentType(unit) ?? fallbackDocumentType;
        var moduleName = ResolveModuleName(unit);

        _stateStore.UpdateActiveDocument(
            unit?.Id,
            documentName,
            documentType,
            moduleName,
            selectedElementName: null,
            selectionSource: selectionSource);
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
                healthUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/health"),
                capabilitiesUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/capabilities"),
                navigationPopulateUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/navigation/populate")
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

    private static string? ResolveDocumentName(IAbstractUnit? unit)
    {
        if (unit is IDocument document)
        {
            return document.Name;
        }

        return null;
    }

    private static string? ResolveDocumentType(IAbstractUnit? unit)
    {
        if (unit is null)
        {
            return null;
        }

        var typeName = unit.GetType().Name;
        return typeName.StartsWith("I", StringComparison.Ordinal) && typeName.Length > 1
            ? typeName[1..]
            : typeName;
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

    private IReadOnlyList<DocumentDescriptor> SearchDocuments(string? query, string? moduleName, string? type, int limit)
    {
        if (CurrentApp?.Root is not IProject project)
        {
            return [];
        }

        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var normalizedModule = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName.Trim();
        var normalizedType = string.IsNullOrWhiteSpace(type) ? null : type.Trim();

        var documents = new List<DocumentDescriptor>();
        foreach (var moduleEntry in project.GetDocuments())
        {
            var module = moduleEntry.Key;
            foreach (var (document, documentType) in moduleEntry.Value)
            {
                if (document.Excluded)
                {
                    continue;
                }

                var descriptor = new DocumentDescriptor(
                    document.Id,
                    document.Name,
                    documentType.Name,
                    module?.Name);

                if (!MatchesDocument(descriptor, normalizedQuery, normalizedModule, normalizedType))
                {
                    continue;
                }

                documents.Add(descriptor);
            }
        }

        return documents
            .OrderBy(document => RankDocument(document, normalizedQuery))
            .ThenBy(document => document.ModuleName)
            .ThenBy(document => document.Name)
            .Take(limit)
            .ToList();
    }

    private IAbstractUnit? ResolveDocument(string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId) || CurrentApp?.Root is not IProject project)
        {
            return null;
        }

        foreach (var moduleEntry in project.GetDocuments())
        {
            foreach (var (document, _) in moduleEntry.Value)
            {
                if (string.Equals(document.Id, documentId, StringComparison.OrdinalIgnoreCase))
                {
                    return document;
                }
            }
        }

        return null;
    }

    private static bool MatchesDocument(DocumentDescriptor document, string? query, string? moduleName, string? type)
    {
        if (!string.IsNullOrWhiteSpace(moduleName) &&
            !string.Equals(document.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(type) &&
            !string.Equals(document.Type, type, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return document.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(document.ModuleName) && document.ModuleName.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static int RankDocument(DocumentDescriptor document, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return 3;
        }

        if (string.Equals(document.Name, query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (document.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
    }

    private static int TryParseLimit(string? value)
    {
        if (int.TryParse(value, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return 25;
    }
}
