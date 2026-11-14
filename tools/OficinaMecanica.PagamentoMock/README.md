# Provedor de pagamento ficticio

Ferramenta local de teste da Fase 4, nao um servico financeiro real. O contrato
e simplificado e nao implementa a API de producao do Mercado Pago.

- POST /oauth/token: grant_type=client_credentials, client_id e client_secret.
- POST /payments: Bearer, X-Idempotency-Key, X-Correlation-ID e JSON com
  external_reference (GUID), transaction_amount (decimal) e currency_id=BRL.
- GET /payments/ID_DO_PAGAMENTO: consulta por external_reference, com Bearer.
- POST /refunds: Bearer e os mesmos headers, external_reference do estorno,
  payment_id do provedor original, transaction_amount integral e currency_id=BRL.
- GET /refunds/ID_DO_ESTORNO: consulta por external_reference, com Bearer.
- GET /health: disponibilidade do mock.

O modo Mock:Resultado aceita approved, rejected e unavailable. Aplica-se
somente a chaves ainda nao persistidas. Transacoes anteriores sao devolvidas
com o mesmo ID e resultado, mesmo apos reiniciar. O SQLite do mock e separado
dos bancos de negocio e fica no volume local configurado pelo Compose.

Mock:ResultadoEstorno aceita refunded, rejected ou unavailable, independentemente
do modo de pagamento. A operacao de estorno e persistida com chave idempotente e
referencia unica a cobranca aprovada; nao permite dois estornos integrais da
mesma cobranca ou outro valor. Reiniciar nao muda um resultado ja persistido.
unavailable retorna 503 sem salvar um resultado conhecido.

Para testar resposta perdida APOS efetivar, use `approved_response_lost` em
Mock:Resultado ou `refunded_response_lost` em Mock:ResultadoEstorno. O mock
persiste a aprovacao/estorno uma vez, mas os POSTs devolvem 503; os GETs revelam
o resultado original. Isso permite testar reconciliacao sem nova chave ou
segunda operacao. Retorne a approved/refunded antes de rodar os testes BDD
normais. Esses modos sao somente de simulacao local.

Use o Compose integrado de Ordens. O [guia de pagamento mock](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/05-pagamento-mock.md)
documenta configuracao, testes e limites. Nunca use credenciais reais ou exponha
este mock como um provedor publico de pagamentos.
