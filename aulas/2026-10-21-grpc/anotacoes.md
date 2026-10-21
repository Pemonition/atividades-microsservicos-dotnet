# Aula de 21/10/2026 — .NET, Python e gRPC

Registro incremental das capturas enviadas pelo aluno. As imagens mostram trechos da apresentacao e do editor; nao substituem o repositorio completo. Nao foram transcritas mensagens pessoais, participantes ou endereco da reuniao.

## Captura 1 — arquitetura demonstrada

Titulo: "Exemplo: API .NET + Servico Python de web scraping".

Componentes visiveis: Usuario (app/site), API .NET (regras de negocio), Servico Python (scraping) e Site alvo (pagina da loja).

Primeiro passo visivel: HTTP + JSON. O usuario pergunta a API o preco de um produto. A apresentacao esta no passo 1 de 7; os demais passos ainda nao foram enviados.

Pacotes mencionados no chat da aula: Google.Protobuf 3.28, Grpc.Net.Client 2.66 e Grpc.Tools 2.66. O comando parcialmente visivel especifica Google.Protobuf 3.28.3. As versoes exatas devem ser confirmadas no csproj completo; nao sao uma recomendacao de atualizacao de dependencias.

## Captura 2 — contrato produto.proto

- syntax = "proto3";
- package produto;
- service ScraperService;
- rpc ObterProdutos recebe VazioRequest; o nome completo do retorno fica cortado na imagem.
- message VazioRequest {};
- message ProdutoDto: string titulo = 1; string preco = 2;
- message ListaProdutosResponse: repeated ProdutoDto produtos = 1;

Pelo contexto, ListaProdutosResponse parece ser o retorno da operacao, mas isso e inferencia ate termos a linha completa.

O preco esta representado como string no exemplo. Registrar a escolha como material didatico; ao analisar o repositorio, verificar se representa texto bruto da pagina ou um valor para calculos, e discutir moeda e precisao conforme o requisito.

## Captura 3 — configuracao do projeto

Projeto ScraperApi com SDK Microsoft.NET.Sdk.Web. Um ItemGroup inclui Protobuf apontando para Protos\produto.proto. O atributo seguinte comeca com GrpcSer..., mas o nome/valor completo nao aparece.

Ha uma referencia de pacote com PrivateAssets igual a all; o pacote correspondente nao esta visivel nessa captura. Tambem aparecem referencias Microsoft.AspNetCore... e Swashbuckle.AspNetCore..., cortadas a direita.

Nao assumir o valor de GrpcServices nem as versoes desses pacotes sem o arquivo completo. A arvore do projeto mostra Controllers, GrpcClient, Protos e Services.

## Captura 4 — geracao de codigo Python

A arvore mostra produto.proto, produto_pb2.py, produto_pb2_grpc.py e server.py. O arquivo aberto e produto_pb2.py, com cabecalho de codigo gerado a partir de produto.proto, versao de geracao Protobuf Python 7.35.1 e validacao de versao do runtime.

Comando mostrado no terminal, transcrito da captura:

```sh
python3 -m grpc_tools.protoc -I. --python_out=. --grpc_python_out=. produto.proto
```

Os arquivos gerados estao presentes, mas a captura nao comprova que o servidor iniciou nem que uma chamada .NET -> Python funcionou. O conteudo de server.py ainda nao aparece. A versao do cabecalho nao comprova as versoes exatas de grpcio/grpcio-tools instaladas.

## Proximas evidencias uteis

- Repositorio/commit da aula ou csproj e proto completos.
- Implementacao do cliente .NET e servidor Python.
- Enderecos, portas e configuracao TLS usados na demonstracao.
- Implementacao do scraping e tratamento de erros/timeouts.
- Requisitos ou exercicio definidos pelo professor.

Ainda nao foi criado ou executado um projeto gRPC local a partir dessas imagens.
