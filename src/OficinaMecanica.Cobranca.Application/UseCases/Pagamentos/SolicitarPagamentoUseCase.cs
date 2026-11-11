using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class SolicitarPagamentoUseCase
{
    private readonly IPagamentoIntegracaoGateway _integracao;
    public SolicitarPagamentoUseCase(IPagamentoIntegracaoGateway integracao) { _integracao = integracao; }

    public void Executar(MensagemIntegracao pedido)
    {
        MensagemValidator.Validar(pedido, "SolicitarPagamento");
        var dados = pedido.Payload;
        if (!pedido.CausationId.HasValue || !dados.ExecucaoId.HasValue || !dados.OrcamentoId.HasValue
            || dados.PagamentoId.HasValue || dados.ProvedorId is not null || dados.Moeda != "BRL"
            || dados.Codigo is not null || dados.Motivo is not null || dados.Diagnostico is not null
            || dados.Itens is not null || dados.DescricaoSolicitacao is not null
            || dados.DiagnosticoIniciadoEm.HasValue || dados.DiagnosticoConcluidoEm.HasValue)
            throw new ValidacaoException("Contrato da solicitacao de pagamento invalido.");
        if (_integracao.FoiProcessada(pedido)) return;
        var orcamento = _integracao.ObterOrcamento(dados.OrcamentoId.Value)
            ?? throw new ValidacaoException("Orcamento desconhecido.");
        var aprovacao = _integracao.ObterEventoAprovacao(pedido.CausationId.Value);
        if (orcamento.Status != StatusOrcamento.Aprovado || orcamento.SagaId != pedido.SagaId
            || orcamento.CorrelationId != pedido.CorrelationId || orcamento.OrdemServicoId != dados.OrdemServicoId
            || orcamento.ExecucaoId != dados.ExecucaoId || orcamento.ValorTotal != dados.ValorTotal
            || aprovacao?.Type != "OrcamentoAprovado" || aprovacao.SagaId != pedido.SagaId
            || aprovacao.Payload.OrcamentoId != orcamento.Id || aprovacao.Payload.ValorTotal != orcamento.ValorTotal)
            throw new ValidacaoException("O pedido nao corresponde ao orcamento e a aprovacao conhecidos.");
        orcamento.RegistrarPagamentoPendente();
        orcamento.Pagamento!.VincularSaga(pedido.SagaId, orcamento.ExecucaoId, pedido.MessageId, pedido.CorrelationId);
        // A pendencia duravel e gravada antes de qualquer chamada externa.
        _integracao.Registrar(pedido, orcamento);
    }
}
