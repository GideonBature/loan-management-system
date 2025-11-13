using FirstLend.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace FirstLend.Domain.Dtos.Request;

[SwaggerSchema(Description = "Request to upload a KYC document")]
public class UploadKycDocumentRequest
{
    [Required]
    [SwaggerSchema(Description = "The type of KYC document being uploaded")]
    public KycDocTypes DocumentType { get; set; }

    [Required]
    [SwaggerSchema(Description = "The document file to upload (images: JPG, PNG, GIF or PDF, max 10MB)")]
    public IFormFile File { get; set; } = null!;
}
