# Provedor de pagamento ficticio

Ferramenta local de teste da Fase 4, nao um servico financeiro real. O contrato
e simplificado e nao implementa a API de producao do Mercado Pago.

- POST /oauth/token: grant_type=client_credentials, client_id e client_secret.
- POST /payments: Bearer, X-Idempotency-Key, X-Correlation-ID e JSON com
  external_reference (GUID), transaction_amount (decimal) e currency_id=BRL.
- GET /payments/ID_DO_PAGAMENTO: consulta por external_reference, com Bearer.
- GET /health: disponibilidade do mock.

O modo Mock:Resultado aceita approved, rejected e unavailable. Aplica-se
somente a chaves ainda nao persistidas. Transacoes anteriores sao devolvidas
com o mesmo ID e resultado, mesmo apos reiniciar. O SQLite do mock e separado
dos bancos de negocio e fica no volume local configurado pelo Compose.

Use o Compose integrado de Ordens. O [guia de pagamento mock](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/05-pagamento-mock.md)
documenta configuracao, testes e limites. Nunca use credenciais reais ou exponha
este mock como um provedor publico de pagamentos.
