using Microsoft.AspNetCore.Http;

namespace Topstar.Web.Services.Storage;

public sealed class FileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".svg", ".ico"
    };

    public async Task<string?> SaveImageAsync(IFormFile? file, string subFolder, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0) return null;

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return null;

        var webRoot = env.WebRootPath;
        var safeSubFolder = string.IsNullOrWhiteSpace(subFolder) ? "uploads" : subFolder.Trim().Replace('\\', '/');
        var targetFolder = Path.Combine(webRoot, safeSubFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(targetFolder);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(targetFolder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return "/" + safeSubFolder.Trim('/').Replace('\\', '/') + "/" + fileName;
    }
}
