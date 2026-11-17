using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingManagementMvc.Models;
using Microsoft.Extensions.Hosting;

namespace BuildingManagementMvc.Services;

/// <summary>Reads retained Serilog files and returns sanitized, filtered application log events.</summary>
public sealed class ApplicationLogQueryService
{
    private const int MaxPage = 100;
    private const int MaxPageSize = 100;
    private const int MaxMessageLength = 2000;
    private const int MaxExceptionLength = 3000;
    private const int MaxDetailsLength = 4000;

    private static readonly Regex EventLine = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[(?<level>[A-Z]{3})\] (?<body>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SecretAssignment = new(
        @"(?i)[""']?(password|passwd|pwd|token|access_token|refresh_token|client_secret|api[_-]?key|authorization|cookie|set-cookie|secret)[""']?(\s*[:=]\s*)(""[^""]*""|'[^']*'|[^\s,;]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ConnectionString = new(
        @"(?i)\b(server|data source)\s*=\s*[^;\s]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex BearerToken = new(
        @"(?i)\bBearer\s+[A-Za-z0-9._~+/-]+=*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex JwtToken = new(
        @"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex EmailAddress = new(
        @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex PhoneNumber = new(
        @"(?<!\w)\+?\d[\d\s().-]{8,}\d(?!\w)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex VersionAssignment = new(
        @"\b(?:Version|AssemblyVersion|FileVersion)\s*=\s*\d+(?:\.\d+){1,3}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex IpAddress = new(
        @"(?<![\d.])(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(?![\d.])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ExceptionStart = new(
        @"^\s*(?:[A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)*)?(?:Exception|Error|Fault)(?:\s*\([^)]*\))?:",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _directory;
    private readonly ILogger<ApplicationLogQueryService> _logger;

    public ApplicationLogQueryService(IHostEnvironment environment, ILogger<ApplicationLogQueryService> logger)
    {
        _directory = Path.Combine(environment.ContentRootPath, "logs", "serilog");
        _logger = logger;
    }

    /// <summary>Queries the retained log files and returns the requested bounded result page.</summary>
    /// <param name="query">Date, level, text, source, correlation, and exception filters with paging options.</param>
    /// <param name="cancellationToken">Token used to cancel file enumeration and reading.</param>
    /// <returns>A page of matching sanitized events and the total matching event count.</returns>
    public async Task<PagedResult<ApplicationLogEntry>> QueryPageAsync(
        ApplicationLogQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = Math.Clamp(query.Page, 1, MaxPage);
        var pageSize = Math.Clamp(query.PageSize, 10, MaxPageSize);
        var capacity = checked((page - 1) * pageSize + pageSize);
        var filter = Normalize(query);
        var retained = new PriorityQueue<ApplicationLogEntry, (long Ticks, long Sequence)>();
        var total = 0;
        long sequence = 0;

        if (!Directory.Exists(_directory))
            return Empty(page, pageSize);

        string[] files;
        try
        {
            files = Directory.EnumerateFiles(_directory, "serilog-*.txt", SearchOption.TopDirectoryOnly)
                .OrderByDescending(path => SafeLastWriteTime(path)).Take(14).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Unable to enumerate application log files");
            return Empty(page, pageSize);
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ReadFileAsync(file, filter, entry =>
                {
                    total++;
                    var priority = (entry.Timestamp.UtcDateTime.Ticks, sequence++);
                    if (retained.Count < capacity)
                    {
                        retained.Enqueue(entry, priority);
                    }
                    else if (retained.TryPeek(out _, out var oldest) &&
                             Comparer<(long Ticks, long Sequence)>.Default.Compare(priority, oldest) > 0)
                    {
                        retained.Dequeue();
                        retained.Enqueue(entry, priority);
                    }
                }, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Unable to read application log file {LogFileName}", Path.GetFileName(file));
            }
        }

        var effectivePage = Math.Min(page, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)));
        var ordered = new List<ApplicationLogEntry>(retained.Count);
        while (retained.TryDequeue(out var entry, out _))
            ordered.Add(entry);
        ordered.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

        return new PagedResult<ApplicationLogEntry>
        {
            Items = ordered.Skip((effectivePage - 1) * pageSize).Take(pageSize).ToList(),
            CurrentPage = effectivePage,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    private async Task ReadFileAsync(string path, ApplicationLogQuery filter,
        Action<ApplicationLogEntry> onMatch, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, 64 * 1024, FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        ApplicationLogEntry? current = null;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var match = EventLine.Match(line);
            if (match.Success && TryParseEvent(match, Path.GetFileName(path), out var parsed))
            {
                if (current is not null && Matches(current, filter))
                    onMatch(current);

                current = parsed;
                continue;
            }

            if (current is null || string.IsNullOrWhiteSpace(line))
                continue;

            // Serilog writes structured properties after the final line of
            // multiline messages (for example, EF Core SQL commands).
            // Keep the human-readable prefix as event details, not as metadata.
            var (continuation, correlation, source) = SplitProperties(line);
            var hasContextProperties =
                !string.IsNullOrWhiteSpace(correlation) ||
                !string.IsNullOrWhiteSpace(source);

            if (hasContextProperties)
            {
                // ApplicationLogEntry exposes init-only context properties.
                // Rebuild the entry with the newly discovered metadata rather
                // than mutating those properties after initialization.
                current = new ApplicationLogEntry
                {
                    Timestamp = current.Timestamp,
                    Level = current.Level,
                    Message = current.Message,
                    CorrelationId = string.IsNullOrWhiteSpace(current.CorrelationId)
                        ? Limit(Redact(correlation), 100)
                        : current.CorrelationId,
                    SourceContext = string.IsNullOrWhiteSpace(current.SourceContext)
                        ? Limit(Redact(source), 250)
                        : current.SourceContext,
                    Exception = current.Exception,
                    Details = current.Details,
                    FileName = current.FileName
                };
            }

            // A valid JSON suffix is structured event metadata even when it
            // contains no CorrelationId or SourceContext. Never display it as SQL.
            if (!string.IsNullOrWhiteSpace(continuation))
            {
                if ((current.Exception.Length > 0 || ExceptionStart.IsMatch(continuation)) &&
                    current.Exception.Length < MaxExceptionLength)
                {
                    var safe = Redact(continuation);
                    var remaining = MaxExceptionLength - current.Exception.Length;
                    var fragment = safe[..Math.Min(remaining, safe.Length)];

                    current.Exception = current.Exception.Length == 0
                        ? fragment
                        : current.Exception + Environment.NewLine + fragment;
                }
                else if (current.Exception.Length == 0 &&
                         current.Details.Length < MaxDetailsLength)
                {
                    var safe = Redact(continuation);
                    var remaining = MaxDetailsLength - current.Details.Length;
                    var fragment = safe[..Math.Min(remaining, safe.Length)];

                    current.Details = current.Details.Length == 0
                        ? fragment
                        : current.Details + Environment.NewLine + fragment;
                }
            }
        }

        if (current is not null && Matches(current, filter))
            onMatch(current);
    }

    private static bool TryParseEvent(Match match, string fileName, out ApplicationLogEntry entry)
    {
        entry = new ApplicationLogEntry();
        if (!DateTimeOffset.TryParseExact(match.Groups["timestamp"].Value,
            "yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            return false;

        var level = match.Groups["level"].Value switch
        {
            "VRB" => "Verbose", "DBG" => "Debug", "INF" => "Information",
            "WRN" => "Warning", "ERR" => "Error", "FTL" => "Fatal", _ => match.Groups["level"].Value
        };
        var (message, correlation, source) = SplitProperties(match.Groups["body"].Value);
        entry = new ApplicationLogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = Limit(Redact(message), MaxMessageLength),
            CorrelationId = Limit(Redact(correlation), 100),
            SourceContext = Limit(Redact(source), 250),
            FileName = fileName
        };
        return true;
    }

    private static (string Message, string Correlation, string Source) SplitProperties(string body)
    {
        var marker = body.LastIndexOf(" {", StringComparison.Ordinal);
        if (marker < 0) return (body, "", "");
        try
        {
            using var json = JsonDocument.Parse(body[(marker + 1)..]);
            if (json.RootElement.ValueKind != JsonValueKind.Object) return (body, "", "");
            return (body[..marker].TrimEnd(),
                GetProperty(json.RootElement, "CorrelationId"),
                GetProperty(json.RootElement, "SourceContext"));
        }
        catch (JsonException) { return (body, "", ""); }
    }

    private static string GetProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? "" : property.Value.ToString();
        return "";
    }

    private static bool Matches(ApplicationLogEntry entry, ApplicationLogQuery query)
    {
        if (query.FromUtc.HasValue && entry.Timestamp < query.FromUtc.Value) return false;
        if (query.ToUtc.HasValue && entry.Timestamp > query.ToUtc.Value) return false;
        if (!string.IsNullOrWhiteSpace(query.Level) &&
            !entry.Level.Equals(query.Level, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(query.CorrelationId) &&
            !entry.CorrelationId.Contains(query.CorrelationId, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrWhiteSpace(query.Source) &&
            !entry.SourceContext.Contains(query.Source, StringComparison.OrdinalIgnoreCase)) return false;
        if (query.HasException.HasValue && entry.HasException != query.HasException.Value) return false;
        if (!string.IsNullOrWhiteSpace(query.Search) &&
            !entry.Message.Contains(query.Search, StringComparison.OrdinalIgnoreCase) &&
            !entry.Exception.Contains(query.Search, StringComparison.OrdinalIgnoreCase) &&
            !entry.SourceContext.Contains(query.Search, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    private static ApplicationLogQuery Normalize(ApplicationLogQuery query) => new()
    {
        FromUtc = query.FromUtc, ToUtc = query.ToUtc,
        Level = string.IsNullOrWhiteSpace(query.Level) ? null :
            new[] { "Verbose", "Debug", "Information", "Warning", "Error", "Fatal" }
                .FirstOrDefault(x => x.Equals(query.Level.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "__invalid__",
        Search = Limit(query.Search?.Trim(), 200),
        CorrelationId = Limit(query.CorrelationId?.Trim(), 100),
        Source = Limit(query.Source?.Trim(), 200),
        HasException = query.HasException
    };

    private static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var versions = new List<string>();
        var safe = VersionAssignment.Replace(value, match =>
        {
            var marker = $"__BM_VERSION_{versions.Count}__";
            versions.Add(match.Value);
            return marker;
        });

        safe = ConnectionString.Replace(safe, "[REDACTED_CONNECTION_STRING]");
        safe = SecretAssignment.Replace(safe, "$1$2[REDACTED]");
        safe = BearerToken.Replace(safe, "Bearer [REDACTED]");
        safe = JwtToken.Replace(safe, "[REDACTED_TOKEN]");
        safe = EmailAddress.Replace(safe, "[REDACTED_EMAIL]");
        safe = IpAddress.Replace(safe, "[REDACTED_IP]");
        safe = PhoneNumber.Replace(safe, "[REDACTED_PHONE]");

        for (var i = 0; i < versions.Count; i++)
            safe = safe.Replace($"__BM_VERSION_{i}__", versions[i], StringComparison.Ordinal);

        return safe;
    }

    private static string Limit(string? value, int length) =>
        string.IsNullOrEmpty(value) ? "" : value.Length <= length ? value : value[..length] + "…";

    private static DateTime SafeLastWriteTime(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return DateTime.MinValue; }
    }

    private static PagedResult<ApplicationLogEntry> Empty(int page, int size) =>
        new() { CurrentPage = page, PageSize = size, TotalItems = 0 };
}
