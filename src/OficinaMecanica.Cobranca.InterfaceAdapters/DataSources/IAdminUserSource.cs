namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IAdminUserSource
{
    bool CredenciaisValidas(string username, string password);
}
