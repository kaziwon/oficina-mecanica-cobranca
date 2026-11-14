# Oficina Mecanica - Cobranca - Fase 4

Microsservico responsavel pelos orcamentos e pagamentos. Usa MySQL exclusivo e
nao acessa os bancos de Ordens ou Execucao. Os repositorios da Fase 3 permanecem
intactos.

## Escopo deste primeiro incremento

- Login administrativo com JWT.
- Criacao e consulta de orcamentos com itens copiados do diagnostico.
- Calculo do total com decimal e registro de historico.
- Aprovacao ou recusa, com motivo na recusa.
- Registro e consulta de uma cobranca pendente apos aprovacao.
- Protecao contra respostas concorrentes e cobrancas duplicadas.
- MySQL, migracao inicial, Dockerfile e Compose independentes.
- Swagger, healthchecks e logs JSON com correlacao.
- Testes unitarios, HTTP e persistencia, CI e relatorios de cobertura.

```text
Orcamento: AguardandoAprovacao -> Aprovado ou Recusado
Pagamento: Pendente -> Confirmado, Recusado ou FalhaRequerIntervencao
```

**O pagamento usa o mock autorizado pelo professor, nao o Mercado Pago real.**
O worker obtem token e chama um provedor HTTP ficticio, com idempotencia e
persistencia. O resultado segue para a Saga. Nao movimenta dinheiro nem cria
Pix/checkout real. A API manual de pendencia nao permite marcar pagamento como pago.

As APIs em /api continuam administrativas. O cliente consulta e responde ao
proprio orcamento por Ordens, que usa os endpoints internos autenticados de
Cobranca. Envio por canal externo e webhooks reais ainda nao estao implementados.
Nao ha aprovacao publica sem autenticacao nem webhook de pagamento sem verificacao.

## Geracao por mensagens

Quando `Messaging:Enabled=true`, o worker consome `GerarOrcamento` da fila
`cobranca-comandos`. O use case valida o snapshot e calcula o total no dominio.
Uma unica transacao MySQL grava orcamento, inbox e resposta na outbox.
O worker publica `OrcamentoGerado` em `ordens-eventos`; somente entao Ordens
tera uma confirmacao para colocar a OS em `AguardandoAprovacao`.

Reentregas nao geram novo orcamento. O mesmo MessageId com outro conteudo e
rejeitado. Um conflito com outro orcamento ou uma regra de negocio invalida
gera `GeracaoOrcamentoRecusada`, com codigo e motivo. Falhas tecnicas de banco
nao sao convertidas em recusas: permitem retentativa. O SDK SQS, EF e o hash
da inbox ficam somente em Infrastructure, sem framework nas camadas internas.

O [guia central de diagnostico e orcamento](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/03-diagnostico-orcamento.md)
explica o contrato, o Compose integrado e o teste dos tres servicos. A API
manual de criacao permanece disponivel. `Messaging:Enabled` e falso por padrao.
As APIs de aprovar/recusar agora publicam eventos para orcamentos da Saga,
gravados atomicamente com o estado e o historico. A correlacao original fica
armazenada no orcamento; a migration recupera esse valor do evento de criacao
para registros existentes. Repetir a mesma resposta nao produz outro evento.
Registrar uma pendencia manual ainda e local. Novos comandos SolicitarPagamento
da Saga vinculam uma pendencia duravel ao processamento HTTP do mock.

O [guia de resposta ao orcamento](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/04-resposta-orcamento.md)
explica os eventos OrcamentoAprovado/OrcamentoRecusado e o roteiro HTTP. Aprovar
deixa inicialmente a OS em AguardandoPagamento. O incremento de pagamento mock
agora completa a parte financeira, sem liberar reparo.

## Pagamento mock

O [guia de pagamento mock](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/05-pagamento-mock.md)
descreve o provedor ficticio em tools/OficinaMecanica.PagamentoMock, a obtencao
de token e os modos approved/rejected/unavailable. Ele e ferramenta de teste
no mesmo repositorio, nao um quarto servico de negocio.

O use case depende de IPagamentoProvedorGateway; HttpClient e credenciais ficam
em Infrastructure. Pendencia/inbox sao gravadas antes do HTTP; resultado e
outbox sao gravados juntos depois. A chave e o ID persistido do pagamento.
Timeout nao e recusa: apos retentativas, resultado desconhecido exige
reconciliacao sem presumir que nao houve cobranca. PagamentoMock:Enabled e
falso por padrao e habilitado no Compose integrado. O CI testa o mock em processo,
inclusive reinicio, concorrencia e falha de gravacao depois da resposta HTTP.

O CI inicia tambem ElasticMQ e define `SQS_TEST_ENDPOINT` para testar envio,
reentrega e confirmacao pelo SDK, sem AWS. Na ausencia dessa variavel apenas
o teste de transporte e marcado como ignorado; a persistencia e os handlers
continuam sendo testados. As migrations agora incluem inbox e outbox.

## Clean Architecture

```text
src/
  OficinaMecanica.Cobranca.Domain/             Entidades e regras de negocio
  OficinaMecanica.Cobranca.Application/        Use cases, inputs, outputs e contratos
  OficinaMecanica.Cobranca.InterfaceAdapters/  Controllers Clean, gateways e presenters
  OficinaMecanica.Cobranca.Infrastructure/     EF Core, MySQL e JWT
  OficinaMecanica.Cobranca.Api/                HTTP, validacoes de entrada e composicao
tests/
  OficinaMecanica.Cobranca.UnitTests/
  OficinaMecanica.Cobranca.IntegrationTests/
```

Domain, Application e InterfaceAdapters nao possuem pacotes externos. Entidades
nao usam atributos de EF, HTTP ou validadores externos. A configuracao relacional
fica no `AppDbContext`, na camada Infrastructure. Inputs, outputs, mappers e use
cases ficam em pastas separadas, com uma classe por arquivo.

```text
Controller HTTP -> Controller Clean -> Use case -> Interface do gateway
                                                        |
                           Gateway -> Interface da source -> Source EF -> MySQL
```

O `Program.cs` e o `DependencyInjection.cs` conectam as implementacoes. A
criacao do pagamento e uma operacao do orcamento e e persistida na mesma
transacao local, por um unico `SaveChanges`.

## Dados e regras

| Tabela | Conteudo |
|---|---|
| `Orcamentos` | IDs externos da OS/execucao, diagnostico, total, resposta e versao |
| `ItensOrcamento` | Tipo, referencia externa, nome, quantidade e preco copiados |
| `HistoricoOrcamento` | Entrada em cada status, horario UTC e motivo |
| `Pagamentos` | Valor, moeda BRL, estado, contexto da Saga, provedor e retentativas |

OS, execucao, servicos e pecas sao apenas referencias por ID, sem chaves
estrangeiras para bancos de outros servicos. Os dados podem chegar pelo snapshot
do diagnostico na mensageria ou pela API administrativa. Nao sao buscados via
consulta direta aos bancos de Ordens/Execucao. O catalogo nao e duplicado: apenas a copia dos itens daquele
orcamento e persistida.

- Um orcamento por OS e por execucao neste incremento, garantido por indices unicos.
- Entre 1 e 100 itens; mesmo tipo/referencia nao pode se repetir.
- Quantidade de 1 a 10000; preco positivo, ate 999999.99 e duas casas decimais.
- Total calculado pelo servico, limitado a 999999999.99.
- Itens e precos nao sao editaveis depois da criacao.
- A mesma aprovacao ou recusa com o mesmo motivo pode ser repetida sem novo historico.
- Depois da resposta nao se pode trocar aprovacao por recusa ou vice-versa.
- O registro de pagamento recebe somente o ID do orcamento: valor e status nao vem do cliente.
- Repetir o registro de pagamento devolve o mesmo ID, sem uma segunda cobranca.
- Versao e indices protegem contra concorrencia; HTTP 409 pede nova consulta e tentativa.

A consistencia acima e local ao banco de Cobranca. Outbox e deduplicacao ligam
essas transacoes a Saga orquestrada por Ordens. A recusa ja solicita a retirada
da execucao nao liberada; compensacoes financeiras ficam para a etapa de pagamento.

## Executar localmente

Com Docker Desktop aberto, na raiz do repositorio:

```bash
docker compose --env-file .env.example up --build
```

O comando cria MySQL com volume proprio, usuario limitado ao banco `cobranca`
e API. A API aguarda o healthcheck do MySQL e aplica sua migracao inicial.

- Swagger: <http://localhost:18083/swagger/index.html>
- Processo ativo: <http://localhost:18083/health>
- Conectividade com o banco: <http://localhost:18083/health/ready>
- MySQL no host: `127.0.0.1:3308`.

As portas nao conflitam com Ordens (18081/3307) ou Execucao (18082/27018).
O `.env.example` contem credenciais publicas apenas para desenvolvimento.
Credenciais reais devem vir de variaveis do ambiente ou `.env` ignorado pelo Git.
Alterar o arquivo nao troca a senha de um usuario ja persistido no volume.

Para encerrar mantendo os dados:

```bash
docker compose --env-file .env.example down
```

## Primeiro fluxo no Postman

1. `POST /api/auth/login`, sem autenticacao:

```json
{
  "username": "admin",
  "password": "LocalAdminCobranca@123"
}
```

Use o token em `Authorization: Bearer TOKEN` nas proximas chamadas.

2. `POST /api/orcamentos`. Substitua os IDs pelos retornados em Ordens e Execucao;
os valores abaixo sao somente exemplos de GUIDs:

```json
{
  "ordemServicoId": "11111111-1111-1111-1111-111111111111",
  "execucaoId": "22222222-2222-2222-2222-222222222222",
  "diagnostico": "Pastilhas desgastadas; substituir o jogo dianteiro",
  "itens": [
    {
      "tipo": "Servico",
      "referenciaId": "33333333-3333-3333-3333-333333333333",
      "nome": "Revisao de freios",
      "quantidade": 1,
      "precoUnitario": 100.15
    },
    {
      "tipo": "PecaInsumo",
      "referenciaId": "44444444-4444-4444-4444-444444444444",
      "nome": "Pastilha de freio",
      "quantidade": 2,
      "precoUnitario": 80.50
    }
  ]
}
```

A resposta retorna `valorTotal: 261.15` e `status: AguardandoAprovacao`.

3. `POST /api/orcamentos/ID_DO_ORCAMENTO/aprovar`, sem body.

4. `POST /api/orcamentos/ID_DO_ORCAMENTO/pagamento`, sem body.
Retorna o registro `Pendente`, ainda sem transacao no Mercado Pago.

5. `GET /api/pagamentos/ID_DO_PAGAMENTO` consulta a pendencia.
`GET /api/orcamentos/ID_DO_ORCAMENTO` mostra o historico e o ID do pagamento.

Para testar recusa, crie outro orcamento para outra OS e use
`POST /api/orcamentos/ID_DO_OUTRO_ORCAMENTO/recusar`:

```json
{ "motivo": "Cliente nao autorizou o valor" }
```

## Endpoints

| Metodo | Rota | Uso |
|---|---|---|
| POST | `/api/auth/login` | Login administrativo |
| POST | `/api/orcamentos` | Criar orcamento |
| GET | `/api/orcamentos?status=Aprovado&ordemServicoId=GUID` | Listar, mais antigos primeiro |
| GET | `/api/orcamentos/{id}` | Consultar itens e historico |
| POST | `/api/orcamentos/{id}/aprovar` | Aprovar |
| POST | `/api/orcamentos/{id}/recusar` | Recusar com motivo |
| POST | `/api/orcamentos/{id}/pagamento` | Registrar ou consultar a mesma pendencia |
| GET | `/api/pagamentos/{id}` | Consultar pagamento |

Todas as rotas de negocio exigem JWT administrativo. Campos JSON desconhecidos
sao rejeitados nos DTOs, incluindo `valorTotal` e `status` na criacao do orcamento.
O header `X-Correlation-ID` acompanha a resposta e os logs JSON.

## Testes e CI

Sem Docker ou AWS:

```bash
dotnet test OficinaMecanica.Cobranca.slnx --configuration Release --collect:"XPlat Code Coverage" --results-directory TestResults
```

Sem variavel de conexao, a integracao roda HTTP e persistencia relacional em
SQLite em memoria. Tambem verifica a geracao do schema MySQL e a existencia da
migracao. Isso nao substitui a verificacao do driver MySQL.

Para executar a mesma bateria em MySQL real:

```bash
docker compose --env-file .env.example up -d mysql
export MYSQL_TEST_CONNECTION_STRING='Server=127.0.0.1;Port=3308;User=root;Password=LocalRootCobranca@123;AllowPublicKeyRetrieval=true;SslMode=None'
dotnet test OficinaMecanica.Cobranca.slnx --configuration Release --collect:"XPlat Code Coverage" --results-directory TestResults
unset MYSQL_TEST_CONNECTION_STRING
```

Os testes criam e removem apenas bancos novos com prefixo `cob_test_`,
sem tocar o banco da aplicacao. A conexao de testes deve permitir criar e
remover esses bancos. Use-a somente em ambiente de desenvolvimento.

O prefixo curto preserva o GUID completo e permite que o nome do lock de
migracao gerado pelo Pomelo (`__NOME_DO_BANCO_EFMigrationsLock`) fique abaixo do
limite de 64 caracteres do MySQL.

No GitHub Actions, os jobs de build e testes sao separados. O job de testes
inicia MySQL 8.4 temporario, configura a variavel acima, aplica as migracoes e
publica a cobertura. As senhas fixas desse job valem somente para esse banco
descartavel. Nao e necessario cadastrar Secrets nem usar a AWS.

## Estorno mock apos cancelamento confirmado

O comando interno SolicitarEstorno vem de Ordens, somente depois da confirmacao
de parada do reparo em Execucao. Cobranca valida a prova financeira contra seu
pagamento local confirmado e cria um Estorno duravel separado. A cobranca
original permanece Confirmada como historico, sem nova cobranca ou alteracao
retroativa. GET /api/pagamentos/{pagamentoId}/estorno, com JWT Admin, mostra
Confirmado, Recusado ou FalhaRequerIntervencao, alem de Pendente.

O worker usa o mesmo client_id -> token OAuth do mock e chama POST /refunds
com o ID do estorno como chave idempotente. Estado e evento outbox sao atomicos.
Se a resposta chegou, mas a gravacao falhou, repetir recupera o mesmo resultado,
sem segunda operacao financeira. Timeout ou resposta divergente nao confirmam
reembolso; tentativas esgotadas exigem intervencao, sem gerar outra chave.
O mock aceita apenas estorno integral de cobranca aprovada. Nao ha dinheiro real.

O [guia de compensacao](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/10-compensacao-paga.md)
documenta mensagens, testes e o status CancelamentoPendente mantido em Ordens
quando o estorno nao e confirmado. Nao ha endpoint publico para forcar estorno.
Depois de esgotar tentativas, o Admin pode consultar o resultado original:
`POST /api/pagamentos/{id}/reconciliar` ou
`POST /api/pagamentos/{id}/estorno/reconciliar`, sem body ou com `{}`.
O servidor busca token OAuth e faz somente GET no registro financeiro original.
Nao recebe status, valor ou ID de provedor do operador e nao faz nova operacao.
Ausencia no mock mantem intervencao; indisponibilidade retorna 503; pendencia
ainda em processamento retorna 409. Consulte os mesmos GETs existentes para
ver contagem/data das consultas e `reconciliadoEm`. Resultado e evento outbox
sao atomicos. Repetir apos resultado conhecido nao consulta nem publica de novo.
O [guia de recuperacao](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/13-recuperacao-saga.md)
detalha testes, eventos atrasados, DLQ e limites.

## Orcamento pelo cliente via Ordens

Ordens verifica cliente ativo e propriedade da OS antes de chamar os endpoints
de atendimento de Cobranca. O JWT do cliente nao e repassado: a chamada usa
credencial de servico exclusiva, com expiracao curta e audience restrita.
GET /internal/orcamentos/{id} e os POSTs /aprovar e /recusar dessa rota exigem
esse esquema, nao token de administrador ou cliente.

Os use cases conferem OS, execucao, Saga, pedido original, valor e correlacao
contra o orcamento local. A entidade aplica a mesma regra de aprovacao/recusa
dos fluxos anteriores. Decisao, historico e evento outbox sao atomicos; repetir
a mesma decisao nao gera outra cobranca. Nao acessa o banco de Ordens.

OrdensAtendimento:Enabled e SecretKey habilitam o atendimento. Fora do Compose
integrado fica desabilitado por padrao. A chave deve ser exclusiva, com pelo
menos 32 bytes. O token de servico nao autoriza endpoints administrativos.
No futuro deploy, a rota interna deve permanecer fora do gateway publico e
usar rede/transporte protegidos e Secrets, nunca a chave local de exemplo.

O [guia central do cliente](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/12-orcamento-cliente.md)
documenta as rotas publicas em Ordens, configuracao e testes de ponta a ponta.
Nenhum recurso AWS foi criado nesta etapa.

## Proximos incrementos

- Validar runner/token e primeira analise do Sonar local; automatizar BDD no CI.
- Recursos Kubernetes, CD no kind local e observabilidade distribuida no New Relic.

## Cobertura local

Na raiz deste servico, apos configurar os bancos locais de teste:

```bash
dotnet test --configuration Release --settings cobertura.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults/cobertura-atual
python3 tools/verificar_cobertura.py --servico Cobranca --diretorio TestResults/cobertura-atual
```

Use uma pasta de resultados nova por execucao. O verificador une linhas de
unidade/integracao, exige as cinco camadas e falha abaixo de 80%. Exclui somente
migrations geradas, nao toda a Infrastructure. O BDD dos tres servicos fica em
Ordens; veja o [guia de qualidade](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/14-testes-qualidade-local.md).

O CI atual apenas valida o codigo; nao publica imagens e nao faz deploy.

O ambiente de entrega da Fase 4 sera LOCAL, em um cluster kind compartilhado,
nao AWS. Cada servico preserva banco, recursos e CD independentes. Sonar e local
e New Relic continua externo. A base Terraform fica em oficina-mecanica-infra-local;
os recursos de Cobranca ficarao neste repositorio. Veja o [guia do cluster local](https://github.com/kaziwon/oficina-mecanica-infra-local/blob/main/docs/16-kubernetes-local.md).

## SonarQube local

O CI passa a exigir pelo menos 80% de cobertura. O novo workflow
qualidade-local.yml foi preparado para analisar a main no SonarQube Community
do notebook, somente depois de CI bem-sucedido ou disparo manual na main.
Usa runner self-hosted macOS com label sonar-local e Secret SONAR_TOKEN deste
projeto. Runner e token cadastrados; falta validar a primeira analise.
Nao usa Sonar Cloud e nao executa forks/PRs no runner pessoal. O servidor
compartilhado e iniciado pelo compose.sonar.yml de oficina-mecanica-infra-local. Veja o
[guia completo](https://github.com/kaziwon/oficina-mecanica-infra-local/blob/main/docs/15-sonarqube-local.md).
