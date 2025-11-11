
using System.ComponentModel.DataAnnotations;
namespace FirstLend.Domain.Entities
{
    
    public class UserCreditAccount
    {
        [Key]
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Guid CreditAccountId { get; set; }
        public CreditAccount? CreditAccount { get; set; }
    }
}