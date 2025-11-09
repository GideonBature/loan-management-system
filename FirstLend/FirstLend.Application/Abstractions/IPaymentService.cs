using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;
using System.Threading.Tasks;

namespace FirstLend.Application.Abstractions
{
    public interface IPaymentService
    {
        Task<PaymentInitiationResponse> InitiatePayment(InitiatePaymentRequest request, string userId);
        Task<PaymentVerificationResponse> VerifyPayment(string reference);
        Task<bool> ProcessPaystackWebhook(string payload, string signature);
    }
}
