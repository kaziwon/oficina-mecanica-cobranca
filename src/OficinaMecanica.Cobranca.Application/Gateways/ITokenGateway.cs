namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface ITokenGateway
{
    TokenGerado GerarToken(string username, string role);
}
