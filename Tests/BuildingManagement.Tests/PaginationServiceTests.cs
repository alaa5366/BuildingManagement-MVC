using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace BuildingManagement.Tests;

public class PaginationServiceTests
{
    private static IQueryCollection CreateQuery(Dictionary<string, string> values)
    {
        var dict = values.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value));
        return new QueryCollection(dict);
    }

    // ================================================================
    // ReadFromQuery
    // ================================================================

    [Fact]
    public void ReadFromQuery_ValidParams_ReadsAll()
    {
        var query = CreateQuery(new()
        {
            ["page"] = "3",
            ["pageSize"] = "25",
            ["sortBy"] = "name",
            ["sortDir"] = "asc",
            ["search"] = "test"
        });

        var req = PaginationService.ReadFromQuery(query);

        Assert.Equal(3, req.Page);
        Assert.Equal(25, req.PageSize);
        Assert.Equal("name", req.SortBy);
        Assert.Equal("asc", req.SortDir);
        Assert.Equal("test", req.Search);
    }

    [Fact]
    public void ReadFromQuery_EmptyQuery_UsesDefaults()
    {
        var query = CreateQuery(new());
        var req = PaginationService.ReadFromQuery(query);

        Assert.Equal(1, req.Page);
        Assert.Equal(50, req.PageSize);
        Assert.Equal("desc", req.SortDir);
    }

    [Fact]
    public void ReadFromQuery_InvalidPage_UsesDefault()
    {
        var query = CreateQuery(new() { ["page"] = "invalid" });
        var req = PaginationService.ReadFromQuery(query);
        Assert.Equal(1, req.Page);
    }

    [Fact]
    public void ReadFromQuery_NegativePageSize_UsesDefault()
    {
        var query = CreateQuery(new() { ["pageSize"] = "-5" });
        var req = PaginationService.ReadFromQuery(query);
        Assert.True(req.PageSize >= 1); // Normalize بيظبطها
    }

    // ================================================================
    // BuildQuery
    // ================================================================

    [Fact]
    public void BuildQuery_AddsOverrides()
    {
        var query = CreateQuery(new() { ["page"] = "1", ["search"] = "test" });
        var result = PaginationService.BuildQuery(query, new() { ["page"] = "2" });

        Assert.Contains("page=2", result);
        Assert.Contains("search=test", result);
        Assert.DoesNotContain("page=1", result);
    }

    [Fact]
    public void BuildQuery_EmptyOverrides_ReturnsOriginal()
    {
        var query = CreateQuery(new() { ["page"] = "1" });
        var result = PaginationService.BuildQuery(query, new());

        Assert.Contains("page=1", result);
    }

    [Fact]
    public void BuildQuery_RemovesNullValues()
    {
        var query = CreateQuery(new() { ["page"] = "1" });
        var result = PaginationService.BuildQuery(query, new() { ["filter"] = null });

        Assert.DoesNotContain("filter=", result);
    }

    [Fact]
    public void BuildQuery_EmptyQuery_WithOverrides_ReturnsOnlyOverrides()
    {
        var query = CreateQuery(new());
        var result = PaginationService.BuildQuery(query, new() { ["page"] = "2" });

        Assert.Equal("?page=2", result);
    }

    [Fact]
    public void BuildQuery_EmptyQuery_NoOverrides_ReturnsEmptyString()
    {
        var query = CreateQuery(new());
        var result = PaginationService.BuildQuery(query, new());

        Assert.Equal("", result);
    }
}