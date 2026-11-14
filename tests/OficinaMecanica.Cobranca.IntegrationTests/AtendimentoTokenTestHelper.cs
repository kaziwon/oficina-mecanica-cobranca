using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public static class AtendimentoTokenTestHelper
{
    public static string Gerar(string erro = "")
    {
        var agora = DateTime.UtcNow;
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            erro == "issuer" ? "outro" : "OficinaMecanica.Ordens.Integracao",
            erro == "audience" ? "OficinaMecanica" : "OficinaMecanica.Cobranca.Orcamentos",
            [new Claim("sub", erro == "sub" ? "cliente" : "ordens"), new Claim("role", erro == "role" ? "Admin" : "OrcamentosCliente")],
            agora.AddHours(-1), erro == "expirado" ? agora.AddMinutes(-1) : agora.AddMinutes(2),
            erro == "sem-assinatura" ? null : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                erro == "assinatura" ? AtendimentoApiFactory.Secret + "errada" : AtendimentoApiFactory.Secret)), SecurityAlgorithms.HmacSha256)));
    }
}
