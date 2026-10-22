using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Restaurante.Server;

var builder = WebApplication.CreateBuilder(args);
var port = builder.Configuration.GetValue("Grpc:Port", 5301);
builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port, endpoint => endpoint.Protocols = HttpProtocols.Http2));
var database = Path.GetFullPath(builder.Configuration["Database:Path"] ?? "data/estoque.db");
Directory.CreateDirectory(Path.GetDirectoryName(database)!);
builder.Services.AddDbContext<EstoqueDb>(options => options.UseSqlite(
    new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = database }.ToString()));
builder.Services.AddGrpc();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EstoqueDb>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Lanches.AnyAsync())
    {
        db.Lanches.AddRange(
            new LancheEstoque { Id = 1, Nome = "X-Salada", PrecoCentavos = 2250, Estoque = 10 },
            new LancheEstoque { Id = 2, Nome = "X-Bacon", PrecoCentavos = 2990, Estoque = 5 },
            new LancheEstoque { Id = 3, Nome = "Vegetariano", PrecoCentavos = 2590, Estoque = 0 });
        await db.SaveChangesAsync();
    }
}
app.MapGrpcService<CozinhaService>();
app.Run();
