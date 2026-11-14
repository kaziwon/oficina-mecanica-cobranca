using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Domain.Exceptions;

namespace OficinaMecanica.Cobranca.UnitTests;

public class ReconciliacaoDomainTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Pagamento_ConsultaNaoEhNovaTentativaNemPermiteTrocarResultado(bool aprovado)
    {
        var pagamento = Criar(); var agora = DateTime.UtcNow;
        Assert.Throws<RegraNegocioException>(() => pagamento.RegistrarConsulta(aprovado, "mock-id", agora));
        pagamento.RegistrarFalhaTecnica(agora, 1, TimeSpan.FromSeconds(1)); var id = pagamento.Id;
        pagamento.RegistrarConsulta(null, null, agora);
        Assert.Equal(StatusPagamento.FalhaRequerIntervencao, pagamento.Status); Assert.Null(pagamento.ReconciliadoEm);
        Assert.Throws<DadosInvalidosException>(() => pagamento.RegistrarConsulta(aprovado, "\n", agora));
        Assert.Equal(1, pagamento.ConsultasReconciliacao);
        pagamento.RegistrarConsulta(aprovado, "mock-id", agora);
        Assert.Equal(id, pagamento.Id); Assert.Equal(1, pagamento.Tentativas); Assert.Equal(2, pagamento.ConsultasReconciliacao);
        Assert.Equal(aprovado ? StatusPagamento.Confirmado : StatusPagamento.Recusado, pagamento.Status);
        Assert.Equal(agora, pagamento.ReconciliadoEm); Assert.Equal(agora, pagamento.UltimaConsultaReconciliacaoEm);
        Assert.Throws<RegraNegocioException>(() => pagamento.RegistrarConsulta(!aprovado, "outro", agora));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Estorno_ConsultaPreservaPagamentoEIdentificadorOriginal(bool confirmado)
    {
        var pagamento = Criar(); var agora = DateTime.UtcNow; pagamento.RegistrarResultado(true, "charge", agora);
        var id = Guid.NewGuid(); var estorno = Estorno.Solicitar(pagamento, id, Guid.NewGuid());
        Assert.Throws<RegraNegocioException>(() => estorno.RegistrarConsulta(confirmado, "refund", agora));
        estorno.RegistrarFalhaTecnica(agora, 1, TimeSpan.FromSeconds(1));
        estorno.RegistrarConsulta(null, null, agora); Assert.Null(estorno.ReconciliadoEm);
        Assert.Throws<DadosInvalidosException>(() => estorno.RegistrarConsulta(confirmado, "", agora));
        estorno.RegistrarConsulta(confirmado, "refund", agora);
        Assert.Equal(id, estorno.Id); Assert.Equal(1, estorno.Tentativas); Assert.Equal(2, estorno.ConsultasReconciliacao);
        Assert.Equal(confirmado ? StatusEstorno.Confirmado : StatusEstorno.Recusado, estorno.Status);
        Assert.Equal(StatusPagamento.Confirmado, pagamento.Status); Assert.Equal(agora, estorno.ReconciliadoEm);
        Assert.Throws<RegraNegocioException>(() => estorno.RegistrarConsulta(!confirmado, "outro", agora));
    }

    private static Pagamento Criar()
    {
        var orcamento = OrcamentoTestData.Entidade(); orcamento.Aprovar(); orcamento.RegistrarPagamentoPendente();
        var pagamento = orcamento.Pagamento!; pagamento.VincularSaga(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "reconciliacao");
        return pagamento;
    }
}
