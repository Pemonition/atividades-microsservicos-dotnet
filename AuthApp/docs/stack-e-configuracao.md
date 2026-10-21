# Stack e configuração

| Tecnologia | Versão/configuração | Uso |
| --- | --- | --- |
| C# | 12, padrão do .NET 8 | Linguagem |
| .NET / ASP.NET Core | net8.0 | Runtime, controllers, DI e hosted service |
| RabbitMQ.Client | 7.2.2 | AMQP assíncrono |
| MailKit | 4.17.0 | SMTP e TLS |
| MimeKit | Transitiva do MailKit | MIME e UTF-8 |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger/OpenAPI |
| Microsoft.AspNetCore.OpenApi | 8.0.30 | Dependência mantida da base |
| System.Text.Json / DataAnnotations | .NET | JSON e validação |
| RabbitMQ | rabbitmq:4-management | Broker e painel |
| Mailpit | axllent/mailpit:v1.27 | SMTP de teste |
| Gmail SMTP | smtp.gmail.com, 587, STARTTLS | Envio externo opcional |
| Docker Compose | Docker Desktop | Infraestrutura local |

SDK posterior é aceito pelo global.json, mantendo o target net8.0. Sem EF Core, banco ou autenticação. Tags Docker não fixam digest.

## Variáveis de ambiente

RabbitMQ__Uri é lida pelos dois serviços; Smtp__* pelo EmailService. O separador __ representa seção do ASP.NET Core. Não versionar credenciais.

| Variável | Padrão local |
| --- | --- |
| RabbitMQ__Uri | amqp://guest:guest@localhost:5672/ |
| Smtp__Host | localhost |
| Smtp__Port | 1025 |
| Smtp__From | cadastro@loja.local |
| Smtp__FromName | AuthApp |
| Smtp__Security | None, SMTP local sem login |
| Smtp__User / Smtp__Password | vazios |

## Gmail

No terminal do EmailService:

```powershell
$env:Smtp__Host = 'smtp.gmail.com'
$env:Smtp__Port = '587'
$env:Smtp__Security = 'StartTls'
$env:Smtp__From = 'SEU_EMAIL@gmail.com'
$env:Smtp__User = 'SEU_EMAIL@gmail.com'
$senhaSmtp = Read-Host 'Senha de aplicativo Google' -AsSecureString
$env:Smtp__Password = [System.Net.NetworkCredential]::new('', $senhaSmtp).Password
dotnet run --project EmailService --launch-profile http
Remove-Item Env:Smtp__Password
```

Para porta 465, usar SslOnConnect. Não desabilitar validação de certificados. Use a conta autorizada como remetente.

A senha de aplicativo exige verificação em duas etapas e pode estar indisponível conforme a conta/política da organização. Não usar senha normal da conta. O Google recomenda OAuth quando disponível; esta atividade implementa senha de aplicativo, não OAuth. O titular configura a conta e a credencial no próprio terminal; não precisa enviá-la na conversa.

Referências: [senhas de app](https://support.google.com/mail/answer/185833?hl=pt-BR) e [SMTP Gmail](https://developers.google.com/workspace/gmail/imap/imap-smtp).

Para voltar ao Mailpit, abrir terminal sem as variáveis Smtp__* ou removê-las. O modo local não precisa de senha nem encaminha e-mails para a internet.
