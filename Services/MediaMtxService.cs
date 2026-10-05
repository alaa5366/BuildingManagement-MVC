using System.Net.Http.Json;

namespace BuildingManagementMvc.Services;

public class MediaMtxService
{
    private readonly HttpClient _http;
    private readonly ILogger<MediaMtxService> _log;

    public MediaMtxService(HttpClient http, IConfiguration config, ILogger<MediaMtxService> log)
    {
        _http = http;
        _log = log;

        var baseUrl = config["MediaMtx:ApiUrl"] ?? "http://mediamtx:9997/";
        _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<bool> AddOrUpdatePathAsync(string pathName, string rtspUrl)
    {
        try
        {
            var body = new
            {
                source = rtspUrl,
                sourceOnDemand = true  // يفتح الـ stream لما حد يتفرج
            };
            var res = await _http.PostAsJsonAsync($"v3/config/paths/add/{pathName}", body);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                _log.LogError("MediaMTX add path failed: {Err}", err);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "MediaMTX connection failed");
            return false;
        }
    }

    public async Task<bool> DeletePathAsync(string pathName)
    {
        try
        {
            var res = await _http.PostAsync($"v3/config/paths/delete/{pathName}", null);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "MediaMTX delete path failed");
            return false;
        }
    }

    public static string BuildRtspUrl(
        string ip, int port, string username, string password, string rtspPath)
    {
        var userInfo = string.IsNullOrEmpty(username)
            ? ""
            : $"{Uri.EscapeDataString(username)}:{Uri.EscapeDataString(password)}@";
        return $"rtsp://{userInfo}{ip}:{port}/{rtspPath.TrimStart('/')}";
    }

    public static string GetDefaultRtspPath(string brand, int channel)
    {
        return brand?.ToLowerInvariant() switch
        {
            "hikvision" => $"Streaming/Channels/{channel}01",     // 101, 201, 301
            "dahua" => $"cam/realmonitor?channel={channel}&subtype=0",
            "onvif" => $"onvif1",
            _ => $"ch{channel}"
        };
    }
}