namespace Fmc.Api.Branding;

/// <summary>Nombre comercial e identificador interno del backend FMC.</summary>
public static class FmcApp
{
    public const string ProductName = "Find my coffee";
    public const string InternalCode = "FMC";

    public static string ApiTitle => $"{ProductName} ({InternalCode}) API";

    public const string ApiDescription =
        "FMC: cafeterías registradas por Enterprise, ponderación Enterprise Premium, descuentos solo para consumidor Premium.";
}
