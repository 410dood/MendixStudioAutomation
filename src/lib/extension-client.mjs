import { access, readFile, readdir, stat } from "node:fs/promises";
import { constants as fsConstants } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const defaultEndpointFile = path.resolve(
    __dirname,
    "..",
    "..",
    "extensions",
    "MendixStudioAutomation_Extension",
    "runtime",
    "endpoint.json"
);
const installMetadataFile = path.resolve(
    __dirname,
    "..",
    "..",
    ".automation-state",
    "hybrid-extension-install.json"
);

export class HybridExtensionClient {
    async getStatus(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: true,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        try {
            const health = await fetchJson(discovery.endpoints.healthUrl, options.timeoutMs);
            return {
                ok: true,
                available: true,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                endpoints: discovery.endpoints,
                health
            };
        }
        catch (error) {
            return {
                ok: true,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                endpoints: discovery.endpoints,
                reason: error instanceof Error ? error.message : String(error)
            };
        }
    }

    async getContext(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const context = await fetchJson(discovery.endpoints.contextUrl, options.timeoutMs);
        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            context
        };
    }

    async getCapabilities(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const capabilities = await fetchJson(discovery.endpoints.capabilitiesUrl, options.timeoutMs);
        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            capabilities
        };
    }

    async searchDocuments(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const documents = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "documents/search", {
            query: options.query ?? options.q,
            module: options.module,
            type: options.type,
            limit: options.limit
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            documents
        };
    }

    async openDocument(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "documents/open", {
            name: options.name,
            module: options.module,
            type: options.type
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async openQuickCreateObjectDialog(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "ui/quick-create-object/open", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            outputVariableName: options.outputVariableName ?? options.outputVariable
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowDeleteObject(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/delete-object", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            variable: options.variable
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowCommitObject(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/commit-object", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            variable: options.variable,
            withEvents: options.withEvents,
            refreshInClient: options.refreshInClient
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowRollbackObject(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/rollback-object", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            variable: options.variable,
            refreshInClient: options.refreshInClient
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowChangeAttribute(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/change-attribute", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            attribute: options.attribute,
            variable: options.variable,
            value: options.value,
            changeType: options.changeType,
            commit: options.commit
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addNavigationShortcut(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "navigation/populate", {
            page: options.page ?? options.pageName ?? options.name,
            caption: options.caption,
            module: options.module,
            type: options.type ?? "Page"
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowCreateObject(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/create-object", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            outputVariableName: options.outputVariableName ?? options.outputVariable,
            commit: options.commit,
            refreshInClient: options.refreshInClient,
            initialValues: options.initialValues
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowCreateList(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/create-list", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            outputVariableName: options.outputVariableName ?? options.outputVariable
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowCallMicroflow(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/call-microflow", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            calledMicroflow: options.calledMicroflow ?? options.called ?? options.call,
            calledModule: options.calledModule,
            outputVariableName: options.outputVariableName ?? options.outputVariable,
            parameterMappings: options.parameterMappings ?? options.parameters
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowRetrieveDatabase(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/retrieve-database", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            outputVariableName: options.outputVariableName ?? options.outputVariable,
            xPathConstraint: options.xPathConstraint ?? options.xpath,
            retrieveFirst: options.retrieveFirst
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowRetrieveAssociation(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/retrieve-association", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            association: options.association,
            entityVariable: options.entityVariable ?? options.entityVar ?? options.fromVariable ?? options.variable,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowFilterByAssociation(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/filter-by-association", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            association: options.association,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            filterExpression: options.filterExpression ?? options.expression ?? options.value
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowFindByAssociation(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/find-by-association", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            association: options.association,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            findExpression: options.findExpression ?? options.expression ?? options.value
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowFilterByAttribute(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/filter-by-attribute", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            attribute: options.attribute,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            filterExpression: options.filterExpression ?? options.expression ?? options.value
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowFindByAttribute(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/find-by-attribute", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            attribute: options.attribute,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            findExpression: options.findExpression ?? options.expression ?? options.value
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowFindByExpression(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/find-by-expression", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            findExpression: options.findExpression ?? options.expression ?? options.value
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowAggregateList(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/aggregate-list", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            aggregateFunction: options.aggregateFunction ?? options.function
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowAggregateByAttribute(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/aggregate-by-attribute", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            attribute: options.attribute,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            aggregateFunction: options.aggregateFunction ?? options.function
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowAggregateByExpression(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/aggregate-by-expression", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            aggregateExpression: options.aggregateExpression ?? options.expression ?? options.value,
            aggregateFunction: options.aggregateFunction ?? options.function
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowChangeList(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/change-list", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            changeListOperation: options.changeListOperation ?? options.operation ?? options.changeType,
            value: options.value ?? options.expression ?? options.itemExpression
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowSortList(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/sort-list", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            attribute: options.attribute,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            sortDescending: options.sortDescending ?? options.descending
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowReduceAggregate(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/reduce-aggregate", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output,
            aggregateExpression: options.aggregateExpression ?? options.expression ?? options.value,
            initialExpression: options.initialExpression ?? options.initialValue ?? options.initial,
            reduceType: options.reduceType ?? options.dataType ?? options.type
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowListHead(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/list-head", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowListTail(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/list-tail", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowListContains(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/list-contains", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            objectVariable: options.objectVariable ?? options.value ?? options.itemVariable ?? options.variable,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowListUnion(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/list-union", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            otherListVariable: options.otherListVariable ?? options.secondListVariable ?? options.objectVariable ?? options.value ?? options.itemVariable ?? options.variable,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowListIntersect(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/list-intersect", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            otherListVariable: options.otherListVariable ?? options.secondListVariable ?? options.objectVariable ?? options.value ?? options.itemVariable ?? options.variable,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowListSubtract(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/list-subtract", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            otherListVariable: options.otherListVariable ?? options.secondListVariable ?? options.objectVariable ?? options.value ?? options.itemVariable ?? options.variable,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowListEquals(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension runtime endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/list-equals", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            listVariable: options.listVariable ?? options.list ?? options.sourceList,
            otherListVariable: options.otherListVariable ?? options.secondListVariable ?? options.objectVariable ?? options.value ?? options.itemVariable ?? options.variable,
            outputVariableName: options.outputVariableName ?? options.outputVariable ?? options.output
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }

    async addMicroflowChangeAssociation(options = {}) {
        const discovery = await resolveEndpointDiscovery(options);
        if (!discovery.available) {
            return {
                ok: false,
                available: false,
                source: discovery.source,
                endpointFile: discovery.endpointFile,
                reason: discovery.reason ?? "The extension endpoint file is not available."
            };
        }

        const payload = await fetchJson(buildExtensionUrl(discovery.endpoints.baseUrl, "microflows/change-association", {
            microflow: options.microflow ?? options.item,
            module: options.module,
            entity: options.entity,
            association: options.association,
            variable: options.variable,
            value: options.value,
            changeType: options.changeType,
            commit: options.commit
        }), options.timeoutMs);

        return {
            ok: true,
            available: true,
            source: discovery.source,
            endpointFile: discovery.endpointFile,
            endpoints: discovery.endpoints,
            payload
        };
    }
}

async function resolveEndpointDiscovery(options) {
    if (options.endpointUrl) {
        const endpointUrl = String(options.endpointUrl).replace(/\/$/, "");
        return {
            available: true,
            source: "explicitUrl",
            endpointFile: null,
            endpoints: {
                healthUrl: `${endpointUrl}/mendix-studio-automation/health`,
                contextUrl: `${endpointUrl}/mendix-studio-automation/context`,
                capabilitiesUrl: `${endpointUrl}/mendix-studio-automation/capabilities`,
                baseUrl: endpointUrl,
                routePrefix: "mendix-studio-automation"
            }
        };
    }

    const endpointFile = options.endpointFile
        || process.env.MENDIX_EXTENSION_ENDPOINT_FILE
        || defaultEndpointFile;

    if (!(await pathExists(endpointFile))) {
        const installedEndpointFile = await resolveEndpointFileFromInstallMetadata();
        if (installedEndpointFile && installedEndpointFile !== endpointFile) {
            return resolveEndpointDiscovery({
                ...options,
                endpointFile: installedEndpointFile
            });
        }
    }

    try {
        await access(endpointFile, fsConstants.F_OK);
    }
    catch {
        return {
            available: false,
            source: "endpointFile",
            endpointFile,
            reason: "Endpoint file not found. Build and load the Mendix Studio Automation extension in Studio Pro first."
        };
    }

    const parsed = await readJsonFile(endpointFile);

    return {
        available: true,
        source: "endpointFile",
        endpointFile,
        endpoints: {
            healthUrl: parsed.healthUrl,
            contextUrl: parsed.contextUrl,
                capabilitiesUrl: parsed.capabilitiesUrl
                ?? `${(parsed.baseUrl ?? "").replace(/\/$/, "")}/mendix-studio-automation/capabilities`,
                quickCreateObjectDialogUrl: parsed.quickCreateObjectDialogUrl,
                quickCreateObjectDialogOpenUrl: parsed.quickCreateObjectDialogOpenUrl,
                navigationPopulateUrl: parsed.navigationPopulateUrl,
                microflowCreateObjectUrl: parsed.microflowCreateObjectUrl,
                microflowCreateListUrl: parsed.microflowCreateListUrl,
                microflowCallMicroflowUrl: parsed.microflowCallMicroflowUrl,
                microflowRetrieveDatabaseUrl: parsed.microflowRetrieveDatabaseUrl,
                microflowRetrieveAssociationUrl: parsed.microflowRetrieveAssociationUrl,
                microflowFilterByAssociationUrl: parsed.microflowFilterByAssociationUrl,
                microflowFindByAssociationUrl: parsed.microflowFindByAssociationUrl,
                microflowFilterByAttributeUrl: parsed.microflowFilterByAttributeUrl,
                microflowFindByAttributeUrl: parsed.microflowFindByAttributeUrl,
                microflowFindByExpressionUrl: parsed.microflowFindByExpressionUrl,
                microflowAggregateListUrl: parsed.microflowAggregateListUrl,
                microflowAggregateByAttributeUrl: parsed.microflowAggregateByAttributeUrl,
                microflowAggregateByExpressionUrl: parsed.microflowAggregateByExpressionUrl,
                microflowChangeListUrl: parsed.microflowChangeListUrl,
                microflowSortListUrl: parsed.microflowSortListUrl,
                microflowReduceAggregateUrl: parsed.microflowReduceAggregateUrl,
                microflowListHeadUrl: parsed.microflowListHeadUrl,
                microflowListTailUrl: parsed.microflowListTailUrl,
                microflowListContainsUrl: parsed.microflowListContainsUrl,
                microflowDeleteObjectUrl: parsed.microflowDeleteObjectUrl,
                microflowCommitObjectUrl: parsed.microflowCommitObjectUrl,
                microflowRollbackObjectUrl: parsed.microflowRollbackObjectUrl,
                microflowChangeAttributeUrl: parsed.microflowChangeAttributeUrl,
                microflowChangeAssociationUrl: parsed.microflowChangeAssociationUrl,
                baseUrl: parsed.baseUrl,
                routePrefix: parsed.routePrefix
            }
        };
}

async function resolveEndpointFileFromInstallMetadata() {
    try {
        await access(installMetadataFile, fsConstants.F_OK);
        const parsed = await readJsonFile(installMetadataFile);
        if (parsed.endpointFile && await pathExists(parsed.endpointFile)) {
            return parsed.endpointFile;
        }

        if (parsed.appDirectory) {
            const discovered = await findEndpointFileInExtensionCache(parsed.appDirectory);
            if (discovered) {
                return discovered;
            }
        }

        if (process.env.USERPROFILE) {
            const userRoot = path.join(process.env.USERPROFILE, "Mendix");
            const fallback = await findEndpointFileInTopLevelAppRoots(userRoot);
            if (fallback) {
                return fallback;
            }
        }

        return parsed.endpointFile || null;
    }
    catch {
        return null;
    }
}

async function findEndpointFileInExtensionCache(appDirectory) {
    const cacheRoot = path.join(appDirectory, ".mendix-cache", "extensions-cache");

    if (!(await pathExists(cacheRoot))) {
        return null;
    }

    const cacheEntries = await readdir(cacheRoot, {
        withFileTypes: true
    });

    const candidates = [];
    for (const entry of cacheEntries) {
        if (!entry.isDirectory()) {
            continue;
        }

        const endpointFile = path.join(cacheRoot, entry.name, "runtime", "endpoint.json");
        if (!(await pathExists(endpointFile))) {
            continue;
        }

        try {
            const parsed = await readJsonFile(endpointFile);
            if (parsed.extensionName === "Mendix Studio Automation") {
                const endpointStat = await stat(endpointFile);
                candidates.push({
                    endpointFile,
                    rank: endpointStat.mtimeMs
                });
            }
        }
        catch {
            // Ignore malformed endpoint files from unrelated extensions.
        }
    }

    if (candidates.length === 0) {
        return null;
    }

    candidates.sort((left, right) => right.rank - left.rank);
    return candidates[0].endpointFile;
}

async function findEndpointFileInTopLevelAppRoots(rootDirectory) {
    if (!(await pathExists(rootDirectory))) {
        return null;
    }

    const appCandidates = await readdir(rootDirectory, {
        withFileTypes: true
    });
    for (const app of appCandidates) {
        if (!app.isDirectory()) {
            continue;
        }

        const appDirectory = path.join(rootDirectory, app.name);
        const discovered = await findEndpointFileInExtensionCache(appDirectory);
        if (discovered) {
            return discovered;
        }
    }

    return null;
}

async function pathExists(targetPath) {
    try {
        await access(targetPath, fsConstants.F_OK);
        return true;
    }
    catch {
        return false;
    }
}

async function readJsonFile(targetPath) {
    const raw = await readFile(targetPath, "utf8");
    return JSON.parse(raw.replace(/^\uFEFF/, ""));
}

async function fetchJson(url, timeoutMs = 3000) {
    if (!url) {
        throw new Error("The extension endpoint URL is missing.");
    }

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), numberOrDefault(timeoutMs, 3000));
    try {
        const response = await fetch(url, {
            method: "GET",
            signal: controller.signal
        });
        if (!response.ok) {
            throw new Error(`The extension endpoint returned HTTP ${response.status}.`);
        }

        return response.json();
    }
    finally {
        clearTimeout(timeout);
    }
}

function buildExtensionUrl(baseUrl, routeSuffix, query = {}) {
    if (!baseUrl) {
        throw new Error("The extension base URL is missing.");
    }

    const url = new URL(`mendix-studio-automation/${routeSuffix.replace(/^\/+/, "")}`, ensureTrailingSlash(baseUrl));
    for (const [key, value] of Object.entries(query)) {
        if (value === undefined || value === null || value === "") {
            continue;
        }

        url.searchParams.set(key, String(value));
    }

    return url.toString();
}

function ensureTrailingSlash(value) {
    return value.endsWith("/") ? value : `${value}/`;
}

function numberOrDefault(value, fallback) {
    if (value === undefined || value === null || value === "") {
        return fallback;
    }

    const parsed = Number.parseInt(String(value), 10);
    return Number.isFinite(parsed) ? parsed : fallback;
}
