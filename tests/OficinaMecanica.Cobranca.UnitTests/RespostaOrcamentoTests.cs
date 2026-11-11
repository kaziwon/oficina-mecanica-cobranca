using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.UnitTests.Fakes;

namespace OficinaMecanica.Cobranca.UnitTests;

public class RespostaOrcamentoTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RespostaVinculada_EmiteUmaVezEPreservaContexto(bool aprovado)
    {
        var orcamento = Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Diagnostico",
            [new ItemOrcamento(TipoItemOrcamento.Servico, Guid.NewGuid(), "Revisao", 1, 100m)]);
        var saga = Guid.NewGuid(); var pedido = Guid.NewGuid();
        orcamento.VincularSaga(saga, pedido, "correlacao-original");
        var gateway = new OrcamentoGatewayFake(); gateway.Orcamentos.Add(orcamento);
        for (var i = 0; i < 2; i++)
        {
            if (aprovado) new AprovarOrcamentoUseCase(gateway).Executar(orcamento.Id);
            else new RecusarOrcamentoUseCase(gateway).Executar(orcamento.Id, new RecusarOrcamentoInput("Valor alto"));
        }
        Assert.Equal(1, gateway.Gravacoes);
        Assert.Equal(aprovado ? "OrcamentoAprovado" : "OrcamentoRecusado", gateway.UltimoEvento!.Type);
        Assert.Equal(saga, gateway.UltimoEvento.SagaId);
        Assert.Equal(pedido, gateway.UltimoEvento.CausationId);
        Assert.Equal("correlacao-original", gateway.UltimoEvento.CorrelationId);
        Assert.Equal(100m, gateway.UltimoEvento.Payload.ValorTotal);
        Assert.Null(orcamento.Pagamento);
    }
}
