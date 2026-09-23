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

    public async Task<FirebaseAuthResult> SignInWithPasswordAsync(string email, string password)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
        return await PostAsync(url, email, password);
    }

    // بيستخدم في إنشاء حساب مقيم/أدمن جديد لأول مرة (accounts:signUp
    // بيعمل نفس createUserWithEmailAndPassword في الـ JS SDK).
    public async Task<FirebaseAuthResult> CreateUserAsync(string email, string password)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
        var result = await PostAsync(url, email, password);

        // لو الحساب موجود بالفعل، جرّب تسجيل الدخول بنفس الباسورد
        // (بالظبط زي منطق createResidentAuthAccount / createAdminAuthAccount في الأصل)
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
