# Endpoints

Profiles HTTP de desenvolvimento, sem autenticação. JSON HTTP em camelCase.

| Serviço | Método e rota | Finalidade | Sucesso |
| --- | --- | --- | --- |
| UserService, porta 5104 | POST /api/Usuario | Validar cadastro e publicar evento | 202 com dados gerados |
| EmailService, porta 5289 | POST /api/Email/boas-vindas | Enviar diretamente, sem fila | 200 após aceitação SMTP |

## Cadastro

```http
POST http://localhost:5104/api/Usuario
Content-Type: application/json

{"nome":"João da Silva","email":"aluno@example.com"}
```

Nome obrigatório até 200 caracteres; e-mail obrigatório em formato válido até 320. Não verifica a existência da caixa ou duplicidade de usuários.

Resposta 202:

```json
{"id":"17fb72e9-62da-43da-b57f-11fb19845b49","name":"João da Silva","email":"aluno@example.com","dataCriacao":"2026-10-21T20:00:00Z"}
```

Entrada usa nome; resposta/evento usam name conforme contrato do professor. O envio é posterior. Não existe GET para consultar esse ID.

Erros: 400 para JSON/DTO inválido; 500 para falha de publicação/configuração RabbitMQ.

## Boas-vindas diretas

```http
POST http://localhost:5289/api/Email/boas-vindas
Content-Type: application/json

{"id":"17fb72e9-62da-43da-b57f-11fb19845b49","name":"João da Silva","email":"aluno@example.com","dataCriacao":"2026-10-21T20:00:00Z"}
```

Obrigatórios: ID diferente de UUID vazio, name não vazio até 200, e-mail válido até 320 e dataCriacao não padrão. Use ISO 8601 com Z ou deslocamento explícito.

Resposta 200:

```json
{"status":"Email aceito pelo servidor SMTP","filaMonitorada":"fila-usuario-criado-v2","verficadoEm":"2026-10-21T20:00:01Z"}
```

verficadoEm mantém a grafia original. filaMonitorada descreve o consumidor hospedado; esta chamada não passou pela fila.

Erros: 400 para dados inválidos; 502 para recusa, indisponibilidade ou falha de autenticação SMTP. Aceite SMTP não significa entrega final.

## OpenAPI e evento

Em Development: GET /swagger e GET /swagger/v1/swagger.json em ambos os serviços. Há exemplos nos arquivos .http. Não existem rotas de login, consulta de usuários, exclusão ou recuperação de senha nesta base.

O evento UsuarioCriadoEventDto contém Id, Name, Email e DataCriacao, serializados pelo produtor com nomes PascalCase. HTTP e mensageria usam políticas de nomes diferentes, compatíveis com seus respectivos consumidores.
