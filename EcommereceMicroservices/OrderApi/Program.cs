using System.Net.Mail;
using OrderApi.Dtos;
using OrderApi.Services;
using Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IPedidoProducer, RabbitMqProducer>();
builder.Services.AddSingleton<PedidoStore>();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/pedidos", async (CriarPedidoRequest request, PedidoStore store, IPedidoProducer producer) =>
{
    if (string.IsNullOrWhiteSpace(request.ClienteNome) ||
        !MailAddress.TryCreate(request.ClienteEmail, out var endereco) ||
        endereco.Address != request.ClienteEmail || request.ValorTotal < 0)
        return Results.BadRequest(new { Erro = "Informe nome, email valido e valor total nao negativo." });

    var evento = new PedidoCriadoEvent(Guid.NewGuid(), request.ClienteNome.Trim(),
        request.ClienteEmail, request.ValorTotal, DateTime.UtcNow);
    var pedido = await store.CriarAsync(evento, producer);
    return Results.Accepted($"/pedidos/{pedido.PedidoId}", new { Status = "Processamento iniciado", Pedido = pedido });
});

app.MapGet("/pedidos/{id:guid}", async (Guid id, PedidoStore store) =>
{
    var pedido = await store.ObterAsync(id);
    return pedido is null ? Results.NotFound() : Results.Ok(pedido);
});

app.MapPut("/pedidos/{id:guid}", async (Guid id, AtualizarPedidoRequest request, PedidoStore store, IPedidoProducer producer) =>
{
    if (string.IsNullOrWhiteSpace(request.Status) || request.Status.Length > 100 || request.ValorTotal < 0)
        return Results.BadRequest(new { Erro = "Informe status de ate 100 caracteres e valor total nao negativo." });
    var pedido = await store.AtualizarAsync(id, request.Status.Trim(), request.ValorTotal, producer);
    return pedido is null ? Results.NotFound() : Results.Ok(pedido);
});

app.Run();
