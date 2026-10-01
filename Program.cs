using BuildingManagementMvc.Services;
using BuildingManagementMvc.Resources;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

// ✅ إعدادات QuestPDF
QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.UseEnvironmentFonts = false;

var builder = WebApplication.CreateBuilder(args);

// MVC + Localization
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    });

// ✅ Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "bm_session";
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
        new CultureInfo("en")
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
    });

builder.Services.AddAuthorization();

var app = builder.Build();

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
}

app.UseStaticFiles();

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();