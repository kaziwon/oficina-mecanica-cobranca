using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Sources;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class PersistenciaTests
{
    [Fact]
    public void RespostasConcorrentes_NaoSobrescrevemStatusNemHistorico()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        using var primeiroScope = factory.Services.CreateScope();
        using var segundoScope = factory.Services.CreateScope();
        var primeiro = primeiroScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var segundo = segundoScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orcamento = NovoOrcamento();
        new EfOrcamentoDataSource(primeiro).Inserir(orcamento);
        var source = new EfOrcamentoDataSource(segundo);
        var copia = source.ObterPorId(orcamento.Id)!;
        orcamento.Aprovar();
        new EfOrcamentoDataSource(primeiro).Salvar(orcamento);
        copia.Recusar("Valor alto");
        Assert.Throws<ConflitoException>(() => source.Salvar(copia));
        primeiro.ChangeTracker.Clear();
        var persistido = new EfOrcamentoDataSource(primeiro).ObterPorId(orcamento.Id)!;
        Assert.Equal(StatusOrcamento.Aprovado, persistido.Status);
        Assert.Equal(2, persistido.Historico.Count);
    }

    [Fact]
    public void PagamentosConcorrentes_NaoGeramDuasCobrancas()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        using var primeiroScope = factory.Services.CreateScope();
        using var segundoScope = factory.Services.CreateScope();
        var primeiro = primeiroScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var segundo = segundoScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orcamento = NovoOrcamento();
        orcamento.Aprovar();
        new EfOrcamentoDataSource(primeiro).Inserir(orcamento);
        var source = new EfOrcamentoDataSource(segundo);
        var copia = source.ObterPorId(orcamento.Id)!;
        orcamento.RegistrarPagamentoPendente();
        new EfOrcamentoDataSource(primeiro).Salvar(orcamento);
        copia.RegistrarPagamentoPendente();
        Assert.Throws<ConflitoException>(() => source.Salvar(copia));
        Assert.Equal(1, primeiro.Pagamentos.Count());
    }

    [Fact]
    public void ModeloMySql_PossuiMigracaoEIndicesUnicos()
    {
        using var database = new AppDbContextFactory().CreateDbContext([]);
        Assert.Equal(3, database.Database.GetMigrations().Count());
        var sql = database.Database.GenerateCreateScript();
        Assert.Contains("decimal(11,2)", sql);
        Assert.Contains("UNIQUE INDEX `IX_Orcamentos_OrdemServicoId`", sql);
        Assert.Contains("UNIQUE INDEX `IX_Pagamentos_OrcamentoId`", sql);
    }

    private static Orcamento NovoOrcamento() => Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Diagnostico",
        [new ItemOrcamento(TipoItemOrcamento.Servico, Guid.NewGuid(), "Servico", 1, 100m)]);
}
