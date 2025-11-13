using FirstLend.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace FirstLend.Domain.Dtos.Response;

[SwaggerSchema(Description = "Response containing KYC document information")]
public class KycDocumentResponse
{
    [SwaggerSchema(Description = "Unique identifier for the document")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "The type of document uploaded")]
    public KycDocTypes DocumentType { get; set; }

    [SwaggerSchema(Description = "The URL to view/download the document")]
    public string DocumentUrl { get; set; } = string.Empty;

    [SwaggerSchema(Description = "The date and time when the document was uploaded")]
    public DateTime UploadedAt { get; set; }
}
