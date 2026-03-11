namespace MendixStudioAutomation.Extension.Models;

public sealed record DocumentContextSnapshot(
    string? DocumentId,
    string? DocumentName,
    string? DocumentType,
    string? ModuleName,
    string? SelectedElementName,
    string SelectionSource
);
