namespace Fmc.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveImageAsync(Stream content, string contentType, CancellationToken ct = default);

    string GetPublicUrl(string storageKey);
}
