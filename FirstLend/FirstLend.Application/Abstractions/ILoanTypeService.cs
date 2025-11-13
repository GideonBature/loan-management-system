using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;

namespace FirstLend.Application.Abstractions
{
    public interface ILoanTypeService
    {
        Task<ServiceResponse<LoanTypeResponse>> CreateAsync(CreateLoanTypeRequest request);
        Task<ServiceResponse<LoanTypeResponse>> GetByIdAsync(Guid id);
        Task<ServiceResponse<List<LoanTypeResponse>>> GetAllAsync(int page = 1, int pageSize = 10, bool? activeOnly = null);
        Task<ServiceResponse<LoanTypeResponse>> UpdateAsync(Guid id, UpdateLoanTypeRequest request);
        Task<ServiceResponse<bool>> DeleteAsync(Guid id);
        Task<ServiceResponse<LoanTypeResponse>> ToggleStatusAsync(Guid id);
    }
}
