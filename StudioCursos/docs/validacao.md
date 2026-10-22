# Validação — 21/10/2026

## Ambiente e build

Windows, SDK .NET 10.0.302, Python 3.14.7. API compilada sem erros/avisos após atualização explícita do bundle SQLite. Dependências .NET registradas em Api/packages.lock.json; dependências Python fixadas em python/requirements.txt.

## Testes executados

Comandos na raiz do repositório:

```powershell
dotnet restore StudioCursos/Api/StudioCursos.Api.csproj
dotnet build StudioCursos/Api/StudioCursos.Api.csproj --no-restore
cd StudioCursos/python
.\.venv\Scripts\python.exe -m unittest -v test_server test_integracao
```

Cinco métodos de teste: quatro verificações de parsing/erros Python e um cenário integrado que cobre o ciclo do curso. A integração usa processos reais .NET e Python, gRPC real, fonte HTTP local e arquivo SQLite temporário.

| Verificação | Evidência |
| --- | --- |
| Parsing | Extrai título/preço e limita a cinco itens |
| HTML inválido | Falha explícita; não representa erro como lista vazia |
| Timeout HTTP | Configurado e traduzido para status gRPC indisponível |
| Criação | POST retorna 201 com três módulos |
| Persistência | Reinicia a API e consulta o mesmo curso no mesmo arquivo SQLite |
| Importação gRPC | Duas referências da fonte local gravadas |
| Repetição | Segunda importação adiciona zero referências |
| Validação | Ordens de módulos duplicadas retornam 400 |
| Personalização | Altera estúdio, substitui módulos e mantém referências |
| Falha HTTP da fonte | API retorna 503; quantidade de referências não muda |
| Python encerrado | Consulta do curso funciona; importação retorna 503 |
| Exclusão | DELETE 204; GET 404; módulos/referências removidos em cascata |

O primeiro teste integrado identificou rastreamento incorreto de novas referências no EF Core; a inserção foi explicitada e o ciclo passou após a correção. O teste permanece cobrindo esse comportamento.

## Limites da evidência

O teste controlado não comprova scraping de books.toscrape.com, disponibilidade pública, experiência de aluno, cobrança ou isolamento entre clientes SaaS. Não executa cenários de carga, edição concorrente ou migração de esquema. O mapeamento de deadline para HTTP 504 existe no código, mas não foi exercitado pelo cenário integrado.

Não foram alterados os testes das duas atividades anteriores de email para acomodar esta proposta. Elas continuam em pastas próprias.

## Resultado final

Suite final: 5 testes aprovados, 0 falhas, em 13,912 segundos. A verificacao final inclui referencias mantidas no retorno da edicao do curso. Build final sem erros e sem avisos. Nenhum pagamento, envio de email ou conta SaaS foi criado por esta atividade.

## Verificacao real da fonte da aula

A demonstracao local executou API .NET -> gRPC Python -> https://books.toscrape.com/ com sucesso: HTTP 200 na importacao, 5 referencias adicionadas e 5 referencias no total do curso. O curso de desenho autoral foi cadastrado no SQLite local. Os testes automatizados continuam usando fonte local para serem repetiveis. Esta consulta pontual nao garante disponibilidade futura do site.
