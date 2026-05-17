namespace Fmc.Application.Services;

/// <summary>Validaciones de coordenadas geográficas WGS84.</summary>
public static class LocationValidation
{
    /// <summary>
    /// Verifica que latitud esté en [-90, 90] y longitud en [-180, 180].
    /// </summary>
    public static bool IsValidLocation(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 &&
        longitude is >= -180 and <= 180;
}
