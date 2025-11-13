using Microsoft.AspNetCore.Http;

namespace FirstLend.Domain.Abstractions;

public interface ICloudinaryService
{
    Task<(bool Success, string? Url, string? PublicId, string? ErrorMessage)> UploadDocumentAsync(IFormFile file, string folder = "kyc-documents");
    Task<(bool Success, string? ErrorMessage)> DeleteDocumentAsync(string publicId);
}
