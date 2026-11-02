using OficinaMecanica.Cobranca.Application.Inputs.Auth;
namespace OficinaMecanica.Cobranca.Application.Outputs.Auth;

public class LoginOutput
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
