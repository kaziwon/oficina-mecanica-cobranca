namespace OficinaMecanica.Cobranca.Application.Gateways;

public class TokenGerado
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
