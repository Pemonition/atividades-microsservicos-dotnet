# Validação — 21/10/2026

Solução compilada no Windows com SDK .NET 10.0.302, zero erros e zero avisos. O executor iniciou processos reais Restaurante.Server e Cardapio.Api, usou HTTP/2/gRPC real, HTTP/JSON real e arquivo SQLite temporário.

Resultado: **8/8 cenários aprovados**.

1. Cozinha retorna três lanches por gRPC e preço em centavos.
2. Vitrine retorna JSON com preço decimal, estoque e disponibilidade.
3. Filtro de disponíveis elimina itens sem estoque.
4. Alteração gRPC de estoque é refletida na próxima consulta HTTP.
5. Estoque negativo retorna InvalidArgument.
6. ID inexistente retorna NotFound.
7. Reinício da cozinha preserva a alteração no SQLite.
8. Cozinha encerrada resulta em HTTP 503; health da vitrine continua 200.

Reprodução na pasta Lanchonete:

```powershell
dotnet restore Lanchonete.sln
dotnet build Lanchonete.sln --no-restore
dotnet run --project Tests --no-build
```

Nesta máquina foi reutilizado o cache C:\Users\hugoj\Dev\.nuget-aulas por meio da opção --packages no restore; isso não é obrigatório em outros ambientes.

Os testes não dependem do banco da demonstração, não fazem pedidos de venda nem simulam cargas concorrentes. O mapeamento de deadline para 504 existe no código, mas o cenário de deadline não foi exercitado. HTTP/2 local sem TLS foi usado na integração; TLS/autorização em hospedagem pública não foram configurados.
