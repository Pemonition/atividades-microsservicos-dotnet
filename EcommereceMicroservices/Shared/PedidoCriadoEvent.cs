namespace Shared;

public record PedidoCriadoEvent(
    Guid PedidoId,
    string ClienteNome,
    string ClienteEmail,
    decimal ValorTotal,
    DateTime DataCriacao
    );
