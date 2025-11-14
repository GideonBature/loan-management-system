using FirstLend.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace FirstLend.Domain.Dtos.Request;

[SwaggerSchema(Description = "Request to upload a KYC document")]
public class UploadKycDocumentRequest
{
    [Required]
    [SwaggerSchema(Description = @"The type of KYC document being uploaded:
- government_issued_id: National ID, Passport, or Driver's License
- proof_of_address: Utility bill or bank statement (last 3 months)
- bank_statement: Recent bank statement (last 6 months)
- guarantor_document: Combined PDF/DOCS containing: (1) Passport photograph, (2) Guarantor's means of identification, (3) Guarantor's CAC or work ID card to prove ability to pay if user defaults, (4) Guarantor's form with signature appended, (5) Utility bill for proof of address")]
    public KycDocTypes DocumentType { get; set; }

    [Required]
    [SwaggerSchema(Description = "The document file to upload (images: JPG, PNG, GIF or PDF, max 10MB). For guarantor_document, combine all required documents into a single PDF.")]
    public IFormFile File { get; set; } = null!;
}
