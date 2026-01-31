using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Enums
{
    public enum DataShareStatus
    {
        Active = 1,
        Expired = 2,
        Revoked = 3,
        MaxAccessReached = 4
    }
}
