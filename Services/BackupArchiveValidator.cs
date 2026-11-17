using System.IO.Compression;
using System.Text.Json;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Validates the exact ZIP structure accepted by the backup restore workflow.
/// </summary>
internal static class BackupArchiveValidator
{
    private const int MaxEntries = 100;
    private const long MaxEntryBytes = 100L * 1024 * 1024;
    private const long MaxReadmeBytes = 1024 * 1024;
    private const long MaxExpandedBytes = 500L * 1024 * 1024;

    public static async Task<List<RestoreCollectionPreview>> ValidateAsync(ZipArchive archive)
    {
        if (archive.Entries.Count == 0 || archive.Entries.Count > MaxEntries)
            throw new InvalidDataException("The archive has an invalid entry count.");

        var knownCollections = BackupService.GetKnownCollections();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var previews = new List<RestoreCollectionPreview>();
        long expandedBytes = 0;

        foreach (var entry in archive.Entries)
        {
            // The application creates only root-level files. Do not accept directories,
            // traversal paths, or alternate names that restore code might interpret differently.
            if (string.IsNullOrWhiteSpace(entry.Name) ||
                !string.Equals(entry.FullName, entry.Name, StringComparison.Ordinal) ||
                entry.Name is "." or ".." ||
                entry.Name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            {
                throw new InvalidDataException("The archive contains an invalid entry name.");
            }

            if (!seenNames.Add(entry.Name))
                throw new InvalidDataException("The archive contains duplicate entry names.");

            if (entry.Length < 0 || entry.Length > MaxEntryBytes)
                throw new InvalidDataException("The archive contains an oversized entry.");

            expandedBytes = checked(expandedBytes + entry.Length);
            if (expandedBytes > MaxExpandedBytes)
                throw new InvalidDataException("The archive exceeds the expanded size limit.");

            if (string.Equals(entry.Name, "README.md", StringComparison.Ordinal))
            {
                if (entry.Length > MaxReadmeBytes)
                    throw new InvalidDataException("The archive README is too large.");
                continue;
            }

            if (!entry.Name.EndsWith(".json", StringComparison.Ordinal))
                throw new InvalidDataException("The archive contains an unsupported file.");

            var tableName = entry.Name[..^".json".Length];
            if (!knownCollections.ContainsKey(tableName))
                throw new InvalidDataException("The archive contains an unknown collection.");

            await using var stream = entry.Open();
            using var document = await JsonDocument.ParseAsync(
                stream,
                new JsonDocumentOptions { MaxDepth = 64 });

            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException($"Collection '{tableName}' must contain a JSON array.");

            var count = 0;
            foreach (var row in document.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object ||
                    !row.TryGetProperty("id", out var idElement) ||
                    idElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(idElement.GetString()) ||
                    idElement.GetString()!.Length > 512 ||
                    !row.TryGetProperty("data", out var dataElement) ||
                    dataElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException($"Collection '{tableName}' contains an invalid row.");
                }

                count++;
            }

            previews.Add(new RestoreCollectionPreview
            {
                Collection = tableName,
                LabelAr = knownCollections[tableName],
                DocCount = count,
                SizeBytes = entry.Length,
                Selected = true
            });
        }

        if (previews.Count == 0)
            throw new InvalidDataException("The archive contains no recognized collections.");

        return previews;
    }
}
