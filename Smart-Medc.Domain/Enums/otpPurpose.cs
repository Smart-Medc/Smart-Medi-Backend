using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Enums
{
    public enum OtpPurpose
    {
        EmailVerification = 1,
        PhoneVerification = 2,
        PasswordReset = 3,
        TwoFactorAuthentication = 4
    }
}
