namespace OficinaMecanica.PagamentoMock;

public class MockSettings
{
    public string ResultadoEstorno { get; set; } = "refunded";
    public string ClientId { get; set; } = "oficina-local";
    public string ClientSecret { get; set; } = "segredo-do-mock-somente-local";
    public string Token { get; set; } = "token-ficticio-do-mock-local";
    public string Resultado { get; set; } = "approved";
    public string ConnectionString { get; set; } = "Data Source=pagamentos-mock.db;Default Timeout=5";
}
