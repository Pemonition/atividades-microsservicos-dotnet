namespace EmailService.DTOs;

public class StatusEmailResponseDTo
{
    public string Status { get; set; } = string.Empty;
    public string FilaMonitorada { get; set; } = string.Empty;
    public DateTime VerficadoEm { get; set; }

}
