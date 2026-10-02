using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using TurnosLavadero.API.Configuration;
using TurnosLavadero.API.Notifications;
using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.IntegrationTests;

public sealed class SmtpNotificationSenderTests
{
    [Fact]
    public async Task Send_WithLocalSmtp_DeliversRealMessageToTransport()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var received = ReceiveAsync(listener, timeout.Token);
            var sender = new SmtpNotificationSender(Options.Create(new SmtpOptions {
                Host = "127.0.0.1", Port = port, From = "lavadero@example.test", EnableSsl = false }));
            var turno = new Turno {
                Id = Guid.NewGuid(), FechaHora = new DateTimeOffset(2026, 12, 10, 15, 0, 0, TimeSpan.FromHours(-3)),
                Cliente = new Cliente { Nombre = "Juan", EmailContacto = "cliente@example.test" },
                Servicio = new Servicio { Nombre = "Lavado completo" } };
            await sender.SendTurnoReminderAsync(turno, timeout.Token);
            var message = (await received).Replace("\r\n", "\n");
            Assert.Contains("cliente@example.test", message);
            Assert.Contains("lavadero@example.test", message);
            var body = message[(message.IndexOf("\n\n", StringComparison.Ordinal) + 2)..];
            if (message.Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
                body = Encoding.UTF8.GetString(Convert.FromBase64String(body));
            Assert.Contains("10/12/2026", body);
            Assert.Contains("15:00", body);
        }
        finally { listener.Stop(); }
    }

    [Fact]
    public async Task Send_WithoutConfiguration_ThrowsInsteadOfSimulatingDelivery()
    {
        var sender = new SmtpNotificationSender(Options.Create(new SmtpOptions()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendTurnoReminderAsync(new Turno()));
    }

    private static async Task<string> ReceiveAsync(TcpListener listener, CancellationToken token)
    {
        using var connection = await listener.AcceptTcpClientAsync(token);
        using var stream = connection.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };
        await writer.WriteLineAsync("220 localhost SMTP test");
        var message = new StringBuilder();
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 End data with <CRLF>.<CRLF>");
                while (await reader.ReadLineAsync(token) is { } body && body != ".") message.AppendLine(body);
                await writer.WriteLineAsync("250 Accepted");
                return message.ToString();
            }
            await writer.WriteLineAsync("250 OK");
        }
        throw new InvalidOperationException("No se recibió el mensaje SMTP de prueba.");
    }
}
