using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class SolicitarEstornoUseCase
{
    private readonly IEstornoGateway _estornos;
    public SolicitarEstornoUseCase(IEstornoGateway estornos) { _estornos = estornos; }
    public void Executar(MensagemIntegracao mensagem)
    {
        MensagemValidator.Validar(mensagem, "SolicitarEstorno"); var dados = mensagem.Payload;
        if (dados.EstornoId != mensagem.MessageId || dados.EstornoProvedorId is not null || dados.Codigo is not null || dados.Motivo is not null)
            throw new ValidacaoException("Solicitacao de estorno invalida.");
        if (_estornos.FoiProcessada(mensagem)) return;
        var pagamento = _estornos.ObterPagamento(dados.PagamentoId!.Value) ?? throw new ValidacaoException("Pagamento desconhecido.");
        if (pagamento.SagaId != mensagem.SagaId || pagamento.CorrelationId != mensagem.CorrelationId
            || pagamento.OrdemServicoId != dados.OrdemServicoId || pagamento.ExecucaoId != dados.ExecucaoId
            || pagamento.OrcamentoId != dados.OrcamentoId || pagamento.ProvedorId != dados.ProvedorId
            || pagamento.Valor != dados.ValorTotal || pagamento.Moeda != dados.Moeda
            || _estornos.ObterPorPagamento(pagamento.Id) is not null)
            throw new ValidacaoException("Estorno nao corresponde ao pagamento ou ja possui outra operacao.");
        _estornos.Registrar(mensagem, Estorno.Solicitar(pagamento, mensagem.MessageId, mensagem.CausationId!.Value));
    }
}
