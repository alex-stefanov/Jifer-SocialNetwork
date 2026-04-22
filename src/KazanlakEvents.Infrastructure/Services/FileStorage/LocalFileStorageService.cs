using KazanlakEvents.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KazanlakEvents.Infrastructure.Services.FileStorage;

public class LocalFileStorageService(ILogger<LocalFileStorageService> logger) : IFileStorageService
{
    private const string UploadDir = "wwwroot/uploads";

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var path = Path.Combine(UploadDir, uniqueName);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var fs = new FileStream(path, FileMode.Create);
        await fileStream.CopyToAsync(fs, ct);

        logger.LogInformation("File uploaded: {Path}", path);
        return $"/uploads/{uniqueName}";
    }

    public Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine("wwwroot", filePath.TrimStart('/'));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetUrl(string filePath) => filePath;
}
