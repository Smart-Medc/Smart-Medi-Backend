using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Medc.Application.DTOs.MedicalRecord
{
    public class MedicalRecordDetailDto : MedicalRecordDto
    {
        public List<DocumentDto> Documents { get; set; } = new();
    }
}
