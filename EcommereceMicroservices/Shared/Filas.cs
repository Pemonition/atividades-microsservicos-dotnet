namespace Shared;

public static class Filas
{
    public const string PedidosCriados = "pedidos-queue";
    public const string EmailsAtualizacao = "pedidos-atualizados-email";
    public const string EmailsFalhos = "pedidos-atualizados-email-falhos";

    public static Dictionary<string, object?> ArgumentosEmail() => new()
    {
        ["x-dead-letter-exchange"] = "",
        ["x-dead-letter-routing-key"] = EmailsFalhos
    };
}
