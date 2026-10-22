using Microsoft.EntityFrameworkCore;

namespace StudioCursos;

public sealed class CatalogoDb(DbContextOptions<CatalogoDb> options) : DbContext(options)
{
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<Referencia> Referencias => Set<Referencia>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Curso>().HasMany(c => c.Modulos).WithOne().HasForeignKey(m => m.CursoId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Curso>().HasMany(c => c.Referencias).WithOne().HasForeignKey(r => r.CursoId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Modulo>().HasIndex(m => new { m.CursoId, m.Ordem }).IsUnique();
        model.Entity<Referencia>().HasIndex(r => new { r.CursoId, r.Titulo, r.PrecoTexto }).IsUnique();
    }
}

public sealed class Curso
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titulo { get; set; } = "";
    public string Area { get; set; } = "";
    public string PublicoAlvo { get; set; } = "";
    public string Nivel { get; set; } = "";
    public string Objetivo { get; set; } = "";
    public string Criador { get; set; } = "";
    public string Estudio { get; set; } = "";
    public string Cor { get; set; } = "#B85C38";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public List<Modulo> Modulos { get; set; } = [];
    public List<Referencia> Referencias { get; set; } = [];
}

public sealed class Modulo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CursoId { get; set; }
    public int Ordem { get; set; }
    public string Titulo { get; set; } = "";
    public string Conteudo { get; set; } = "";
}

public sealed class Referencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CursoId { get; set; }
    public string Titulo { get; set; } = "";
    public string PrecoTexto { get; set; } = "";
    public string Origem { get; set; } = "";
    public DateTime ImportadoEm { get; set; } = DateTime.UtcNow;
}

public sealed record ModuloInput(int Ordem, string Titulo, string Conteudo);
public sealed record CursoInput(string Titulo, string Area, string PublicoAlvo, string Nivel,
    string Objetivo, string Criador, string Estudio, string Cor, List<ModuloInput> Modulos);
