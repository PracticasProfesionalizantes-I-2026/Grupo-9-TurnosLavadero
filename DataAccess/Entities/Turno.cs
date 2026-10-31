using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.DataAccess.Entities;

public sealed class Turno
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public Guid ServicioId { get; set; }
    private DateTimeOffset fechaHora;
    private int duracionMinutos = 30;
    public DateTimeOffset FechaHora
    {
        get => fechaHora;
        set { fechaHora = value; FechaFin = value.AddMinutes(duracionMinutos); }
    }
    // Copia de la duración al reservar: editar el catálogo no mueve reservas existentes.
    public int DuracionMinutos
    {
        get => duracionMinutos;
        set { duracionMinutos = value; FechaFin = fechaHora.AddMinutes(value); }
    }
    public DateTimeOffset FechaFin { get; private set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Confirmado;
    public DateTimeOffset FechaCreacion { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Servicio Servicio { get; set; } = null!;
    public ICollection<Recordatorio> Recordatorios { get; set; } = [];
}
