# Decisões da proposta

## 1. Curso e módulos como domínio próprio

O professor abriu a atividade para banco de dados e propostas dos alunos. O usuário sugeriu ensino personalizável para artistas/tatuadores. O MVP usa curso, módulos e referências para aplicar o exercício a esse contexto, em vez de apenas guardar o resultado do scraping.

Os campos de criador e estúdio permitem personalizar o catálogo. Eles ainda não representam uma conta autenticada ou um cliente SaaS.

## 2. SQLite e EF Core

SQLite fornece persistência em arquivo e facilita a demonstração sem instalar outro servidor. EF Core mantém relações, índices e transações no código .NET. O teste reinicia a API e abre o banco diretamente para conferir os registros; armazenamento em memória não satisfaria esse requisito.

Limite: EnsureCreated cria o esquema inicial, mas não aplica alterações incrementais. Para continuar usando o mesmo banco em versões futuras, adotar migrações. PostgreSQL é uma alternativa quando implantação e concorrência justificarem um servidor de banco.

## 3. gRPC na importação

O contrato do professor foi preservado para demonstrar interoperabilidade .NET/Python. A API precisa do resultado para concluir a importação, então aguarda a chamada. As atividades anteriores de email usam fila porque o efeito pode acontecer depois da resposta HTTP.

Timeout, deadline e cancelamento reduzem esperas sem limite definido. O canal .NET é reutilizado pelo contêiner DI. Se a fonte ou o Python falhar, a API sinaliza erro e não grava referências parciais.

## 4. Texto de preço e conteúdo das referências

PrecoTexto preserva o dado do contrato da aula, incluindo símbolo monetário. Ele não é usado para cálculos, cobrança ou preço do curso. Para comercializar cursos, definir valor/moeda e integrar pagamentos em outro fluxo.

O catálogo de livros demonstra coleta; ele não garante pertinência pedagógica. Uma evolução deve oferecer curadoria manual, fonte/URL por referência e critérios de relevância. Não presumir que todo livro coletado ensina tatuagem.

## 5. Teste de integração independente da internet

Uma fonte HTTP local entrega HTML de exemplo, enquanto API .NET, servidor gRPC e arquivo SQLite são reais. Isso permite testar erro HTTP, serviço indisponível, repetição e persistência de forma controlada. O resultado não comprova a disponibilidade atual do site externo.

## 6. Caminho para SaaS

Antes de vender: validar uma experiência de curso com um criador, implementar autenticação/propriedade, depois isolamento de organizações com testes. Adicionar publicação, matrícula, progresso e mídia conforme a necessidade. Pagamentos e hospedagem vêm após esses fundamentos.

Não declarar um sistema multi-tenant apenas por existir um campo Estudio. O isolamento deve ser imposto em leitura e escrita, associado à identidade e verificado por testes entre clientes distintos.
