using EmailService.DTOs;
using EmailService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace EmailService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly EmailService.Services.EmailService _service;

    public EmailController(EmailService.Services.EmailService service)
    {
        _service = service;
    }

    [HttpPost("boas-vindas")]
    [ProducesResponseType(typeof(StatusEmailResponseDTo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ProcessarBoasVindas([FromBody] UsuarioCriadoEventDto usuario, CancellationToken cancellationToken)
    {
        if (usuario.Id == Guid.Empty || usuario.DataCriacao == default)
            return BadRequest(new { Erro = "Id e DataCriacao sao obrigatorios." });
        try
        {
            await _service.ProcessarBoasVindasAsync(usuario, cancellationToken);
        }
        catch (Exception ex) when (ex is MailKit.Net.Smtp.SmtpCommandException
            or MailKit.Net.Smtp.SmtpProtocolException or MailKit.Security.AuthenticationException
            or IOException or System.Net.Sockets.SocketException)
        {
            return Problem(statusCode: 502, title: "O servidor SMTP nao confirmou o envio.");
        }

        return Ok(new StatusEmailResponseDTo
        {
            Status = "Email aceito pelo servidor SMTP",
            FilaMonitorada = Shared.Messaging.Filas.UsuariosCriados,
            VerficadoEm = DateTime.UtcNow
        });
    }
}
