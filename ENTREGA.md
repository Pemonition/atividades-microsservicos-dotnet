# Entrega das atividades — professor Rannyer

## Cadastro e email — AuthApp

Fonte: https://github.com/rannyer/AuthApp

Implementado envio SMTP assincrono no EmailService com MailKit, consumo RabbitMQ, validacao, configuracao Gmail e ambiente local Mailpit. Correcao do registro de controllers no UserService. Documentacao de arquitetura, endpoints e stack em AuthApp/docs. Implementacao assistida por IA (Codex).

## Pedidos e EmailWorker

Fonte: https://github.com/rannyer/EcommereceMicroservices

Implementados criacao, consulta e atualizacao de pedidos, contrato de atualizacao, publicacao RabbitMQ e EmailWorker com envio dos dados alterados. Documentacao e limites em EcommereceMicroservices/README.md.

## Demonstrar

1. Abrir uma das solucoes e executar restore/build conforme seu README.
2. Iniciar Docker Desktop. Subir apenas uma infraestrutura RabbitMQ/Mailpit, pois os dois composes usam as mesmas portas.
3. Iniciar os dois servicos da atividade desejada, conforme README.
4. Executar o POST de cadastro ou POST/PUT de pedido via Swagger ou arquivos .http.
5. Mostrar o email em http://localhost:8025 e as filas em http://localhost:15672 (guest / guest, somente local).
6. Mostrar os resultados em docs/validacao.md e, para integracao automatizada, AuthApp/scripts/Test-Integration.ps1.

## Testes

- AuthApp: dotnet run --project Tests, na pasta AuthApp.
- Pedidos: dotnet test Tests/Tests.csproj, na pasta EcommereceMicroservices.
- Integracao dos dois: compilar ambas as solucoes, manter RabbitMQ/Mailpit ativos e executar pwsh -File scripts/Test-Integration.ps1 -IncludeOrders na pasta AuthApp.

Os cenarios de falha deixam mensagens de teste nas filas de falhas para inspecao. Os scripts encerram somente os processos temporarios que iniciaram.

O modo local nao encaminha emails para caixas externas. Gmail esta implementado por configuracao, mas seu uso exige credencial privada do titular; os resultados reais estao documentados nos projetos. As solucoes sao didaticas e nao incluem todos os requisitos de producao.

As capturas da aula de gRPC de 21/10 estao registradas em aulas/2026-10-21-grpc/anotacoes.md. Elas sao material de estudo; nao fazem parte da implementacao das duas atividades acima.

## Resultado das atividades de email

A extensao da aula de 21/10 esta em [StudioCursos](StudioCursos/README.md): proposta autoral de catalogo de cursos para artistas, com SQLite, modulos personalizaveis e importacao de referencias via gRPC/Python. O roteiro de demonstracao e os limites para evoluir a SaaS estao no README dessa atividade.

Ambas as solucoes compilaram sem erros/avisos. AuthApp: 7 testes automatizados aprovados. Pedidos: 4 testes xUnit aprovados. Integracao real com RabbitMQ e Mailpit aprovada nos dois projetos, inclusive falhas SMTP e filas de erros. Envio Gmail externo ainda nao executado.

## Resultado da proposta StudioCursos

Build sem erros/avisos. Cinco testes aprovados, incluindo ciclo integrado com arquivo SQLite e processos .NET/Python reais: persistencia apos reinicio, importacao idempotente, personalizacao, falhas e cascata. [Roteiro da apresentacao](StudioCursos/README.md#como-apresentar-ao-professor).
