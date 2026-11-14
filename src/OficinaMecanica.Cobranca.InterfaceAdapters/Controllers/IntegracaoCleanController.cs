using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

public class IntegracaoCleanController
{
    private readonly ProcessarPedidoOrcamentoUseCase _processar;
    private readonly SolicitarPagamentoUseCase _pagamento;
    private readonly SolicitarEstornoUseCase _estorno;
    public IntegracaoCleanController(ProcessarPedidoOrcamentoUseCase processar, SolicitarPagamentoUseCase pagamento, SolicitarEstornoUseCase estorno)
    { _processar = processar; _pagamento = pagamento; _estorno = estorno; }
    public void Processar(MensagemIntegracao mensagem)
    {
        if (mensagem.Type == "SolicitarPagamento") _pagamento.Executar(mensagem);
        else if (mensagem.Type == "SolicitarEstorno") _estorno.Executar(mensagem);
        else _processar.Executar(mensagem);
    }
}
