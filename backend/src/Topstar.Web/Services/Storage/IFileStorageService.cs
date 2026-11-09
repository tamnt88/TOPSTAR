using Microsoft.AspNetCore.Http;

namespace Topstar.Web.Services.Storage;

public interface IFileStorageService
{
    Task<string?> SaveImageAsync(IFormFile? file, string subFolder, CancellationToken cancellationToken = default);
    Task<string?> SaveEvidenceImageAsync(IFormFile? file, CancellationToken cancellationToken = default);
    Task<string?> SaveDisputeEvidenceAsync(IFormFile? file, CancellationToken cancellationToken = default);
    Task<string?> SavePaymentProofAsync(IFormFile? file, CancellationToken cancellationToken = default);
    Task<string?> SaveCampaignFundingEvidenceAsync(IFormFile? file, CancellationToken cancellationToken = default);
    Task<string?> SavePackageImageAsync(IFormFile? file, CancellationToken cancellationToken = default);
    Task<string?> SaveCampaignImageAsync(IFormFile? file, CancellationToken cancellationToken = default);
    Task DeleteCampaignImageAsync(string? storedUrl, CancellationToken cancellationToken = default);
    Task<string?> SaveSubmissionAttachmentAsync(IFormFile? file, CancellationToken cancellationToken = default);
    Task<string?> SaveKocProfileUploadAsync(IFormFile? file, string mediaKind, CancellationToken cancellationToken = default);
    Task DeleteEvidenceImageAsync(string? storedUrl, CancellationToken cancellationToken = default);
    Task DeleteDisputeEvidenceAsync(string? storedUrl, CancellationToken cancellationToken = default);
    Task DeletePaymentProofAsync(string? storedUrl, CancellationToken cancellationToken = default);
    Task DeleteCampaignFundingEvidenceAsync(string? storedUrl, CancellationToken cancellationToken = default);
    Task DeleteSubmissionAttachmentAsync(string? storedUrl, CancellationToken cancellationToken = default);
    Task DeleteKocProfileUploadAsync(string? storedUrl, CancellationToken cancellationToken = default);
}
