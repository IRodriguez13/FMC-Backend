namespace Fmc.Application.Configuration;

public class MediaOptions
{
    public const string SectionName = "Media";

    public string UploadRoot { get; set; } = "uploads";
    public string PublicUrlPath { get; set; } = "/media";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
