namespace TurnosLavadero.API.Configuration;

public sealed class RecordatorioOptions
{
    public const string SectionName = "Recordatorios";
    public bool Habilitados { get; set; }
    public int IntervaloMinutos { get; set; } = 5;
    public int AnticipacionHoras { get; set; } = 24;
}
