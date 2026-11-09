using Microsoft.AspNetCore.Http;

namespace Topstar.Web.Services.Storage;

public sealed class FileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const long MaxEvidenceBytes = 5 * 1024 * 1024;
    private const long MaxSubmissionBytes = 25 * 1024 * 1024;
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

    public async Task<string?> SaveEvidenceImageAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0 || file.Length > MaxEvidenceBytes) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp") return null;
        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < 12) return null;
        }
        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF && extension is ".jpg" or ".jpeg";
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 && extension == ".png";
        var isWebp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50 && extension == ".webp";
        if (!isJpeg && !isPng && !isWebp) return null;
        return await SaveImageAsync(file, "uploads/shipment-evidence", cancellationToken);
    }

    public async Task<string?> SaveDisputeEvidenceAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0 || file.Length > MaxEvidenceBytes) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp" and not ".pdf") return null;
        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < 5) return null;
        }
        var valid = extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".webp" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            ".pdf" => header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            _ => false
        };
        if (!valid) return null;
        var targetFolder = Path.Combine(env.WebRootPath, "uploads", "dispute-evidence");
        Directory.CreateDirectory(targetFolder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var output = new FileStream(Path.Combine(targetFolder, fileName), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(output, cancellationToken);
        return "/uploads/dispute-evidence/" + fileName;
    }

    public async Task<string?> SavePackageImageAsync(IFormFile? file, CancellationToken cancellationToken = default)
        => await SaveValidatedWebImageAsync(file, "uploads/package-images", cancellationToken);

    public async Task<string?> SavePaymentProofAsync(IFormFile? file, CancellationToken cancellationToken = default)
        => await SaveValidatedWebImageAsync(file, "uploads/payment-proofs", cancellationToken);

    public async Task<string?> SaveCampaignFundingEvidenceAsync(IFormFile? file, CancellationToken cancellationToken = default)
        => await SaveValidatedWebImageAsync(file, "uploads/campaign-funding-evidence", cancellationToken);

    public Task DeletePaymentProofAsync(string? storedUrl, CancellationToken cancellationToken = default)
        => DeleteStoredFileAsync(storedUrl, "/uploads/payment-proofs/", "payment-proofs", cancellationToken);

    public Task DeleteCampaignFundingEvidenceAsync(string? storedUrl, CancellationToken cancellationToken = default)
        => DeleteStoredFileAsync(storedUrl, "/uploads/campaign-funding-evidence/", "campaign-funding-evidence", cancellationToken);

    private Task DeleteStoredFileAsync(string? storedUrl, string prefix, string folder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(storedUrl) || !storedUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
        var fileName = Path.GetFileName(storedUrl);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != storedUrl[prefix.Length..]) return Task.CompletedTask;
        var fullPath = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads", folder, fileName));
        var allowedRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads", folder)) + Path.DirectorySeparatorChar;
        if (fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public async Task<string?> SaveCampaignImageAsync(IFormFile? file, CancellationToken cancellationToken = default)
        => await SaveValidatedWebImageAsync(file, "uploads/campaign-content", cancellationToken);
    public Task DeleteCampaignImageAsync(string? storedUrl, CancellationToken cancellationToken = default)
        => DeleteStoredFileAsync(storedUrl, "/uploads/campaign-content/", "campaign-content", cancellationToken);

    private async Task<string?> SaveValidatedWebImageAsync(IFormFile? file, string folder, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0 || file.Length > MaxEvidenceBytes) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp") return null;
        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < 12) return null;
        }
        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF && extension is ".jpg" or ".jpeg";
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 && extension == ".png";
        var isWebp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50 && extension == ".webp";
        if (!isJpeg && !isPng && !isWebp) return null;
        return await SaveImageAsync(file, folder, cancellationToken);
    }

    public async Task<string?> SaveSubmissionAttachmentAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0 || file.Length > MaxSubmissionBytes) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp"
            and not ".pdf" and not ".doc" and not ".docx" and not ".mp4" and not ".mov")
            return null;

        var targetFolder = Path.Combine(env.WebRootPath, "uploads", "submission-files");
        Directory.CreateDirectory(targetFolder);
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetFolder, fileName);
        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);
        return "/uploads/submission-files/" + fileName;
    }

    public async Task<string?> SaveKocProfileUploadAsync(IFormFile? file, string mediaKind, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0) return null;
        var policy = mediaKind.Trim().ToLowerInvariant() switch
        {
            "avatar" => (Folder: "koc-avatar", MaxBytes: 5L * 1024 * 1024, Extensions: new[] { ".jpg", ".jpeg", ".png", ".webp" }),
            "document" => (Folder: "koc-document", MaxBytes: 10L * 1024 * 1024, Extensions: new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp" }),
            "gallery" => (Folder: "koc-gallery", MaxBytes: 8L * 1024 * 1024, Extensions: new[] { ".jpg", ".jpeg", ".png", ".webp" }),
            "video" => (Folder: "koc-video", MaxBytes: 100L * 1024 * 1024, Extensions: new[] { ".mp4", ".webm", ".mov" }),
            _ => default
        };
        if (string.IsNullOrWhiteSpace(policy.Folder) || file.Length > policy.MaxBytes) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!policy.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) return null;
        if (!await HasValidKocProfileSignatureAsync(file, extension, cancellationToken)) return null;
        var targetFolder = Path.Combine(env.WebRootPath, "uploads", policy.Folder);
        Directory.CreateDirectory(targetFolder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetFolder, fileName);
        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);
        return $"/uploads/{policy.Folder}/{fileName}";
    }

    private static async Task<bool> HasValidKocProfileSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        var header = new byte[16];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (read < 4) return false;
        return extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            ".pdf" => read >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".doc" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            ".docx" => header[0] == 0x50 && header[1] == 0x4B && header[2] is 0x03 or 0x05 or 0x07 && header[3] is 0x04 or 0x06 or 0x08,
            ".mp4" or ".mov" => read >= 12 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            ".webm" => header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            _ => false
        };
    }

    public Task DeleteKocProfileUploadAsync(string? storedUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(storedUrl)) return Task.CompletedTask;
        var allowedPrefixes = new[] { "/uploads/koc-avatar/", "/uploads/koc-document/", "/uploads/koc-gallery/", "/uploads/koc-video/" };
        var prefix = allowedPrefixes.FirstOrDefault(value => storedUrl.StartsWith(value, StringComparison.OrdinalIgnoreCase));
        if (prefix is null) return Task.CompletedTask;
        var fileName = Path.GetFileName(storedUrl);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != storedUrl[prefix.Length..]) return Task.CompletedTask;
        var root = Path.GetFullPath(Path.Combine(env.WebRootPath, prefix.Trim('/').Replace('/', Path.DirectorySeparatorChar))) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, fileName));
        if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task DeleteEvidenceImageAsync(string? storedUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string prefix = "/uploads/shipment-evidence/";
        if (string.IsNullOrWhiteSpace(storedUrl) || !storedUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
        var fileName = Path.GetFileName(storedUrl);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != storedUrl[prefix.Length..]) return Task.CompletedTask;
        var fullPath = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads", "shipment-evidence", fileName));
        var allowedRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads", "shipment-evidence")) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task DeleteDisputeEvidenceAsync(string? storedUrl, CancellationToken cancellationToken = default)
        => DeleteStoredFileAsync(storedUrl, "/uploads/dispute-evidence/", "dispute-evidence", cancellationToken);

    public Task DeleteSubmissionAttachmentAsync(string? storedUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string prefix = "/uploads/submission-files/";
        if (string.IsNullOrWhiteSpace(storedUrl) || !storedUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;
        var fileName = Path.GetFileName(storedUrl);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != storedUrl[prefix.Length..]) return Task.CompletedTask;
        var root = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads", "submission-files")) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, fileName));
        if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
