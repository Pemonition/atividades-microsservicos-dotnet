using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController :  ControllerBase
{
     private readonly UsuarioService _service;

     public UsuarioController(UsuarioService service)
     {
          _service = service;
     }

     [HttpPost]
     [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status202Accepted)]
     [ProducesResponseType(StatusCodes.Status400BadRequest)]
     [ProducesResponseType(StatusCodes.Status500InternalServerError)]
     public async Task<IActionResult> Criar(
          [FromBody] CriarUsuarioRequestDto request,
          CancellationToken cancellationToken)
     {
          var resultado = await _service.CadastrarAsync(request, cancellationToken);
          return Accepted(resultado);
     }

}
