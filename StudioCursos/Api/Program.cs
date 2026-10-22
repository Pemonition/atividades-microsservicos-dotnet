using System.Text.RegularExpressions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Produto;
using StudioCursos;

var builder = WebApplication.CreateBuilder(args);
var databasePath = Path.GetFullPath(builder.Configuration["Database:Path"] ?? "data/studio.db");
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
builder.Services.AddDbContext<CatalogoDb>(o => o.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
    { DataSource = databasePath, ForeignKeys = true }.ToString()));
builder.Services.AddSingleton(_ => GrpcChannel.ForAddress(builder.Configuration["PythonGrpc:Url"] ?? "http://127.0.0.1:50051"));
builder.Services.AddSingleton(sp => new ScraperService.ScraperServiceClient(sp.GetRequiredService<GrpcChannel>()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<CatalogoDb>().Database.EnsureCreatedAsync();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.MapGet("/health", () => Results.Ok(new { status = "ok", aplicacao = "StudioCursos" }));
app.MapGet("/api/cursos", async (CatalogoDb db, CancellationToken ct) =>
    Results.Ok(await db.Cursos.AsNoTracking().OrderBy(c => c.Titulo).Take(100)
        .Select(c => new { c.Id, c.Titulo, c.Area, c.PublicoAlvo, c.Nivel, c.Criador, c.Estudio, c.Cor,
            QuantidadeModulos = c.Modulos.Count, QuantidadeReferencias = c.Referencias.Count }).ToListAsync(ct)));
app.MapGet("/api/cursos/{id:guid}", async (Guid id, CatalogoDb db, CancellationToken ct) =>
{
    var curso = await db.Cursos.AsNoTracking().Include(c => c.Modulos.OrderBy(m => m.Ordem))
        .Include(c => c.Referencias).AsSplitQuery().SingleOrDefaultAsync(c => c.Id == id, ct);
    return curso is null ? Results.NotFound() : Results.Ok(curso);
});
app.MapPost("/api/cursos", async (CursoInput input, CatalogoDb db, CancellationToken ct) =>
{
    var errors = Validar(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var curso = new Curso(); Preencher(curso, input);
    db.Cursos.Add(curso); await db.SaveChangesAsync(ct);
    return Results.Created($"/api/cursos/{curso.Id}", curso);
});
app.MapPut("/api/cursos/{id:guid}", async (Guid id, CursoInput input, CatalogoDb db, CancellationToken ct) =>
{
    var errors = Validar(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var curso = await db.Cursos.Include(c => c.Modulos).Include(c => c.Referencias).AsSplitQuery()
        .SingleOrDefaultAsync(c => c.Id == id, ct);
    if (curso is null) return Results.NotFound();
    // Exclusao e substituicao atomicas evitam colisao no indice de ordem dos modulos.
    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    db.Modulos.RemoveRange(curso.Modulos); curso.Modulos.Clear(); await db.SaveChangesAsync(ct);
    Preencher(curso, input); db.Modulos.AddRange(curso.Modulos);
    await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    return Results.Ok(curso);
});
app.MapDelete("/api/cursos/{id:guid}", async (Guid id, CatalogoDb db, CancellationToken ct) =>
{
    var curso = await db.Cursos.FindAsync([id], ct);
    if (curso is null) return Results.NotFound();
    db.Cursos.Remove(curso); await db.SaveChangesAsync(ct); return Results.NoContent();
});
app.MapPost("/api/cursos/{id:guid}/referencias/importar", async (Guid id, CatalogoDb db,
    ScraperService.ScraperServiceClient scraper, CancellationToken ct) =>
{
    var curso = await db.Cursos.Include(c => c.Referencias).SingleOrDefaultAsync(c => c.Id == id, ct);
    if (curso is null) return Results.NotFound();
    ListaProdutosResponse response;
    try
    {
        response = await scraper.ObterProdutosAsync(new VazioRequest(), deadline: DateTime.UtcNow.AddSeconds(10), cancellationToken: ct);
    }
    catch (RpcException ex) when (!ct.IsCancellationRequested)
    {
        var status = ex.StatusCode == StatusCode.DeadlineExceeded ? 504 : ex.StatusCode == StatusCode.Unavailable ? 503 : 502;
        return Results.Problem(statusCode: status, title: "Nao foi possivel importar as referencias do servico Python.");
    }
    if (response.Produtos.Count > 5 || response.Produtos.Any(p => string.IsNullOrWhiteSpace(p.Titulo) || p.Titulo.Length > 500 || p.Preco.Length > 100))
        return Results.Problem(statusCode: 502, title: "O servico Python retornou referencias invalidas.");
    var existentes = curso.Referencias.Select(r => (r.Titulo, r.PrecoTexto)).ToHashSet();
    var adicionadas = 0;
    foreach (var produto in response.Produtos)
    {
        if (!existentes.Add((produto.Titulo, produto.Preco))) continue;
        var referencia = new Referencia { CursoId = curso.Id, Titulo = produto.Titulo, PrecoTexto = produto.Preco,
            Origem = "Catalogo demonstrativo da aula (books.toscrape.com)" };
        curso.Referencias.Add(referencia); db.Referencias.Add(referencia); adicionadas++;
    }
    curso.AtualizadoEm = DateTime.UtcNow;
    try { await db.SaveChangesAsync(ct); }
    catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19 })
    { return Results.Conflict(new { erro = "Conflito na importacao; consulte o curso e tente novamente." }); }
    return Results.Ok(new { adicionadas, total = curso.Referencias.Count });
});

app.Run();

static void Preencher(Curso curso, CursoInput input)
{
    curso.Titulo = input.Titulo.Trim(); curso.Area = input.Area.Trim(); curso.PublicoAlvo = input.PublicoAlvo.Trim();
    curso.Nivel = input.Nivel.Trim(); curso.Objetivo = input.Objetivo.Trim(); curso.Criador = input.Criador.Trim();
    curso.Estudio = input.Estudio.Trim(); curso.Cor = input.Cor.Trim(); curso.AtualizadoEm = DateTime.UtcNow;
    curso.Modulos = input.Modulos.OrderBy(m => m.Ordem).Select(m => new Modulo
        { CursoId = curso.Id, Ordem = m.Ordem, Titulo = m.Titulo.Trim(), Conteudo = m.Conteudo.Trim() }).ToList();
}

static Dictionary<string, string[]> Validar(CursoInput input)
{
    var errors = new Dictionary<string, string[]>();
    foreach (var (field, value, limit) in new[] { ("titulo", input.Titulo, 200), ("area", input.Area, 100),
        ("publicoAlvo", input.PublicoAlvo, 300), ("nivel", input.Nivel, 50), ("objetivo", input.Objetivo, 2000),
        ("criador", input.Criador, 150), ("estudio", input.Estudio, 150) })
        if (string.IsNullOrWhiteSpace(value) || value.Length > limit) errors[field] = [$"Campo obrigatorio, ate {limit} caracteres."];
    if (input.Cor is null || !Regex.IsMatch(input.Cor, "^#[0-9a-fA-F]{6}$")) errors["cor"] = ["Use cor hexadecimal, exemplo #B85C38."];
    if (input.Modulos is null || input.Modulos.Count is < 1 or > 30 || input.Modulos.Any(m => m is null || m.Ordem < 1 ||
        string.IsNullOrWhiteSpace(m.Titulo) || m.Titulo.Length > 200 || string.IsNullOrWhiteSpace(m.Conteudo) || m.Conteudo.Length > 20000) ||
        input.Modulos.Select(m => m.Ordem).Distinct().Count() != input.Modulos.Count)
        errors["modulos"] = ["Informe 1 a 30 modulos com ordem positiva unica, titulo ate 200 e conteudo ate 20000 caracteres."];
    return errors;
}
