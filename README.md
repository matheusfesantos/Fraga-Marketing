# Fraga Bank: motor de saldos de contas

Solução full-stack para gerenciar contas bancárias e processar eventos financeiros (crédito e débito) com **idempotência**, **consistência**, **transacionalidade** e **limitação de requisições**, com API em .NET e interface em Angular.

## Sumário

1. [Como executar](#como-executar)
2. [Visão geral](#visão-geral)
3. [Arquitetura](#arquitetura)
4. [Os quatro pilares: como foram garantidos](#os-quatro-pilares-como-foram-garantidos)
5. [API](#api)
6. [Modelagem de dados](#modelagem-de-dados)
7. [Frontend](#frontend)
8. [Testes](#testes)
9. [Decisões e trade-offs](#decisões-e-trade-offs)
10. [O que não foi feito](#o-que-não-foi-feito)

## Como executar

### Com Docker Compose (recomendado)

Pré-requisito: Docker com suporte a Compose. Na raiz do repositório:

```bash
docker compose up --build
```

| Serviço | URL |
|---|---|
| Frontend | http://localhost:4200 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| Health check | http://localhost:8080/health |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |

- As **migrations são aplicadas automaticamente** na subida da API.
- Se a base estiver vazia, são criadas 3 contas de exemplo (Conta Corrente, Conta Poupança, Conta Investimentos).
- No container, o frontend é servido por nginx, que também encaminha `/api/*` para a API. O navegador fala só com `localhost:4200`.
- O Redis armazena os contadores do limite de requisições; PostgreSQL e Redis usam volumes persistentes.
- Para encerrar: `docker compose down` (os dados ficam nos volumes `postgres_data` e `redis_data`; use `-v` para apagá-los).

### Backend localmente

Pré-requisitos: .NET 10 SDK, PostgreSQL 16 e Redis 7 em execução (pode iniciar as dependências pelo Compose: `docker compose up postgres redis`).

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=fraga;Username=postgres;Password=postgres"
dotnet run --project backend/src/Fraga.Api/Fraga.Api.csproj --no-launch-profile --urls http://localhost:8080
```

A connection string padrão já está em `backend/src/Fraga.Api/appsettings.json`; as configurações padrão apontam para PostgreSQL e Redis locais. Use `ConnectionStrings__DefaultConnection` ou `ConnectionStrings__Redis` para substituir os endereços conforme necessário. Usamos a porta 8080 para coincidir com o proxy do frontend (`frontend/proxy.conf.json`).

### Frontend localmente

```bash
cd frontend
npm install --legacy-peer-deps
npm start
```

Abre em http://localhost:4200 e usa o proxy para `http://localhost:8080`.

### Testes

```bash
# Backend (os testes de integração exigem Docker em execução: usam Testcontainers)
dotnet test backend/Fraga.slnx

# Frontend (Jest)
cd frontend
npm run test:jest -- --runInBand
```

O CI (`.github/workflows/ci.yml`) executa build e testes dos dois lados a cada push/PR para `develop` e `main`.

## Visão geral

- Criar e consultar contas.
- Processar eventos de **crédito** e **débito**, atualizando saldo e histórico.
- Consultar o **extrato paginado** de uma conta, com o saldo resultante de cada lançamento.
- Limitar requisições por endereço IP com contadores compartilhados no Redis.
- Interface com listagem de contas, extrato e formulário de lançamento.

**Stack:** .NET 10 / ASP.NET Core, EF Core + PostgreSQL 16, Redis 7, Angular 21 + Angular Material, xUnit + Moq + Testcontainers, Jest, Docker Compose.

```text
.
├── backend/
│   ├── src/
│   │   ├── Fraga.Domain/          # entidades, invariantes, exceções de negócio
│   │   ├── Fraga.Application/     # casos de uso, DTOs, contratos (interfaces)
│   │   ├── Fraga.Infrastructure/  # EF Core, repositórios, migrations
│   │   └── Fraga.Api/             # controllers, DI, tratamento de erros, OpenAPI
│   └── tests/Fraga.Tests/         # unitários e de integração
├── frontend/
│   └── src/app/
│       ├── core/                  # models, services (acesso à API)
│       ├── features/              # páginas: accounts, transactions
│       └── shared/                # componentes reutilizáveis (toast)
└── docker-compose.yml
```

## Arquitetura

### Backend: camadas com dependência para dentro

```text
Api ──► Application ──► Domain
 │           ▲
 └─► Infrastructure ─┘
```

- **Domain:** `Account` e `Transaction`. As regras que não podem ser violadas ficam **na entidade**: `Account.Debit` lança `InsufficientBalanceException` se o saldo não cobre o valor, e `Credit`/`Debit` exigem valor positivo. Setters privados: o estado só muda por métodos do domínio. O nome da conta é normalizado e validado no construtor.
- **Application:** `TransactionService` orquestra o caso de uso; `AccountService` cuida de contas e extrato. Dependem de **interfaces** (`ITransactionRepository`, `IAccountRepository`, `ILogService`), implementadas fora do núcleo.
- **Infrastructure:** repositórios EF Core, configurações de mapeamento (`IEntityTypeConfiguration`) e migrations. É a única camada que conhece PostgreSQL/Npgsql.
- **Api:** controllers finos (sem regra de negócio), injeção de dependência, enum como string (`CREDIT`/`DEBIT`), **`GlobalExceptionHandler`** e middleware de limite de requisições com contadores no Redis.

Efeito prático: a regra de negócio é testável com mocks, sem banco nem HTTP, e trocar a persistência não afeta o domínio.

### Frontend: organização por responsabilidade

- **`core`:** modelos tipados e serviços (`AccountsService`, `TransactionsService`, `ToastService`) que concentram a comunicação com a API e o feedback ao usuário.
- **`features`:** páginas standalone (`AccountsList`, `TransactionsStatement`, `TransactionForm`) com rotas carregadas sob demanda (`loadComponent`).
- **`shared`:** componentes reutilizáveis (`Toast`).
- Serviços retornam `Observable`; as páginas orquestram estados de **carregando / erro / vazio / sucesso**.

## Os quatro pilares: como foram garantidos

### 1. Idempotência: um `eventId` nunca é processado duas vezes

Defesa em três camadas:

1. **Pré-checagem** (`ExistsByEventIdAsync`) antes de qualquer trabalho, que devolve 409 rapidamente no caso comum (reenvio).
2. **Nova checagem após o lock da conta**, que cobre duas requisições simultâneas do mesmo evento para a mesma conta: a segunda só prossegue depois de a primeira confirmar e então enxerga o evento já gravado.
3. **Índice único** `IX_transactions_EventId` no banco, a barreira definitiva. Cobre o caso em que o mesmo `eventId` chega em paralelo para **contas diferentes** (o lock de uma conta não protege a outra). A violação do índice é capturada no repositório e convertida em `DuplicateEventException` (HTTP 409).

Sem depender de cache ou memória da aplicação: a garantia vive no banco e vale com várias instâncias da API.

### 2. Consistência: saldo nunca negativo e igual ao histórico

- **Regra de domínio:** `Account.Debit` recusa débito acima do saldo.
- **Rede de proteção no banco:** `CK_accounts_balance_non_negative`, `CK_transactions_amount_positive` e `CK_transactions_balance_after_non_negative`.
- **Concorrência:** a conta é lida com `SELECT ... FOR UPDATE` (bloqueio pessimista). Operações na mesma conta são **serializadas**, então não há *lost update* nem dois débitos simultâneos "gastando" o mesmo saldo.
- **Saldo refletindo o histórico:** cada transação grava `BalanceAfter`, e o saldo da conta é atualizado na mesma operação. Os testes de integração verificam que `saldo == soma do histórico`.
- **Precisão monetária:** `numeric(18,2)`. Valores com mais de duas casas decimais são **rejeitados** (400) em vez de arredondados em silêncio, para o histórico refletir exatamente o que foi enviado.

### 3. Transacionalidade: tudo ou nada

`ExecuteAtomicAsync` abre uma transação de banco que engloba: checagem de duplicidade → lock da conta → alteração do saldo → inserção da transação → `SaveChanges` → `Commit`. Qualquer exceção (saldo insuficiente, duplicidade, falha de banco) interrompe antes do commit e a transação sofre **rollback** ao ser descartada. Há teste de integração garantindo que um débito rejeitado não deixa nenhum rastro (nem transação, nem alteração de saldo).

### 4. Integridade ponta a ponta: o backend é a fonte da verdade

- O frontend valida para melhorar a experiência (valor obrigatório, > 0, até 2 casas), mas **toda regra é reaplicada no backend**.
- Erros da API (`409` duplicado, `422` saldo insuficiente, `400` validação) chegam como `ProblemDetails`; o frontend exibe o `detail` retornado pelo servidor.
- Após um lançamento bem-sucedido, o formulário **recarrega as contas** do backend, então o saldo exibido é o real, e não um valor calculado no cliente.
- **`eventId` estável por tentativa:** o frontend gera o identificador uma vez e o **reutiliza** se o usuário reenviar os mesmos dados após uma falha (por exemplo, queda de rede). Assim, se a primeira requisição chegou ao servidor mas a resposta se perdeu, o reenvio resulta em "evento já processado" em vez de um lançamento em dobro. Alterar qualquer campo, ou um sucesso, gera um novo `eventId`.

## API

Documentação interativa: `http://localhost:8080/swagger`. Valores de `type`: `CREDIT` e `DEBIT`.

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/accounts` | Cria conta com saldo zero |
| `GET` | `/api/accounts` | Lista contas e saldos |
| `GET` | `/api/accounts/{accountId}` | Consulta uma conta |
| `GET` | `/api/accounts/{accountId}/transactions?page=1&pageSize=10` | Extrato paginado (mais recente primeiro; `pageSize` entre 1 e 100) |
| `POST` | `/api/transactions` | Processa um evento financeiro |
| `GET` | `/health` | Health check (inclui o PostgreSQL) |

### Limite de requisições

A API aceita até **100 requisições por endereço IP a cada minuto**. O contador fica no Redis, então o limite é compartilhado entre instâncias da API. O endereço IP é armazenado como hash na chave do contador. A rota `/health` não consome o limite.

As respostas às demais rotas incluem os cabeçalhos `X-RateLimit-Limit` e `X-RateLimit-Remaining`. Ao exceder o limite, a API retorna `429 Too Many Requests`, o cabeçalho `Retry-After: 60` e um corpo JSON:

```json
{
  "error": "Limite de requisições excedido.",
  "message": "Tente novamente em instantes."
}
```

### Códigos de resposta do `POST /api/transactions`

| Status | Quando |
|---|---|
| `200` | Evento processado; devolve `transactionId`, `eventId`, `accountId` e o saldo resultante |
| `400` | Payload inválido (ids vazios, tipo inválido, valor ≤ 0, mais de 2 casas decimais) |
| `404` | Conta inexistente |
| `409` | `eventId` já processado (idempotência) |
| `422` | Saldo insuficiente |
| `500` | Erro inesperado: nenhuma informação interna é exposta ao cliente |

### Exemplos

```bash
curl -X POST http://localhost:8080/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"name":"Conta de Teste"}'

curl -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "eventId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "accountId":"11111111-1111-1111-1111-111111111111",
    "type":"CREDIT",
    "amount":125.50,
    "occurredAt":"2026-10-09T12:00:00Z"
  }'
```

Repetir a segunda chamada devolve `409 Conflict`. O arquivo `backend/src/Fraga.Api/Fraga.Api.http` traz requisições prontas.

## Modelagem de dados

**`accounts`**: `Id` (uuid, PK), `Name` (varchar(100)), `Balance` (numeric(18,2)), com check `Balance >= 0`.

**`transactions`**: `Id` (uuid, PK), `EventId` (uuid), `AccountId` (uuid, FK → accounts, `ON DELETE RESTRICT`), `Type` (text: `Credit`/`Debit`), `Amount` (numeric(18,2)), `OccurredAt` (timestamptz, normalizado para UTC), `BalanceAfter` (numeric(18,2)), com checks `Amount > 0` e `BalanceAfter >= 0`.

**Índices:**

| Índice | Finalidade |
|---|---|
| `IX_transactions_EventId` (**único**) | Garantia final de idempotência |
| `IX_transactions_AccountId_OccurredAt_Id` (`AccountId`, `OccurredAt DESC`, `Id DESC`) | Atende exatamente a consulta do extrato (filtro por conta + ordenação), sem *sort* extra; o `Id` desempata de forma determinística entre lançamentos no mesmo instante |

`ON DELETE RESTRICT` impede apagar uma conta com histórico.

## Frontend

- **Angular 21** (standalone components, control flow `@if/@for`, rotas lazy) e **Angular Material** (campos, select, botões, paginador), com tema alinhado à identidade visual. Layout responsivo com SCSS.
- **Visão geral (`/contas`):** saldo consolidado, cards de contas e extrato recente da conta selecionada.
- **Extrato (`/transacoes`):** seleção de conta, saldo atual, tabela com **valor, impacto (+/−) e saldo após cada lançamento**, e paginação com `mat-paginator`.
- **Nova transação (`/transacoes/nova`):** Reactive Forms com validação inline, formulário bloqueado durante envio/carregamento (evita duplo clique), toasts de sucesso/erro com a mensagem da API.
- **Estados de interface:** carregamento, erro (com "tentar novamente" no extrato), vazio e sucesso, tratados em cada página.

## Testes

**Backend (xUnit + Moq + Testcontainers)**

- *Unitários:* regras de `Account` (crédito, débito, saldo exato, saldo insuficiente, valores inválidos, nome), `TransactionService` (valor zero, duplicidade antes/depois do lock, conta inexistente, débito sem saldo, normalização UTC, mais de 2 casas decimais), `AccountService` (paginação, ids inválidos) e `GlobalExceptionHandler` (mapeamento de exceção para status, sem vazamento de detalhes em 500).
- *Integração, contra um PostgreSQL real em container:*
  - mesmo `eventId` em paralelo na mesma conta → processa **uma** vez;
  - mesmo `eventId` em paralelo em **contas diferentes** → processa **uma** vez (índice único);
  - débitos concorrentes que excedem o saldo → saldo nunca fica negativo;
  - créditos concorrentes → nenhuma atualização é perdida e o saldo é igual ao histórico;
  - débito rejeitado → nada é gravado.

**Frontend (Jest)**

- Serviços: chamadas HTTP, parâmetros de paginação e propagação de erros.
- Formulário: validações (obrigatório, ≤ 0, mais de 2 casas), envio de crédito/débito, reuso e renovação do `eventId`, mensagens de erro da API (`detail`/`message`/padrão).
- Páginas: carregamento, seleção de conta, paginação, estados de erro e vazio.
- `ToastService` e `Toast`.

## Decisões e trade-offs

| Decisão | Por quê | Custo / alternativa |
|---|---|---|
| **Bloqueio pessimista** (`FOR UPDATE`) na conta | Corretude simples e previsível: serializa operações da mesma conta, sem laços de retry | Limita a vazão *por conta* (contas diferentes seguem em paralelo). Alternativa: concorrência otimista (coluna de versão) com retry, melhor com pouca contenção, mais complexa |
| **Checagem de duplicidade antes e depois do lock** + índice único | Resposta rápida (409) no caso comum; o índice cobre as corridas | Uma consulta a mais por requisição. O índice, sozinho, já bastaria para a corretude |
| **Saldo desnormalizado** em `accounts.Balance` | Leitura O(1) do saldo consolidado | Risco teórico de divergência com o histórico. Mitigado: atualização na mesma transação, checks no banco e testes que comparam saldo × histórico |
| **Extrato ordenado por `OccurredAt`** | É a ordem natural para o usuário, e o índice composto a atende | `BalanceAfter` reflete a ordem de **processamento**. Se chegar um evento com data anterior a lançamentos já processados, a coluna "saldo após" não segue a sequência visual. Alternativa: ordenar por ordem de gravação |
| **Rejeitar > 2 casas decimais** (400) | O histórico reflete exatamente o enviado, sem arredondamento silencioso | Clientes precisam respeitar a precisão |
| **Processamento síncrono** | Resposta imediata com o saldo resultante; menos infraestrutura | Sem desacoplamento/buffer de picos. Alternativa: fila (RabbitMQ) com processamento assíncrono, ao custo de consistência eventual na UI |
| **Limite de 100 requisições por IP/minuto no Redis** | Protege a API contra excesso de chamadas e compartilha contadores entre instâncias | Requer Redis disponível e aplica o mesmo limite por IP a todas as rotas, exceto `/health`; limites por endpoint e configuração dinâmica ficam para uma evolução futura |
| **Migrations na inicialização** | Sobe com um único `docker compose up` | Em produção com várias instâncias, preferir um passo de migração dedicado no deploy |
| **Exceções de domínio + `GlobalExceptionHandler`** | Controllers finos e mapeamento HTTP em um único lugar; 500 sem vazamento | Exceções como fluxo de controle em regras de negócio esperadas; aceitável nesta escala |
| **Enum persistido como texto** | Legível e seguro contra reordenação de valores | Ocupa mais espaço que um inteiro |
| **Jest em vez de Karma** | Execução mais rápida, ótima ergonomia de mocks | Diferente do runner padrão do CLI |
| **`eventId` gerado pelo cliente, mas estável por tentativa** | Torna reenvios seguros (idempotentes) | O usuário não informa o ID manualmente |