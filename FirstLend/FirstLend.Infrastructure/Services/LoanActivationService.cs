using FirstLend.Domain.Enums;
using FirstLend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FirstLend.Infrastructure.Services;

public class LoanActivationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoanActivationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute
    private readonly TimeSpan _activationDelay = TimeSpan.FromMinutes(5); // Activate after 5 minutes

    public LoanActivationService(
        IServiceProvider serviceProvider,
        ILogger<LoanActivationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Loan Activation Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ActivateApprovedLoansAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while activating loans");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Loan Activation Service stopped");
    }

    private async Task ActivateApprovedLoansAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FirstLendDbContext>();

        var cutoffTime = DateTime.UtcNow.Subtract(_activationDelay);

        var loansToActivate = await context.Loans
            .Where(l => l.Status == LoanStatus.approved 
                     && l.ApprovedAt != null 
                     && l.ApprovedAt <= cutoffTime)
            .ToListAsync(cancellationToken);

        if (loansToActivate.Any())
        {
            foreach (var loan in loansToActivate)
            {
                loan.Status = LoanStatus.active;
                loan.ActivatedAt = DateTime.UtcNow;
                
                _logger.LogInformation(
                    "Loan {LoanId} activated. Approved at: {ApprovedAt}, Activated at: {ActivatedAt}",
                    loan.Id, loan.ApprovedAt, loan.ActivatedAt);
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("{Count} loan(s) activated", loansToActivate.Count);
        }
    }
}
