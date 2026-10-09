namespace Eurocar.Api.Infrastructure;

public interface IPhotoStorage
{
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default);
    Stream? OpenRead(string key);
    void Delete(string key);
    void Clear();
}

/// <summary>Files on local disk (Docker volume later). Swap for Blob/S3 behind the same interface.</summary>
public sealed class LocalDiskPhotoStorage : IPhotoStorage
{
    readonly string _root;

    public LocalDiskPhotoStorage(IConfiguration cfg, IHostEnvironment env)
    {
        _root = Path.GetFullPath(Path.Combine(env.ContentRootPath, cfg["Photos:Path"] ?? "data/photos"));
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default)
    {
        var ext = contentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
        var key = $"{Guid.NewGuid():N}{ext}";
        await using var file = File.Create(PathOf(key));
        await content.CopyToAsync(file, ct);
        return key;
    }

    public Stream? OpenRead(string key) => File.Exists(PathOf(key)) ? File.OpenRead(PathOf(key)) : null;

    public void Delete(string key)
    {
        if (File.Exists(PathOf(key))) File.Delete(PathOf(key));
    }

    public void Clear()
    {
        foreach (var f in Directory.EnumerateFiles(_root)) File.Delete(f);
    }

    string PathOf(string key) => Path.Combine(_root, Path.GetFileName(key));
}
