namespace MendixStudioAutomation.Extension.Models;

public sealed record StudioContextSnapshot(
    string ExtensionName,
    string ExtensionVersion,
    DateTimeOffset GeneratedAtUtc,
    string? StudioWebServerBaseUrl,
    string? AppName,
    string? AppDirectory,
    string? BranchName,
    bool IsVersionControlled,
    int? ErrorCount,
    DocumentContextSnapshot ActiveDocument,
    IReadOnlyList<string> Capabilities
);
