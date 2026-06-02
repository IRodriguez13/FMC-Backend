namespace Fmc.Domain.Constants;

/// <summary>
/// FMC opera solo en la Ciudad Autónoma de Buenos Aires (CABA).
/// Bounding box aproximado WGS84 para validar consultas y altas de cafeterías.
/// </summary>
public static class CabaServiceArea
{
    public const string DisplayName = "Ciudad Autónoma de Buenos Aires (CABA)";

    /// <summary>Obelisco / centro — referencia para seed, demos y fallback del cliente.</summary>
    public const double CenterLatitude = -34.6037;
    public const double CenterLongitude = -58.3816;

    public const double MinLatitude = -34.705;
    public const double MaxLatitude = -34.527;
    public const double MinLongitude = -58.535;
    public const double MaxLongitude = -58.335;

    public static bool Contains(double latitude, double longitude) =>
        latitude >= MinLatitude && latitude <= MaxLatitude &&
        longitude >= MinLongitude && longitude <= MaxLongitude;

    public static void EnsureContains(double latitude, double longitude)
    {
        if (!IsValidWgs84(latitude, longitude))
            throw new ArgumentException("Coordenadas geográficas inválidas.");

        if (!Contains(latitude, longitude))
            throw new ArgumentException(
                $"Find My Coffee solo opera en {DisplayName}. Las coordenadas indicadas están fuera del área de servicio.");
    }

    private static bool IsValidWgs84(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
}
