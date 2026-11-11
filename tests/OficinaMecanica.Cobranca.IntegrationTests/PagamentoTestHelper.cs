using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
using OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public static class PagamentoTestHelper
{
    public static MensagemIntegracao CriarPedido(CobrancaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var gerar = new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = "GerarOrcamento",
            SagaId = Guid.NewGuid(),
            CausationId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "pagamento-teste",
            Payload = new DadosMensagem
            {
                OrdemServicoId = Guid.NewGuid(),
                ExecucaoId = Guid.NewGuid(),
                Diagnostico = "Revisao",
                Itens = [new ItemMensagem { Tipo = "Servico", ReferenciaId = Guid.NewGuid(), Nome = "Revisao", Quantidade = 1, PrecoUnitario = 100m }]
            }
        };
        scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(gerar);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var orcamento = db.Orcamentos.Single();
        scope.ServiceProvider.GetRequiredService<AprovarOrcamentoUseCase>().Executar(orcamento.Id);
        var aprovacao = new EfOutboxDataSource(db).ObterPendentes(10).Single(item => item.Type == "OrcamentoAprovado");
        return new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = "SolicitarPagamento",
            SagaId = gerar.SagaId,
            CausationId = aprovacao.MessageId,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = gerar.CorrelationId,
            Payload = new DadosMensagem
            {
                OrdemServicoId = orcamento.OrdemServicoId,
                ExecucaoId = orcamento.ExecucaoId,
                OrcamentoId = orcamento.Id,
                ValorTotal = 100m,
                Moeda = "BRL"
            }
        };
    }

    public static void Processar(CobrancaApiFactory factory, MensagemIntegracao mensagem)
    {
        using var scope = factory.Services.CreateScope(); scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(mensagem);
    }
}
