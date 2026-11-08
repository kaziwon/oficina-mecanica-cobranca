using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.UnitTests.Fakes;

namespace OficinaMecanica.Cobranca.UnitTests;

public class PedidoOrcamentoTests
{
    [Fact]
    public void Sucesso_PreservaCausaECalculaValorNoDominio()
    {
        var gateway = new OrcamentoIntegracaoGatewayFake(); var pedido = Pedido();
        new ProcessarPedidoOrcamentoUseCase(gateway).Executar(pedido);
        Assert.Equal(300m, gateway.Resposta!.Payload.ValorTotal);
        Assert.Equal(pedido.MessageId, gateway.Resposta.CausationId);
        Assert.Equal(pedido.SagaId, gateway.Orcamento!.SagaId);
    }

    [Fact]
    public void PedidoJaProcessado_NaoGravaNovamente()
    {
        var gateway = new OrcamentoIntegracaoGatewayFake { Processada = true };
        new ProcessarPedidoOrcamentoUseCase(gateway).Executar(Pedido());
        Assert.Null(gateway.Resposta);
    }

    [Fact]
    public void FalhaTecnica_NaoViraRecusaDeNegocio()
    {
        var gateway = new OrcamentoIntegracaoGatewayFake { Falhar = true };
        Assert.Throws<InvalidOperationException>(() => new ProcessarPedidoOrcamentoUseCase(gateway).Executar(Pedido()));
        Assert.False(gateway.Processada); Assert.Null(gateway.Resposta);
    }

    private static MensagemIntegracao Pedido() => new()
    {
        MessageId = Guid.NewGuid(),
        Type = "GerarOrcamento",
        SagaId = Guid.NewGuid(),
        CausationId = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow,
        CorrelationId = "teste",
        Payload = new DadosMensagem
        {
            OrdemServicoId = Guid.NewGuid(),
            ExecucaoId = Guid.NewGuid(),
            Diagnostico = "Revisao",
            Itens = [new ItemMensagem { Tipo = "Servico", ReferenciaId = Guid.NewGuid(), Nome = "Revisao", Quantidade = 2, PrecoUnitario = 150m }]
        }
    };
}
