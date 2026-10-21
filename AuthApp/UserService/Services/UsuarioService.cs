using Shared.DTOs;
using UserService.DTOs;
using UserService.Messaging;

namespace UserService.Services;

public class UsuarioService
{
    private readonly RabbitMqProducer _producer;

    public UsuarioService(RabbitMqProducer producer)
    {
        _producer = producer;
    }

    public async Task<UsuarioResponseDto> CadastrarAsync(
        CriarUsuarioRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var usarioId = Guid.NewGuid();
        var dataCriaco = DateTime.UtcNow;
        Console.WriteLine($"[UsuarioService] Usuário '{request.Nome}' criado");

        var evento = new UsuarioCriadoEventDto()
        {
            Id = usarioId,
            Name = request.Nome,
            Email = request.Email,
            DataCriacao = dataCriaco
        };
        await _producer.PublicaUsuarioCriadoAsync(evento, cancellationToken);

        return new UsuarioResponseDto()
        {
            Id = usarioId,
            Name = request.Nome,
            Email = request.Email,
            DataCriacao = dataCriaco
        };


    }
}
