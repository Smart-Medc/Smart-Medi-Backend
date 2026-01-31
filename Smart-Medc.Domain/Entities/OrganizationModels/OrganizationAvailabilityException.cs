using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class OrganizationAvailabilityException
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }

        public DateOnly Date { get; set; }
        public bool IsFullDayOff { get; set; } = true;
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
    }
}
