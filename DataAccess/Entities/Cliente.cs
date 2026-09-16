namespace TurnosLavadero.DataAccess.Entities;

public sealed class Cliente
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string EmailContacto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public bool NotificacionesHabilitadas { get; set; } = true;
    public bool Activo { get; set; } = true;

    public ICollection<Turno> Turnos { get; set; } = [];
    public Usuario? Usuario { get; set; }
}
