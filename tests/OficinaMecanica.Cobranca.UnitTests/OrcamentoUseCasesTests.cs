using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.UnitTests.Fakes;

namespace OficinaMecanica.Cobranca.UnitTests;

public class OrcamentoUseCasesTests
{
    [Fact]
    public void Criacao_PersisteMapeiaEImpedeSegundaCriacaoParaOS()
    {
        var gateway = new OrcamentoGatewayFake();
        var useCase = new CriarOrcamentoUseCase(gateway);
        var input = OrcamentoTestData.Input();
        var output = useCase.Executar(input);
        Assert.Equal(261.15m, output.ValorTotal);
        Assert.Equal(2, output.Itens.Count);
        Assert.Single(output.Historico);
        Assert.Equal(1, gateway.Gravacoes);
        Assert.Throws<ConflitoException>(() => useCase.Executar(input));
    }

    [Fact]
    public void Criacao_RejeitaListaNulaOuItemNuloAntesDeGravar()
    {
        var gateway = new OrcamentoGatewayFake();
        var input = OrcamentoTestData.Input();
        input.Itens = null!;
        Assert.Throws<ValidacaoException>(() => new CriarOrcamentoUseCase(gateway).Executar(input));
        input.Itens = [null!];
        Assert.Throws<ValidacaoException>(() => new CriarOrcamentoUseCase(gateway).Executar(input));
        Assert.Equal(0, gateway.Gravacoes);
    }

    [Fact]
    public void Consultas_FiltramPorStatusEOSEValidamFiltros()
    {
        var gateway = new OrcamentoGatewayFake();
        var orcamento = OrcamentoTestData.Entidade();
        gateway.Inserir(orcamento);
        var listar = new ObterTodosOrcamentosUseCase(gateway);
        Assert.Single(listar.Executar(StatusOrcamento.AguardandoAprovacao, orcamento.OrdemServicoId));
        Assert.Empty(listar.Executar(StatusOrcamento.Recusado, null));
        Assert.NotNull(new ObterOrcamentoPorIdUseCase(gateway).Executar(orcamento.Id));
        Assert.Null(new ObterOrcamentoPorIdUseCase(gateway).Executar(Guid.NewGuid()));
        Assert.Throws<ValidacaoException>(() => listar.Executar((StatusOrcamento)99, null));
        Assert.Throws<ValidacaoException>(() => listar.Executar(null, Guid.Empty));
    }

    [Fact]
    public void AprovacaoEPagamento_RepetidosNaoGeramNovasGravacoes()
    {
        var gateway = new OrcamentoGatewayFake();
        var orcamento = OrcamentoTestData.Entidade();
        gateway.Inserir(orcamento);
        var aprovar = new AprovarOrcamentoUseCase(gateway);
        aprovar.Executar(orcamento.Id);
        aprovar.Executar(orcamento.Id);
        var registrar = new RegistrarPagamentoPendenteUseCase(gateway);
        var pagamento = registrar.Executar(orcamento.Id)!;
        Assert.Equal(pagamento.Id, registrar.Executar(orcamento.Id)!.Id);
        Assert.Equal(3, gateway.Gravacoes);
        Assert.Equal(pagamento.Id, new ObterPagamentoPorIdUseCase(gateway).Executar(pagamento.Id)!.Id);
        Assert.Null(new ObterPagamentoPorIdUseCase(gateway).Executar(Guid.NewGuid()));
    }

    [Fact]
    public void Recusa_PersisteUmaUnicaVez()
    {
        var gateway = new OrcamentoGatewayFake();
        var orcamento = OrcamentoTestData.Entidade();
        gateway.Inserir(orcamento);
        var useCase = new RecusarOrcamentoUseCase(gateway);
        Assert.Equal(StatusOrcamento.Recusado, useCase.Executar(orcamento.Id, new("Valor alto"))!.Status);
        Assert.Equal("Valor alto", useCase.Executar(orcamento.Id, new("Valor alto"))!.Historico.Last().Motivo);
        Assert.Equal(2, gateway.Gravacoes);
    }

    [Fact]
    public void Comandos_RetornamNullParaOrcamentoInexistente()
    {
        var gateway = new OrcamentoGatewayFake();
        var id = Guid.NewGuid();
        Assert.Null(new AprovarOrcamentoUseCase(gateway).Executar(id));
        Assert.Null(new RecusarOrcamentoUseCase(gateway).Executar(id, new RecusarOrcamentoInput("Motivo")));
        Assert.Null(new RegistrarPagamentoPendenteUseCase(gateway).Executar(id));
        Assert.Equal(0, gateway.Gravacoes);
    }
}
