using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class TokenGateway : ITokenGateway
{
    private readonly ITokenSource _tokenSource;

    public TokenGateway(ITokenSource tokenSource)
    {
        _tokenSource = tokenSource;
    }

    public TokenGerado GerarToken(string username, string role)
    {
        return _tokenSource.GerarToken(username, role);
    }
}
