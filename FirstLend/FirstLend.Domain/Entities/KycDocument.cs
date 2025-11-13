using FirstLend.Domain.Enums;

namespace FirstLend.Domain.Entities;

public class KycDocument
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public KycDocTypes DocumentType { get; set; }
    public string DocumentUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
