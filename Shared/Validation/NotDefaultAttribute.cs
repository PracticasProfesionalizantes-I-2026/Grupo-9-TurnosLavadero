using System.ComponentModel.DataAnnotations;

namespace TurnosLavadero.Shared.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotDefaultAttribute : ValidationAttribute
{
    public NotDefaultAttribute() : base("El campo {0} es obligatorio y no puede tener su valor predeterminado.") { }
    public override bool IsValid(object? value) => value switch
    {
        Guid id => id != Guid.Empty,
        DateTimeOffset date => date != default,
        _ => false
    };
}
