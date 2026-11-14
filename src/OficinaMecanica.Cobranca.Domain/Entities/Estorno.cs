namespace OficinaMecanica.Cobranca.Domain.Entities;

public class Estorno
{
    public Guid Id { get; private set; }
    public Guid PagamentoId { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public Guid OrcamentoId { get; private set; }
    public Guid ExecucaoId { get; private set; }
    public Guid SagaId { get; private set; }
    public Guid EventoCancelamentoId { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string PagamentoProvedorId { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public string Moeda { get; private set; } = "BRL";
    public StatusEstorno Status { get; private set; } = StatusEstorno.Pendente;
    public DateTime CriadoEm { get; private set; }
    public DateTime? FinalizadoEm { get; private set; }
    public DateTime? ProximaTentativaEm { get; private set; }
    public string? ProvedorId { get; private set; }
    public string? CodigoFalha { get; private set; }
    public int Tentativas { get; private set; }
    public int ConsultasReconciliacao { get; private set; }
    public DateTime? UltimaConsultaReconciliacaoEm { get; private set; }
    public DateTime? ReconciliadoEm { get; private set; }
    public long Versao { get; private set; } = 1;
    private Estorno() { }

    public static Estorno Solicitar(Pagamento pagamento, Guid id, Guid eventoCancelamentoId)
    {
        if (pagamento.Status != StatusPagamento.Confirmado || !pagamento.SagaId.HasValue || !pagamento.ExecucaoId.HasValue
            || string.IsNullOrWhiteSpace(pagamento.ProvedorId) || id == Guid.Empty || eventoCancelamentoId == Guid.Empty)
            throw new Exceptions.RegraNegocioException("Somente pagamento confirmado e vinculado a Saga permite estorno.");
        return new Estorno
        {
            Id = id,
            PagamentoId = pagamento.Id,
            OrdemServicoId = pagamento.OrdemServicoId,
            OrcamentoId = pagamento.OrcamentoId,
            ExecucaoId = pagamento.ExecucaoId.Value,
            SagaId = pagamento.SagaId.Value,
            EventoCancelamentoId = eventoCancelamentoId,
            CorrelationId = pagamento.CorrelationId!,
            PagamentoProvedorId = pagamento.ProvedorId,
            Valor = pagamento.Valor,
            Moeda = pagamento.Moeda,
            CriadoEm = DateTime.UtcNow
        };
    }

    public bool PodeProcessar(DateTime agora) => Status == StatusEstorno.Pendente
        && (!ProximaTentativaEm.HasValue || ProximaTentativaEm <= agora);

    public void RegistrarResultado(bool confirmado, string provedorId, DateTime agora)
    {
        if (!PodeProcessar(agora) || string.IsNullOrWhiteSpace(provedorId) || provedorId.Length > 100 || provedorId.Any(char.IsControl))
            throw new Exceptions.RegraNegocioException("Resultado do estorno invalido.");
        Status = confirmado ? StatusEstorno.Confirmado : StatusEstorno.Recusado;
        ProvedorId = provedorId; CodigoFalha = confirmado ? null : "EstornoRecusado";
        FinalizadoEm = agora; ProximaTentativaEm = null; Tentativas++; Versao++;
    }

    public void RegistrarFalhaTecnica(DateTime agora, int maxTentativas, TimeSpan intervalo)
    {
        if (!PodeProcessar(agora) || maxTentativas < 1 || intervalo <= TimeSpan.Zero)
            throw new Exceptions.RegraNegocioException("Tentativa de estorno invalida.");
        CodigoFalha = "ResultadoEstornoNaoConfirmado"; Tentativas++; Versao++;
        if (Tentativas >= maxTentativas) { Status = StatusEstorno.FalhaRequerIntervencao; FinalizadoEm = agora; ProximaTentativaEm = null; }
        else ProximaTentativaEm = agora.Add(intervalo);
    }

    public void RegistrarConsulta(bool? confirmado, string? provedorId, DateTime agora)
    {
        if (Status != StatusEstorno.FalhaRequerIntervencao)
            throw new Exceptions.RegraNegocioException("Somente estorno sem resultado confirmado pode ser reconciliado.");
        if (confirmado.HasValue && (string.IsNullOrWhiteSpace(provedorId) || provedorId.Length > 100 || provedorId.Any(char.IsControl)))
            throw new Exceptions.DadosInvalidosException("Identificador do provedor invalido.");
        ConsultasReconciliacao++; UltimaConsultaReconciliacaoEm = agora; Versao++;
        if (!confirmado.HasValue) return;
        Status = confirmado.Value ? StatusEstorno.Confirmado : StatusEstorno.Recusado;
        ProvedorId = provedorId; CodigoFalha = confirmado.Value ? null : "EstornoRecusado";
        FinalizadoEm = agora; ReconciliadoEm = agora;
    }
}
