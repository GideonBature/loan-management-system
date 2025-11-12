using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FirstLend.Domain.Entities
{
    public class LoanType
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Interest { get; set; }
        public int MaxTermMonths { get; set; } // Maximum loan term in months
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}