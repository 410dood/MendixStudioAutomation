namespace MendixStudioAutomation.Extension.Models;

public sealed record DocumentDescriptor(
    string Id,
    string Name,
    string Type,
    string? ModuleName
);
