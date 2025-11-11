using FirstLend.Domain.Dtos.Request;
using FirstLend.Domain.Dtos.Response;

namespace FirstLend.Domain.Abstractions
{
    public interface IKycService
    {
        Task<KycVerificationResponse> VerifyKycAsync(KycVerificationRequest request, string userId);
        Task<KycStatusResponse> GetKycStatusAsync(string userId);
        Task<KycStatusResponse> UpdateKycStatusAsync(string userId, UpdateKycStatusRequest request);
    }
}
