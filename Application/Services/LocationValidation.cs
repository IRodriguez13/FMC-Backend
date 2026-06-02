using Fmc.Domain.Constants;

namespace Fmc.Application.Services;

/// <summary>Validaciones de coordenadas geográficas WGS84 y área de servicio CABA.</summary>
public static class LocationValidation
{
    public static bool IsValidLocation(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 &&
        longitude is >= -180 and <= 180;

    public static bool IsWithinCabaServiceArea(double latitude, double longitude) =>
        IsValidLocation(latitude, longitude) && CabaServiceArea.Contains(latitude, longitude);

    public static void EnsureWithinCabaServiceArea(double latitude, double longitude) =>
        CabaServiceArea.EnsureContains(latitude, longitude);
}
