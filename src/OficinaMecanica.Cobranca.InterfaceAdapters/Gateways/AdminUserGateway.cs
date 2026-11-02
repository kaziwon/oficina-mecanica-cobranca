using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class AdminUserGateway : IAdminUserGateway
{
    private readonly IAdminUserSource _adminUserSource;

    public AdminUserGateway(IAdminUserSource adminUserSource)
    {
        _adminUserSource = adminUserSource;
    }

    public bool CredenciaisValidas(string username, string password)
    {
        return _adminUserSource.CredenciaisValidas(username, password);
    }
}
