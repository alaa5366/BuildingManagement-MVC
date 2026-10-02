using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace BuildingManagementMvc.Services;

public class FirebaseAdminService
{
    private readonly ILogger<FirebaseAdminService> _logger;

    public FirebaseAdminService(IConfiguration config, IWebHostEnvironment env, ILogger<FirebaseAdminService> logger)
    {
        _logger = logger;

        if (FirebaseApp.DefaultInstance == null)
        {
            var json = FirebaseCredentials.TryLoadJson(config, env.ContentRootPath);

            if (json == null)
            {
                _logger.LogError("[FirebaseAdmin] Service account credentials not found " +
                                 "(set Firebase:ServiceAccountJson or provide firebase-service-account.json)");
                return;
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(json)
            });

            _logger.LogInformation("[FirebaseAdmin] Initialized");
        }
    }

    // ============================================================
    // التحقق من Firebase ID Token (للدخول بجوجل)
    // ============================================================
    public async Task<VerifiedFirebaseUser?> VerifyIdTokenAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken) || FirebaseApp.DefaultInstance == null)
            return null;

        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

            var email = decoded.Claims.TryGetValue("email", out var e) ? e?.ToString() : null;
            var emailVerified = decoded.Claims.TryGetValue("email_verified", out var ev) && ev is bool b && b;

            if (string.IsNullOrWhiteSpace(email)) return null;
            return new VerifiedFirebaseUser(decoded.Uid, email, emailVerified);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FirebaseAdmin] ID token verification failed");
            return null;
        }
    }

    // ============================================================
    // إنشاء أو جلب مستخدم
    // ============================================================
    public async Task<string?> CreateOrGetUserAsync(string email, string password)
    {
        try
        {
            try
            {
                var existing = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
                if (existing != null)
                {
                    _logger.LogInformation($"[FirebaseAdmin] User exists: {email}");
                    return existing.Uid;
                }
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
            {
                // مش موجود — نكمّل
            }

            var args = new UserRecordArgs
            {
                Email = email,
                Password = password,
                EmailVerified = true
            };

            var user = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
            _logger.LogInformation($"[FirebaseAdmin] Created: {email}");
            return user.Uid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[FirebaseAdmin] CreateUser failed: {email}");
            return null;
        }
    }

    // ============================================================
    // ✅ جديد: جلب UID بالإيميل
    // ============================================================
    public async Task<string?> GetUidByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        try
        {
            var user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
            return user?.Uid;
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[FirebaseAdmin] GetUidByEmail failed: {email}");
            return null;
        }
    }

    // ============================================================
    // تحديث كلمة السر
    // ============================================================
    public async Task<bool> UpdatePasswordAsync(string email, string newPassword)
    {
        try
        {
            var user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
            if (user == null) return false;

            await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            {
                Uid = user.Uid,
                Password = newPassword
            });

            _logger.LogInformation($"[FirebaseAdmin] Password updated: {email}");
            return true;
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[FirebaseAdmin] UpdatePassword failed: {email}");
            return false;
        }
    }

    // ============================================================
    // حذف مستخدم
    // ============================================================
    public async Task<bool> DeleteUserAsync(string email)
    {
        try
        {
            var user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
            if (user == null) return false;

            await FirebaseAuth.DefaultInstance.DeleteUserAsync(user.Uid);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public record VerifiedFirebaseUser(string Uid, string Email, bool EmailVerified);
