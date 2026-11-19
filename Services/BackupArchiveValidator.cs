using System.IO.Compression;
using System.Text.Json;
using BuildingManagementMvc.Models;

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

    /// <summary>
    /// Validates archive entry names, sizes, collection identities, and JSON row envelopes.
    /// </summary>
    /// <param name="archive">The ZIP archive to validate.</param>
    /// <returns>A preview of each recognized collection and its record count.</returns>
    /// <exception cref="InvalidDataException">The archive does not match the supported backup format.</exception>
    public static async Task<List<RestoreCollectionPreview>> ValidateAsync(ZipArchive archive)
    {
        if (archive.Entries.Count == 0 || archive.Entries.Count > MaxEntries)
            throw new InvalidDataException("The archive has an invalid entry count.");

        var knownCollections = BackupService.GetKnownCollections();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var previews = new List<RestoreCollectionPreview>();
        long declaredExpandedBytes = 0;
        long actualExpandedBytes = 0;

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

            declaredExpandedBytes = checked(declaredExpandedBytes + entry.Length);
            if (declaredExpandedBytes > MaxExpandedBytes)
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
            using var boundedStream = new BoundedReadStream(
                stream, Math.Min(MaxEntryBytes, MaxExpandedBytes - actualExpandedBytes));
            using var document = await JsonDocument.ParseAsync(
                boundedStream,
                new JsonDocumentOptions { MaxDepth = 64 });

            actualExpandedBytes = checked(actualExpandedBytes + boundedStream.BytesRead);
            if (boundedStream.BytesRead != entry.Length || actualExpandedBytes > MaxExpandedBytes)
                throw new InvalidDataException("The archive entry size does not match its declared size.");

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

    /// <summary>
    /// Counts decompressed bytes while parsing an entry, independent of ZIP central-directory
    /// size metadata. The underlying entry stream remains owned by the caller.
    /// </summary>
    private sealed class BoundedReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;

        public BoundedReadStream(Stream inner, long maxBytes)
        {
            _inner = inner;
            _maxBytes = maxBytes;
        }

        public long BytesRead { get; private set; }
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        private void Track(int count)
        {
            if (count <= 0) return;
            if (BytesRead > _maxBytes - count)
                throw new InvalidDataException("The archive entry exceeds the decompressed size limit.");
            BytesRead += count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            Track(read);
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = _inner.Read(buffer);
            Track(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken);
            Track(read);
            return read;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
            Track(read);
            return read;
        }

        public override int ReadByte()
        {
            var value = _inner.ReadByte();
            if (value >= 0) Track(1);
            return value;
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            // The caller owns and disposes the underlying ZIP entry stream.
            base.Dispose(disposing);
        }
    }
}
