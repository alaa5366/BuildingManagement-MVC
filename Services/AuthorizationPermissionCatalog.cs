using BuildingManagementMvc.Data.Entities;

namespace BuildingManagementMvc.Services;

/// <summary>Scope types supported by the target authorization model.</summary>
public enum AuthorizationScopeType : byte
{
    System = 0,
    Building = 1,
    Apartment = 2,
    Personal = 3
}

/// <summary>Allow/deny effect stored in role permissions and account overrides.</summary>
public enum AuthorizationEffect : byte
{
    Deny = 0,
    Allow = 1
}

/// <summary>Immutable policy definition for one permission key.</summary>
public sealed record AuthorizationPermissionDefinition(
    string Key,
    IReadOnlySet<AuthorizationScopeType> AllowedGrantScopes,
    bool SystemGrantIncludesChildResources = false,
    bool RequiresFreshMfa = false,
    TimeSpan? MfaFreshnessWindow = null);

/// <summary>Looks up code-owned permission definitions; unknown keys must fail closed.</summary>
public interface IAuthorizationPermissionCatalog
{
    /// <summary>Looks up a permission definition by its stable key.</summary>
    /// <param name="key">Permission key supplied by the protected operation.</param>
    /// <param name="definition">The known definition, if the key is supported.</param>
    /// <returns><see langword="true"/> when the key is known.</returns>
    bool TryGet(string key, out AuthorizationPermissionDefinition definition);
}

/// <summary>
/// Code-owned target permission catalog. Broad or stale legacy permissions are omitted
/// until their action and resource policy are split and approved.
/// </summary>
public sealed class DefaultAuthorizationPermissionCatalog : IAuthorizationPermissionCatalog
{
    private static readonly IReadOnlyDictionary<string, AuthorizationPermissionDefinition> Definitions =
        new Dictionary<string, AuthorizationPermissionDefinition>(StringComparer.Ordinal)
        {
            ["buildings.manage"] = Define("buildings.manage", AuthorizationScopeType.System),
            ["admins.manage"] = Define("admins.manage", AuthorizationScopeType.System, mfa: true),
            ["residents.manage"] = Define("residents.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["apartment.disable"] = Define("apartment.disable", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["dvrs.manage"] = Define("dvrs.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["cameras.view"] = Define("cameras.view", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["cameras.manage"] = Define("cameras.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["reports.view"] = Define("reports.view", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["polls.manage"] = Define("polls.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["maintenance.manage"] = Define("maintenance.manage", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["audit.view.all"] = Define("audit.view.all", AuthorizationScopeType.System, mfa: true),
            ["audit.view.building"] = Define("audit.view.building", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["audit.view.own"] = Define("audit.view.own", AuthorizationScopeType.Personal),
            ["apartment.access.grant.manage"] = Define("apartment.access.grant.manage", AuthorizationScopeType.Building, AuthorizationScopeType.Apartment, mfa: true),
            ["identity.mapping.review"] = Define("identity.mapping.review", AuthorizationScopeType.System, mfa: true),
            ["system.logs.view"] = Define("system.logs.view", AuthorizationScopeType.System, mfa: true),
            ["settings.manage.global"] = Define("settings.manage.global", AuthorizationScopeType.System, mfa: true),
            ["settings.manage.building"] = Define("settings.manage.building", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["settings.manage.own"] = Define("settings.manage.own", AuthorizationScopeType.Personal),
            ["wallet.read"] = Define("wallet.read", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["deposit.read"] = Define("deposit.read", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["deposit.confirm"] = Define("deposit.confirm", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["deposit.cancel"] = Define("deposit.cancel", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["wallet.adjust"] = Define("wallet.adjust", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["expense.read"] = Define("expense.read", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["expense.post"] = Define("expense.post", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["expense.reverse"] = Define("expense.reverse", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["revenue.read"] = Define("revenue.read", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["revenue.post"] = Define("revenue.post", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["revenue.reverse"] = Define("revenue.reverse", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["finance.export"] = Define("finance.export", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["finance.period.close"] = Define("finance.period.close", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["finance.period.reopen"] = Define("finance.period.reopen", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["notification.dispatch"] = Define("notification.dispatch", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true),
            ["notification.self.read"] = Define("notification.self.read", AuthorizationScopeType.Personal),
            ["integration.whatsapp.send"] = Define("integration.whatsapp.send", AuthorizationScopeType.Building),
            ["integration.whatsapp.configure"] = Define("integration.whatsapp.configure", AuthorizationScopeType.System, AuthorizationScopeType.Building, systemChildren: true, mfa: true),
            ["integration.whatsapp.secrets.manage"] = Define("integration.whatsapp.secrets.manage", AuthorizationScopeType.System, mfa: true),
            ["camera.credential.reveal"] = Define("camera.credential.reveal", AuthorizationScopeType.System, mfa: true),
            ["backup.create"] = Define("backup.create", AuthorizationScopeType.System, mfa: true),
            ["backup.download"] = Define("backup.download", AuthorizationScopeType.System, mfa: true),
            ["backup.restore"] = Define("backup.restore", AuthorizationScopeType.System, mfa: true),
            ["backup.delete"] = Define("backup.delete", AuthorizationScopeType.System, mfa: true),
            ["system.maintenance.execute"] = Define("system.maintenance.execute", AuthorizationScopeType.System, mfa: true),
            ["impersonation.start"] = Define("impersonation.start", AuthorizationScopeType.System, mfa: true)
        };

    /// <summary>All code-owned permissions that support a global system-scope grant.</summary>
    public static IReadOnlyCollection<AuthorizationPermissionDefinition> SystemGrantablePermissions =>
        Definitions.Values.Where(item => item.AllowedGrantScopes.Contains(AuthorizationScopeType.System)).ToArray();

    /// <inheritdoc />
    public bool TryGet(string key, out AuthorizationPermissionDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(key) && Definitions.TryGetValue(key, out definition!))
            return true;

        definition = null!;
        return false;
    }

    private static AuthorizationPermissionDefinition Define(
        string key,
        AuthorizationScopeType first,
        AuthorizationScopeType? second = null,
        bool systemChildren = false,
        bool mfa = false) =>
        new(
            key,
            second is null ? new HashSet<AuthorizationScopeType> { first } : new HashSet<AuthorizationScopeType> { first, second.Value },
            systemChildren,
            mfa,
            mfa ? TimeSpan.FromMinutes(10) : null);
}
