namespace Fmc.Application.Configuration;

public class DiscoveryOptions
{
    public const string SectionName = "Discovery";

    /// <summary>Metros virtuales que mejoran posición si la cuenta Enterprise es Premium.</summary>
    public double PremiumEnterpriseRankingBoostMeters { get; set; } = 2500;

    public int FreeTierMaxResults { get; set; } = 10;
    public int PremiumTierMaxResults { get; set; } = 50;
    public double FreeTierMaxRadiusKm { get; set; } = 5;
    public double PremiumTierMaxRadiusKm { get; set; } = 15;
}
