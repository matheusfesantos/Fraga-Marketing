# Fraga Marketing

Sistema full-stack para gestão de contas e transações financeiras, com backend em .NET e frontend em Angular.

## Visão geral

O repositório implementa uma API para:

- criar e consultar contas;
- registrar créditos e débitos;
- consultar o extrato de uma conta;
- garantir idempotência por `eventId`;
- impedir saldo negativo e rejeitar eventos duplicados;
- manter uma arquitetura de domínio, aplicação e infraestrutura separadas.

A aplicação também inclui uma interface frontend em Angular para consumo da API, com listagem de contas, formulário de transações e visão do extrato.

## Stack

- .NET 10 / ASP.NET Core
- PostgreSQL 16
- Entity Framework Core
- Angular 21
- xUnit para testes do backend
- Docker Compose para ambiente local

## Estrutura do repositório

```text
.
├── backend/
│   ├── src/
│   │   ├── Fraga.Api/
│   │   ├── Fraga.Application/
│   │   ├── Fraga.Domain/
│   │   └── Fraga.Infrastructure/
│   ├── Fraga.slnx
│   └── Dockerfile
├── frontend/
│   ├── src/
│   ├── package.json
│   └── Dockerfile
├── docker-compose.yml
├── README.md
└── .gitignore
```

## Como executar

### Opção 1: com Docker Compose

Pré-requisitos: Docker Desktop ou Docker Engine com suporte a Compose.

Na raiz do repositório, execute:

```bash
docker compose up --build
```

Isso inicia os containers:

- Frontend: `http://localhost:4200`
- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`
- PostgreSQL: `localhost:5432`

O Compose aplica as migrations do banco automaticamente e cria contas de exemplo quando necessário. Para encerrar os serviços:

```bash
docker compose down
```

Os dados do PostgreSQL ficam persistidos no volume `postgres_data`.

### Opção 2: backend localmente

Pré-requisitos: .NET 10 SDK e PostgreSQL 16 em execução.

Configure a connection string antes de iniciar a API:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=fraga;Username=postgres;Password=postgres"
```

Ou ajuste o valor em `backend/src/Fraga.Api/appsettings.Development.json`.

Em seguida:

```bash
dotnet run --project backend/src/Fraga.Api/Fraga.Api.csproj --launch-profile http
```

A API fica disponível em `http://localhost:5007` com o perfil `http`.

### Opção 3: frontend localmente

```bash
cd frontend
npm install
npm start
```

O frontend usa proxy para a API local e normalmente fica em `http://localhost:4200`.

## Frontend: arquitetura e componentização

O frontend foi organizado em camadas conceituais para manter a interface mais previsível e fácil de evoluir:

- `core`: serviços, modelos e regras de integração com a API.
  - `AccountsService`: consulta contas da API.
  - `TransactionsService`: consulta extrato e cria transações.
  - `ToastService`: centraliza feedback visual para sucesso, erro e warning.
- `features`: páginas e fluxos específicos do domínio.
  - `accounts`: visão geral das contas e resumo financeiro.
  - `transactions`: extrato e formulário de criação de transações.
- `shared`: componentes reutilizáveis e visuais compartilhados pela aplicação.
  - `app-header`: navegação principal.
  - `toast`: notificação de feedback.
  - `summary-card`: blocos de resumo.

Além disso, a aplicação usa Angular standalone components, que deixam cada página/componentes independentes, com imports explícitos e menos acoplamento com módulos globais. Isso aumenta modularidade e facilita manutenção.

A estrutura de navegação também segue esse princípio:

- `/contas`: listar contas e resumo financeiro;
- `/transacoes`: visualizar extrato;
- `/transacoes/nova`: registrar nova transação.

Esse modelo separa responsabilidades:

- serviços cuidam da comunicação com a API;
- páginas orquestram a experiência do usuário;
- componentes visuais ficam focados na apresentação;
- o roteamento controla a navegação entre fluxos sem misturar lógica de negócio com a UI.

## API

Os valores de `type` aceitos na API são `CREDIT` e `DEBIT`.

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/accounts` | Cria uma conta com saldo zero |
| `GET` | `/api/accounts` | Lista as contas e saldos |
| `GET` | `/api/accounts/{accountId}` | Consulta uma conta específica |
| `GET` | `/api/accounts/{accountId}/transactions?page=1&pageSize=10` | Consulta o extrato paginado |
| `POST` | `/api/transactions` | Processa uma transação financeira |

### Exemplo: criar conta

```bash
curl -X POST http://localhost:8080/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"name":"Conta de Teste"}'
```

### Exemplo: processar crédito

```bash
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

Observações:

- `eventId` deve ser único por evento;
- eventos duplicados são rejeitados;
- débitos maiores que o saldo disponível são bloqueados;
- o extrato é paginado e limitado por página para evitar consultas muito grandes;
- valores monetários são armazenados com precisão de 2 casas decimais.

## Arquitetura e componentização

A arquitetura foi pensada para separar claramente responsabilidades e facilitar evolução do sistema sem acoplar regras de negócio a detalhes de infraestrutura, HTTP ou interface.

### Backend: separação por camadas

A estrutura do backend segue uma abordagem orientada a domínio com divisões bem definidas:

- `Fraga.Domain`: entidades, invariantes e regras centrais do negócio. Aqui ficam as regras que não dependem de banco, API ou framework.
- `Fraga.Application`: casos de uso, serviços, contratos e DTOs. É o núcleo da aplicação, responsável por coordenar a lógica de negócio sem saber como os dados são persistidos.
- `Fraga.Infrastructure`: implementação concreta de acesso a dados, Entity Framework Core, PostgreSQL e repositórios.
- `Fraga.Api`: endpoints HTTP, serialização JSON, injeção de dependências e mapeamento de erros em respostas estruturadas.

Essa divisão foi escolhida por três motivos principais:

1. baixo acoplamento: a regra de negócio não depende de implementação específica de banco ou transporte;
2. testabilidade: é mais simples testar casos de negócio diretamente em camadas de domínio/aplicação sem depender de infraestrutura;
3. manutenção: mudanças em banco, API, validações ou contratos de entrada podem ser feitas com menor impacto em outras partes do sistema.

Além disso, serviços e repositórios foram abstraídos por interfaces (`ITransactionService`, `IAccountRepository`, etc.), o que favorece inversão de dependência e facilita a criação de testes e substituição de implementações sem quebrar o restante da aplicação.

### Componentização do frontend

No frontend, a organização também foi feita de forma modular e orientada por responsabilidade:

- `core`: modelos de domínio, serviços e abstrações reutilizáveis;
- `features`: páginas e fluxos de negócio, como contas, transações e extrato;
- `shared`: componentes reutilizáveis e utilitários visuais;
- `routes`: roteamento da aplicação.

A aplicação segue uma estrutura de componentes standalone e serviços dedicados para acesso à API. Por exemplo:

- `AccountsService` concentra a comunicação com `/api/accounts`;
- `TransactionsService` centraliza o acesso aos endpoints de transação e extrato;
- páginas como `AccountsList`, `TransactionForm` e `TransactionsStatement` são responsáveis pela experiência de usuário e composição visual, enquanto os serviços cuidam do transporte de dados.

Essa organização foi escolhida para manter a UI mais previsível, reduzir duplicação de código e facilitar a evolução do produto em funcionalidades futuras sem misturar regras de negócio com renderização.

### Por que essa arquitetura foi usada

A solução foi desenhada como um projeto de nível pleno, com foco em:

- clareza de responsabilidades;
- separação entre domínio, aplicação e infraestrutura;
- baixo acoplamento e alta coesão;
- facilidade de manutenção e extensão;
- qualidade operacional com testes e tratamento centralizado de exceções.

Em outras palavras, o objetivo não foi apenas "funcionar", mas demonstrar capacidade de construir software de forma sustentável, profissional e escalável.

## Testes

### Backend

```bash
dotnet test backend/Fraga.slnx
```

### Frontend

```bash
cd frontend
npm run test:jest -- --runInBand
```

Também é possível validar o build do frontend com:

```bash
cd frontend
npm run build
```

## Observações

- o PostgreSQL é usado para garantir integridade transacional e concorrência no processamento de saldo;
- as migrations são aplicadas automaticamente na inicialização da API;
- a estrutura atual é adequada para desenvolvimento e validação local; em produção, é recomendável revisar autenticação, autorização, políticas de deploy e configuração de ambiente.

