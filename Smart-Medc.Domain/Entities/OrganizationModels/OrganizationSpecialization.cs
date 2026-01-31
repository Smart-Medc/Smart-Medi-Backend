using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Entities.OrganizationModels
{
    public class OrganizationSpecialization
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid SpecializationId { get; set; }
        public bool IsPrimary { get; set; } = false;

        // Navigation
        public virtual Organization Organization { get; set; } = null!;
        public virtual Specialization Specialization { get; set; } = null!;
    }
}
