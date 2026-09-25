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
            var serviceAccountPath = config["Firebase:ServiceAccountJsonPath"]
                ?? "firebase-service-account.json";
            var fullPath = Path.Combine(env.ContentRootPath, serviceAccountPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogError($"[FirebaseAdmin] Service account not found: {fullPath}");
                return;
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(fullPath)
            });

            _logger.LogInformation($"[FirebaseAdmin] Initialized");
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