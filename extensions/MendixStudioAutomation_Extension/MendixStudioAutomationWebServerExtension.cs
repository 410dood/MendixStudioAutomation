using System.ComponentModel.Composition;
using System.Net;
using System.Text;
using System.Text.Json;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Model.DomainModels;
using Mendix.StudioPro.ExtensionsAPI.Model.MicroflowExpressions;
using Mendix.StudioPro.ExtensionsAPI.Model.Microflows;
using Mendix.StudioPro.ExtensionsAPI.Model.Microflows.Actions;
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
    private readonly IMicroflowService _microflowService;
    private readonly IMicroflowActivitiesService _microflowActivitiesService;
    private readonly IMicroflowExpressionService _microflowExpressionService;
    private readonly IVersionControlService _versionControlService;
    private readonly INavigationManagerService _navigationManagerService;
    private IEventSubscription? _activeDocumentSubscription;

    [ImportingConstructor]
    public MendixStudioAutomationWebServerExtension(
        ILogService logService,
        IExtensionFileService extensionFileService,
        IDockingWindowService dockingWindowService,
        IMicroflowService microflowService,
        IMicroflowActivitiesService microflowActivitiesService,
        IMicroflowExpressionService microflowExpressionService,
        IVersionControlService versionControlService,
        INavigationManagerService navigationManagerService)
    {
        _logService = logService;
        _extensionFileService = extensionFileService;
        _dockingWindowService = dockingWindowService;
        _microflowService = microflowService;
        _microflowActivitiesService = microflowActivitiesService;
        _microflowExpressionService = microflowExpressionService;
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
        webServer.AddRoute($"{RoutePrefix}/microflows/create-object", HandleCreateMicroflowObjectAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/create-list", HandleCreateMicroflowListAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/delete-object", HandleDeleteMicroflowObjectAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/commit-object", HandleCommitMicroflowObjectAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/change-attribute", HandleChangeMicroflowAttributeAsync);

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
            "microflow.createObject",
            "microflow.createList",
            "microflow.deleteObject",
            "microflow.commitObject",
            "microflow.changeAttribute",
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

    private Task HandleCreateMicroflowObjectAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (CurrentApp?.Root is not IProject project)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "No active Mendix app model is available."
            }, HttpStatusCode.ServiceUnavailable, cancellationToken);
        }

        var microflowName = request.QueryString["microflow"] ?? request.QueryString["name"];
        var moduleName = request.QueryString["module"];
        var entityName = request.QueryString["entity"];
        var outputVariableName = request.QueryString["outputVariableName"]
            ?? request.QueryString["outputVariable"]
            ?? request.QueryString["output"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(entityName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'entity' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        try
        {
            var normalizedModuleName = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName!.Trim();
            var module = ResolveModule(project, normalizedModuleName);
            var entityModuleHint = module;
            if (normalizedModuleName is not null && module is null)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = $"No module named '{normalizedModuleName}' was found.",
                    module = normalizedModuleName
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            var microflowMatches = ResolveMicroflows(project, module, microflowName, allowAllModules: true);
            if (microflowMatches.Length == 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = module is null
                        ? $"No matching microflow named '{microflowName}' was found."
                        : $"No matching microflow named '{microflowName}' was found in module '{module.Name}'.",
                    microflow = microflowName,
                    module = module?.Name
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            if (microflowMatches.Length > 1)
            {
                var ambiguousMicroflows = microflowMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple microflows matched the request. Include --module to disambiguate.",
                    microflow = microflowName,
                    module = normalizedModuleName,
                    matches = ambiguousMicroflows
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var targetMicroflow = microflowMatches[0].Microflow;
            var targetMicroflowModule = microflowMatches[0].ModuleName;

            var entityMatches = ResolveEntities(project, entityName, entityModuleHint);
            if (entityMatches.Length == 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = $"No entity named '{entityName}' was found.",
                    entity = entityName,
                    module = normalizedModuleName
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            if (entityMatches.Length > 1)
            {
                var ambiguousEntities = entityMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple entities matched the request. Include --module to disambiguate.",
                    entity = entityName,
                    module = normalizedModuleName,
                    matches = ambiguousEntities
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var targetEntity = entityMatches[0].Entity;
            var targetEntityModule = entityMatches[0].ModuleName;
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "CreatedObject" : outputVariableName!.Trim();

            if (!TryParseCommitEnum(request.QueryString["commit"], out var commitMode))
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Invalid commit mode. Use Yes, YesWithoutEvents, or No.",
                    commit = request.QueryString["commit"]
                }, HttpStatusCode.BadRequest, cancellationToken);
            }

            var refreshInClient = TryParseBool(request.QueryString["refreshInClient"], false);
            var initialValues = ParseInitialValues(
                request.QueryString["initialValues"],
                _microflowExpressionService,
                targetEntity);

            if (initialValues is null)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "initialValues must be a JSON object whose values are valid Mendix expressions.",
                    raw = request.QueryString["initialValues"]
                }, HttpStatusCode.BadRequest, cancellationToken);
            }

            using var tx = CurrentApp.StartTransaction($"Add Create object activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateCreateObjectActivity(
                CurrentApp,
                targetEntity,
                output,
                commitMode,
                refreshInClient,
                initialValues
                    .Select(value => (value.AttributeName, value.Expression))
                    .ToArray());

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert a Create object activity at the start of the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                entity = targetEntity.Name,
                entityModule = targetEntityModule,
                outputVariableName = output,
                commit = commitMode.ToString(),
                refreshInClient,
                initialValueCount = initialValues.Length,
                route = "microflows/create-object",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow object activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleCreateMicroflowListAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (CurrentApp?.Root is not IProject project)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "No active Mendix app model is available."
            }, HttpStatusCode.ServiceUnavailable, cancellationToken);
        }

        var microflowName = request.QueryString["microflow"] ?? request.QueryString["name"];
        var moduleName = request.QueryString["module"];
        var entityName = request.QueryString["entity"];
        var outputVariableName = request.QueryString["outputVariableName"]
            ?? request.QueryString["outputVariable"]
            ?? request.QueryString["output"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(entityName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'entity' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        try
        {
            var normalizedModuleName = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName!.Trim();
            var module = ResolveModule(project, normalizedModuleName);
            var entityModuleHint = module;
            if (normalizedModuleName is not null && module is null)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = $"No module named '{normalizedModuleName}' was found.",
                    module = normalizedModuleName
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            var microflowMatches = ResolveMicroflows(project, module, microflowName, allowAllModules: true);
            if (microflowMatches.Length == 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = module is null
                        ? $"No matching microflow named '{microflowName}' was found."
                        : $"No matching microflow named '{microflowName}' was found in module '{module.Name}'.",
                    microflow = microflowName,
                    module = module?.Name
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            if (microflowMatches.Length > 1)
            {
                var ambiguousMicroflows = microflowMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple microflows matched the request. Include --module to disambiguate.",
                    microflow = microflowName,
                    module = normalizedModuleName,
                    matches = ambiguousMicroflows
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var targetMicroflow = microflowMatches[0].Microflow;
            var targetMicroflowModule = microflowMatches[0].ModuleName;

            var entityMatches = ResolveEntities(project, entityName, entityModuleHint);
            if (entityMatches.Length == 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = $"No entity named '{entityName}' was found.",
                    entity = entityName,
                    module = normalizedModuleName
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            if (entityMatches.Length > 1)
            {
                var ambiguousEntities = entityMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple entities matched the request. Include --module to disambiguate.",
                    entity = entityName,
                    module = normalizedModuleName,
                    matches = ambiguousEntities
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var targetEntity = entityMatches[0].Entity;
            var targetEntityModule = entityMatches[0].ModuleName;
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "CreatedList" : outputVariableName!.Trim();

            using var tx = CurrentApp.StartTransaction($"Add Create list activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateCreateListActivity(
                CurrentApp,
                targetEntity,
                output);

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert a Create list activity at the start of the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                entity = targetEntity.Name,
                entityModule = targetEntityModule,
                outputVariableName = output,
                route = "microflows/create-list",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow list activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleDeleteMicroflowObjectAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (CurrentApp?.Root is not IProject project)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "No active Mendix app model is available."
            }, HttpStatusCode.ServiceUnavailable, cancellationToken);
        }

        var microflowName = request.QueryString["microflow"] ?? request.QueryString["name"];
        var moduleName = request.QueryString["module"];
        var variableName = request.QueryString["variable"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(variableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'variable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        try
        {
            var normalizedModuleName = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName.Trim();
            var module = ResolveModule(project, normalizedModuleName);
            if (normalizedModuleName is not null && module is null)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = $"No module named '{normalizedModuleName}' was found.",
                    microflow = microflowName,
                    module = normalizedModuleName
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            var microflowMatches = ResolveMicroflows(project, module, microflowName, allowAllModules: true);
            if (microflowMatches.Length == 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = module is null
                        ? $"No matching microflow named '{microflowName}' was found."
                        : $"No matching microflow named '{microflowName}' was found in module '{module.Name}'.",
                    microflow = microflowName,
                    module = module?.Name
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            if (microflowMatches.Length > 1)
            {
                var ambiguousMicroflows = microflowMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple microflows matched the request. Include --module to disambiguate.",
                    microflow = microflowName,
                    module = normalizedModuleName,
                    matches = ambiguousMicroflows
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var targetMicroflow = microflowMatches[0].Microflow;
            var targetMicroflowModule = microflowMatches[0].ModuleName;
            var variable = variableName.Trim();

            using var tx = CurrentApp.StartTransaction($"Add Delete object activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateDeleteObjectActivity(CurrentApp, variable);
            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert a Delete object activity at the start of the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                variableName = variable,
                route = "microflows/delete-object",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow delete activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleCommitMicroflowObjectAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (CurrentApp?.Root is not IProject project)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "No active Mendix app model is available."
            }, HttpStatusCode.ServiceUnavailable, cancellationToken);
        }

        var microflowName = request.QueryString["microflow"] ?? request.QueryString["name"];
        var moduleName = request.QueryString["module"];
        var variableName = request.QueryString["variable"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(variableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'variable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        try
        {
            var normalizedModuleName = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName.Trim();
            var module = ResolveModule(project, normalizedModuleName);
            if (normalizedModuleName is not null && module is null)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = $"No module named '{normalizedModuleName}' was found.",
                    microflow = microflowName,
                    module = normalizedModuleName
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            var microflowMatches = ResolveMicroflows(project, module, microflowName, allowAllModules: true);
            if (microflowMatches.Length == 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = module is null
                        ? $"No matching microflow named '{microflowName}' was found."
                        : $"No matching microflow named '{microflowName}' was found in module '{module.Name}'.",
                    microflow = microflowName,
                    module = module?.Name
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            if (microflowMatches.Length > 1)
            {
                var ambiguousMicroflows = microflowMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple microflows matched the request. Include --module to disambiguate.",
                    microflow = microflowName,
                    module = normalizedModuleName,
                    matches = ambiguousMicroflows
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var targetMicroflow = microflowMatches[0].Microflow;
            var targetMicroflowModule = microflowMatches[0].ModuleName;
            var variable = variableName.Trim();
            var withEvents = TryParseBool(request.QueryString["withEvents"], false);
            var refreshInClient = TryParseBool(request.QueryString["refreshInClient"], false);

            using var tx = CurrentApp.StartTransaction($"Add Commit object activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateCommitObjectActivity(CurrentApp, variable, withEvents, refreshInClient);
            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert a Commit object activity at the start of the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                variableName = variable,
                withEvents,
                refreshInClient,
                route = "microflows/commit-object",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow commit activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleChangeMicroflowAttributeAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (CurrentApp?.Root is not IProject project)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "No active Mendix app model is available."
            }, HttpStatusCode.ServiceUnavailable, cancellationToken);
        }

        var microflowName = request.QueryString["microflow"] ?? request.QueryString["name"];
        var moduleName = request.QueryString["module"];
        var entityName = request.QueryString["entity"];
        var attributeName = request.QueryString["attribute"];
        var variableName = request.QueryString["variable"];
        var value = request.QueryString["value"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(attributeName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'attribute' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(variableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'variable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (value is null)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'value' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        try
        {
            var normalizedModuleName = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName.Trim();
            var module = ResolveModule(project, normalizedModuleName);
            if (normalizedModuleName is not null && module is null)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = $"No module named '{normalizedModuleName}' was found.",
                    module = normalizedModuleName
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            var microflowMatches = ResolveMicroflows(project, module, microflowName, allowAllModules: true);
            if (microflowMatches.Length == 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = module is null
                        ? $"No matching microflow named '{microflowName}' was found."
                        : $"No matching microflow named '{microflowName}' was found in module '{module.Name}'.",
                    microflow = microflowName,
                    module = module?.Name
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            if (microflowMatches.Length > 1)
            {
                var ambiguousMicroflows = microflowMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple microflows matched the request. Include --module to disambiguate.",
                    microflow = microflowName,
                    module = normalizedModuleName,
                    matches = ambiguousMicroflows
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var targetMicroflow = microflowMatches[0].Microflow;
            var targetMicroflowModule = microflowMatches[0].ModuleName;

            var attributeResolution = ResolveAttribute(project, module, entityName, attributeName);
            if (!attributeResolution.Ok)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = attributeResolution.Error,
                    entity = entityName,
                    attribute = attributeName,
                    module = normalizedModuleName,
                    matches = attributeResolution.Candidates
                }, attributeResolution.StatusCode, cancellationToken);
            }

            if (!TryParseChangeActionItemType(request.QueryString["changeType"], out var changeType))
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Invalid change type. Use Set, Add, or Remove.",
                    changeType = request.QueryString["changeType"]
                }, HttpStatusCode.BadRequest, cancellationToken);
            }

            if (!TryParseCommitEnum(request.QueryString["commit"], out var commitMode))
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Invalid commit mode. Use Yes, YesWithoutEvents, or No.",
                    commit = request.QueryString["commit"]
                }, HttpStatusCode.BadRequest, cancellationToken);
            }

            var expression = _microflowExpressionService.CreateFromString(ToExpressionText(value));
            var variable = variableName.Trim();

            using var tx = CurrentApp.StartTransaction($"Add Change attribute activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateChangeAttributeActivity(
                CurrentApp,
                attributeResolution.Attribute!,
                changeType,
                expression,
                variable,
                commitMode);

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert a Change object activity at the start of the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                entity = attributeResolution.EntityName,
                entityModule = attributeResolution.EntityModuleName,
                attribute = attributeResolution.Attribute!.Name,
                variableName = variable,
                changeType = changeType.ToString(),
                commit = commitMode.ToString(),
                value,
                route = "microflows/change-attribute",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow change-attribute activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
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
                navigationPopulateUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/navigation/populate"),
                microflowCreateObjectUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/create-object"),
                microflowCreateListUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/create-list"),
                microflowDeleteObjectUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/delete-object"),
                microflowCommitObjectUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/commit-object"),
                microflowChangeAttributeUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/change-attribute")
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

    private static bool TryParseBool(string? raw, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (bool.TryParse(raw, out var parsedBool))
        {
            return parsedBool;
        }

        return raw switch
        {
            "1" => true,
            "0" => false,
            _ => fallback
        };
    }

    private static bool TryParseChangeActionItemType(string? raw, out ChangeActionItemType changeType)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            changeType = ChangeActionItemType.Set;
            return true;
        }

        return Enum.TryParse(raw, ignoreCase: true, out changeType);
    }

    private static AttributeResolutionResult ResolveAttribute(
        IProject project,
        IModule? moduleHint,
        string? entityName,
        string attributeInput)
    {
        var attributeText = attributeInput.Trim();
        if (attributeText.Length == 0)
        {
            return new AttributeResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.BadRequest,
                Error: "Attribute cannot be empty.",
                Attribute: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: null);
        }

        string? resolvedModuleName = null;
        string? resolvedEntityName = entityName?.Trim();
        string resolvedAttributeName = attributeText;

        var parts = attributeText.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3)
        {
            resolvedModuleName = parts[0].Trim();
            resolvedEntityName = parts[1].Trim();
            resolvedAttributeName = parts[2].Trim();
        }
        else if (parts.Length == 2 && string.IsNullOrWhiteSpace(resolvedEntityName))
        {
            resolvedEntityName = parts[0].Trim();
            resolvedAttributeName = parts[1].Trim();
        }
        else if (parts.Length > 3)
        {
            return new AttributeResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.BadRequest,
                Error: "Attribute must be in the format Attribute, Entity.Attribute, or Module.Entity.Attribute.",
                Attribute: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: null);
        }

        if (string.IsNullOrWhiteSpace(resolvedEntityName))
        {
            return new AttributeResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.BadRequest,
                Error: "Entity context is required. Use --entity or pass Module.Entity.Attribute.",
                Attribute: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: null);
        }

        IModule? attributeModuleHint = moduleHint;
        if (string.IsNullOrWhiteSpace(resolvedModuleName) is false)
        {
            attributeModuleHint = ResolveModule(project, resolvedModuleName);
            if (attributeModuleHint is null)
            {
                return new AttributeResolutionResult(
                    Ok: false,
                    StatusCode: HttpStatusCode.NotFound,
                    Error: $"No module named '{resolvedModuleName}' was found for attribute resolution.",
                    Attribute: null,
                    EntityName: null,
                    EntityModuleName: null,
                    Candidates: null);
            }
        }

        var qualifiedEntity = string.IsNullOrWhiteSpace(resolvedModuleName)
            ? resolvedEntityName
            : $"{resolvedModuleName}.{resolvedEntityName}";
        var entityMatches = ResolveEntities(project, qualifiedEntity, attributeModuleHint);
        if (entityMatches.Length == 0)
        {
            return new AttributeResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.NotFound,
                Error: $"No entity named '{resolvedEntityName}' was found for attribute resolution.",
                Attribute: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: null);
        }

        if (entityMatches.Length > 1)
        {
            var candidates = entityMatches
                .Select(match => new { name = match.Name, module = match.ModuleName })
                .ToArray();

            return new AttributeResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.Conflict,
                Error: "Multiple entities matched the request. Include --module or module-qualified attribute name.",
                Attribute: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: candidates);
        }

        var targetEntity = entityMatches[0].Entity;
        var targetAttribute = targetEntity.GetAttributes()
            .FirstOrDefault(attribute => string.Equals(attribute.Name, resolvedAttributeName, StringComparison.OrdinalIgnoreCase));

        if (targetAttribute is null)
        {
            return new AttributeResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.NotFound,
                Error: $"Entity '{targetEntity.Name}' does not define attribute '{resolvedAttributeName}'.",
                Attribute: null,
                EntityName: targetEntity.Name,
                EntityModuleName: entityMatches[0].ModuleName,
                Candidates: null);
        }

        return new AttributeResolutionResult(
            Ok: true,
            StatusCode: HttpStatusCode.OK,
            Error: null,
            Attribute: targetAttribute,
            EntityName: targetEntity.Name,
            EntityModuleName: entityMatches[0].ModuleName,
            Candidates: null);
    }

    private static bool TryParseCommitEnum(string? raw, out CommitEnum commitMode)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            commitMode = CommitEnum.No;
            return true;
        }

        if (Enum.TryParse<CommitEnum>(raw, ignoreCase: true, out commitMode))
        {
            return true;
        }

        return false;
    }

    private static (IMicroflow Microflow, string Name, string ModuleName)[] ResolveMicroflows(
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

    private static (IEntity Entity, string Name, string ModuleName)[] ResolveEntities(
        IProject project,
        string entityName,
        IModule? entityModuleHint)
    {
        var normalizedEntity = entityName.Trim();
        var parsedEntityParts = normalizedEntity.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        var entityModuleName = parsedEntityParts.Length == 2 ? parsedEntityParts[0].Trim() : null;
        var targetEntityName = parsedEntityParts.Length == 2 ? parsedEntityParts[1].Trim() : normalizedEntity;
        var candidates = new List<(IEntity Entity, string Name, string ModuleName)>();

        var modulesToSearch = entityModuleName is not null
            ? project.GetModules()
                .Where(module => string.Equals(module.Name, entityModuleName, StringComparison.OrdinalIgnoreCase))
                .ToList()
            : entityModuleHint is null
                ? project.GetModules()
                : [entityModuleHint];

        foreach (var candidateModule in modulesToSearch)
        {
            if (candidateModule.DomainModel is null)
            {
                continue;
            }

            foreach (var entity in candidateModule.DomainModel.GetEntities())
            {
                if (string.Equals(entity.Name, targetEntityName, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add((entity, entity.Name, candidateModule.Name));
                }
            }
        }

        return [.. candidates];
    }

    private static (string AttributeName, IMicroflowExpression Expression)[]? ParseInitialValues(
        string? rawInitialValues,
        IMicroflowExpressionService expressionService,
        IEntity targetEntity)
    {
        if (string.IsNullOrWhiteSpace(rawInitialValues))
        {
            return Array.Empty<(string, IMicroflowExpression)>();
        }

        try
        {
            using var document = JsonDocument.Parse(rawInitialValues);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var attributeLookup = targetEntity.GetAttributes()
                .ToDictionary(
                    attribute => attribute.Name,
                    attribute => attribute,
                    StringComparer.OrdinalIgnoreCase);

            var assignments = new List<(string AttributeName, IMicroflowExpression Expression)>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!attributeLookup.ContainsKey(property.Name))
                {
                    throw new ArgumentException($"Entity '{targetEntity.Name}' does not define attribute '{property.Name}'.");
                }

                var expressionText = property.Value.ValueKind switch
                {
                    JsonValueKind.String when !string.IsNullOrEmpty(property.Value.GetString())
                        => ToExpressionText(property.Value.GetString()!),
                    JsonValueKind.String => "\"\"",
                    _ => property.Value.GetRawText()
                };

                var expression = expressionService.CreateFromString(expressionText);
                assignments.Add((property.Name, expression));
            }

            return [.. assignments];
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string ToExpressionText(string value)
    {
        if (value.StartsWith("$", StringComparison.Ordinal)
            || value.StartsWith("[", StringComparison.Ordinal)
            || value.StartsWith("!", StringComparison.Ordinal)
            || value.StartsWith("(", StringComparison.Ordinal))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private static IModule? ResolveModule(IProject project, string? moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return null;
        }

        return project.GetModules()
            .FirstOrDefault(module => string.Equals(module.Name, moduleName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record AttributeResolutionResult(
        bool Ok,
        HttpStatusCode StatusCode,
        string? Error,
        IAttribute? Attribute,
        string? EntityName,
        string? EntityModuleName,
        object[]? Candidates);
}
