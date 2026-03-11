using System.ComponentModel.Composition;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Mendix.StudioPro.ExtensionsAPI.Model;
using Mendix.StudioPro.ExtensionsAPI.Model.DataTypes;
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
    private readonly QuickMicroflowActionDialogController _quickMicroflowActionDialogController;
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
        INavigationManagerService navigationManagerService,
        QuickMicroflowActionDialogController quickMicroflowActionDialogController)
    {
        _logService = logService;
        _extensionFileService = extensionFileService;
        _dockingWindowService = dockingWindowService;
        _microflowService = microflowService;
        _microflowActivitiesService = microflowActivitiesService;
        _microflowExpressionService = microflowExpressionService;
        _versionControlService = versionControlService;
        _navigationManagerService = navigationManagerService;
        _quickMicroflowActionDialogController = quickMicroflowActionDialogController;
        _logService.Info("[MendixStudioAutomation] WebServerExtension constructed.");
    }

    public override void InitializeWebServer(IWebServer webServer)
    {
        _logService.Info("[MendixStudioAutomation] InitializeWebServer invoked.");
        _activeDocumentSubscription ??= Subscribe<ActiveDocumentChanged>(OnActiveDocumentChanged);

        webServer.AddRoute($"{RoutePrefix}/health", HandleHealthAsync);
        webServer.AddRoute($"{RoutePrefix}/context", HandleContextAsync);
        webServer.AddRoute($"{RoutePrefix}/capabilities", HandleCapabilitiesAsync);
        webServer.AddRoute($"{RoutePrefix}/ui/quick-create-object", HandleQuickCreateObjectDialogPageAsync);
        webServer.AddRoute($"{RoutePrefix}/ui/quick-create-object/open", HandleOpenQuickCreateObjectDialogAsync);
        webServer.AddRoute($"{RoutePrefix}/documents/search", HandleSearchDocumentsAsync);
        webServer.AddRoute($"{RoutePrefix}/documents/open", HandleOpenDocumentAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/list-activities", HandleListMicroflowActivitiesAsync);
        webServer.AddRoute($"{RoutePrefix}/navigation/populate", HandlePopulateNavigationAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/create-object", HandleCreateMicroflowObjectAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/create-list", HandleCreateMicroflowListAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/call-microflow", HandleCallMicroflowActivityAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/retrieve-database", HandleRetrieveDatabaseMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/retrieve-association", HandleRetrieveAssociationMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/filter-by-association", HandleFilterByAssociationMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/find-by-association", HandleFindByAssociationMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/filter-by-attribute", HandleFilterByAttributeMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/find-by-attribute", HandleFindByAttributeMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/find-by-expression", HandleFindByExpressionMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/aggregate-list", HandleAggregateListMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/aggregate-by-attribute", HandleAggregateByAttributeMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/aggregate-by-expression", HandleAggregateByExpressionMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/change-list", HandleChangeListMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/sort-list", HandleSortListMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/reduce-aggregate", HandleReduceAggregateMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/list-head", HandleListHeadMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/list-tail", HandleListTailMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/list-contains", HandleListContainsMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/list-union", HandleListUnionMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/list-intersect", HandleListIntersectMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/list-subtract", HandleListSubtractMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/list-equals", HandleListEqualsMicroflowAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/delete-object", HandleDeleteMicroflowObjectAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/commit-object", HandleCommitMicroflowObjectAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/rollback-object", HandleRollbackMicroflowObjectAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/change-attribute", HandleChangeMicroflowAttributeAsync);
        webServer.AddRoute($"{RoutePrefix}/microflows/change-association", HandleChangeMicroflowAssociationAsync);

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

    private async Task HandleQuickCreateObjectDialogPageAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(GetQuickCreateObjectDialogHtml());
        response.StatusCode = (int)HttpStatusCode.OK;
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = body.Length;
        await response.OutputStream.WriteAsync(body, cancellationToken);
        response.OutputStream.Close();
    }

    private Task HandleOpenQuickCreateObjectDialogAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (CurrentApp is null || WebServerBaseUrl is null)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "No active app or webserver context is available."
            }, HttpStatusCode.ServiceUnavailable, cancellationToken);
        }

        var microflowName = request.QueryString["microflow"];
        var moduleName = request.QueryString["module"];
        var entityName = request.QueryString["entity"] ?? "Document.ClientDocument";
        var outputVariableName = request.QueryString["outputVariableName"] ?? "CreatedObject";

        try
        {
            _quickMicroflowActionDialogController.ShowDialog(
                CurrentApp,
                WebServerBaseUrl,
                initialMicroflowName: microflowName,
                initialModuleName: moduleName,
                initialEntityName: entityName,
                initialOutputVariableName: outputVariableName);

            return WriteJsonAsync(response, new
            {
                ok = true,
                route = "ui/quick-create-object/open",
                microflow = microflowName,
                module = moduleName,
                entity = entityName,
                outputVariableName
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to open quick create object dialog.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
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
            "microflow.listActivities",
            "ui.quickCreateObjectDialog",
            "navigation.populate",
            "microflow.createObject",
            "microflow.createList",
            "microflow.callMicroflow",
            "microflow.retrieveDatabase",
            "microflow.retrieveAssociation",
            "microflow.filterByAssociation",
            "microflow.findByAssociation",
            "microflow.filterByAttribute",
            "microflow.findByAttribute",
            "microflow.findByExpression",
            "microflow.aggregateList",
            "microflow.aggregateByAttribute",
            "microflow.aggregateByExpression",
            "microflow.changeList",
            "microflow.sortList",
            "microflow.reduceAggregate",
            "microflow.listHead",
            "microflow.listTail",
            "microflow.listContains",
            "microflow.listUnion",
            "microflow.listIntersect",
            "microflow.listSubtract",
            "microflow.listEquals",
            "microflow.deleteObject",
            "microflow.commitObject",
            "microflow.rollbackObject",
            "microflow.changeAttribute",
            "microflow.changeAssociation",
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

    private Task HandleListMicroflowActivitiesAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
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
            var activities = _microflowService.GetAllMicroflowActivities(targetMicroflow)
                .Select((activity, index) => SummarizeMicroflowActivity(activity, index))
                .ToArray();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                count = activities.Length,
                items = activities,
                route = "microflows/list-activities"
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to list microflow activities.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
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
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

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

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Create object activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
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
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
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
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

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

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Create list activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
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
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
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

    private Task HandleCallMicroflowActivityAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var calledMicroflowInput = request.QueryString["calledMicroflow"] ?? request.QueryString["call"];
        var calledModuleInput = request.QueryString["calledModule"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];
        var parameterMappingsRaw = request.QueryString["parameterMappings"] ?? request.QueryString["parameters"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(calledMicroflowInput))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'calledMicroflow' query parameter is required."
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

            var targetMicroflowMatches = ResolveMicroflows(project, module, microflowName, allowAllModules: true);
            if (targetMicroflowMatches.Length == 0)
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

            if (targetMicroflowMatches.Length > 1)
            {
                var ambiguousTargetMicroflows = targetMicroflowMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple target microflows matched the request. Include --module to disambiguate.",
                    microflow = microflowName,
                    module = normalizedModuleName,
                    matches = ambiguousTargetMicroflows
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var targetMicroflow = targetMicroflowMatches[0].Microflow;
            var targetMicroflowModule = targetMicroflowMatches[0].ModuleName;

            var calledMicroflowText = calledMicroflowInput.Trim();
            string calledMicroflowName;
            string? calledModuleName = string.IsNullOrWhiteSpace(calledModuleInput) ? null : calledModuleInput.Trim();
            var calledParts = calledMicroflowText.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
            if (calledParts.Length == 2)
            {
                calledModuleName ??= calledParts[0].Trim();
                calledMicroflowName = calledParts[1].Trim();
            }
            else
            {
                calledMicroflowName = calledMicroflowText;
            }

            var calledModule = ResolveModule(project, calledModuleName);
            if (calledModuleName is not null && calledModule is null)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = $"No module named '{calledModuleName}' was found for called microflow resolution.",
                    calledMicroflow = calledMicroflowText,
                    calledModule = calledModuleName
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            var calledMicroflowMatches = ResolveMicroflows(project, calledModule, calledMicroflowName, allowAllModules: true);
            if (calledMicroflowMatches.Length == 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = calledModule is null
                        ? $"No called microflow named '{calledMicroflowName}' was found."
                        : $"No called microflow named '{calledMicroflowName}' was found in module '{calledModule.Name}'.",
                    calledMicroflow = calledMicroflowText,
                    calledModule = calledModule?.Name
                }, HttpStatusCode.NotFound, cancellationToken);
            }

            if (calledMicroflowMatches.Length > 1)
            {
                var ambiguousCalledMicroflows = calledMicroflowMatches
                    .Select(match => new { name = match.Name, module = match.ModuleName })
                    .ToArray();

                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "Multiple called microflows matched the request. Include --called-module or qualify --called-microflow with Module.Microflow.",
                    calledMicroflow = calledMicroflowText,
                    calledModule = calledModuleName,
                    matches = ambiguousCalledMicroflows
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            var calledMicroflow = calledMicroflowMatches[0].Microflow;
            var calledMicroflowModule = calledMicroflowMatches[0].ModuleName;
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "CallResult" : outputVariableName.Trim();

            var parsedMappings = ParseCallParameterMappings(parameterMappingsRaw, _microflowExpressionService);
            if (parsedMappings is null)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "parameterMappings must be a JSON object whose values are valid Mendix expressions.",
                    raw = parameterMappingsRaw
                }, HttpStatusCode.BadRequest, cancellationToken);
            }

            var calledParameters = _microflowService.GetParameters(calledMicroflow);
            var parameterLookup = calledParameters.ToDictionary(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase);
            var unknownMappings = parsedMappings
                .Where(mapping => !parameterLookup.ContainsKey(mapping.ParameterName))
                .Select(mapping => mapping.ParameterName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (unknownMappings.Length > 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "parameterMappings contains parameters that do not exist on the called microflow.",
                    unknownParameters = unknownMappings,
                    calledMicroflow = calledMicroflow.QualifiedName
                }, HttpStatusCode.BadRequest, cancellationToken);
            }

            var missingMappings = calledParameters
                .Where(parameter => !parsedMappings.Any(mapping => string.Equals(mapping.ParameterName, parameter.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => parameter.Name)
                .ToArray();

            if (missingMappings.Length > 0)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "parameterMappings is missing required called microflow parameters.",
                    missingParameters = missingMappings,
                    calledMicroflow = calledMicroflow.QualifiedName
                }, HttpStatusCode.BadRequest, cancellationToken);
            }

            using var tx = CurrentApp.StartTransaction($"Add Call microflow activity to {targetMicroflow}");
            var microflowCallActivity = CurrentApp.Create<IActionActivity>();
            var microflowCallAction = CurrentApp.Create<IMicroflowCallAction>();
            microflowCallAction.MicroflowCall = CurrentApp.Create<IMicroflowCall>();
            microflowCallAction.MicroflowCall.Microflow = calledMicroflow.QualifiedName;
            microflowCallAction.OutputVariableName = output;

            foreach (var mapping in parsedMappings)
            {
                var parameter = parameterLookup[mapping.ParameterName];
                var parameterMapping = CurrentApp.Create<IMicroflowCallParameterMapping>();
                parameterMapping.Parameter = parameter.QualifiedName;
                parameterMapping.Argument = mapping.Expression;
                microflowCallAction.MicroflowCall.AddParameterMapping(parameterMapping);
            }

            microflowCallActivity.Action = microflowCallAction;
            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                microflowCallActivity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Call microflow activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                calledMicroflow = calledMicroflow.Name,
                calledMicroflowModule,
                calledQualifiedName = calledMicroflow.QualifiedName,
                outputVariableName = output,
                parameterMappingCount = parsedMappings.Length,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/call-microflow",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow call activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleRetrieveDatabaseMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];
        var xPathConstraint = request.QueryString["xPathConstraint"] ?? request.QueryString["xpath"] ?? request.QueryString["xPath"];
        var retrieveJustFirst = TryParseBool(request.QueryString["retrieveFirst"], false);
        var sortAttributeName = request.QueryString["sortAttribute"] ?? request.QueryString["sortBy"] ?? request.QueryString["orderByAttribute"];
        var sortDescending = TryParseBool(request.QueryString["sortDescending"] ?? request.QueryString["descending"], false);
        var rangeOffsetExpressionRaw = request.QueryString["rangeOffsetExpression"]
            ?? request.QueryString["rangeStartExpression"]
            ?? request.QueryString["offsetExpression"]
            ?? request.QueryString["startExpression"];
        var rangeAmountExpressionRaw = request.QueryString["rangeAmountExpression"]
            ?? request.QueryString["rangeLengthExpression"]
            ?? request.QueryString["limitExpression"]
            ?? request.QueryString["amountExpression"];
        var hasRangeOffsetExpression = !string.IsNullOrWhiteSpace(rangeOffsetExpressionRaw);
        var hasRangeAmountExpression = !string.IsNullOrWhiteSpace(rangeAmountExpressionRaw);

        if (hasRangeOffsetExpression ^ hasRangeAmountExpression)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "Both rangeOffsetExpression and rangeAmountExpression are required when specifying a retrieval range."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (retrieveJustFirst && hasRangeOffsetExpression)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "retrieveFirst cannot be combined with rangeOffsetExpression/rangeAmountExpression."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

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
            var defaultOutput = retrieveJustFirst ? "RetrievedObject" : "RetrievedObjects";
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? defaultOutput : outputVariableName!.Trim();
            var normalizedXpath = xPathConstraint ?? string.Empty;
            AttributeSorting[] sorting = Array.Empty<AttributeSorting>();
            string? resolvedSortAttribute = null;
            if (!string.IsNullOrWhiteSpace(sortAttributeName))
            {
                var sortAttributeResolution = ResolveAttribute(project, module, entityName, sortAttributeName);
                if (!sortAttributeResolution.Ok)
                {
                    return WriteJsonAsync(response, new
                    {
                        ok = false,
                        error = sortAttributeResolution.Error,
                        entity = entityName,
                        attribute = sortAttributeName,
                        module = normalizedModuleName,
                        matches = sortAttributeResolution.Candidates
                    }, sortAttributeResolution.StatusCode, cancellationToken);
                }

                sorting =
                [
                    new AttributeSorting(sortAttributeResolution.Attribute!, sortDescending)
                ];
                resolvedSortAttribute = sortAttributeResolution.Attribute!.Name;
            }

            using var tx = CurrentApp.StartTransaction($"Add Retrieve database activity to {targetMicroflow}");
            var activity = hasRangeOffsetExpression
                ? _microflowActivitiesService.CreateDatabaseRetrieveSourceActivity(
                    CurrentApp,
                    output,
                    targetEntity,
                    normalizedXpath,
                    (
                        _microflowExpressionService.CreateFromString(ToExpressionText(rangeOffsetExpressionRaw!)),
                        _microflowExpressionService.CreateFromString(ToExpressionText(rangeAmountExpressionRaw!))
                    ),
                    sorting)
                : _microflowActivitiesService.CreateDatabaseRetrieveSourceActivity(
                    CurrentApp,
                    output,
                    targetEntity,
                    normalizedXpath,
                    retrieveJustFirst,
                    sorting);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Retrieve activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
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
                retrieveFirst = retrieveJustFirst,
                xPathConstraint = normalizedXpath,
                sortAttribute = resolvedSortAttribute,
                sortDescending,
                rangeOffsetExpression = hasRangeOffsetExpression ? rangeOffsetExpressionRaw : null,
                rangeAmountExpression = hasRangeAmountExpression ? rangeAmountExpressionRaw : null,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/retrieve-database",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow retrieve-database activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleRetrieveAssociationMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var associationName = request.QueryString["association"];
        var entityVariable = request.QueryString["entityVariable"] ?? request.QueryString["entityVar"] ?? request.QueryString["fromVariable"];
        var outputVariableName = request.QueryString["outputVariableName"]
            ?? request.QueryString["outputVariable"]
            ?? request.QueryString["output"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(associationName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'association' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(entityVariable))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'entityVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        try
        {
            var normalizedModuleName = string.IsNullOrWhiteSpace(moduleName) ? null : moduleName!.Trim();
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
            var associationResolution = ResolveAssociation(project, module, entityName, associationName);
            if (!associationResolution.Ok)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = associationResolution.Error,
                    entity = entityName,
                    association = associationName,
                    module = normalizedModuleName,
                    matches = associationResolution.Candidates
                }, associationResolution.StatusCode, cancellationToken);
            }

            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "RetrievedByAssociation" : outputVariableName!.Trim();
            var fromVariable = entityVariable.Trim();

            using var tx = CurrentApp.StartTransaction($"Add Retrieve association activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateAssociationRetrieveSourceActivity(
                CurrentApp,
                associationResolution.Association!,
                output,
                fromVariable);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Retrieve by association activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                entity = associationResolution.EntityName,
                entityModule = associationResolution.EntityModuleName,
                association = associationResolution.Association!.Name,
                entityVariable = fromVariable,
                outputVariableName = output,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/retrieve-association",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow retrieve-association activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleFilterByAssociationMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var associationName = request.QueryString["association"];
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var filterExpressionText = request.QueryString["filterExpression"] ?? request.QueryString["expression"] ?? request.QueryString["value"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(associationName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'association' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(filterExpressionText))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'filterExpression' query parameter is required."
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
            var associationResolution = ResolveAssociation(project, module, entityName, associationName);
            if (!associationResolution.Ok)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = associationResolution.Error,
                    entity = entityName,
                    association = associationName,
                    module = normalizedModuleName,
                    matches = associationResolution.Candidates
                }, associationResolution.StatusCode, cancellationToken);
            }

            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "FilteredByAssociation" : outputVariableName.Trim();
            var expression = _microflowExpressionService.CreateFromString(ToExpressionText(filterExpressionText.Trim()));

            using var tx = CurrentApp.StartTransaction($"Add Filter by association activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateFilterListByAssociationActivity(
                CurrentApp,
                associationResolution.Association!,
                sourceList,
                output,
                expression);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Filter by association activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                entity = associationResolution.EntityName,
                entityModule = associationResolution.EntityModuleName,
                association = associationResolution.Association!.Name,
                listVariable = sourceList,
                outputVariableName = output,
                filterExpression = filterExpressionText.Trim(),
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/filter-by-association",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow filter-by-association activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleFindByAssociationMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var associationName = request.QueryString["association"];
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var findExpressionText = request.QueryString["findExpression"] ?? request.QueryString["expression"] ?? request.QueryString["value"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(associationName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'association' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(findExpressionText))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'findExpression' query parameter is required."
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
            var associationResolution = ResolveAssociation(project, module, entityName, associationName);
            if (!associationResolution.Ok)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = associationResolution.Error,
                    entity = entityName,
                    association = associationName,
                    module = normalizedModuleName,
                    matches = associationResolution.Candidates
                }, associationResolution.StatusCode, cancellationToken);
            }

            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "FoundByAssociation" : outputVariableName.Trim();
            var expression = _microflowExpressionService.CreateFromString(ToExpressionText(findExpressionText.Trim()));

            using var tx = CurrentApp.StartTransaction($"Add Find by association activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateFindByAssociationActivity(
                CurrentApp,
                associationResolution.Association!,
                sourceList,
                output,
                expression);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Find by association activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                entity = associationResolution.EntityName,
                entityModule = associationResolution.EntityModuleName,
                association = associationResolution.Association!.Name,
                listVariable = sourceList,
                outputVariableName = output,
                findExpression = findExpressionText.Trim(),
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/find-by-association",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow find-by-association activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleFilterByAttributeMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var filterExpressionText = request.QueryString["filterExpression"] ?? request.QueryString["expression"] ?? request.QueryString["value"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

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

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(filterExpressionText))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'filterExpression' query parameter is required."
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

            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "FilteredByAttribute" : outputVariableName.Trim();
            var expression = _microflowExpressionService.CreateFromString(ToExpressionText(filterExpressionText.Trim()));

            using var tx = CurrentApp.StartTransaction($"Add Filter by attribute activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateFilterListByAttributeActivity(
                CurrentApp,
                attributeResolution.Attribute!,
                sourceList,
                output,
                expression);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Filter by attribute activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
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
                listVariable = sourceList,
                outputVariableName = output,
                filterExpression = filterExpressionText.Trim(),
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/filter-by-attribute",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow filter-by-attribute activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleFindByAttributeMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var findExpressionText = request.QueryString["findExpression"] ?? request.QueryString["expression"] ?? request.QueryString["value"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

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

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(findExpressionText))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'findExpression' query parameter is required."
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

            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "FoundByAttribute" : outputVariableName.Trim();
            var expression = _microflowExpressionService.CreateFromString(ToExpressionText(findExpressionText.Trim()));

            using var tx = CurrentApp.StartTransaction($"Add Find by attribute activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateFindByAttributeActivity(
                CurrentApp,
                attributeResolution.Attribute!,
                sourceList,
                output,
                expression);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Find by attribute activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
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
                listVariable = sourceList,
                outputVariableName = output,
                findExpression = findExpressionText.Trim(),
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/find-by-attribute",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow find-by-attribute activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleFindByExpressionMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];
        var findExpressionText = request.QueryString["findExpression"] ?? request.QueryString["expression"] ?? request.QueryString["value"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(findExpressionText))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'findExpression' query parameter is required."
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
            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "FoundByExpression" : outputVariableName.Trim();
            var expression = _microflowExpressionService.CreateFromString(ToExpressionText(findExpressionText.Trim()));

            using var tx = CurrentApp.StartTransaction($"Add Find by expression activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateFindByExpressionActivity(
                CurrentApp,
                sourceList,
                output,
                expression);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Find by expression activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                listVariable = sourceList,
                outputVariableName = output,
                findExpression = findExpressionText.Trim(),
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/find-by-expression",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow find-by-expression activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleAggregateListMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];

        if (!TryParseAggregateFunctionEnum(request.QueryString["aggregateFunction"] ?? request.QueryString["function"], out var aggregateFunction))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"The 'aggregateFunction' query parameter is invalid. Allowed values: {string.Join(", ", Enum.GetNames<AggregateFunctionEnum>())}."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
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
            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "AggregatedValue" : outputVariableName.Trim();

            using var tx = CurrentApp.StartTransaction($"Add Aggregate list activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateAggregateListActivity(
                CurrentApp,
                sourceList,
                output,
                aggregateFunction);

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert an Aggregate list activity at the start of the microflow.",
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
                listVariable = sourceList,
                outputVariableName = output,
                aggregateFunction = aggregateFunction.ToString(),
                route = "microflows/aggregate-list",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow aggregate-list activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleAggregateByAttributeMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];

        if (!TryParseAggregateFunctionEnum(request.QueryString["aggregateFunction"] ?? request.QueryString["function"], out var aggregateFunction))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"The 'aggregateFunction' query parameter is invalid. Allowed values: {string.Join(", ", Enum.GetNames<AggregateFunctionEnum>())}."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

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

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
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

            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "AggregatedAttributeValue" : outputVariableName.Trim();

            using var tx = CurrentApp.StartTransaction($"Add Aggregate by attribute activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateAggregateListByAttributeActivity(
                CurrentApp,
                attributeResolution.Attribute!,
                sourceList,
                output,
                aggregateFunction);

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert an Aggregate by attribute activity at the start of the microflow.",
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
                listVariable = sourceList,
                outputVariableName = output,
                aggregateFunction = aggregateFunction.ToString(),
                route = "microflows/aggregate-by-attribute",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow aggregate-by-attribute activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleAggregateByExpressionMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var aggregateExpressionText = request.QueryString["aggregateExpression"] ?? request.QueryString["expression"] ?? request.QueryString["value"];

        if (!TryParseAggregateFunctionEnum(request.QueryString["aggregateFunction"] ?? request.QueryString["function"], out var aggregateFunction))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"The 'aggregateFunction' query parameter is invalid. Allowed values: {string.Join(", ", Enum.GetNames<AggregateFunctionEnum>())}."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(aggregateExpressionText))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'aggregateExpression' query parameter is required."
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
            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "AggregatedExpressionValue" : outputVariableName.Trim();
            var aggregateExpression = _microflowExpressionService.CreateFromString(ToExpressionText(aggregateExpressionText.Trim()));

            using var tx = CurrentApp.StartTransaction($"Add Aggregate by expression activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateAggregateListByExpressionActivity(
                CurrentApp,
                aggregateExpression,
                sourceList,
                output,
                aggregateFunction);

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert an Aggregate by expression activity at the start of the microflow.",
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
                listVariable = sourceList,
                outputVariableName = output,
                aggregateExpression = aggregateExpressionText.Trim(),
                aggregateFunction = aggregateFunction.ToString(),
                route = "microflows/aggregate-by-expression",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow aggregate-by-expression activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleChangeListMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var rawOperation = request.QueryString["changeListOperation"] ?? request.QueryString["operation"] ?? request.QueryString["changeType"];
        var rawValueExpression = request.QueryString["value"] ?? request.QueryString["expression"] ?? request.QueryString["itemExpression"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];

        if (!TryParseChangeListActionOperation(rawOperation, out var changeListOperation))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"The 'changeListOperation' query parameter is invalid. Allowed values: {string.Join(", ", Enum.GetNames<ChangeListActionOperation>())}."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        var expressionInput = string.IsNullOrWhiteSpace(rawValueExpression)
            ? (changeListOperation == ChangeListActionOperation.Clear ? "empty" : null)
            : rawValueExpression.Trim();
        if (string.IsNullOrWhiteSpace(expressionInput))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'value' (or 'expression') query parameter is required unless changeListOperation is Clear."
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
            var listVariable = listVariableName.Trim();
            var expressionText = string.Equals(expressionInput, "empty", StringComparison.OrdinalIgnoreCase)
                ? "empty"
                : ToExpressionText(expressionInput);
            var expression = _microflowExpressionService.CreateFromString(expressionText);

            using var tx = CurrentApp.StartTransaction($"Add Change list activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateChangeListActivity(
                CurrentApp,
                changeListOperation,
                listVariable,
                expression);

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert a Change list activity at the start of the microflow.",
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
                listVariable,
                changeListOperation = changeListOperation.ToString(),
                expression = expressionInput,
                route = "microflows/change-list",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow change-list activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleSortListMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var sortDescending = TryParseBool(request.QueryString["sortDescending"] ?? request.QueryString["descending"], false);

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

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
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

            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "SortedList" : outputVariableName.Trim();
            var sorting = new AttributeSorting(attributeResolution.Attribute!, sortDescending);

            using var tx = CurrentApp.StartTransaction($"Add Sort list activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateSortListActivity(
                CurrentApp,
                sourceList,
                output,
                [sorting]);

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert a Sort list activity at the start of the microflow.",
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
                listVariable = sourceList,
                outputVariableName = output,
                sortDescending,
                route = "microflows/sort-list",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow sort-list activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleReduceAggregateMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var aggregateExpressionText = request.QueryString["aggregateExpression"] ?? request.QueryString["expression"] ?? request.QueryString["value"];
        var initialExpressionText = request.QueryString["initialExpression"] ?? request.QueryString["initialValue"] ?? request.QueryString["initial"];
        var reduceTypeText = request.QueryString["reduceType"] ?? request.QueryString["dataType"] ?? request.QueryString["type"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(aggregateExpressionText))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'aggregateExpression' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(initialExpressionText))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'initialExpression' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (!TryParseReduceDataType(reduceTypeText, out var reduceDataType))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'reduceType' query parameter is invalid. Allowed values: String, Integer, Decimal, Float, Boolean, DateTime."
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
            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "ReducedAggregate" : outputVariableName.Trim();
            var aggregateExpression = _microflowExpressionService.CreateFromString(ToExpressionText(aggregateExpressionText.Trim()));
            var initialExpression = _microflowExpressionService.CreateFromString(ToExpressionText(initialExpressionText.Trim()));

            using var tx = CurrentApp.StartTransaction($"Add Reduce aggregate activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateReduceAggregateActivity(
                CurrentApp,
                sourceList,
                output,
                aggregateExpression,
                initialExpression,
                reduceDataType);

            var inserted = _microflowService.TryInsertAfterStart(targetMicroflow, [activity]);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = "The API could not insert a Reduce aggregate activity at the start of the microflow.",
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
                listVariable = sourceList,
                outputVariableName = output,
                aggregateExpression = aggregateExpressionText.Trim(),
                initialExpression = initialExpressionText.Trim(),
                reduceType = reduceTypeText?.Trim() ?? "Decimal",
                route = "microflows/reduce-aggregate",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow reduce-aggregate activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleListHeadMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
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
            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "ListHead" : outputVariableName.Trim();
            var operation = CurrentApp.Create<IHead>();

            using var tx = CurrentApp.StartTransaction($"Add List head activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateListOperationActivity(
                CurrentApp,
                sourceList,
                output,
                operation);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a List head activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                listVariable = sourceList,
                outputVariableName = output,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/list-head",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow list-head activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleListTailMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
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
            var sourceList = listVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "ListTail" : outputVariableName.Trim();
            var operation = CurrentApp.Create<ITail>();

            using var tx = CurrentApp.StartTransaction($"Add List tail activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateListOperationActivity(
                CurrentApp,
                sourceList,
                output,
                operation);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a List tail activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                listVariable = sourceList,
                outputVariableName = output,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/list-tail",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow list-tail activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleListContainsMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var objectVariableName = request.QueryString["objectVariable"] ?? request.QueryString["value"] ?? request.QueryString["itemVariable"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(objectVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'objectVariable' query parameter is required."
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
            var sourceList = listVariableName.Trim();
            var objectVariable = objectVariableName.Trim();
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? "ListContainsResult" : outputVariableName.Trim();
            var operation = CurrentApp.Create<IContains>();
            var binaryOperation = (IBinaryListOperation)operation;
            binaryOperation.SecondListOrObjectVariableName = objectVariable;

            using var tx = CurrentApp.StartTransaction($"Add List contains activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateListOperationActivity(
                CurrentApp,
                sourceList,
                output,
                operation);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a List contains activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                listVariable = sourceList,
                objectVariable,
                outputVariableName = output,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/list-contains",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow list-contains activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleListUnionMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
        => HandleBinaryListOperationMicroflowAsync(request, response, cancellationToken, "union");

    private Task HandleListIntersectMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
        => HandleBinaryListOperationMicroflowAsync(request, response, cancellationToken, "intersect");

    private Task HandleListSubtractMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
        => HandleBinaryListOperationMicroflowAsync(request, response, cancellationToken, "subtract");

    private Task HandleListEqualsMicroflowAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
        => HandleBinaryListOperationMicroflowAsync(request, response, cancellationToken, "equals");

    private Task HandleBinaryListOperationMicroflowAsync(
        HttpListenerRequest request,
        HttpListenerResponse response,
        CancellationToken cancellationToken,
        string operationKind)
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
        var listVariableName = request.QueryString["listVariable"] ?? request.QueryString["list"] ?? request.QueryString["sourceList"];
        var secondVariableName = request.QueryString["otherListVariable"]
            ?? request.QueryString["secondListVariable"]
            ?? request.QueryString["secondVariable"]
            ?? request.QueryString["objectVariable"]
            ?? request.QueryString["value"]
            ?? request.QueryString["itemVariable"];
        var outputVariableName = request.QueryString["outputVariableName"] ?? request.QueryString["outputVariable"] ?? request.QueryString["output"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

        var routeSuffix = operationKind switch
        {
            "union" => "list-union",
            "intersect" => "list-intersect",
            "subtract" => "list-subtract",
            "equals" => "list-equals",
            _ => null
        };

        if (routeSuffix is null)
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = $"Unsupported binary list operation kind '{operationKind}'."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(listVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'listVariable' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(secondVariableName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "A second variable is required (otherListVariable / secondListVariable / objectVariable)."
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
            var sourceList = listVariableName.Trim();
            var secondVariable = secondVariableName.Trim();
            var defaultOutput = operationKind == "equals" ? "ListEqualsResult" : "ListBinaryResult";
            var output = string.IsNullOrWhiteSpace(outputVariableName) ? defaultOutput : outputVariableName.Trim();

            var operation = operationKind switch
            {
                "union" => (IListOperation)CurrentApp.Create<IUnion>(),
                "intersect" => CurrentApp.Create<IIntersect>(),
                "subtract" => CurrentApp.Create<ISubtract>(),
                "equals" => CurrentApp.Create<IListEquals>(),
                _ => throw new InvalidOperationException($"Unsupported binary list operation kind '{operationKind}'.")
            };

            var binaryOperation = (IBinaryListOperation)operation;
            binaryOperation.SecondListOrObjectVariableName = secondVariable;

            using var tx = CurrentApp.StartTransaction($"Add {routeSuffix} activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateListOperationActivity(
                CurrentApp,
                sourceList,
                output,
                operation);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? $"The API could not insert a {routeSuffix} activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                listVariable = sourceList,
                secondVariable,
                outputVariableName = output,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = $"microflows/{routeSuffix}",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error($"Failed to create microflow {routeSuffix} activity.", ex);
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
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

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
            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Delete object activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                variableName = variable,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
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
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

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
            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Commit object activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
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
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
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

    private Task HandleRollbackMicroflowObjectAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

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
            var refreshInClient = TryParseBool(request.QueryString["refreshInClient"], false);

            using var tx = CurrentApp.StartTransaction($"Add Rollback object activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateRollbackObjectActivity(CurrentApp, variable, refreshInClient);
            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Rollback object activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                variableName = variable,
                refreshInClient,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/rollback-object",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow rollback activity.", ex);
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = ex.Message
            }, HttpStatusCode.InternalServerError, cancellationToken);
        }
    }

    private Task HandleChangeMicroflowAssociationAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
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
        var associationName = request.QueryString["association"];
        var variableName = request.QueryString["variable"];
        var value = request.QueryString["value"];
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

        if (string.IsNullOrWhiteSpace(microflowName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'microflow' query parameter is required."
            }, HttpStatusCode.BadRequest, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(associationName))
        {
            return WriteJsonAsync(response, new
            {
                ok = false,
                error = "The 'association' query parameter is required."
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
            var associationResolution = ResolveAssociation(project, module, entityName, associationName);
            if (!associationResolution.Ok)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = associationResolution.Error,
                    entity = entityName,
                    association = associationName,
                    module = normalizedModuleName,
                    matches = associationResolution.Candidates
                }, associationResolution.StatusCode, cancellationToken);
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

            using var tx = CurrentApp.StartTransaction($"Add Change association activity to {targetMicroflow}");
            var activity = _microflowActivitiesService.CreateChangeAssociationActivity(
                CurrentApp,
                associationResolution.Association!,
                changeType,
                expression,
                variable,
                commitMode);

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Change association activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
                }, HttpStatusCode.Conflict, cancellationToken);
            }

            tx.Commit();

            return WriteJsonAsync(response, new
            {
                ok = true,
                microflow = microflowName,
                microflowModule = targetMicroflowModule,
                entity = associationResolution.EntityName,
                entityModule = associationResolution.EntityModuleName,
                association = associationResolution.Association!.Name,
                variableName = variable,
                changeType = changeType.ToString(),
                commit = commitMode.ToString(),
                value,
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
                route = "microflows/change-association",
                inserted
            }, HttpStatusCode.OK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logService.Error("Failed to create microflow change-association activity.", ex);
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
        var insertBeforeActivity = request.QueryString["insertBeforeActivity"]
            ?? request.QueryString["insertBefore"]
            ?? request.QueryString["beforeActivity"]
            ?? request.QueryString["beforeCaption"];
        var insertBeforeIndex = request.QueryString["insertBeforeIndex"] ?? request.QueryString["beforeIndex"];

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

            var inserted = TryInsertMicroflowActivity(
                targetMicroflow,
                activity,
                insertBeforeActivity,
                insertBeforeIndex,
                out var insertionMode,
                out var insertedBeforeCaption,
                out var insertedBeforeActionType,
                out var insertionError);
            if (!inserted)
            {
                return WriteJsonAsync(response, new
                {
                    ok = false,
                    error = insertionError ?? "The API could not insert a Change object activity into the microflow.",
                    microflow = microflowName,
                    module = targetMicroflowModule,
                    insertBeforeActivity,
                    insertBeforeIndex
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
                insertionMode,
                insertBeforeActivity,
                insertBeforeIndex,
                insertedBeforeCaption,
                insertedBeforeActionType,
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
                quickCreateObjectDialogUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/ui/quick-create-object"),
                quickCreateObjectDialogOpenUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/ui/quick-create-object/open"),
                microflowListActivitiesUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/list-activities"),
                navigationPopulateUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/navigation/populate"),
                microflowCreateObjectUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/create-object"),
                microflowCreateListUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/create-list"),
                microflowCallMicroflowUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/call-microflow"),
                microflowRetrieveDatabaseUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/retrieve-database"),
                microflowRetrieveAssociationUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/retrieve-association"),
                microflowFilterByAssociationUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/filter-by-association"),
                microflowFindByAssociationUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/find-by-association"),
                microflowFilterByAttributeUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/filter-by-attribute"),
                microflowFindByAttributeUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/find-by-attribute"),
                microflowFindByExpressionUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/find-by-expression"),
                microflowAggregateListUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/aggregate-list"),
                microflowAggregateByAttributeUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/aggregate-by-attribute"),
                microflowAggregateByExpressionUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/aggregate-by-expression"),
                microflowChangeListUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/change-list"),
                microflowSortListUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/sort-list"),
                microflowReduceAggregateUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/reduce-aggregate"),
                microflowListHeadUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/list-head"),
                microflowListTailUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/list-tail"),
                microflowListContainsUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/list-contains"),
                microflowListUnionUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/list-union"),
                microflowListIntersectUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/list-intersect"),
                microflowListSubtractUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/list-subtract"),
                microflowListEqualsUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/list-equals"),
                microflowDeleteObjectUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/delete-object"),
                microflowCommitObjectUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/commit-object"),
                microflowRollbackObjectUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/rollback-object"),
                microflowChangeAttributeUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/change-attribute"),
                microflowChangeAssociationUrl = CombineUrl(WebServerBaseUrl, $"{RoutePrefix}/microflows/change-association")
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

    private static string GetQuickCreateObjectDialogHtml()
    {
        return """
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <title>Quick Create Object</title>
  <style>
    body { font-family: Segoe UI, sans-serif; margin: 12px; }
    label { display: block; font-size: 12px; margin: 8px 0 4px 0; color: #444; }
    input, select { width: 100%; box-sizing: border-box; padding: 6px; }
    .actions { margin-top: 14px; display: flex; gap: 8px; justify-content: flex-end; }
    button { padding: 6px 10px; }
    .hint { font-size: 12px; color: #666; margin-top: 6px; }
  </style>
  <script>
    function sendToHost(payload) {
      const json = JSON.stringify(payload);
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(json);
      } else if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.studioPro) {
        window.webkit.messageHandlers.studioPro.postMessage(json);
      }
    }

    function readInitialValue(name, fallbackValue) {
      const params = new URLSearchParams(window.location.search);
      const value = params.get(name);
      return value && value.trim().length > 0 ? value : fallbackValue;
    }

    function init() {
      document.getElementById("microflow").value = readInitialValue("microflow", "");
      document.getElementById("module").value = readInitialValue("module", "");
      document.getElementById("entity").value = readInitialValue("entity", "Document.ClientDocument");
      document.getElementById("outputVariableName").value = readInitialValue("outputVariableName", "CreatedObject");
      document.getElementById("commit").value = readInitialValue("commit", "No");
    }

    function createObject() {
      sendToHost({
        action: "createObject",
        microflow: document.getElementById("microflow").value,
        module: document.getElementById("module").value,
        entity: document.getElementById("entity").value,
        outputVariableName: document.getElementById("outputVariableName").value,
        commit: document.getElementById("commit").value,
        refreshInClient: document.getElementById("refreshInClient").checked
      });
    }

    function cancelDialog() {
      sendToHost({ action: "cancel" });
    }
  </script>
</head>
<body onload="init()">
  <label for="microflow">Microflow name</label>
  <input id="microflow" type="text" />

  <label for="module">Module name (optional, for disambiguation)</label>
  <input id="module" type="text" />

  <label for="entity">Entity (Entity or Module.Entity)</label>
  <input id="entity" type="text" />

  <label for="outputVariableName">Output variable name</label>
  <input id="outputVariableName" type="text" />

  <label for="commit">Commit mode</label>
  <select id="commit">
    <option value="No" selected>No</option>
    <option value="Yes">Yes</option>
    <option value="YesWithoutEvents">YesWithoutEvents</option>
  </select>

  <label>
    <input id="refreshInClient" type="checkbox" />
    Refresh in client
  </label>

  <div class="hint">This inserts a Create object activity at the start of the selected microflow.</div>

  <div class="actions">
    <button onclick="cancelDialog()">Cancel</button>
    <button onclick="createObject()">Insert Create Object</button>
  </div>
</body>
</html>
""";
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

    private bool TryInsertMicroflowActivity(
        IMicroflow microflow,
        IActivity activity,
        string? insertBeforeActivity,
        string? insertBeforeIndexRaw,
        out string insertionMode,
        out string? insertedBeforeCaption,
        out string? insertedBeforeActionType,
        out string? error)
    {
        insertedBeforeCaption = null;
        insertedBeforeActionType = null;
        error = null;
        if (!string.IsNullOrWhiteSpace(insertBeforeIndexRaw))
        {
            if (!int.TryParse(insertBeforeIndexRaw, out var index) || index < 0)
            {
                insertionMode = "before-index";
                error = $"Invalid insert-before index '{insertBeforeIndexRaw}'.";
                return false;
            }

            var activities = _microflowService.GetAllMicroflowActivities(microflow).ToArray();
            if (index >= activities.Length)
            {
                insertionMode = "before-index";
                error = $"insert-before index '{index}' is out of range for this microflow ({activities.Length} activities).";
                return false;
            }

            var targetActivityByIndex = activities[index];
            if (targetActivityByIndex is IActionActivity targetActionActivityByIndex)
            {
                insertedBeforeCaption = targetActionActivityByIndex.Caption;
                insertedBeforeActionType = targetActionActivityByIndex.Action?.GetType().Name;
            }
            else
            {
                insertedBeforeCaption = null;
                insertedBeforeActionType = targetActivityByIndex.GetType().Name;
            }

            insertionMode = "before-index";
            var insertedByIndex = _microflowService.TryInsertBeforeActivity(targetActivityByIndex, [activity]);
            if (!insertedByIndex)
            {
                error = "The selected insert-before index does not point to an activity with exactly one incoming sequence flow.";
            }

            return insertedByIndex;
        }

        if (string.IsNullOrWhiteSpace(insertBeforeActivity))
        {
            insertionMode = "after-start";
            var insertedAfterStart = _microflowService.TryInsertAfterStart(microflow, [activity]);
            if (!insertedAfterStart)
            {
                error = "The API could not insert the activity at the start of the microflow.";
            }

            return insertedAfterStart;
        }

        var targetSelector = insertBeforeActivity.Trim();
        var targetActivity = _microflowService.GetAllMicroflowActivities(microflow)
            .OfType<IActionActivity>()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Caption, targetSelector, StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.Action?.GetType().Name, targetSelector, StringComparison.OrdinalIgnoreCase));

        if (targetActivity is null)
        {
            insertionMode = "before-activity";
            error = $"No microflow activity matched '{targetSelector}' by caption or action type.";
            return false;
        }

        insertionMode = "before-activity";
        insertedBeforeCaption = targetActivity.Caption;
        insertedBeforeActionType = targetActivity.Action?.GetType().Name;
        var insertedBefore = _microflowService.TryInsertBeforeActivity(targetActivity, [activity]);
        if (!insertedBefore)
        {
            error = "The selected insert-before activity does not have exactly one incoming sequence flow.";
        }

        return insertedBefore;
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

    private static object SummarizeMicroflowActivity(IActivity activity, int index)
    {
        var actionActivity = activity as IActionActivity;
        var action = actionActivity?.Action;
        var variables = new List<string>();
        var listOperationType = action is IListOperationAction listOperationAction
            ? listOperationAction.Operation?.GetType().Name
            : null;
        var secondListOrObjectVariableName = action is IListOperationAction listOperationActionWithBinary
            && listOperationActionWithBinary.Operation is IBinaryListOperation binaryListOperation
                ? binaryListOperation.SecondListOrObjectVariableName
                : null;

        static void AddIfPresent(List<string> target, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value.Trim());
            }
        }

        if (action is ICreateObjectAction createObjectAction)
        {
            AddIfPresent(variables, createObjectAction.OutputVariableName);
        }

        if (action is ICreateListAction createListAction)
        {
            AddIfPresent(variables, createListAction.OutputVariableName);
        }

        if (action is IRetrieveAction retrieveAction)
        {
            AddIfPresent(variables, retrieveAction.OutputVariableName);
        }

        if (action is IAggregateListAction aggregateListAction)
        {
            AddIfPresent(variables, aggregateListAction.InputListVariableName);
            AddIfPresent(variables, aggregateListAction.OutputVariableName);
        }

        if (action is IChangeObjectAction changeObjectAction)
        {
            AddIfPresent(variables, changeObjectAction.ChangeVariableName);
        }

        if (action is IChangeListAction changeListAction)
        {
            AddIfPresent(variables, changeListAction.ChangeVariableName);
        }

        if (action is ICommitAction commitAction)
        {
            AddIfPresent(variables, commitAction.CommitVariableName);
        }

        if (action is IDeleteAction deleteAction)
        {
            AddIfPresent(variables, deleteAction.DeleteVariableName);
        }

        if (action is IRollbackAction rollbackAction)
        {
            AddIfPresent(variables, rollbackAction.RollbackVariableName);
        }

        if (action is IListOperationAction operationAction)
        {
            AddIfPresent(variables, operationAction.OutputVariableName);
        }

        if (action is IMicroflowCallAction microflowCallAction)
        {
            AddIfPresent(variables, microflowCallAction.OutputVariableName);
        }

        AddIfPresent(variables, secondListOrObjectVariableName);

        var uniqueVariables = variables
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new
        {
            index,
            activityType = activity.GetType().Name,
            caption = actionActivity?.Caption,
            disabled = actionActivity?.Disabled,
            actionType = action?.GetType().Name,
            listOperationType,
            secondListOrObjectVariableName,
            variables = uniqueVariables
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

    private static AssociationResolutionResult ResolveAssociation(
        IProject project,
        IModule? moduleHint,
        string? entityName,
        string associationInput)
    {
        var associationText = associationInput.Trim();
        if (associationText.Length == 0)
        {
            return new AssociationResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.BadRequest,
                Error: "Association cannot be empty.",
                Association: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: null);
        }

        string? parsedModuleName = null;
        string? parsedEntityName = entityName?.Trim();
        string targetAssociationName = associationText;

        var parts = associationText.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3)
        {
            parsedModuleName = parts[0].Trim();
            parsedEntityName = parts[1].Trim();
            targetAssociationName = parts[2].Trim();
        }
        else if (parts.Length == 2)
        {
            if (string.IsNullOrWhiteSpace(parsedEntityName))
            {
                parsedModuleName = parts[0].Trim();
                targetAssociationName = parts[1].Trim();
            }
            else
            {
                var left = parts[0].Trim();
                var right = parts[1].Trim();
                if (string.Equals(left, parsedEntityName, StringComparison.OrdinalIgnoreCase))
                {
                    parsedEntityName = left;
                    targetAssociationName = right;
                }
                else
                {
                    parsedModuleName = left;
                    targetAssociationName = right;
                }
            }
        }
        else if (parts.Length > 3)
        {
            return new AssociationResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.BadRequest,
                Error: "Association must be in the format Association, Module.Association, Entity.Association, or Module.Entity.Association.",
                Association: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: null);
        }

        List<IModule> modulesToSearch;
        if (string.IsNullOrWhiteSpace(parsedModuleName))
        {
            modulesToSearch = moduleHint is null
                ? project.GetModules().ToList()
                : [moduleHint];
        }
        else
        {
            modulesToSearch = project.GetModules()
                .Where(module => string.Equals(module.Name, parsedModuleName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (modulesToSearch.Count == 0)
        {
            return new AssociationResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.NotFound,
                Error: $"No module named '{parsedModuleName}' was found for association resolution.",
                Association: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: null);
        }

        var candidates = new List<(IAssociation Association, string EntityName, string ModuleName)>();
        foreach (var candidateModule in modulesToSearch)
        {
            if (candidateModule.DomainModel is null)
            {
                continue;
            }

            foreach (var entity in candidateModule.DomainModel.GetEntities())
            {
                if (string.IsNullOrWhiteSpace(parsedEntityName) is false
                    && !string.Equals(entity.Name, parsedEntityName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var entityAssociation in entity.GetAssociations(AssociationDirection.Both, null))
                {
                    var association = entityAssociation.Association;
                    if (string.Equals(association.Name, targetAssociationName, StringComparison.OrdinalIgnoreCase))
                    {
                        candidates.Add((association, entity.Name, candidateModule.Name));
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            return new AssociationResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.NotFound,
                Error: $"No association named '{targetAssociationName}' was found.",
                Association: null,
                EntityName: parsedEntityName,
                EntityModuleName: parsedModuleName,
                Candidates: null);
        }

        if (candidates.Count > 1)
        {
            var ambiguous = candidates
                .Select(candidate => new { association = candidate.Association.Name, entity = candidate.EntityName, module = candidate.ModuleName })
                .ToArray();

            return new AssociationResolutionResult(
                Ok: false,
                StatusCode: HttpStatusCode.Conflict,
                Error: "Multiple associations matched the request. Include --module and/or --entity to disambiguate.",
                Association: null,
                EntityName: null,
                EntityModuleName: null,
                Candidates: ambiguous);
        }

        return new AssociationResolutionResult(
            Ok: true,
            StatusCode: HttpStatusCode.OK,
            Error: null,
            Association: candidates[0].Association,
            EntityName: candidates[0].EntityName,
            EntityModuleName: candidates[0].ModuleName,
            Candidates: null);
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

    private static bool TryParseAggregateFunctionEnum(string? raw, out AggregateFunctionEnum aggregateFunction)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            aggregateFunction = AggregateFunctionEnum.Count;
            return true;
        }

        if (Enum.TryParse<AggregateFunctionEnum>(raw, ignoreCase: true, out aggregateFunction))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseChangeListActionOperation(string? raw, out ChangeListActionOperation operation)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            operation = ChangeListActionOperation.Add;
            return true;
        }

        if (Enum.TryParse<ChangeListActionOperation>(raw, ignoreCase: true, out operation))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseReduceDataType(string? raw, out DataType dataType)
    {
        var normalized = string.IsNullOrWhiteSpace(raw) ? "Decimal" : raw.Trim();
        switch (normalized.ToLowerInvariant())
        {
            case "string":
                dataType = DataType.String;
                return true;
            case "integer":
            case "int":
                dataType = DataType.Integer;
                return true;
            case "decimal":
                dataType = DataType.Decimal;
                return true;
            case "float":
                dataType = DataType.Float;
                return true;
            case "boolean":
            case "bool":
                dataType = DataType.Boolean;
                return true;
            case "datetime":
                dataType = DataType.DateTime;
                return true;
            default:
                dataType = DataType.Decimal;
                return false;
        }
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

    private static (string ParameterName, IMicroflowExpression Expression)[]? ParseCallParameterMappings(
        string? rawParameterMappings,
        IMicroflowExpressionService expressionService)
    {
        if (string.IsNullOrWhiteSpace(rawParameterMappings))
        {
            return Array.Empty<(string, IMicroflowExpression)>();
        }

        try
        {
            using var document = JsonDocument.Parse(rawParameterMappings);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var mappings = new List<(string ParameterName, IMicroflowExpression Expression)>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(property.Name))
                {
                    return null;
                }

                var expressionText = property.Value.ValueKind switch
                {
                    JsonValueKind.String when !string.IsNullOrWhiteSpace(property.Value.GetString())
                        => ToExpressionText(property.Value.GetString()!),
                    JsonValueKind.String => "\"\"",
                    _ => property.Value.GetRawText()
                };

                var expression = expressionService.CreateFromString(expressionText);
                mappings.Add((property.Name, expression));
            }

            return [.. mappings];
        }
        catch
        {
            return null;
        }
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
        if (bool.TryParse(value, out _))
        {
            return value.ToLowerInvariant();
        }

        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "empty", StringComparison.OrdinalIgnoreCase))
        {
            return value.ToLowerInvariant();
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return value;
        }

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

    private sealed record AssociationResolutionResult(
        bool Ok,
        HttpStatusCode StatusCode,
        string? Error,
        IAssociation? Association,
        string? EntityName,
        string? EntityModuleName,
        object[]? Candidates);
}
