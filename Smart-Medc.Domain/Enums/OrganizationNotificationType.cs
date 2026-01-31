using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Enums
{
    public enum OrganizationNotificationType
    {
        AppointmentRequest = 1,
        AppointmentCancelled = 2,
        AppointmentUpcoming = 3,
        PatientNoShow = 4,
        AppointmentRescheduled = 5,
        SystemNotification = 6
    }
}
