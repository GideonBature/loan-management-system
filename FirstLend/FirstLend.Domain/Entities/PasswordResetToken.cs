namespace FirstLend.Domain.Entities
{
    public class PasswordResetToken
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = "";
        public string TokenHash { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}