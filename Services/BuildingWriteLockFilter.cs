using BuildingManagementMvc.Data;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BuildingManagementMvc.Services;

// بيسلسل طلبات MVC الكتابية على نفس العمارة داخل نفس الـ process.
// الحماية عبر أكثر من application instance تتم بشكل مستقل عن طريق Buildings.RowVersion
// داخل BuildingSqlMapper، بما يشمل عمليات الحفظ التي تعدّل جداول الأبناء فقط.
/// <summary>
/// Global MVC filter that serializes write requests (POST/PUT/PATCH/DELETE) per building so two writers cannot overwrite each other's changes.
/// </summary>
public sealed class BuildingWriteLockFilter : IAsyncActionFilter
{
    /// <summary>
    /// Holds a per-building lock while a write action runs. The building id comes from the user's claims, or from the <c>buildingId</c> action argument for super admins.
    /// </summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var m = http.Request.Method;

        var isWrite = HttpMethods.IsPost(m) || HttpMethods.IsPut(m) ||
                      HttpMethods.IsPatch(m) || HttpMethods.IsDelete(m);
        if (!isWrite) { await next(); return; }

        var buildingId = http.User.FindFirst("buildingId")?.Value;

        // السوبر أدمن (من غير claim) — ناخد العمارة من باراميتر الأكشن لو موجود
        if (string.IsNullOrEmpty(buildingId) &&
            context.ActionArguments.TryGetValue("buildingId", out var arg))
            buildingId = arg as string;

        if (string.IsNullOrEmpty(buildingId)) { await next(); return; }

        using (await KeyedLock.AcquireAsync("building:" + buildingId, http.RequestAborted))
        {
            await next();
        }
    }
}
