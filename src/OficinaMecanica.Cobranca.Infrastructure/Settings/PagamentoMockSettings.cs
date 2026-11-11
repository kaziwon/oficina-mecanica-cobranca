namespace OficinaMecanica.Cobranca.Infrastructure.Settings;

public class PagamentoMockSettings
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public int MaxTentativas { get; set; } = 5;
    public int IntervaloRetentativaSegundos { get; set; } = 10;
}
