using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;

namespace FirstLend.Application.Abstractions
{
    public interface ILoanService
    {
        Task<ServiceResponse<LoanResponse>> CreateAsync(string userId, CreateLoanRequest request);
        Task<ServiceResponse<LoanResponse>> GetByIdAsync(Guid id, string userId);
        Task<ServiceResponse<List<LoanResponse>>> GetUserLoansAsync(string userId, int page = 1, int pageSize = 10);
        Task<ServiceResponse<List<LoanResponse>>> GetAllLoansAsync(int page = 1, int pageSize = 10);
    }
}
