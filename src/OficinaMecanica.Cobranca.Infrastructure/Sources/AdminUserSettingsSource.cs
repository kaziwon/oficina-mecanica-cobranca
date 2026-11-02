using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Infrastructure.Settings;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.Infrastructure.Sources;

public class AdminUserSettingsSource : IAdminUserSource
{
    private readonly AdminUserSettings _adminUserSettings;

    public AdminUserSettingsSource(IOptions<AdminUserSettings> adminUserSettings)
    {
        _adminUserSettings = adminUserSettings.Value;
    }

    public bool CredenciaisValidas(string username, string password)
    {
        return username == _adminUserSettings.Username && password == _adminUserSettings.Password;
    }
}
