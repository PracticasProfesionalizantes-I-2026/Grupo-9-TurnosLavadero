namespace TurnosLavadero.DataAccess.Entities;

public sealed class Servicio
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Turno> Turnos { get; set; } = [];
}
