namespace Fmc.Application.Configuration;

public class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];
    public string[] AllowedHeaders { get; set; } = ["Authorization", "Content-Type", "Accept", "X-Correlation-Id"];
}
