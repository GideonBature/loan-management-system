using FirstLend.Application.Abstractions;
using FirstLend.Application.Dtos.Request;
using FirstLend.Application.Dtos.Response;
using FirstLend.Domain.Entities;
using FirstLend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FirstLend.Infrastructure.Services
{
    public class LoanTypeService : ILoanTypeService
    {
        private readonly FirstLendDbContext _context;

        public LoanTypeService(FirstLendDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<LoanTypeResponse>> CreateAsync(CreateLoanTypeRequest request)
        {
            try
            {
                // Check if loan type with same name exists
                var exists = await _context.LoanTypes.AnyAsync(lt => lt.Name.ToLower() == request.Name.ToLower());
                if (exists)
                {
                    return new ServiceResponse<LoanTypeResponse>
                    {
                        Success = false,
                        Message = "Loan type with this name already exists",
                        Code = "LOAN_TYPE_EXISTS",
                        Data = null,
                        Errors = new[] { "A loan type with this name already exists" }
                    };
                }

                var loanType = new LoanType
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Interest = request.Interest
                };

                _context.LoanTypes.Add(loanType);
                await _context.SaveChangesAsync();

                return new ServiceResponse<LoanTypeResponse>
                {
                    Success = true,
                    Message = "Loan type created successfully",
                    Code = "",
                    Data = new LoanTypeResponse
                    {
                        Id = loanType.Id,
                        Name = loanType.Name,
                        Interest = loanType.Interest
                    },
                    Errors = null
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<LoanTypeResponse>
                {
                    Success = false,
                    Message = "Failed to create loan type",
                    Code = "CREATE_FAILED",
                    Data = null,
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<ServiceResponse<LoanTypeResponse>> GetByIdAsync(Guid id)
        {
            try
            {
                var loanType = await _context.LoanTypes.FindAsync(id);

                if (loanType == null)
                {
                    return new ServiceResponse<LoanTypeResponse>
                    {
                        Success = false,
                        Message = "Loan type not found",
                        Code = "NOT_FOUND",
                        Data = null,
                        Errors = new[] { $"Loan type with ID {id} not found" }
                    };
                }

                return new ServiceResponse<LoanTypeResponse>
                {
                    Success = true,
                    Message = "Loan type retrieved successfully",
                    Code = "",
                    Data = new LoanTypeResponse
                    {
                        Id = loanType.Id,
                        Name = loanType.Name,
                        Interest = loanType.Interest
                    },
                    Errors = null
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<LoanTypeResponse>
                {
                    Success = false,
                    Message = "Failed to retrieve loan type",
                    Code = "RETRIEVAL_FAILED",
                    Data = null,
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<ServiceResponse<List<LoanTypeResponse>>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var totalCount = await _context.LoanTypes.CountAsync();
                var loanTypes = await _context.LoanTypes
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = loanTypes.Select(lt => new LoanTypeResponse
                {
                    Id = lt.Id,
                    Name = lt.Name,
                    Interest = lt.Interest
                }).ToList();

                return new ServiceResponse<List<LoanTypeResponse>>
                {
                    Success = true,
                    Message = "Loan types retrieved successfully",
                    Code = "",
                    Data = response,
                    Errors = null,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<LoanTypeResponse>>
                {
                    Success = false,
                    Message = "Failed to retrieve loan types",
                    Code = "RETRIEVAL_FAILED",
                    Data = null,
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<ServiceResponse<LoanTypeResponse>> UpdateAsync(Guid id, UpdateLoanTypeRequest request)
        {
            try
            {
                var loanType = await _context.LoanTypes.FindAsync(id);

                if (loanType == null)
                {
                    return new ServiceResponse<LoanTypeResponse>
                    {
                        Success = false,
                        Message = "Loan type not found",
                        Code = "NOT_FOUND",
                        Data = null,
                        Errors = new[] { $"Loan type with ID {id} not found" }
                    };
                }

                if (request.Name != null)
                {
                    var exists = await _context.LoanTypes.AnyAsync(lt => lt.Name.ToLower() == request.Name.ToLower() && lt.Id != id);
                    if (exists)
                    {
                        return new ServiceResponse<LoanTypeResponse>
                        {
                            Success = false,
                            Message = "Loan type with this name already exists",
                            Code = "LOAN_TYPE_EXISTS",
                            Data = null,
                            Errors = new[] { "A loan type with this name already exists" }
                        };
                    }
                    loanType.Name = request.Name;
                }

                if (request.Interest.HasValue)
                {
                    loanType.Interest = request.Interest.Value;
                }

                await _context.SaveChangesAsync();

                return new ServiceResponse<LoanTypeResponse>
                {
                    Success = true,
                    Message = "Loan type updated successfully",
                    Code = "",
                    Data = new LoanTypeResponse
                    {
                        Id = loanType.Id,
                        Name = loanType.Name,
                        Interest = loanType.Interest
                    },
                    Errors = null
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<LoanTypeResponse>
                {
                    Success = false,
                    Message = "Failed to update loan type",
                    Code = "UPDATE_FAILED",
                    Data = null,
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var loanType = await _context.LoanTypes.FindAsync(id);

                if (loanType == null)
                {
                    return new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Loan type not found",
                        Code = "NOT_FOUND",
                        Data = false,
                        Errors = new[] { $"Loan type with ID {id} not found" }
                    };
                }

                // Check if any loans are using this loan type
                var hasLoans = await _context.Loans.AnyAsync(l => l.LoanTypeId == id);
                if (hasLoans)
                {
                    return new ServiceResponse<bool>
                    {
                        Success = false,
                        Message = "Cannot delete loan type with existing loans",
                        Code = "HAS_LOANS",
                        Data = false,
                        Errors = new[] { "This loan type is being used by existing loans and cannot be deleted" }
                    };
                }

                _context.LoanTypes.Remove(loanType);
                await _context.SaveChangesAsync();

                return new ServiceResponse<bool>
                {
                    Success = true,
                    Message = "Loan type deleted successfully",
                    Code = "",
                    Data = true,
                    Errors = null
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete loan type",
                    Code = "DELETE_FAILED",
                    Data = false,
                    Errors = new[] { ex.Message }
                };
            }
        }
    }
}
