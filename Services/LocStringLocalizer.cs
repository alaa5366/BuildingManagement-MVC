using Microsoft.Extensions.Localization;

namespace BuildingManagementMvc.Services;

// Adapter بسيط بيخلّي DataAnnotations (ErrorMessage = "Key") والـ _Layout يقروا من نفس ملفات Shared.*.resx
public class LocStringLocalizer : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            var v = Loc.T(name);
            return new LocalizedString(name, v, resourceNotFound: v == name);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var v = Loc.T(name, arguments);
            return new LocalizedString(name, v, resourceNotFound: v == name);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        Enumerable.Empty<LocalizedString>();
}
