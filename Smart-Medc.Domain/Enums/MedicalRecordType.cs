using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Domain.Enums
{
    public enum MedicalRecordType
    {
        LabReport = 1,
        Imaging = 2,
        ConsultationNotes = 3,
        Immunization = 4,
        Prescription = 5,
        Surgery = 6,
        Pathology = 7,
        Other = 8
    }
}
