using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLend.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace FirstLend.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Address { get; set; } = "";
        public string PhotoUrl { get; set; } = "";
        public string PublicId { get; set; } = "";
        public UserType UserType { get; set; } = UserType.Customer;
        public UserStatus Status { get; set; } = UserStatus.Active;
        public bool EmailVerified { get; set; } = false;
        public bool PhoneVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        
        // KYC Verification Properties
        public bool KycVerified { get; set; } = false;
        public string BVN { get; set; } = "";
        public string NIN { get; set; } = "";
        public DateTime? KycVerificationDate { get; set; }
    }
}