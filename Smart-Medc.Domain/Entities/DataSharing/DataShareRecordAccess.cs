using Smart_Medc.Domain.Entities.PatientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Entities.DataSharing
{
    public class DataShareRecordAccess
    {
        public Guid Id { get; set; }
        public Guid DataShareCodeId { get; set; }
        public Guid MedicalRecordId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual DataShareCode DataShareCode { get; set; } = null!;
        public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
