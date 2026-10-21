# Validação — 21/10/2026

| Verificação | Resultado observado |
| --- | --- |
| Restauração dos quatro projetos | Concluída |
| Build AuthApp.sln | Aprovado, zero erros e zero avisos |
| Testes automatizados via dotnet run --project Tests | 7/7 aprovados |
| SMTP TCP local | Aceita mensagem e confere destinatário, remetente, assunto, nome, ID e data |
| Recusa SMTP | Erro propagado; envio não tratado como sucesso |
| Dados inválidos | E-mail inválido e ID vazio rejeitados antes da rede; validação do DTO de cadastro |
| Configuração SMTP | Gmail com STARTTLS aceito; servidor remoto sem TLS e login incompleto/inseguro rejeitados |
| UserService HTTP real | Inicia; Swagger expõe POST /api/Usuario; entrada inválida retorna 400 |
| Integração HTTP → RabbitMQ → SMTP | Aprovada: cadastro 202, evento consumido e email conferido no Mailpit |
| Gmail externo | Não executado: conta e senha de aplicativo não configuradas |

Na primeira verificacao, o Docker Engine estava indisponivel. Apos o usuario abrir o Docker Desktop, o motor respondeu, as imagens foram baixadas e RabbitMQ/Mailpit iniciaram. A integracao foi retomada e aprovada. Nao foi necessario reinstalar Docker/WSL.

Os testes SMTP usam um servidor de teste TCP que implementa o protocolo necessário ao MailKit. Isso comprova montagem, envio local e propagação de recusa; não equivale a integração com RabbitMQ, Gmail ou entrega externa.

Comandos de reprodução:

```powershell
dotnet restore AuthApp.sln
dotnet build AuthApp.sln --no-restore
dotnet run --project Tests --no-build
```

Nesta máquina a restauração reutilizou o cache de pacotes da atividade EcommereceMicroservices, passando --packages ../EcommereceMicroservices/.packages. Isso não é requisito para outros ambientes.

Para reproduzir a integracao: subir o compose, iniciar os dois serviços, cadastrar um usuário e conferir o Mailpit. Para testar a fila de falhas, parar somente o Mailpit, cadastrar outro usuário e verificar fila-usuario-criado-v2-falhas no RabbitMQ. Após corrigir o SMTP, republicar o evento pela ferramenta de administração.

Nenhum envio de email externo foi realizado. A entrega esta publicada no repositorio Pemonition/atividades-microsservicos-dotnet.

## Integracao executada

Script: scripts/Test-Integration.ps1 -IncludeOrders. Cadastro pela fila, envio direto e falha SMTP aprovados. Conferidos nome, ID, data e destinatario no email; entrada invalida retornou 400; envio direto retornou 200 apos SMTP e 502 quando indisponivel; mensagem com falha preservada na fila de falhas. Gmail externo permanece sem teste.

O script teve um filtro de destinatario corrigido: no PowerShell/.NET deste ambiente, Address tambem e metodo de array; os destinatarios agora sao enumerados explicitamente. O email ja havia sido recebido antes dessa correcao na verificacao.

Resultados detalhados e logs locais: artifacts/integration-result.json e artifacts/*.log. O ZIP de entrega inclui a documentacao dos resultados, mas exclui logs e saidas de compilacao.
