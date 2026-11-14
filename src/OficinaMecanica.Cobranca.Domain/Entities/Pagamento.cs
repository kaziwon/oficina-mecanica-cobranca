namespace OficinaMecanica.Cobranca.Domain.Entities;

public class Pagamento
{
    public Guid Id { get; private set; }
    public Guid OrcamentoId { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public decimal Valor { get; private set; }
    public string Moeda { get; private set; } = "BRL";
    public StatusPagamento Status { get; private set; } = StatusPagamento.Pendente;
    public DateTime CriadoEm { get; private set; }
    public Guid? SagaId { get; private set; }
    public Guid? ExecucaoId { get; private set; }
    public Guid? PedidoPagamentoId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? ProvedorId { get; private set; }
    public string? CodigoFalha { get; private set; }
    public DateTime? FinalizadoEm { get; private set; }
    public DateTime? ProximaTentativaEm { get; private set; }
    public int Tentativas { get; private set; }
    public int ConsultasReconciliacao { get; private set; }
    public DateTime? UltimaConsultaReconciliacaoEm { get; private set; }
    public DateTime? ReconciliadoEm { get; private set; }
    public long Versao { get; private set; } = 1;

    private Pagamento() { }

    internal Pagamento(Guid orcamentoId, Guid ordemServicoId, decimal valor)
    {
        Id = Guid.NewGuid();
        OrcamentoId = orcamentoId;
        OrdemServicoId = ordemServicoId;
        Valor = valor;
        CriadoEm = DateTime.UtcNow;
    }

    public void VincularSaga(Guid sagaId, Guid execucaoId, Guid pedidoId, string correlationId)
    {
        if (sagaId == Guid.Empty || execucaoId == Guid.Empty || pedidoId == Guid.Empty
            || string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 100 || correlationId.Any(char.IsControl))
            throw new Exceptions.DadosInvalidosException("Vinculo do pagamento invalido.");
        if (PedidoPagamentoId.HasValue)
        {
            if (SagaId == sagaId && ExecucaoId == execucaoId && PedidoPagamentoId == pedidoId && CorrelationId == correlationId) return;
            throw new Exceptions.RegraNegocioException("O pagamento ja pertence a outro pedido.");
        }
        if (Status != StatusPagamento.Pendente)
            throw new Exceptions.RegraNegocioException("Somente uma pendencia pode ser vinculada a Saga.");
        SagaId = sagaId; ExecucaoId = execucaoId; PedidoPagamentoId = pedidoId; CorrelationId = correlationId;
        Versao++;
    }

    public bool PodeProcessar(DateTime agora) => Status == StatusPagamento.Pendente && PedidoPagamentoId.HasValue
        && (!ProximaTentativaEm.HasValue || ProximaTentativaEm <= agora);

    public void RegistrarResultado(bool aprovado, string provedorId, DateTime agora)
    {
        if (!PodeProcessar(agora)) throw new Exceptions.RegraNegocioException("O pagamento nao esta pronto para processamento.");
        if (string.IsNullOrWhiteSpace(provedorId) || provedorId.Length > 100 || provedorId.Any(char.IsControl))
            throw new Exceptions.DadosInvalidosException("Identificador do provedor invalido.");
        Status = aprovado ? StatusPagamento.Confirmado : StatusPagamento.Recusado;
        ProvedorId = provedorId; CodigoFalha = aprovado ? null : "PagamentoRecusado";
        Tentativas++; Versao++; FinalizadoEm = agora; ProximaTentativaEm = null;
    }

    public void RegistrarFalhaTecnica(DateTime agora, int maxTentativas, TimeSpan intervalo)
    {
        if (!PodeProcessar(agora) || maxTentativas < 1 || intervalo <= TimeSpan.Zero)
            throw new Exceptions.RegraNegocioException("Tentativa de pagamento invalida.");
        Tentativas++; Versao++; CodigoFalha = "ResultadoNaoConfirmado";
        if (Tentativas >= maxTentativas)
        { Status = StatusPagamento.FalhaRequerIntervencao; FinalizadoEm = agora; ProximaTentativaEm = null; }
        else ProximaTentativaEm = agora.Add(intervalo);
    }

    public void RegistrarConsulta(bool? aprovado, string? provedorId, DateTime agora)
    {
        if (Status != StatusPagamento.FalhaRequerIntervencao)
            throw new Exceptions.RegraNegocioException("Somente pagamento sem resultado confirmado pode ser reconciliado.");
        if (aprovado.HasValue && (string.IsNullOrWhiteSpace(provedorId) || provedorId.Length > 100 || provedorId.Any(char.IsControl)))
            throw new Exceptions.DadosInvalidosException("Identificador do provedor invalido.");
        ConsultasReconciliacao++; UltimaConsultaReconciliacaoEm = agora; Versao++;
        // Ausencia no provedor nao comprova recusa e nao autoriza nova cobranca.
        if (!aprovado.HasValue) return;
        Status = aprovado.Value ? StatusPagamento.Confirmado : StatusPagamento.Recusado;
        ProvedorId = provedorId; CodigoFalha = aprovado.Value ? null : "PagamentoRecusado";
        FinalizadoEm = agora; ReconciliadoEm = agora;
    }
}
