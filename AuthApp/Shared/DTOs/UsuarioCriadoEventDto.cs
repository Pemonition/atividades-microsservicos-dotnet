using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs;

public class UsuarioCriadoEventDto
{
    public Guid Id { get; set; }
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(320)]
    public string Email { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}
