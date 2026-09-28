using BuildingManagementMvc.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingManagementMvc.Services;

// Helper للـ Pagination + بناء الروابط
public static class PaginationService
{
    // ✅ قراءة الـ Pagination من الـ Query String
    public static PaginationRequest ReadFromQuery(IQueryCollection query)
    {
        var req = new PaginationRequest();

        if (query.TryGetValue("page", out var page) && int.TryParse(page, out var p))
            req.Page = p;

        if (query.TryGetValue("pageSize", out var ps) && int.TryParse(ps, out var pz))
            req.PageSize = pz;

        if (query.TryGetValue("sortBy", out var sb))
            req.SortBy = sb;

        if (query.TryGetValue("sortDir", out var sd))
            req.SortDir = sd;

        if (query.TryGetValue("search", out var s))
            req.Search = s;

        req.Normalize();
        return req;
    }

    // ✅ بناء QueryString مع override
    public static string BuildQuery(
        IQueryCollection query,
        Dictionary<string, string?> overrides)
    {
        var parts = new List<string>();

        // 1. نبني من الـ query الحالية (مع تجاهل اللي هنغيّره)
        foreach (var kv in query)
        {
            if (overrides.ContainsKey(kv.Key)) continue;
            if (string.IsNullOrWhiteSpace(kv.Value)) continue;
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString())}");
        }

        // 2. نضيف الـ overrides
        foreach (var kv in overrides)
        {
            if (string.IsNullOrWhiteSpace(kv.Value)) continue;
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
        }

        return parts.Count > 0 ? "?" + string.Join("&", parts) : "";
    }
}