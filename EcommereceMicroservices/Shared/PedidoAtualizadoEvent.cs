namespace Shared;

public record PedidoAtualizadoEvent(
    Guid EventoId,
    Guid PedidoId,
    string ClienteNome,
    string ClienteEmail,
    decimal ValorTotal,
    string StatusAnterior,
    string StatusAtual,
    DateTime DataAtualizacao);
