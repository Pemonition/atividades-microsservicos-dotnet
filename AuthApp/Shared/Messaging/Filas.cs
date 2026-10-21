namespace Shared.Messaging;

public static class Filas
{
    // Nova fila evita conflito com a fila nao duravel da aula original.
    public const string UsuariosCriados = "fila-usuario-criado-v2";
    public const string Falhas = "fila-usuario-criado-v2-falhas";
    public static Dictionary<string, object?> Argumentos() => new()
    {
        ["x-dead-letter-exchange"] = "",
        ["x-dead-letter-routing-key"] = Falhas
    };
}
