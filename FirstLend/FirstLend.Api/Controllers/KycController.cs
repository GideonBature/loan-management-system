using FirstLend.Domain.Abstractions;
using FirstLend.Domain.Dtos.Request;
using FirstLend.Domain.Dtos.Response;
using FirstLend.Infrastructure.Data;
using FirstLend.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FirstLend.Api.Controllers
{
    [ApiController]
    [Route("api/kyc")]
    [Authorize]
    public class KycController : ControllerBase
    {
        private readonly IKycService _kycService;
        private readonly ILogger<KycController> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly FirstLendDbContext _context;

        public KycController(
            IKycService kycService, 
            ILogger<KycController> logger,
            ICloudinaryService cloudinaryService,
            FirstLendDbContext context)
        {
            _kycService = kycService;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
            _context = context;
        }

        /// <summary>
        /// Verify KYC with BVN and NIN
        /// </summary>
        /// <param name="request">KYC verification request containing BVN and NIN</param>
        /// <returns>KYC verification result</returns>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(KycVerificationResponse), 200)]
        [ProducesResponseType(typeof(KycVerificationResponse), 400)]
        [ProducesResponseType(typeof(KycVerificationResponse), 401)]
        [ProducesResponseType(typeof(KycVerificationResponse), 500)]
        public async Task<IActionResult> VerifyKyc([FromBody] KycVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new KycVerificationResponse
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Code = "VALIDATION_ERROR"
                    });
                }

                // Get current user ID from JWT token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new KycVerificationResponse
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "UNAUTHORIZED"
                    });
                }

                _logger.LogInformation($"KYC verification request for user: {userId}");

                var result = await _kycService.VerifyKycAsync(request, userId);

                if (result.Success)
                {
                    return Ok(result);
                }

                if (result.Code == "USER_NOT_FOUND")
                {
                    return NotFound(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during KYC verification: {ex.Message}", ex);
                return StatusCode(500, new KycVerificationResponse
                {
                    Success = false,
                    Message = "An error occurred during KYC verification",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get the current KYC verification status
        /// </summary>
        /// <returns>KYC verification status</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(KycStatusResponse), 200)]
        [ProducesResponseType(typeof(KycStatusResponse), 401)]
        [ProducesResponseType(typeof(KycStatusResponse), 404)]
        [ProducesResponseType(typeof(KycStatusResponse), 500)]
        public async Task<IActionResult> GetKycStatus()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new KycStatusResponse
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "UNAUTHORIZED"
                    });
                }

                _logger.LogInformation($"Retrieving KYC status for user: {userId}");

                var result = await _kycService.GetKycStatusAsync(userId);

                if (!result.Success && result.Code == "USER_NOT_FOUND")
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception retrieving KYC status: {ex.Message}", ex);
                return StatusCode(500, new KycStatusResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving KYC status",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Update KYC verification status (Admin only)
        /// </summary>
        /// <param name="request">Update request with new verification status</param>
        /// <returns>Updated KYC status</returns>
        [HttpPut("status")]
        [ProducesResponseType(typeof(KycStatusResponse), 200)]
        [ProducesResponseType(typeof(KycStatusResponse), 400)]
        [ProducesResponseType(typeof(KycStatusResponse), 401)]
        [ProducesResponseType(typeof(KycStatusResponse), 403)]
        [ProducesResponseType(typeof(KycStatusResponse), 404)]
        [ProducesResponseType(typeof(KycStatusResponse), 500)]
        public async Task<IActionResult> UpdateKycStatus([FromBody] UpdateKycStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new KycStatusResponse
                    {
                        Success = false,
                        Message = "Invalid request data",
                        Code = "VALIDATION_ERROR"
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new KycStatusResponse
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Code = "UNAUTHORIZED"
                    });
                }

                _logger.LogInformation($"Updating KYC status for user: {userId}. New status: {request.IsVerified}");

                var result = await _kycService.UpdateKycStatusAsync(userId, request);

                if (!result.Success && result.Code == "USER_NOT_FOUND")
                {
                    return NotFound(result);
                }

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception updating KYC status: {ex.Message}", ex);
                return StatusCode(500, new KycStatusResponse
                {
                    Success = false,
                    Message = "An error occurred while updating KYC status",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Upload a KYC document (Government ID, Proof of Address, etc.)
        /// </summary>
        /// <param name="request">Document upload request with file and document type</param>
        /// <returns>Uploaded document information with URL</returns>
        [HttpPost("documents/upload")]
        [ProducesResponseType(typeof(KycDocumentResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadKycDocument([FromForm] UploadKycDocumentRequest request)
        {
            try
            {
                // Get current user ID from JWT token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file uploaded" });
                }

                _logger.LogInformation($"Uploading KYC document for user {userId}, type: {request.DocumentType}");

                // Upload to Cloudinary
                var uploadResult = await _cloudinaryService.UploadDocumentAsync(request.File, "kyc-documents");

                if (!uploadResult.Success)
                {
                    _logger.LogError($"Cloudinary upload failed: {uploadResult.ErrorMessage}");
                    return BadRequest(new { success = false, message = uploadResult.ErrorMessage });
                }

                // Save document info to database
                var kycDocument = new KycDocument
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DocumentType = request.DocumentType,
                    DocumentUrl = uploadResult.Url!,
                    PublicId = uploadResult.PublicId!,
                    UploadedAt = DateTime.UtcNow
                };

                _context.KycDocuments.Add(kycDocument);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"KYC document saved successfully for user {userId}");

                var response = new KycDocumentResponse
                {
                    Id = kycDocument.Id,
                    DocumentType = kycDocument.DocumentType,
                    DocumentUrl = kycDocument.DocumentUrl,
                    UploadedAt = kycDocument.UploadedAt
                };

                return Ok(new { success = true, message = "Document uploaded successfully", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during KYC document upload");
                return StatusCode(500, new { success = false, message = "An error occurred during document upload" });
            }
        }

        /// <summary>
        /// Get all KYC documents uploaded by the current user
        /// </summary>
        /// <returns>List of uploaded KYC documents</returns>
        [HttpGet("documents")]
        [ProducesResponseType(typeof(IEnumerable<KycDocumentResponse>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKycDocuments()
        {
            try
            {
                // Get current user ID from JWT token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var documents = await _context.KycDocuments
                    .Where(d => d.UserId == userId)
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => new KycDocumentResponse
                    {
                        Id = d.Id,
                        DocumentType = d.DocumentType,
                        DocumentUrl = d.DocumentUrl,
                        UploadedAt = d.UploadedAt
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = documents });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting KYC documents");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving documents" });
            }
        }

        /// <summary>
        /// Delete a KYC document
        /// </summary>
        /// <param name="documentId">The ID of the document to delete</param>
        /// <returns>Success message</returns>
        [HttpDelete("documents/{documentId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteKycDocument(Guid documentId)
        {
            try
            {
                // Get current user ID from JWT token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var document = await _context.KycDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

                if (document == null)
                {
                    return NotFound(new { success = false, message = "Document not found" });
                }

                // Delete from Cloudinary
                var deleteResult = await _cloudinaryService.DeleteDocumentAsync(document.PublicId);
                
                if (!deleteResult.Success)
                {
                    _logger.LogWarning($"Failed to delete document from Cloudinary: {deleteResult.ErrorMessage}");
                }

                // Delete from database
                _context.KycDocuments.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"KYC document {documentId} deleted successfully for user {userId}");

                return Ok(new { success = true, message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception deleting KYC document");
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the document" });
            }
        }
    }
}
