namespace Fmc.Infrastructure.Persistence;

/// <summary>Copia imágenes de demo desde Api/SeedAssets al directorio de uploads.</summary>
internal static class SeedImageFiles
{
    private const int MinValidBytes = 1024;

    public static void EnsureOnDisk(string uploadRoot, string storageKey, string? seedAssetsRoot = null)
    {
        Directory.CreateDirectory(uploadRoot);
        var dest = Path.Combine(uploadRoot, storageKey);

        if (File.Exists(dest) && new FileInfo(dest).Length < MinValidBytes)
            File.Delete(dest);

        if (seedAssetsRoot != null)
        {
            foreach (var candidate in SourceCandidates(storageKey))
            {
                var source = Path.Combine(seedAssetsRoot, candidate);
                if (File.Exists(source) && new FileInfo(source).Length >= MinValidBytes)
                {
                    File.Copy(source, dest, overwrite: true);
                    return;
                }
            }
        }

        if (File.Exists(dest) && new FileInfo(dest).Length >= MinValidBytes)
            return;

        throw new InvalidOperationException(
            $"No se encontró imagen seed válida para '{storageKey}' en SeedAssets.");
    }

    /// <summary>Elimina PNG legacy y archivos seed corruptos (&lt;1KB) cuando existe el .jpg.</summary>
    public static void CleanupLegacySeedFiles(string uploadRoot)
    {
        if (!Directory.Exists(uploadRoot))
            return;

        foreach (var jpg in Directory.GetFiles(uploadRoot, "seed-*.jpg"))
        {
            if (new FileInfo(jpg).Length < MinValidBytes)
                continue;

            var png = Path.ChangeExtension(jpg, ".png");
            if (File.Exists(png))
                File.Delete(png);
        }

        foreach (var orphan in Directory.GetFiles(uploadRoot, "seed-*"))
        {
            if (new FileInfo(orphan).Length < MinValidBytes)
                File.Delete(orphan);
        }
    }

    private static IEnumerable<string> SourceCandidates(string storageKey)
    {
        yield return storageKey;
        if (storageKey.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            yield return Path.ChangeExtension(storageKey, ".png");
        else if (storageKey.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            yield return Path.ChangeExtension(storageKey, ".jpg");
    }

    /// <summary>Ruta a SeedAssets junto al contenido publicado (Api/SeedAssets).</summary>
    public static string? ResolveSeedAssetsRoot(string uploadRoot)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SeedAssets"),
            Path.GetFullPath(Path.Combine(uploadRoot, "..", "SeedAssets")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "SeedAssets")),
        };

        return candidates.FirstOrDefault(Directory.Exists);
    }
}
