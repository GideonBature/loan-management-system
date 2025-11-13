using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FirstLend.Domain.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FirstLend.Infrastructure.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            throw new ArgumentException("Cloudinary configuration is missing or incomplete.");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _logger = logger;
    }

    public async Task<(bool Success, string? Url, string? PublicId, string? ErrorMessage)> UploadDocumentAsync(
        IFormFile file, 
        string folder = "kyc-documents")
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return (false, null, null, "No file provided or file is empty.");
            }

            // Validate file type (images and PDFs)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, null, null, "Invalid file type. Only images (JPG, PNG, GIF) and PDF files are allowed.");
            }

            // Validate file size (max 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                return (false, null, null, "File size exceeds 10MB limit.");
            }

            using var stream = file.OpenReadStream();
            
            string? secureUrl = null;
            string? publicId = null;
            System.Net.HttpStatusCode statusCode;
            CloudinaryDotNet.Actions.Error? error = null;

            if (fileExtension == ".pdf")
            {
                // For PDF files, use RawUploadParams
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };
                var rawResult = await _cloudinary.UploadAsync(uploadParams);
                secureUrl = rawResult.SecureUrl?.ToString();
                publicId = rawResult.PublicId;
                statusCode = rawResult.StatusCode;
                error = rawResult.Error;
            }
            else
            {
                // For images, use ImageUploadParams
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };
                var imageResult = await _cloudinary.UploadAsync(uploadParams);
                secureUrl = imageResult.SecureUrl?.ToString();
                publicId = imageResult.PublicId;
                statusCode = imageResult.StatusCode;
                error = imageResult.Error;
            }

            if (statusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation($"Document uploaded successfully to Cloudinary: {secureUrl}");
                return (true, secureUrl, publicId, null);
            }
            else
            {
                _logger.LogError($"Cloudinary upload failed: {error?.Message}");
                return (false, null, null, error?.Message ?? "Upload failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during document upload to Cloudinary");
            return (false, null, null, $"Upload error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteDocumentAsync(string publicId)
    {
        try
        {
            if (string.IsNullOrEmpty(publicId))
            {
                return (false, "Public ID is required.");
            }

            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result == "ok")
            {
                _logger.LogInformation($"Document deleted successfully from Cloudinary: {publicId}");
                return (true, null);
            }
            else
            {
                _logger.LogWarning($"Cloudinary deletion result: {result.Result}");
                return (false, $"Deletion result: {result.Result}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception occurred during document deletion from Cloudinary: {publicId}");
            return (false, $"Deletion error: {ex.Message}");
        }
    }
}
