using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

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

// Cookie authentication (الجلسة بتتخزن في كوكي بعد التحقق من Firebase)
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
// للعمل على Google App Engine
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
