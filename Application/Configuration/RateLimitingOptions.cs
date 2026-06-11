namespace Fmc.Application.Configuration;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; set; } = 10;
    public int AuthWindowSeconds { get; set; } = 60;
    public int UploadPermitLimit { get; set; } = 5;
    public int UploadWindowSeconds { get; set; } = 60;
}
