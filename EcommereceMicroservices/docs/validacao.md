# Validacao — 21/10/2026

- Testes xUnit: 4 aprovados, 0 falhas. Verificam dados do evento de atualizacao, pedido inexistente sem evento, falha de publicacao sem alterar o pedido e sequencia consistente sob atualizacoes concorrentes.
- A API e o EmailWorker compilam com .NET 8 usando SDK 10 instalado.
- Integracao completa HTTP → RabbitMQ → SMTP aprovada apos iniciar Docker: pedido criado (202), atualizado (200) e email recebido no Mailpit com nome, ID, status anterior/atual, valor e data.
- Gmail nao foi testado: credenciais da conta nao configuradas.

Comandos:

```powershell
dotnet restore EcommereceMicroservices.sln
dotnet test Tests/Tests.csproj --no-restore
dotnet build EcommereceMicroservices.sln --no-restore
```

Nesta maquina, o cache inicial localizado dentro da pasta longa do repositorio teve arquivos de recursos ausentes. A restauracao em C:\Users\hugoj\Dev\.nuget-aulas resolveu a compilacao dos testes. Para reproduzir essa alternativa, acrescente --packages C:\Users\hugoj\Dev\.nuget-aulas ao comando restore.

Os testes isolam o RabbitMQ com fake. Nao comprovam publicacao no broker real nem recebimento de email externo. O teste SMTP automatizado da atividade AuthApp cobre o servico daquele projeto, nao o EmailWorker desta atividade.

Para concluir a verificacao integrada, iniciar Docker, subir o compose, executar API/worker, criar pedido e fazer PUT conforme o README; conferir dados do email no Mailpit. Depois provocar indisponibilidade de SMTP e verificar a fila pedidos-atualizados-email-falhos.

Entrega publicada no repositorio Pemonition/atividades-rannyer-microsservicos. As fontes do professor estao referenciadas no README.

## Integracao final executada

Script AuthApp/scripts/Test-Integration.ps1 -IncludeOrders aprovado. Alem do email, verificou inexistente (404), atualizacao invalida (400), falha SMTP com tres tentativas e preservacao em pedidos-atualizados-email-falhos. Processos temporarios encerrados pelo script; RabbitMQ/Mailpit mantidos para inspecao. Gmail externo nao testado.
