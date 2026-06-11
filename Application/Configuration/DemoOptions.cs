namespace Fmc.Application.Configuration;

public class DemoOptions
{
    public const string SectionName = "Demo";

    /// <summary>Permite POST /api/auth/*/register. Desactivar en demo pública.</summary>
    public bool AllowRegistration { get; set; } = true;

    /// <summary>Expone Swagger UI fuera de Development (p. ej. staging demo).</summary>
    public bool EnableSwagger { get; set; }
}
