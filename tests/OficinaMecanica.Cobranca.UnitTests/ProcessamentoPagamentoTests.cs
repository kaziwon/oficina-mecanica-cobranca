using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.UnitTests.Fakes;

namespace OficinaMecanica.Cobranca.UnitTests;

public class ProcessamentoPagamentoTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResultadoConhecido_GravaUmaVezENotificaSaga(bool aprovado)
    {
        var pagamento = Novo(); var gateway = new PagamentoProcessamentoGatewayFake { Pagamento = pagamento };
        var provedor = new PagamentoProvedorGatewayFake { Aprovado = aprovado }; var useCase = new ProcessarPagamentoUseCase(gateway, provedor);
        await useCase.Executar(pagamento.Id, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        await useCase.Executar(pagamento.Id, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        Assert.Equal(aprovado ? StatusPagamento.Confirmado : StatusPagamento.Recusado, pagamento.Status);
        Assert.Equal(aprovado ? "PagamentoConfirmado" : "PagamentoRecusado", Assert.Single(gateway.Eventos).Type);
        Assert.Equal(pagamento.PedidoPagamentoId, gateway.Eventos[0].CausationId);
        Assert.Single(provedor.Chaves); Assert.Equal(1, gateway.Gravacoes);
    }

    [Fact]
    public async Task FalhaTecnica_NaoConfundeTimeoutComRecusaOuConfirmacao()
    {
        var pagamento = Novo(); var gateway = new PagamentoProcessamentoGatewayFake { Pagamento = pagamento };
        var useCase = new ProcessarPagamentoUseCase(gateway, new PagamentoProvedorGatewayFake { Falhar = true });
        await useCase.Executar(pagamento.Id, 2, TimeSpan.FromSeconds(1), CancellationToken.None);
        Assert.Equal(StatusPagamento.Pendente, pagamento.Status); Assert.Empty(gateway.Eventos); Assert.Null(pagamento.ProvedorId);
        Assert.False(pagamento.PodeProcessar(pagamento.ProximaTentativaEm!.Value.AddTicks(-1)));
        pagamento.RegistrarFalhaTecnica(pagamento.ProximaTentativaEm!.Value, 2, TimeSpan.FromSeconds(1));
        Assert.Equal(StatusPagamento.FalhaRequerIntervencao, pagamento.Status); Assert.Null(pagamento.ProvedorId);
    }

    [Fact]
    public async Task LimiteDeFalhas_PublicaResultadoNaoConfirmado()
    {
        var pagamento = Novo(); var gateway = new PagamentoProcessamentoGatewayFake { Pagamento = pagamento };
        await new ProcessarPagamentoUseCase(gateway, new PagamentoProvedorGatewayFake { Falhar = true })
            .Executar(pagamento.Id, 1, TimeSpan.FromSeconds(1), CancellationToken.None);
        Assert.Equal("FalhaProcessamentoPagamento", Assert.Single(gateway.Eventos).Type);
        Assert.Equal("ResultadoNaoConfirmado", gateway.Eventos[0].Payload.Codigo);
    }

    [Fact]
    public async Task EncerramentoDoWorker_NaoGravaRecusaNemConsomeTentativa()
    {
        var pagamento = Novo(); var gateway = new PagamentoProcessamentoGatewayFake { Pagamento = pagamento };
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ProcessarPagamentoUseCase(gateway, new PagamentoProvedorGatewayFake())
            .Executar(pagamento.Id, 3, TimeSpan.FromSeconds(1), cancellation.Token));
        Assert.Equal(StatusPagamento.Pendente, pagamento.Status); Assert.Equal(0, pagamento.Tentativas); Assert.Equal(0, gateway.Gravacoes);
    }

    private static Pagamento Novo()
    {
        var orcamento = Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Revisao",
            [new ItemOrcamento(TipoItemOrcamento.Servico, Guid.NewGuid(), "Revisao", 1, 100m)]);
        orcamento.Aprovar(); orcamento.RegistrarPagamentoPendente();
        orcamento.Pagamento!.VincularSaga(Guid.NewGuid(), orcamento.ExecucaoId, Guid.NewGuid(), "teste"); return orcamento.Pagamento;
    }
}
