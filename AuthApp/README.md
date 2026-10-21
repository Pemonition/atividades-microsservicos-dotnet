# AuthApp — cadastro e email por mensageria

Atividade de 20/10/2026 baseada em [rannyer/AuthApp](https://github.com/rannyer/AuthApp): documentar arquitetura, endpoints e stack e usar IA para implementar o envio no EmailService. Implementacao com assistencia do Codex.

- [Arquitetura e diagrama](docs/arquitetura.md)
- [Endpoints, contratos e respostas](docs/endpoints.md)
- [Stack, configuracao e Gmail](docs/stack-e-configuracao.md)
- [Resultados de validacao](docs/validacao.md)

## Executar

Requisitos: SDK .NET 8 ou posterior, runtime .NET 8 e Docker Desktop com motor Linux ativo.

```powershell
dotnet restore AuthApp.sln
dotnet build AuthApp.sln --no-restore
docker compose -p authapp-aula up -d
```

Em terminais separados na pasta da solucao:

```powershell
dotnet run --project UserService --launch-profile http
```

```powershell
dotnet run --project EmailService --launch-profile http
```

Swagger: [UserService](http://localhost:5104/swagger) e [EmailService](http://localhost:5289/swagger).

```powershell
Invoke-RestMethod http://localhost:5104/api/Usuario -Method Post `
  -ContentType application/json -Body '{"nome":"Aluno","email":"aluno@example.com"}'
```

Veja as boas-vindas no [Mailpit](http://localhost:8025). Nenhum email e encaminhado a internet no modo local. O [RabbitMQ](http://localhost:15672) usa guest / guest. Esta infraestrutura e a da atividade EcommereceMicroservices usam as mesmas portas: reutilize os containers existentes ou pare o outro compose primeiro.

A fila atual e fila-usuario-criado-v2, duravel, com fila de falhas. Mensagens da fila original nao sao migradas automaticamente.

## Testes automatizados

```powershell
dotnet run --project Tests
```

O executavel retorna 0 em sucesso e 1 em falha. Testa conteudo e rejeicao SMTP por servidor TCP local, dados invalidos e configuracao TLS, sem Docker nem Gmail. Nao e um projeto xUnit. Para testar todo o fluxo RabbitMQ com containers ativos, use o cadastro acima e confira o email no Mailpit.

Teste integrado automatizado, com os containers ativos e os projetos compilados:

```powershell
pwsh -File scripts/Test-Integration.ps1
# Inclui tambem a atividade de pedidos, previamente compilada:
pwsh -File scripts/Test-Integration.ps1 -IncludeOrders
```

O script usa portas temporarias 15104, 15289 e 15109, encerra somente os processos que iniciou e grava logs/resultados em artifacts/. Cria emails locais de teste e mensagens na fila de falhas; nao usa Gmail nem altera credenciais. Os containers permanecem disponiveis para inspecao.

Pare a infraestrutura com docker compose -p authapp-aula stop. Apesar do nome, AuthApp nao implementa login, senha, JWT, autorizacao ou banco. Cadastro apenas gera identificador e publica evento. Veja as limitacoes e diferencas da base na documentacao de arquitetura.
