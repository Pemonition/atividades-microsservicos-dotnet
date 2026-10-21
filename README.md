# Aulas e práticas de desenvolvimento

| Material | Atividade | Guia |
| --- | --- | --- |
| EcommereceMicroservices — 19/10/2026 | EmailWorker com dados de atualização de pedidos via RabbitMQ | [Executar e entender](EcommereceMicroservices/README.md) |
| AuthApp — 20/10/2026 | Documentar arquitetura/endpoints/stack e implementar EmailService com SMTP | [Executar e entender](AuthApp/README.md) |

Atividades de Hugo (Pemonition), desenvolvidas com assistencia de IA a partir dos repositorios publicos do professor Rannyer. Fontes: [AuthApp](https://github.com/rannyer/AuthApp) e [EcommereceMicroservices](https://github.com/rannyer/EcommereceMicroservices).

Cada projeto documenta o que implementa, o que foi testado e as limitações didáticas. Os composes usam as mesmas portas para RabbitMQ/Mailpit: execute uma infraestrutura por vez ou reutilize os containers.

Aprendizados iniciais: [padrões da atividade de pedidos](EcommereceMicroservices/docs/padroes-da-atividade.md) e [decisões da atividade de cadastro](AuthApp/docs/arquitetura.md). Essas decisões têm escopo da aula e não constituem regras universais de produção.

## Entrega e validacao

Veja [roteiro de apresentacao](ENTREGA.md) e [resultados da integracao real](validacao/integration-result.json). Ambas as solucoes compilam; 7 testes no AuthApp e 4 testes xUnit nos pedidos passaram. Os dois fluxos foram validados com RabbitMQ e Mailpit, incluindo falha SMTP e filas de erros. Gmail externo ainda nao testado.
