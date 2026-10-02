using BuildingManagementMvc.Services;
using BuildingManagementMvc.Resources;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

// ✅ إعدادات QuestPDF
QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.UseEnvironmentFonts = false;

var builder = WebApplication.CreateBuilder(args);

// ✅ الـ cookies لازم Secure في الإنتاج (محلياً على http بنسمح بـ SameAsRequest)
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

// MVC + Localization
builder.Services.AddControllersWithViews(options =>
    {
        // ✅ أي POST/PUT/DELETE لازم يعدّي anti-forgery تلقائياً (إلا لو action عامل [IgnoreAntiforgeryToken] زي Presence)
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        // رسائل الـ validation (ErrorMessage = "Key") بتتقرا من Resources/Shared.*.resx عن طريق Loc
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            new BuildingManagementMvc.Services.LocStringLocalizer();
    });

// Firebase / Firestore
builder.Services.AddSingleton<FirestoreContext>();
builder.Services.AddSingleton<BuildingsService>();
builder.Services.AddSingleton<UsersService>();
builder.Services.AddHttpClient<FirebaseAuthRestService>();
builder.Services.AddScoped<AuthService>();
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
builder.Services.AddSingleton<FirebaseAdminService>();
builder.Services.AddScoped<UnifiedAuthService>();
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

// ✅ Phase 20 — الإعدادات  
builder.Services.AddScoped<SettingsService>();

// ✅ Phase 24 — Presence
builder.Services.AddSingleton<PresenceService>();
builder.Services.AddScoped<BuildingMapService>();

// ✅ Phase 19 — Backup
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<ScheduledBackupService>();
builder.Services.AddHostedService<BackupScheduler>();

// ✅ Phase 18 — الأدوات والمزامنة
builder.Services.AddScoped<MigrationService>();
builder.Services.AddScoped<DbMaintenanceService>();
builder.Services.AddScoped<DbSyncService>();

// ✅ Phase 17 — السجل
builder.Services.AddScoped<AuditLogQueryService>();

// ✅ Phase 16 — الفئات المالية
builder.Services.AddScoped<CategoriesService>();

// ✅ Phase 15 — إدارة الأدمنة 
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();
builder.Services.AddSingleton<AuditLogger>();
builder.Services.AddScoped<AdminManagementService>();

// ✅ Memory Cache 
builder.Services.AddMemoryCache();

// ✅ Localization (معدّل)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

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
    options.RequestCultureProviders.Add(
        new CookieRequestCultureProvider
        {
            CookieName = CookieRequestCultureProvider.DefaultCookieName
        });
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

// Cookie authentication
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

        // ✅ مراجعة الجلسة: أدمن اتعطّل/اتحذف، أو سوبر أدمن اتشال من الـ config => الجلسة تبطل،
        // وصلاحيات الأدمن بتتحدّث من غير login جديد (النتيجة بتتخزّن دقيقتين).
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async ctx =>
            {
                var revalidator = ctx.HttpContext.RequestServices.GetRequiredService<SessionRevalidator>();
                var result = await revalidator.ValidateAsync(ctx.Principal!);

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
                        foreach (var p in fresh) updated.AddClaim(new Claim("perm", p));

                        ctx.ReplacePrincipal(new ClaimsPrincipal(updated));
                        ctx.ShouldRenew = true;
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ✅ ورا Azure App Service لازم نقرأ الـ IP الحقيقي (X-Forwarded-For) عشان الـ LoginThrottle
// وعشان HTTPS redirect يشتغل صح. App Service هو الـ proxy الوحيد قدام التطبيق.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ✅ يفشل التشغيل بدري لو أي سر ناقص (بدل ما يشتغل بمفتاح افتراضي)
ConfigSecrets.Require(app.Configuration, "Firebase:ProjectId");
ConfigSecrets.Require(app.Configuration, "Firebase:WebApiKey");
ConfigSecrets.Require(app.Configuration, "Excel:SecretKey");
ConfigSecrets.Require(app.Configuration, "Qr:SecretKey", "Excel:SecretKey");
AuthService.ConfigureSuperAdmins(ConfigSecrets.Require(app.Configuration, "Auth:SuperAdminEmails"));

app.UseForwardedHeaders();

// ✅ سجّل خطوط Cairo
var fontsDir = Path.Combine(app.Environment.WebRootPath, "fonts");
Console.WriteLine("====================================================");
Console.WriteLine($"[Startup] Fonts dir: {fontsDir}");
Console.WriteLine($"[Startup] Dir exists: {Directory.Exists(fontsDir)}");

if (Directory.Exists(fontsDir))
{
    var fontFiles = Directory.GetFiles(fontsDir, "*.ttf");
    Console.WriteLine($"[Startup] Found {fontFiles.Length} font files");

    foreach (var fontFile in fontFiles)
    {
        try
        {
            using var stream = File.OpenRead(fontFile);
            FontManager.RegisterFont(stream);
            Console.WriteLine($"[Startup] Registered: {Path.GetFileName(fontFile)}");
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
Console.WriteLine("====================================================");

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();