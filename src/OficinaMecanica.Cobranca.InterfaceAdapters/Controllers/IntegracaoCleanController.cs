using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

public class IntegracaoCleanController
{
    private readonly ProcessarPedidoOrcamentoUseCase _processar;
    public IntegracaoCleanController(ProcessarPedidoOrcamentoUseCase processar) { _processar = processar; }
    public void Processar(MensagemIntegracao mensagem) => _processar.Executar(mensagem);
}
