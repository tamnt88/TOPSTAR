using Microsoft.AspNetCore.Http;

namespace Topstar.Web.Services.Storage;

public interface IFileStorageService
{
    Task<string?> SaveImageAsync(IFormFile? file, string subFolder, CancellationToken cancellationToken = default);
}
