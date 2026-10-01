using System.Globalization;
using System.Resources;

namespace BuildingManagementMvc.Services
{
    public class LocalizationService
    {
        private static ResourceManager? _resourceManager;
        private static ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    _resourceManager = new ResourceManager(
                        "BuildingManagementMvc.Resources.Shared",
                        typeof(LocalizationService).Assembly);
                }
                return _resourceManager;
            }
        }

        public static string Get(string key)
        {
            try
            {
                var culture = CultureInfo.CurrentUICulture;
                var value = ResourceManager.GetString(key, culture);
                return value ?? key;
            }
            catch
            {
                return key;
            }
        }
    }
}