using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public class CriarUsuarioRequestDto
{
    [Required, StringLength(200)]
    public string Nome { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(320)]
    public string Email { get; set; } = string.Empty;
}
