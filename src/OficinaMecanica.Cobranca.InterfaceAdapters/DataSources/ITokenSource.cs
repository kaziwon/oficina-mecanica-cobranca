using OficinaMecanica.Cobranca.Application.Gateways;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface ITokenSource
{
    TokenGerado GerarToken(string username, string role);
}
