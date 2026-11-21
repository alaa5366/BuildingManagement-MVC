using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class AdministratorMfaPolicyTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("superadmin")]
    public void Privileged_administrator_roles_require_mfa(string role)
    {
        Assert.True(AdministratorMfaPolicy.RequiresMfa(role));
    }

    [Theory]
    [InlineData("resident")]
    [InlineData("building-visitor")]
    [InlineData(null)]
    public void Non_administrator_roles_do_not_require_mfa(string? role)
    {
        Assert.False(AdministratorMfaPolicy.RequiresMfa(role));
    }
}
