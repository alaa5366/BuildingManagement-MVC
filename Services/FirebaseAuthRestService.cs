using System.Net.Http.Json;
using System.Text.Json;

namespace BuildingManagementMvc.Services;

// نداءات REST مباشرة لـ Firebase Identity Toolkit (نفس اللي بيعمله
// Firebase Auth JS SDK من تحت، بس من السيرفر). محتاجين بس Web API Key.
public class FirebaseAuthRestService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public FirebaseAuthRestService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Firebase:WebApiKey"] ?? "";
    }

    // ============================================================
    // 1. تسجيل الدخول بالإيميل والباسورد
    // ============================================================
    public async Task<FirebaseAuthResult> SignInWithPasswordAsync(string email, string password)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
        return await PostAsync(url, email, password);
    }

    // ============================================================
    // 2. إنشاء مستخدم جديد
    // ============================================================
    public async Task<FirebaseAuthResult> CreateUserAsync(string email, string password)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
        var result = await PostAsync(url, email, password);

        // لو الحساب موجود بالفعل، جرّب تسجيل الدخول بنفس الباسورد
        if (!result.Success && result.Error == "EMAIL_EXISTS")
        {
            var signIn = await SignInWithPasswordAsync(email, password);
            if (signIn.Success)
            {
                return new FirebaseAuthResult { Success = true, Uid = signIn.Uid, Recovered = true };
            }
        }

        return result;
    }

    // ============================================================
    // 3. تحديث كلمة السر (يحتاج UID)
    // ============================================================
    public async Task<FirebaseAuthResult> UpdatePasswordAsync(string uid, string newPassword)
    {
        try
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_apiKey}";
            var payload = new { localId = uid, password = newPassword, returnSecureToken = false };
            var resp = await _http.PostAsJsonAsync(url, payload);
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
                return new FirebaseAuthResult { Success = true, Uid = uid };

            string errMsg = "unknown-error";
            using (var doc = JsonDocument.Parse(body))
            {
                if (doc.RootElement.TryGetProperty("error", out var errEl) &&
                    errEl.TryGetProperty("message", out var msgEl))
                {
                    errMsg = msgEl.GetString() ?? errMsg;
                }
            }

            return new FirebaseAuthResult { Success = false, Error = errMsg };
        }
        catch (Exception ex)
        {
            return new FirebaseAuthResult { Success = false, Error = "connection-error: " + ex.Message };
        }
    }

    // ============================================================
    // 4. البحث عن مستخدم بالإيميل (يرجع UID)
    // ============================================================
    public async Task<string?> GetUidByEmailAsync(string email)
    {
        try
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={_apiKey}";
            var payload = new { email = new[] { email } };
            var resp = await _http.PostAsJsonAsync(url, payload);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("users", out var users) ||
                users.GetArrayLength() == 0)
                return null;

            return users[0].GetProperty("localId").GetString();
        }
        catch
        {
            return null;
        }
    }

    // ============================================================
    // 5. حذف مستخدم (يحتاج UID)
    // ============================================================
    public async Task<bool> DeleteUserAsync(string uid)
    {
        try
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:delete?key={_apiKey}";
            var payload = new { localId = uid };
            var resp = await _http.PostAsJsonAsync(url, payload);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ============================================================
    // 6. مساعدات التعديل (اسم/إيميل/موبايل)
    // ============================================================
    public async Task<bool> UpdateDisplayNameAsync(string uid, string displayName)
    {
        try
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={_apiKey}";
            var payload = new { localId = uid, displayName = displayName };
            var resp = await _http.PostAsJsonAsync(url, payload);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ============================================================
    // Internal
    // ============================================================
    private async Task<FirebaseAuthResult> PostAsync(string url, string email, string password)
    {
        try
        {
            var payload = new { email, password, returnSecureToken = true };
            var resp = await _http.PostAsJsonAsync(url, payload);
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (resp.IsSuccessStatusCode)
            {
                return new FirebaseAuthResult
                {
                    Success = true,
                    Uid = root.GetProperty("localId").GetString(),
                    IdToken = root.TryGetProperty("idToken", out var t) ? t.GetString() : null
                };
            }

            var errMsg = "unknown-error";
            if (root.TryGetProperty("error", out var errEl) &&
                errEl.TryGetProperty("message", out var msgEl))
            {
                errMsg = msgEl.GetString() ?? errMsg;
            }

            return new FirebaseAuthResult { Success = false, Error = errMsg };
        }
        catch (Exception ex)
        {
            return new FirebaseAuthResult { Success = false, Error = "connection-error: " + ex.Message };
        }
    }
}

public class FirebaseAuthResult
{
    public bool Success { get; set; }
    public string? Uid { get; set; }
    public string? IdToken { get; set; }
    public string? Error { get; set; }
    public bool Recovered { get; set; }
}