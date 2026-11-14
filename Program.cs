using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;
using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.DataProtection; // ✅ لحفظ المفاتيح
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.UseEnvironmentFonts = false;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 10000;
    options.KeyLengthLimit = 4096;
    options.ValueLengthLimit = 4 * 1024 * 1024;
    // الافتراضي 10MB — الأكشنز اللي محتاجة أكبر (استعادة النسخ الاحتياطي) عليها [RequestFormLimits] خاص بيها
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
    options.MultipartHeadersCountLimit = 32;
    options.MultipartHeadersLengthLimit = 32 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    // حدود معقولة على مستوى التطبيق. الرفع الكبير (نسخ احتياطي / Excel / صور الإيصالات)
    // له [RequestSizeLimit] مخصوص على الأكشن نفسه.
    options.Limits.MaxRequestHeadersTotalSize = 64 * 1024;
    options.Limits.MaxRequestLineSize = 16 * 1024;
    options.Limits.MaxRequestBodySize = 5 * 1024 * 1024;
    options.Limits.MaxRequestBufferSize = 4 * 1024 * 1024;
});

builder.Services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = 5 * 1024 * 1024);

// ✅ Rate limiting على طلبات الدخول (POST): 30 طلب/دقيقة لكل IP
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // endpoints الاستعلام المفتوحة (أدوار/شقق في صفحة الدخول): 120 طلب/دقيقة لكل IP
    o.AddPolicy("lookup", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    o.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new BuildingManagementMvc.Services.BuildingWriteLockFilter());
})
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            new BuildingManagementMvc.Services.LocStringLocalizer();
    });

// ============================================================
// ✅ SQL Server فقط
// ============================================================
var sqlConn = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(sqlConn))
    throw new InvalidOperationException("ConnectionStrings:Default مطلوب.");

builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlServer(sqlConn, sqlOptions =>
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

Console.WriteLine("[Storage] Provider = Sql");

// ============================================================
// Services
// ============================================================
builder.Services.AddSingleton<BuildingSqlStore>();
builder.Services.AddSingleton<UserSqlStore>();
builder.Services.AddSingleton<ResidentSqlStore>();

builder.Services.AddSingleton<BuildingsService>();
builder.Services.AddSingleton<UsersService>();
builder.Services.AddScoped<SqlAuthService>();
builder.Services.AddSingleton<WalletService>();
builder.Services.AddSingleton<ExpensesService>();
builder.Services.AddSingleton<RevenuesService>();
builder.Services.AddSingleton<AuditLogService>();
builder.Services.AddSingleton<ReportsService>();
builder.Services.AddSingleton<InvoiceService>();
builder.Services.AddSingleton<InvoicePdfService>();
builder.Services.AddSingleton<OcrService>();
builder.Services.AddSingleton<TransactionParser>();
builder.Services.AddSingleton<CloudinaryService>();
builder.Services.AddSingleton<ExcelTemplateService>();
builder.Services.AddSingleton<ExcelImportService>();
builder.Services.AddSingleton<QrSecurityService>();
builder.Services.AddSingleton<QrAuthService>();
builder.Services.AddSingleton<LoginThrottle>();
builder.Services.AddSingleton<SessionRevalidator>();
builder.Services.AddSingleton<QrCodePdfService>();
builder.Services.AddSingleton<QrGeneratorService>();
builder.Services.AddSingleton<QrTokenStoreService>();
builder.Services.AddSingleton<PollsService>();
builder.Services.AddSingleton<MaintenanceService>();
builder.Services.AddSingleton<NotificationsService>();
builder.Services.AddSingleton<WhatsAppTemplateService>();
builder.Services.AddScoped<ImpersonationService>();

builder.Services.AddScoped<SettingsService>();

builder.Services.AddSingleton<PresenceService>();
builder.Services.AddScoped<BuildingMapService>();

builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<ScheduledBackupService>();
builder.Services.AddHostedService<BackupScheduler>();

builder.Services.AddScoped<AuditLogQueryService>();
builder.Services.AddScoped<CategoriesService>();

builder.Services.AddSingleton<IAuditLogger, AuditLogger>();
builder.Services.AddSingleton<AuditLogger>();
builder.Services.AddScoped<AdminManagementService>();

builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
builder.Services.AddSingleton<DvrService>();
builder.Services.AddHttpClient<MediaMtxService>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<ResidentsService>();
builder.Services.AddScoped<PinMigrationService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "bm_session";
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

builder.Services.AddMemoryCache();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// ============================================================
// ✅ إعدادات اللغة
// ============================================================
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("ar"),
        new CultureInfo("en"),
        new CultureInfo("fr"),
        new CultureInfo("de")
    };

    options.DefaultRequestCulture = new RequestCulture("ar");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.RequestCultureProviders.Clear();

    options.RequestCultureProviders.Add(new CookieRequestCultureProvider
    {
        CookieName = CookieRequestCultureProvider.DefaultCookieName
    });

    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

// ============================================================
// ✅ Authentication (بدون Google)
// ============================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/LoginChoice";
        options.AccessDeniedPath = "/Account/LoginChoice";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "bm_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async ctx =>
            {
                var revalidator = ctx.HttpContext.RequestServices.GetRequiredService<SessionRevalidator>();

                SessionRevalidator.Result result;
                try
                {
                    result = await revalidator.ValidateAsync(ctx.Principal!);
                }
                catch (Exception ex)
                {
                    var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "[SessionRevalidator] Failed to validate principal");
                    return;
                }

                if (!result.Valid)
                {
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                if (result.Permissions != null)
                {
                    var current = ctx.Principal!.FindAll("perm").Select(c => c.Value).OrderBy(x => x);
                    var fresh = result.Permissions.Distinct().OrderBy(x => x);

                    if (!current.SequenceEqual(fresh))
                    {
                        var old = ctx.Principal!.Identities.First();
                        var updated = new ClaimsIdentity(
                            old.Claims.Where(c => c.Type != "perm"),
                            old.AuthenticationType, old.NameClaimType, old.RoleClaimType);

                        foreach (var p in fresh)
                            updated.AddClaim(new Claim("perm", p));

                        ctx.ReplacePrincipal(new ClaimsPrincipal(updated));
                        ctx.ShouldRenew = true;
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<Microsoft.AspNetCore.Mvc.CookieTempDataProviderOptions>(options =>
{
    options.Cookie.Name = "bm_tempdata";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ✅ Forwarded Headers
// ✅ لا نثق في X-Forwarded-For إلا من بروكسي معروف (Proxy:KnownProxies = "ip1,ip2").
// من غير إعداد: بنقبل X-Forwarded-Proto بس (للـ HTTPS) وبنتجاهل X-Forwarded-For/Host
// عشان محدّش يزوّر الـ IP ويلف حوالين حماية تخمين الـ PIN.
var knownProxies = (builder.Configuration["Proxy:KnownProxies"] ?? "")
    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    if (knownProxies.Length > 0)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                 | ForwardedHeaders.XForwardedProto
                                 | ForwardedHeaders.XForwardedHost;
        foreach (var p in knownProxies)
            if (System.Net.IPAddress.TryParse(p, out var ip)) options.KnownProxies.Add(ip);
    }
    else
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    }
});

// ✅ حماية المفاتيح (عشان السيرفر ميعملش Logout عشوائي)
// DataProtection:KeysPath = مسار دائم خارج فولدر النشر (عشان النشر مايمسحش المفاتيح ويعمل Logout للكل)
var dpKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dpKeysPath))
    dpKeysPath = Path.Combine(builder.Environment.ContentRootPath, "keys");

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetApplicationName("BuildingManagement");

// ✅ مفاتيح الـ DataProtection بتتشفّر على القرص (AES-GCM بمفتاح مشتق من Encryption:SecretKey)
var encryptionSecret = ConfigSecrets.Require(builder.Configuration, "Encryption:SecretKey");
var dpKeyHolder = new DataProtectionKeyHolder(encryptionSecret);
builder.Services.AddSingleton(dpKeyHolder);
builder.Services.Configure<KeyManagementOptions>(o => o.XmlEncryptor = new AesGcmXmlEncryptor(dpKeyHolder.Key));

var app = builder.Build();

// ============================================================
// التحقق من الأسرار
// ============================================================
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("=== Configuration Sources ===");
    if (app.Configuration is IConfigurationRoot root)
    {
        foreach (var source in root.Providers)
            Console.WriteLine($"  - {source}");
    }
    var _pidclo = app.Configuration["Cloudinary:CloudName"];
    var _keycol = app.Configuration["Cloudinary:ApiKey"];
    var _pthcol = app.Configuration["Cloudinary:ApiSecret"];
    var _xl = app.Configuration["Excel:SecretKey"];
    var _qr = app.Configuration["Qr:SecretKey"];
    var _enc = app.Configuration["Encryption:SecretKey"];
    var _adm = app.Configuration["Auth:SuperAdminEmails"];
    Console.WriteLine("==============================");
    Console.WriteLine($"  Cloudinary:CloudName            = {_pidclo ?? "NULL"}");
    Console.WriteLine($"  Cloudinary:ApiKey               = {(_keycol != null ? $"loaded ({_keycol.Length} chars)" : "NULL")}");
    Console.WriteLine($"  Cloudinary:ApiSecret            = {(_pthcol != null ? $"loaded ({_pthcol.Length} chars)" : "NULL")}");
    Console.WriteLine("==============================");
    Console.WriteLine($"  Excel:SecretKey                 = {(_xl != null ? $"loaded ({_xl.Length} chars)" : "NULL")}");
    Console.WriteLine($"  Qr:SecretKey                    = {(_qr != null ? $"loaded ({_qr.Length} chars)" : "NULL")}");
    Console.WriteLine($"  Encryption:SecretKey            = {(_enc != null ? $"loaded ({_enc.Length} chars)" : "NULL")}");
    Console.WriteLine("==============================");
    Console.WriteLine($"  Auth:SuperAdminEmails           = {(_adm != null ? "loaded" : "NULL")}");
    Console.WriteLine("==============================");
}

ConfigSecrets.Require(app.Configuration, "Cloudinary:CloudName");
ConfigSecrets.Require(app.Configuration, "Cloudinary:ApiKey");
ConfigSecrets.Require(app.Configuration, "Cloudinary:ApiSecret");
ConfigSecrets.Require(app.Configuration, "Excel:SecretKey");
ConfigSecrets.Require(app.Configuration, "Qr:SecretKey");
ConfigSecrets.Require(app.Configuration, "Encryption:SecretKey");

var superAdminEmails = ConfigSecrets.Require(app.Configuration, "Auth:SuperAdminEmails");
SqlAuthService.ConfigureSuperAdmins(superAdminEmails);

// مفتاح طوارئ مؤقت فقط: Auth:AllowLegacyPlainPins=true يسمح بمقارنة PIN نص صريح قديم
SecretVerifier.AllowLegacyPlain = app.Configuration.GetValue<bool>("Auth:AllowLegacyPlainPins");

// تشفير ملفات النسخ الاحتياطي المخزنة على السيرفر
BackupFileProtector.Initialize(encryptionSecret);

// تحويل أي PIN نص صريح لـ BCrypt (مرة واحدة عند الإقلاع، وبعدها مفيش حاجة تتعمل)
try
{
    using var migrationScope = app.Services.CreateScope();
    var pinMigration = migrationScope.ServiceProvider.GetRequiredService<PinMigrationService>();
    var (mUsers, mResidents, mCleared) = await pinMigration.MigrateLegacySecretsAsync();
    Console.WriteLine($"[Startup] Legacy secrets: users={mUsers}, residents={mResidents}, buildingAdminPinsCleared={mCleared}");
}
catch (Exception ex)
{
    Console.WriteLine($"[Startup] Legacy secrets migration FAILED: {ex.Message}");
}

// حالات قفل تسجيل الدخول بتفضل بعد أي restart
await app.Services.GetRequiredService<LoginThrottle>().InitializeAsync();

// ============================================================
// Middleware
// ============================================================
app.UseForwardedHeaders();

// Security:CspMode = enforce (الافتراضي) | report-only | off  — مفتاح طوارئ لو صفحة اتكسرت بسبب الـ CSP
var cspMode = (app.Configuration["Security:CspMode"] ?? "enforce").Trim().ToLowerInvariant();
const string CspPolicy =
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
    "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
    "img-src 'self' data: blob: https:; " +
    "media-src 'self' blob: https: http:; " +
    "connect-src 'self' https: http: ws: wss:; " +
    "frame-src 'self' https: http:; " +
    "worker-src 'self' blob:; " +
    "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'self'";

// ✅ Security headers (CSP الكامل محتاج nonces للـ inline scripts — دلوقتي بنفرض الأجزاء الآمنة بس)
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "SAMEORIGIN";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=()";
    h["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
    h["X-Permitted-Cross-Domain-Policies"] = "none";
    // صفحات الدخول وتعيين الـ PIN وصور الـ QR (فيها توكنات) مايتخزّنوش في الكاش
    if (ctx.Request.Path.StartsWithSegments("/Account") || ctx.Request.Path.StartsWithSegments("/QrImage"))
    {
        // بنحطه وقت بدء الرد ولو مفيش Cache-Control (الـ antiforgery بيحط no-store بنفسه
        // في الصفحات اللي فيها فورم، ولو حطّيناه قبله بيطلع تحذير في اللوج)
        ctx.Response.OnStarting(() =>
        {
            if (!ctx.Response.Headers.ContainsKey("Cache-Control"))
                ctx.Response.Headers["Cache-Control"] = "no-store";
            return Task.CompletedTask;
        });
    }
    if (cspMode != "off")
    {
        var cspHeader = cspMode == "report-only"
            ? "Content-Security-Policy-Report-Only"
            : "Content-Security-Policy";
        h[cspHeader] = CspPolicy;
    }
    await next();
});

var fontsDir = Path.Combine(app.Environment.WebRootPath, "fonts");

if (Directory.Exists(fontsDir))
{
    var fontFiles = Directory.GetFiles(fontsDir, "*.ttf");
    foreach (var fontFile in fontFiles)
    {
        try
        {
            using var stream = File.OpenRead(fontFile);
            FontManager.RegisterFont(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Startup] FAILED {Path.GetFileName(fontFile)}: {ex.Message}");
        }
    }
}
else
{
    Console.WriteLine("[Startup] ERROR: Fonts directory not found!");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseRouting();

app.UseRateLimiter();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();