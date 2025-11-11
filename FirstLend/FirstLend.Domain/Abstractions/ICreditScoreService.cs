using FirstLend.Domain.Dtos.Response;

namespace FirstLend.Domain.Abstractions
{
    public interface ICreditScoreService
    {
        Task<CreditScoreResponse> GetUserCreditScoreAsync(string userId);
        Task AssignRandomCreditAccountsAsync(string userId, int numberOfAccounts = 5);
    }
}
