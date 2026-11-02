using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Domain.Exceptions;

namespace OficinaMecanica.Cobranca.UnitTests;

public class OrcamentoDomainTests
{
    [Fact]
    public void Criacao_CalculaTotalDecimalERegistraHistorico()
    {
        var orcamento = OrcamentoTestData.Entidade();
        Assert.Equal(261.15m, orcamento.ValorTotal);
        Assert.Equal("Trocar pastilhas", orcamento.Diagnostico);
        Assert.Equal(StatusOrcamento.AguardandoAprovacao, orcamento.Status);
        Assert.Single(orcamento.Historico);
        Assert.Null(orcamento.Pagamento);
    }

    [Fact]
    public void Criacao_CopiaListaRecebida()
    {
        var itens = new List<ItemOrcamento> { new(TipoItemOrcamento.Servico, Guid.NewGuid(), "Servico", 1, 10m) };
        var orcamento = Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Diagnostico", itens);
        itens.Clear();
        Assert.Single(orcamento.Itens);
        Assert.Equal(10m, orcamento.ValorTotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Item_RejeitaQuantidadeInvalida(int quantidade)
        => Assert.Throws<DadosInvalidosException>(() => new ItemOrcamento(TipoItemOrcamento.Servico, Guid.NewGuid(), "Item", quantidade, 10m));

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1000000")]
    [InlineData("1.001")]
    public void Item_RejeitaPrecoInvalido(string valor)
        => Assert.Throws<DadosInvalidosException>(() => new ItemOrcamento(TipoItemOrcamento.Servico, Guid.NewGuid(), "Item", 1,
            decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Item_RejeitaTipoReferenciaOuNomeInvalidos()
    {
        Assert.Throws<DadosInvalidosException>(() => new ItemOrcamento((TipoItemOrcamento)99, Guid.NewGuid(), "Item", 1, 10m));
        Assert.Throws<DadosInvalidosException>(() => new ItemOrcamento(TipoItemOrcamento.Servico, Guid.Empty, "Item", 1, 10m));
        Assert.Throws<DadosInvalidosException>(() => new ItemOrcamento(TipoItemOrcamento.Servico, Guid.NewGuid(), " ", 1, 10m));
        Assert.Throws<DadosInvalidosException>(() => new ItemOrcamento(TipoItemOrcamento.Servico, Guid.NewGuid(), new string('a', 151), 1, 10m));
    }

    [Fact]
    public void Criacao_RejeitaIdsDiagnosticoOuListaInvalidos()
    {
        var itens = OrcamentoTestData.Entidade().Itens;
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.Empty, Guid.NewGuid(), "Diagnostico", itens));
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.NewGuid(), Guid.Empty, "Diagnostico", itens));
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), " ", itens));
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), new string('a', 2001), itens));
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Diagnostico", []));
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Diagnostico", null!));
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Diagnostico", [null!]));
    }

    [Fact]
    public void Criacao_RejeitaDuplicatasOuTotalExcessivo()
    {
        var item = new ItemOrcamento(TipoItemOrcamento.PecaInsumo, Guid.NewGuid(), "Peca", 10000, 999999.99m);
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Diagnostico", [item, item]));
        Assert.Throws<DadosInvalidosException>(() => Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Diagnostico", [item]));
    }

    [Fact]
    public void Aprovacao_EhIdempotenteENaoPodeVirarRecusa()
    {
        var orcamento = OrcamentoTestData.Entidade();
        Assert.True(orcamento.Aprovar());
        var respondidoEm = orcamento.RespondidoEm;
        Assert.False(orcamento.Aprovar());
        Assert.Equal(2, orcamento.Historico.Count);
        Assert.Equal(2, orcamento.Versao);
        Assert.Equal(respondidoEm, orcamento.RespondidoEm);
        Assert.Throws<RegraNegocioException>(() => orcamento.Recusar("Mudei de ideia"));
    }

    [Fact]
    public void Recusa_EhIdempotenteParaOMesmoMotivoENaoPodeVirarAprovacao()
    {
        var orcamento = OrcamentoTestData.Entidade();
        Assert.True(orcamento.Recusar(" Valor alto "));
        Assert.False(orcamento.Recusar("Valor alto"));
        Assert.Equal("Valor alto", orcamento.MotivoRecusa);
        Assert.Equal(2, orcamento.Historico.Count);
        Assert.Throws<RegraNegocioException>(() => orcamento.Aprovar());
        Assert.Throws<RegraNegocioException>(() => orcamento.Recusar("Outro motivo"));
        Assert.Throws<RegraNegocioException>(() => orcamento.RegistrarPagamentoPendente());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Recusa_ExigeMotivo(string motivo)
        => Assert.Throws<DadosInvalidosException>(() => OrcamentoTestData.Entidade().Recusar(motivo));

    [Fact]
    public void Recusa_RejeitaMotivoMuitoLongo()
        => Assert.Throws<DadosInvalidosException>(() => OrcamentoTestData.Entidade().Recusar(new string('a', 1001)));

    [Fact]
    public void Pagamento_ExigeAprovacaoECriaUmaUnicaPendenciaComValorDoOrcamento()
    {
        var orcamento = OrcamentoTestData.Entidade();
        Assert.Throws<RegraNegocioException>(() => orcamento.RegistrarPagamentoPendente());
        orcamento.Aprovar();
        Assert.True(orcamento.RegistrarPagamentoPendente());
        var id = orcamento.Pagamento!.Id;
        Assert.False(orcamento.RegistrarPagamentoPendente());
        Assert.Equal(id, orcamento.Pagamento.Id);
        Assert.Equal(orcamento.ValorTotal, orcamento.Pagamento.Valor);
        Assert.Equal(orcamento.OrdemServicoId, orcamento.Pagamento.OrdemServicoId);
        Assert.Equal("BRL", orcamento.Pagamento.Moeda);
        Assert.Equal(StatusPagamento.Pendente, orcamento.Pagamento.Status);
        Assert.Equal(3, orcamento.Versao);
    }
}
