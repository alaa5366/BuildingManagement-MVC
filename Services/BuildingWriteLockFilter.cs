using BuildingManagementMvc.Data;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BuildingManagementMvc.Services;

// بيسلسل كل طلبات الكتابة (POST/PUT/PATCH/DELETE) على نفس العمارة.
// السبب: BuildingsService.SaveFullAsync بيكتب العمارة كلها (آخر كاتب يكسب)،
// فلو ساكن سجّل دفعة وأدمن أكّد دفعة في نفس اللحظة واحدة منهم كانت بتضيع.
// ملاحظة: القفل داخل نفس العملية (instance واحدة). لو شغّلت أكتر من instance
// محتاج RowVersion في الـ DB (مرحلة لاحقة بتحتاج Migration).
public sealed class BuildingWriteLockFilter : IAsyncActionFilter
{
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
