using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstLend.Domain.Enums
{
    public enum LoanStatus
    {
        pending,
        approved,
        rejected,
        active,
        completed,
        defaulted
    }
}