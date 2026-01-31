using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Enums
{
    public enum AdherenceStatus
    {
        Pending = 1,
        Taken = 2,
        Skipped = 3,
        Snoozed = 4,
        Late = 5
    }
}
