namespace OrderApi.Dtos;

public record CriarPedidoRequest(
    string ClienteNome,
    string ClienteEmail,
    decimal ValorTotal
    );
