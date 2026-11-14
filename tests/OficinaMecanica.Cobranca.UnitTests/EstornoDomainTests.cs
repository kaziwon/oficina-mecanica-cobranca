using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Domain.Exceptions;
namespace OficinaMecanica.Cobranca.UnitTests;

public class EstornoDomainTests
{
    [Fact]
    public void Solicitar_SomenteCobrancaConfirmadaComVinculoDaSaga()
    {
        var pagamento = Criar(); Assert.Throws<RegraNegocioException>(() => Estorno.Solicitar(pagamento, Guid.NewGuid(), Guid.NewGuid()));
        pagamento.RegistrarResultado(true, "cobranca-123", DateTime.UtcNow); var id = Guid.NewGuid(); var estorno = Estorno.Solicitar(pagamento, id, Guid.NewGuid());
        Assert.Equal(id, estorno.Id); Assert.Equal(pagamento.Valor, estorno.Valor); Assert.Equal(pagamento.ProvedorId, estorno.PagamentoProvedorId);
        Assert.Equal(StatusPagamento.Confirmado, pagamento.Status);
    }
    [Theory]
    [InlineData(true, StatusEstorno.Confirmado)]
    [InlineData(false, StatusEstorno.Recusado)]
    public void ResultadoConhecido_EncerraSemPermitirNovoProcessamento(bool aceito, StatusEstorno esperado)
    {
        var pagamento = Criar(); pagamento.RegistrarResultado(true, "charge", DateTime.UtcNow); var estorno = Estorno.Solicitar(pagamento, Guid.NewGuid(), Guid.NewGuid());
        estorno.RegistrarResultado(aceito, "refund", DateTime.UtcNow); Assert.Equal(esperado, estorno.Status); Assert.False(estorno.PodeProcessar(DateTime.UtcNow));
        Assert.Throws<RegraNegocioException>(() => estorno.RegistrarResultado(aceito, "outro", DateTime.UtcNow));
    }
    [Fact]
    public void FalhaTecnica_ReagendaComMesmoIdEExigeIntervencaoAoEsgotar()
    {
        var pagamento = Criar(); pagamento.RegistrarResultado(true, "charge", DateTime.UtcNow); var id = Guid.NewGuid(); var estorno = Estorno.Solicitar(pagamento, id, Guid.NewGuid());
        var agora = DateTime.UtcNow; estorno.RegistrarFalhaTecnica(agora, 2, TimeSpan.FromSeconds(1));
        Assert.Equal(StatusEstorno.Pendente, estorno.Status); Assert.False(estorno.PodeProcessar(agora)); Assert.Null(estorno.ProvedorId);
        estorno.RegistrarFalhaTecnica(agora.AddSeconds(2), 2, TimeSpan.FromSeconds(1));
        Assert.Equal(id, estorno.Id); Assert.Equal(StatusEstorno.FalhaRequerIntervencao, estorno.Status); Assert.Null(estorno.ProvedorId);
        Assert.Equal(StatusPagamento.Confirmado, pagamento.Status);
    }
    private static Pagamento Criar()
    {
        var orcamento = OrcamentoTestData.Entidade(); orcamento.Aprovar(); orcamento.RegistrarPagamentoPendente();
        var pagamento = orcamento.Pagamento!; pagamento.VincularSaga(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "teste-estorno"); return pagamento;
    }
}
