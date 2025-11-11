using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLend.Domain.Entities;
using FirstLend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FirstLend.Infrastructure.Data
{
    public class FirstLendDbContext : IdentityDbContext<ApplicationUser>
    {
        public FirstLendDbContext(DbContextOptions<FirstLendDbContext> options) : base(options)
        {
        }

        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanType> LoanTypes { get; set; }
        public DbSet<PaymentHistory> PaymentHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserCreditAccount> UserCreditAccounts { get; set; }
        public DbSet<CreditAccount> CreditAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Loan -> ApplicationUser relationship
            modelBuilder.Entity<Loan>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(l => l.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure CreditAccount -> RepaymentEvent relationship (owned collection)
            modelBuilder.Entity<CreditAccount>()
                .OwnsMany(ca => ca.RepaymentHistory, rh =>
                {
                    rh.WithOwner().HasForeignKey("CreditAccountId");
                    rh.Property<Guid>("CreditAccountId");
                });

            // Configure UserCreditAccount relationships
            modelBuilder.Entity<UserCreditAccount>()
                .HasKey(uca => uca.Id);

            modelBuilder.Entity<UserCreditAccount>()
                .HasIndex(uca => new { uca.UserId, uca.CreditAccountId })
                .IsUnique();
        }
    }
}