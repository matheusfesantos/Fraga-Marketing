# Fraga Marketing API

API de contas e eventos financeiros. O projeto permite criar e consultar contas, processar créditos e débitos e consultar o extrato de uma conta.

## Tecnologias

- .NET 10 / ASP.NET Core
- PostgreSQL 16 e Entity Framework Core
- xUnit para testes
- Docker Compose para subir a API e o banco

## Como executar

### Com Docker Compose

Pré-requisitos: Docker Desktop com Docker Compose.

Na raiz do repositório:

```bash
docker compose up --build
```

A API ficará disponível em `http://localhost:8080`. A documentação interativa está em `http://localhost:8080/swagger` e o health check em `http://localhost:8080/health`.

O Compose inicia o PostgreSQL e aguarda o banco ficar saudável antes de iniciar a API. Na inicialização, a aplicação aplica as migrations pendentes e, se o banco ainda não tiver contas, insere três contas de exemplo. Para parar os serviços, use `docker compose down`; os dados continuam no volume `postgres_data`.

As configurações do banco podem ser sobrescritas pelas variáveis `POSTGRES_DB`, `POSTGRES_USER` e `POSTGRES_PASSWORD` (o Compose possui valores padrão para desenvolvimento).

### Localmente, sem Compose

Pré-requisitos: .NET 10 SDK e PostgreSQL 16 em execução.

Configure a connection string `ConnectionStrings:DefaultConnection` para apontar para seu PostgreSQL. Também é possível defini-la pela variável de ambiente `ConnectionStrings__DefaultConnection`. Em seguida:

```bash
dotnet run --project backend/src/Fraga.Api/Fraga.Api.csproj --launch-profile http
```

Com o perfil `http`, a API fica em `http://localhost:5007`; Swagger em `/swagger` e health check em `/health`. A origem CORS permitida por padrão é `http://localhost:4200`; ajuste `Cors:AllowedOrigins` se necessário.

## API

Os valores de `type` são `CREDIT` e `DEBIT`. Os exemplos abaixo usam `curl` e uma conta criada anteriormente ou uma das contas de exemplo.

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/accounts` | Cria uma conta com saldo zero |
| `GET` | `/api/accounts` | Lista as contas e saldos |
| `GET` | `/api/accounts/{accountId}` | Consulta uma conta |
| `GET` | `/api/accounts/{accountId}/transactions?page=1&pageSize=10` | Consulta o extrato paginado, do mais recente ao mais antigo |
| `POST` | `/api/transactions` | Processa um crédito ou débito |

Criar uma conta:

```bash
curl -X POST http://localhost:8080/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"name":"Conta de Teste"}'
```

Processar um crédito:

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

Substitua `accountId` por uma conta existente e use um `eventId` único por evento. Um débito acima do saldo é rejeitado. Reenviar um evento já registrado não altera o saldo novamente e retorna `409 Conflict`.

## Organização e decisões

O código está separado em quatro projetos para manter as regras de negócio independentes dos detalhes de transporte e persistência:

- `Fraga.Domain`: entidades, invariantes e exceções de negócio.
- `Fraga.Application`: casos de uso, contratos de repositório e DTOs.
- `Fraga.Infrastructure`: Entity Framework Core, PostgreSQL, migrations e repositórios.
- `Fraga.Api`: endpoints HTTP, configuração de dependências e conversão de exceções em respostas HTTP.

Essa separação adiciona mais projetos e interfaces do que uma API pequena estritamente precisaria, mas deixa as regras de negócio testáveis sem depender de HTTP ou banco. O custo de estrutura foi aceito para tornar responsabilidades e limites mais claros.

Uma das minhas principais preocupações durante o desenvolvimento foi a organização e a manutenibilidade do código. Por isso, optei por separar a solução em projetos como `Fraga.Domain` e `Fraga.Api`, buscando deixar as responsabilidades mais bem definidas e facilitar a manutenção e a evolução do sistema por outros desenvolvedores. Também procurei evitar concentrar muitas funcionalidades em uma única classe ou serviço.

Um exemplo é a parte de logs. Em vez de espalhar a lógica de logging pelos serviços, criei um serviço específico para essa responsabilidade, definido pelo contrato `ILogService`. Assim, mudanças futuras relacionadas a logs podem ser feitas de forma mais organizada e sem acoplar essa responsabilidade às regras de negócio.

Segui uma ideia semelhante para os contratos de serviços e repositórios: interfaces como `ITransactionService`, `ITransactionRepository` e `IAccountRepository` ajudam a desacoplar a aplicação dos detalhes de implementação e facilitam a substituição de dependências e a criação de testes. Os DTOs também definem explicitamente os dados recebidos e devolvidos pela API, evitando expor diretamente as entidades do domínio e mantendo os contratos HTTP mais claros.

Para erros, centralizei o mapeamento de exceções para respostas HTTP no `GlobalExceptionHandler`, usando `ProblemDetails`. Não criei um envelope genérico único para todas as respostas de sucesso; dentro do prazo, priorizei os contratos específicos de cada operação e os requisitos principais do teste.

Nos testes, procurei cobrir mais do que os caminhos de sucesso, incluindo entradas inválidas, eventos duplicados, saldo insuficiente e concorrência. Usei o Claude como apoio para levantar cenários adicionais e implementei os que consegui desenvolver dentro do prazo. A intenção foi validar tanto o comportamento esperado quanto as respostas a situações de erro.

Para versionar o esquema do banco, utilizei migrations do Entity Framework Core. Também criei exceções específicas para regras de negócio, como evento duplicado e saldo insuficiente, para tornar os erros mais claros e facilitar seu tratamento. O handler global usa um `switch` para mapear esses tipos às respectivas respostas HTTP, mantendo esse fluxo centralizado e explícito.

Na camada de frontend, a intenção também foi manter contratos de comunicação claros, validar entradas e usar debounce onde isso evita chamadas repetidas à API. No estado atual deste repositório, porém, `frontend/` contém apenas o scaffold inicial do Angular; esses comportamentos ainda não estão implementados aqui. Essa distinção evita apresentar como funcionalidade entregue algo que ainda é uma diretriz de desenvolvimento.

No geral, tomei decisões pensando não apenas em fazer a aplicação funcionar, mas também em como ela poderia ser mantida e evoluída. Há melhorias possíveis, mas, dentro do prazo, priorizei uma estrutura organizada, responsabilidades bem definidas e os cenários de negócio mais importantes.

O PostgreSQL foi escolhido para persistência relacional e para suportar as garantias de concorrência usadas no processamento. Cada operação de transação roda em uma transação do banco e bloqueia a conta durante a atualização do saldo; um índice único em `EventId` impede que o mesmo evento seja aplicado duas vezes, inclusive em chamadas concorrentes. Como contrapartida, o processamento depende de recursos específicos do PostgreSQL e não pode ser validado fielmente usando apenas um banco em memória.

Os valores monetários usam `decimal` e são persistidos com precisão `numeric(18,2)`. A API rejeita valores com mais de duas casas decimais em vez de arredondá-los silenciosamente. Cada registro também guarda o saldo após a operação, facilitando a leitura do histórico; isso duplica informação derivável, mas preserva o resultado daquela transação no extrato.

O extrato é paginado e limita `pageSize` a 100 para evitar consultas sem limite. As migrations são aplicadas ao iniciar a API e as contas de exemplo só são inseridas quando não há contas. Isso simplifica a execução e avaliação local; em produção, migrations e carga inicial normalmente devem fazer parte de um processo controlado de deploy, não da inicialização de cada instância.

## Testes

Na raiz do repositório:

```bash
dotnet test backend/Fraga.slnx
```

Os testes de integração sobem um PostgreSQL 16 com Testcontainers. É necessário ter Docker em execução para executar essa parte da suíte.

Para validar o frontend Angular localmente:

```bash
cd frontend
npm ci --legacy-peer-deps
npm run build
npm test -- --watch=false
```

O workflow de CI em `.github/workflows/ci.yml` executa em paralelo o build e os testes do frontend Angular (Node.js 22) e restaura, compila em Release e testa o backend (.NET 10), em pushes e pull requests para `develop` e `main`. O pipeline valida as alterações, mas não faz deploy automático.

## Escopo

Este repositório implementa o backend da avaliação; não inclui interface web nem autenticação/autorização. As contas de exemplo e configurações padrão do Compose são voltadas a desenvolvimento. Antes de expor a API em produção, configure credenciais seguras, autenticação, políticas de acesso e gestão de migrations apropriada ao ambiente.
