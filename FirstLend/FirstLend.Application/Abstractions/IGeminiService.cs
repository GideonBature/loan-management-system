namespace FirstLend.Application.Abstractions
{
public interface IGeminiService
{
Task<string> AnalyzeLoanDataAsync(string prompt, CancellationToken cancellationToken = default);
 }
}