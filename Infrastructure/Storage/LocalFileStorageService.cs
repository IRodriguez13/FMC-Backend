using Fmc.Application.Configuration;
using Fmc.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Fmc.Infrastructure.Storage;

public class LocalFileStorageService(IOptions<MediaOptions> mediaOptions) : IFileStorageService
{
    private static readonly Dictionary<string, string> ContentTypeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private readonly MediaOptions _media = mediaOptions.Value;

    public async Task<string> SaveImageAsync(Stream content, string contentType, CancellationToken ct = default)
    {
        if (!ContentTypeExtensions.TryGetValue(contentType, out var ext))
            throw new ArgumentException("Formato no permitido.");

        Directory.CreateDirectory(_media.UploadRoot);

        var storageKey = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_media.UploadRoot, storageKey);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, ct);

        return storageKey;
    }

    public string GetPublicUrl(string storageKey)
    {
        var basePath = _media.PublicUrlPath.TrimEnd('/');
        return $"{basePath}/{storageKey}";
    }
}
