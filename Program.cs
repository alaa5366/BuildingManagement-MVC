using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

// ✅ إعدادات QuestPDF
QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.UseEnvironmentFonts = false;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// ✅ Session (لتخزين بيانات الاستيراد بين الطلبات)
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

// ✅ سجّل خطوط Cairo مرة واحدة عند بداية التطبيق
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

app.UseRouting();

// ✅ Session middleware (قبل Authentication)
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();