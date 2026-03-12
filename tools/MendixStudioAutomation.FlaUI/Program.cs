using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true
};

var options = ParseArgs(args);
var command = options.TryGetValue("_command", out var commandValue) && !string.IsNullOrWhiteSpace(commandValue)
    ? commandValue!
    : "snapshot";

try
{
    using var automation = new UIA3Automation();
    object payload = command switch
    {
        "snapshot" => CreateSnapshot(automation, options),
        "list-dialogs" => ListDialogs(automation, options),
        "find-elements" => FindElements(automation, options),
        _ => new
        {
            ok = false,
            action = command,
            error = $"Unknown FlaUI command '{command}'."
        }
    };

    Console.WriteLine(JsonSerializer.Serialize(payload, jsonOptions));
    return payload.GetType().GetProperty("ok")?.GetValue(payload) as bool? == false ? 1 : 0;
}
catch (Exception exception)
{
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ok = false,
        action = $"flaui-{command}",
        error = exception.Message
    }, jsonOptions));
    return 1;
}

static object CreateSnapshot(UIA3Automation automation, IReadOnlyDictionary<string, string?> options)
{
    var process = ResolveProcess(options);
    var mainWindow = ResolveMainWindow(automation, process);
    var windows = FindTopLevelWindows(automation, process);
    var dialogWindows = windows
        .Where(window => GetNativeWindowHandle(window) != process.MainWindowHandle.ToInt32())
        .Select(SummarizeElement)
        .ToArray();

    return new
    {
        ok = true,
        action = "flaui-snapshot",
        processId = process.Id,
        processName = process.ProcessName,
        mainWindow = SummarizeElement(mainWindow),
        dialogCount = dialogWindows.Length,
        dialogs = dialogWindows
    };
}

static object ListDialogs(UIA3Automation automation, IReadOnlyDictionary<string, string?> options)
{
    var process = ResolveProcess(options);
    var dialogs = FindTopLevelWindows(automation, process)
        .Where(window => GetNativeWindowHandle(window) != process.MainWindowHandle.ToInt32())
        .Select(SummarizeElement)
        .ToArray();

    return new
    {
        ok = true,
        action = "flaui-list-dialogs",
        processId = process.Id,
        processName = process.ProcessName,
        count = dialogs.Length,
        items = dialogs
    };
}

static object FindElements(UIA3Automation automation, IReadOnlyDictionary<string, string?> options)
{
    var process = ResolveProcess(options);
    var scope = GetOption(options, "scope") ?? "main";
    var root = ResolveRootElement(automation, process, scope, GetOption(options, "dialog"));
    var limit = GetIntOption(options, "limit") ?? 50;

    var items = Retry.WhileNull(
            () => root.FindAllDescendants(),
            TimeSpan.FromMilliseconds(300),
            TimeSpan.FromMilliseconds(1200))
        .Result ?? Array.Empty<AutomationElement>();

    var filtered = items
        .Where(element => MatchesElement(element, options))
        .Take(limit)
        .Select(SummarizeElement)
        .ToArray();

    return new
    {
        ok = true,
        action = "flaui-find-elements",
        processId = process.Id,
        processName = process.ProcessName,
        scope,
        dialog = GetOption(options, "dialog"),
        count = filtered.Length,
        items = filtered
    };
}

static AutomationElement ResolveRootElement(UIA3Automation automation, Process process, string scope, string? dialogName)
{
    if (scope.Equals("dialog", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(dialogName))
        {
            throw new InvalidOperationException("A --dialog argument is required when --scope dialog is used.");
        }

        var dialog = FindTopLevelWindows(automation, process)
            .FirstOrDefault(window => NamesMatch(window.Name, dialogName));
        return dialog ?? throw new InvalidOperationException($"Could not find a dialog matching '{dialogName}'.");
    }

    return ResolveMainWindow(automation, process);
}

static Process ResolveProcess(IReadOnlyDictionary<string, string?> options)
{
    var processId = GetIntOption(options, "process-id");
    if (processId.HasValue)
    {
        return Process.GetProcessById(processId.Value);
    }

    var requestedTitle = GetOption(options, "title");
    var requestedProcessName = GetOption(options, "process-name");

    List<Process> candidates;
    if (requestedTitle is not null || requestedProcessName is not null)
    {
        candidates = Process.GetProcesses()
            .Where(process => process.MainWindowHandle != IntPtr.Zero)
            .Where(process => !string.IsNullOrWhiteSpace(SafeMainWindowTitle(process)))
            .Where(process => requestedProcessName is null
                || process.ProcessName.Contains(requestedProcessName, StringComparison.OrdinalIgnoreCase))
            .Where(process => requestedTitle is null
                || SafeMainWindowTitle(process).Contains(requestedTitle, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(process => SafeStartTime(process))
            .ToList();
    }
    else
    {
        candidates = Process.GetProcesses()
            .Where(process => process.MainWindowHandle != IntPtr.Zero)
            .Where(process => process.ProcessName.Contains("studiopro", StringComparison.OrdinalIgnoreCase)
                || process.ProcessName.Contains("mendix", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(process => SafeStartTime(process))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = Process.GetProcesses()
                .Where(process => process.MainWindowHandle != IntPtr.Zero)
                .Where(process => SafeMainWindowTitle(process).Contains("Mendix Studio Pro", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(process => SafeStartTime(process))
                .ToList();
        }
    }

    if (candidates.Count == 0)
    {
        throw new InvalidOperationException("Could not find a running Studio Pro window matching the requested process or title.");
    }

    return candidates[0];
}

static Window ResolveMainWindow(UIA3Automation automation, Process process)
{
    if (process.MainWindowHandle == IntPtr.Zero)
    {
        throw new InvalidOperationException($"Process {process.Id} does not currently expose a main window handle.");
    }

    return automation.FromHandle(process.MainWindowHandle).AsWindow();
}

static IReadOnlyList<Window> FindTopLevelWindows(UIA3Automation automation, Process process)
{
    return automation.GetDesktop()
        .FindAllChildren(cf => cf.ByControlType(ControlType.Window).And(cf.ByProcessId(process.Id)))
        .Select(element => element.AsWindow())
        .ToArray();
}

static bool MatchesElement(AutomationElement element, IReadOnlyDictionary<string, string?> options)
{
    var name = GetOption(options, "name");
    var nameContains = GetOption(options, "name-contains");
    var automationId = GetOption(options, "automation-id");
    var className = GetOption(options, "class-name");
    var controlType = GetOption(options, "control-type");

    if (!string.IsNullOrWhiteSpace(name) && !string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (!string.IsNullOrWhiteSpace(nameContains)
        && (element.Name?.Contains(nameContains, StringComparison.OrdinalIgnoreCase) ?? false) == false)
    {
        return false;
    }

    if (!string.IsNullOrWhiteSpace(automationId)
        && !string.Equals(element.AutomationId, automationId, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (!string.IsNullOrWhiteSpace(className)
        && !string.Equals(element.ClassName, className, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (!string.IsNullOrWhiteSpace(controlType)
        && !string.Equals(element.ControlType.ToString(), controlType, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    return true;
}

static object SummarizeElement(AutomationElement element)
{
    var rectangle = element.BoundingRectangle;
    var valuePattern = element.Patterns.Value.PatternOrDefault;
    var togglePattern = element.Patterns.Toggle.PatternOrDefault;
    var selectionItemPattern = element.Patterns.SelectionItem.PatternOrDefault;

    return new
    {
        name = SafeGet(() => element.Name),
        automationId = SafeGet(() => element.AutomationId),
        className = SafeGet(() => element.ClassName),
        controlType = SafeGet(() => element.ControlType.ToString()),
        frameworkType = SafeGet(() => element.FrameworkType.ToString()),
        processId = element.Properties.ProcessId.TryGetValue(out var processId) ? processId : 0,
        nativeWindowHandle = GetNativeWindowHandle(element),
        isEnabled = SafeGet(() => element.IsEnabled),
        isOffscreen = SafeGet(() => element.IsOffscreen),
        isSelected = selectionItemPattern?.IsSelected,
        textValue = valuePattern?.Value,
        toggleState = togglePattern?.ToggleState.ToString(),
        bounds = new
        {
            left = rectangle.Left,
            top = rectangle.Top,
            width = rectangle.Width,
            height = rectangle.Height
        }
    };
}

static int GetNativeWindowHandle(AutomationElement element)
{
    return element.Properties.NativeWindowHandle.TryGetValue(out var nativeWindowHandle) ? (int)nativeWindowHandle : 0;
}

static T? SafeGet<T>(Func<T> callback)
{
    try
    {
        return callback();
    }
    catch
    {
        return default;
    }
}

static string SafeMainWindowTitle(Process process)
{
    try
    {
        return process.MainWindowTitle ?? string.Empty;
    }
    catch
    {
        return string.Empty;
    }
}

static DateTime SafeStartTime(Process process)
{
    try
    {
        return process.StartTime;
    }
    catch
    {
        return DateTime.MinValue;
    }
}

static bool NamesMatch(string? actual, string? requested)
{
    if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(requested))
    {
        return false;
    }

    return string.Equals(actual, requested, StringComparison.OrdinalIgnoreCase)
        || actual.Contains(requested, StringComparison.OrdinalIgnoreCase);
}

static string? GetOption(IReadOnlyDictionary<string, string?> options, string key)
{
    return options.TryGetValue(key, out var value) ? value : null;
}

static int? GetIntOption(IReadOnlyDictionary<string, string?> options, string key)
{
    return options.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
        ? parsed
        : null;
}

static Dictionary<string, string?> ParseArgs(IReadOnlyList<string> args)
{
    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    if (args.Count == 0)
    {
        return values;
    }

    values["_command"] = args[0];
    for (var index = 1; index < args.Count; index++)
    {
        var token = args[index];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = token[2..];
        string? value = "true";
        if (index + 1 < args.Count && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = args[index + 1];
            index++;
        }

        values[key] = value;
    }

    return values;
}
