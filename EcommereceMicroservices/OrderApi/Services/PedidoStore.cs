using Shared;

namespace OrderApi.Services;

// Armazenamento didatico em memoria. Os pedidos sao perdidos ao reiniciar a API.
public sealed class PedidoStore
{
    private readonly Dictionary<Guid, Pedido> _pedidos = new();
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public async Task<Pedido> CriarAsync(PedidoCriadoEvent evento, IPedidoProducer producer)
    {
        await _mutex.WaitAsync();
        try
        {
            await producer.PublicarPedidoAsync(evento);
            var pedido = new Pedido(evento.PedidoId, evento.ClienteNome, evento.ClienteEmail,
                evento.ValorTotal, "Criado", evento.DataCriacao);
            _pedidos.Add(pedido.PedidoId, pedido);
            return pedido;
        }
        finally { _mutex.Release(); }
    }

    public async Task<Pedido?> ObterAsync(Guid id)
    {
        await _mutex.WaitAsync();
        try { return _pedidos.GetValueOrDefault(id); }
        finally { _mutex.Release(); }
    }

    public async Task<Pedido?> AtualizarAsync(Guid id, string status, decimal valor, IPedidoProducer producer)
    {
        await _mutex.WaitAsync();
        try
        {
            if (!_pedidos.TryGetValue(id, out var anterior)) return null;
            var atualizado = anterior with { Status = status, ValorTotal = valor, DataAtualizacao = DateTime.UtcNow };
            var evento = new PedidoAtualizadoEvent(Guid.NewGuid(), id, atualizado.ClienteNome,
                atualizado.ClienteEmail, valor, anterior.Status, status, atualizado.DataAtualizacao);
            await producer.PublicarAtualizacaoAsync(evento);
            _pedidos[id] = atualizado;
            return atualizado;
        }
        finally { _mutex.Release(); }
    }
}

public record Pedido(Guid PedidoId, string ClienteNome, string ClienteEmail,
    decimal ValorTotal, string Status, DateTime DataAtualizacao);
