using Grpc.Core;
using Lanchonete.Grpc;
using Microsoft.EntityFrameworkCore;

namespace Restaurante.Server;

public sealed class CozinhaService(EstoqueDb db) : Cozinha.CozinhaBase
{
    public override async Task<ListarLanchesResponse> ListarLanches(ListarLanchesRequest request, ServerCallContext context)
    {
        var lanches = await db.Lanches.AsNoTracking().OrderBy(l => l.Id).ToListAsync(context.CancellationToken);
        var response = new ListarLanchesResponse();
        response.Lanches.AddRange(lanches.Select(Converter));
        return response;
    }

    public override async Task<Lanche> AtualizarEstoque(AtualizarEstoqueRequest request, ServerCallContext context)
    {
        if (request.LancheId <= 0 || request.Quantidade < 0 || request.Quantidade > 1000000)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id deve ser positivo e quantidade deve estar entre 0 e 1000000."));
        var lanche = await db.Lanches.FindAsync([request.LancheId], context.CancellationToken);
        if (lanche is null) throw new RpcException(new Status(StatusCode.NotFound, "Lanche nao encontrado."));
        lanche.Estoque = request.Quantidade;
        await db.SaveChangesAsync(context.CancellationToken);
        return Converter(lanche);
    }

    private static Lanche Converter(LancheEstoque lanche) => new()
    {
        Id = lanche.Id, Nome = lanche.Nome, PrecoCentavos = lanche.PrecoCentavos, Estoque = lanche.Estoque
    };
}
