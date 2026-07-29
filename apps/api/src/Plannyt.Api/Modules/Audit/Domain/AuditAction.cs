namespace Plannyt.Api.Modules.Audit.Domain;

public readonly record struct AuditAction
{
    private AuditAction(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AuditAction Define(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 120)
        {
            throw new ArgumentException(
                "La acción de auditoría debe tener entre 1 y 120 caracteres.",
                nameof(value));
        }

        return new AuditAction(value);
    }

    public override string ToString() => Value;
}
