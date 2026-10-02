using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TurnosLavadero.API.Configuration;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.API.Notifications;

public sealed class SmtpNotificationSender(IOptions<SmtpOptions> options) : INotificationSender
{
    public async Task SendTurnoReminderAsync(Turno turno, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.Host) || !MailAddress.TryCreate(config.From, out _))
            throw new InvalidOperationException("Configure Smtp:Host y Smtp:From antes de procesar recordatorios.");
        if (!MailAddress.TryCreate(turno.Cliente.EmailContacto, out _))
            throw new InvalidOperationException("El cliente no posee un correo electrónico válido.");

        var argentina = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        var fecha = TimeZoneInfo.ConvertTime(turno.FechaHora, argentina);
        using var message = new MailMessage(config.From, turno.Cliente.EmailContacto)
        {
            Subject = "Recordatorio de tu turno en el lavadero",
            Body = $"Hola {turno.Cliente.Nombre}, recordá tu turno para {turno.Servicio.Nombre} "
                + $"el {fecha:dd/MM/yyyy} a las {fecha:HH:mm} (hora argentina). "
                + "Si necesitás cambiarlo o cancelarlo, ingresá al sistema.",
            IsBodyHtml = false
        };
        using var smtp = new SmtpClient(config.Host, config.Port)
        {
            EnableSsl = config.EnableSsl,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(config.User))
            smtp.Credentials = new NetworkCredential(config.User, config.Password);
        await smtp.SendMailAsync(message, cancellationToken);
    }
}
