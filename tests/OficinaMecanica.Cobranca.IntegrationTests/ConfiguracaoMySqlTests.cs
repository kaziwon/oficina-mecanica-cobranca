namespace OficinaMecanica.Cobranca.IntegrationTests;

public class ConfiguracaoMySqlTests
{
    [Fact]
    public void NomeBancoTeste_MantemGuidCompletoELockDaMigracaoDentroDoLimite()
    {
        var primeiro = CobrancaApiFactory.CriarNomeBancoTeste();
        var segundo = CobrancaApiFactory.CriarNomeBancoTeste();

        Assert.StartsWith("cob_test_", primeiro);
        Assert.True(Guid.TryParseExact(primeiro["cob_test_".Length..], "N", out _));
        Assert.NotEqual(primeiro, segundo);
        Assert.InRange($"__{primeiro}_EFMigrationsLock".Length, 1, 64);
        Assert.InRange($"__{segundo}_EFMigrationsLock".Length, 1, 64);
    }
}
