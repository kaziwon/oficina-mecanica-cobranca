using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Domain.Exceptions;
using OficinaMecanica.Cobranca.UnitTests.Fakes;

namespace OficinaMecanica.Cobranca.UnitTests;

public class OrcamentoAtendimentoTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Decisao_RepetidaNaoDuplicaHistoricoNemEvento(bool aprovar)
    {
        var (gateway, orcamento, vinculo) = Criar(); var useCase = new ResponderOrcamentoAtendimentoUseCase(gateway);
        var motivo = aprovar ? null : "Valor alto";
        Assert.True(useCase.Executar(orcamento.Id, vinculo, aprovar, motivo)); var data = orcamento.RespondidoEm;
        Assert.True(useCase.Executar(orcamento.Id, vinculo, aprovar, motivo));
        Assert.Equal(1, gateway.Gravacoes); Assert.Equal(2, orcamento.Historico.Count); Assert.Equal(data, orcamento.RespondidoEm);
        Assert.Equal(aprovar ? "OrcamentoAprovado" : "OrcamentoRecusado", gateway.UltimoEvento!.Type);
        Assert.Equal(vinculo.PedidoGeracaoId, gateway.UltimoEvento.CausationId);
        Assert.Null(orcamento.Pagamento);
        Assert.Throws<RegraNegocioException>(() => useCase.Executar(orcamento.Id, vinculo, !aprovar, aprovar ? "Mudei de ideia" : null));
    }
    [Theory]
    [InlineData("os")]
    [InlineData("execucao")]
    [InlineData("saga")]
    [InlineData("pedido")]
    [InlineData("valor")]
    [InlineData("correlacao")]
    public void VinculoDivergente_NaoConsultaOuAlteraOrcamento(string erro)
    {
        var (gateway, orcamento, vinculo) = Criar();
        vinculo = erro switch
        {
            "os" => vinculo with { OrdemServicoId = Guid.NewGuid() },
            "execucao" => vinculo with { ExecucaoId = Guid.NewGuid() },
            "saga" => vinculo with { SagaId = Guid.NewGuid() },
            "pedido" => vinculo with { PedidoGeracaoId = Guid.NewGuid() },
            "valor" => vinculo with { ValorTotal = 1m },
            _ => vinculo with { CorrelationId = "outra" }
        };
        Assert.Throws<ConflitoException>(() => new ObterOrcamentoAtendimentoUseCase(gateway).Executar(orcamento.Id, vinculo));
        Assert.Throws<ConflitoException>(() => new ResponderOrcamentoAtendimentoUseCase(gateway).Executar(orcamento.Id, vinculo, true, null));
        Assert.Equal(0, gateway.Gravacoes); Assert.Single(orcamento.Historico);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\nMotivo")]
    public void RecusaInvalida_NaoPublicaEvento(string? motivo)
    {
        var (gateway, orcamento, vinculo) = Criar();
        Assert.Throws<ValidacaoException>(() => new ResponderOrcamentoAtendimentoUseCase(gateway).Executar(orcamento.Id, vinculo, false, motivo));
        Assert.Equal(0, gateway.Gravacoes);
    }
    private static (OrcamentoGatewayFake, Orcamento, VinculoOrcamentoAtendimentoInput) Criar()
    {
        var gateway = new OrcamentoGatewayFake();
        var orcamento = Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Revisao", [new(TipoItemOrcamento.Servico, Guid.NewGuid(), "Revisao", 1, 200m)]);
        orcamento.VincularSaga(Guid.NewGuid(), Guid.NewGuid(), "orcamento-cliente-teste"); gateway.Orcamentos.Add(orcamento);
        return (gateway, orcamento, new(orcamento.OrdemServicoId, orcamento.ExecucaoId, orcamento.SagaId!.Value,
            orcamento.PedidoGeracaoId!.Value, orcamento.ValorTotal, orcamento.CorrelationId!));
    }
}
