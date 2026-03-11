using MendixStudioAutomation.Extension.Models;

namespace MendixStudioAutomation.Extension.Infrastructure;

internal sealed class ExtensionStateStore
{
    private readonly object _sync = new();
    private string? _documentId;
    private string? _documentName;
    private string? _documentType;
    private string? _moduleName;
    private string? _selectedElementName;
    private string _selectionSource = "not-yet-implemented";
    private DateTimeOffset _lastDocumentChangeUtc = DateTimeOffset.MinValue;

    public void UpdateActiveDocument(
        string? documentId,
        string? documentName,
        string? documentType,
        string? moduleName,
        string? selectedElementName = null,
        string? selectionSource = null)
    {
        lock (_sync)
        {
            _documentId = documentId;
            _documentName = documentName;
            _documentType = documentType;
            _moduleName = moduleName;
            _selectedElementName = selectedElementName;
            _selectionSource = string.IsNullOrWhiteSpace(selectionSource)
                ? "not-yet-implemented"
                : selectionSource;
            _lastDocumentChangeUtc = DateTimeOffset.UtcNow;
        }
    }

    public DocumentContextSnapshot GetActiveDocumentSnapshot()
    {
        lock (_sync)
        {
            return new DocumentContextSnapshot(
                _documentId,
                _documentName,
                _documentType,
                _moduleName,
                _selectedElementName,
                _selectionSource);
        }
    }

    public DateTimeOffset LastDocumentChangeUtc
    {
        get
        {
            lock (_sync)
            {
                return _lastDocumentChangeUtc;
            }
        }
    }
}
