using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class OrganizationOperatingHours
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }
        public bool IsOpen { get; set; } = true;
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
    }
}
