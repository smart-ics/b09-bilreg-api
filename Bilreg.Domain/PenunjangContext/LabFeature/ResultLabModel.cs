using Bilreg.Domain.AdmisiContext.PpaFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.PenunjangContext.LabFeature
{
    public class ResultLabModel:IResultLabKey
    {
        private readonly IEnumerable<LabResultItem> _items;

        public ResultLabModel(string resultId, string orderId, 
            DateTime? observationDateTime, DateTime? resultDateTime, 
            string resultStatus, string patientId, string patientName, 
            ValidationInfo validation, IEnumerable<LabResultItem> items)
        {
            ResultId = resultId;
            OrderId = orderId;
            ObservationDateTime = observationDateTime;
            ResultDateTime = resultDateTime;
            ResultStatus = resultStatus;
            PatientId = patientId;
            PatientName = patientName;
            Validation = validation;
            _items = items;
        }

        public string ResultId { get; private set; }           
        public string OrderId { get; private set; }

        public DateTime? ObservationDateTime { get; private set; }  // OBR-7
        public DateTime? ResultDateTime { get; private set; }   // OBR-22

        public string ResultStatus { get; private set; }    // (F, P, C)

        public string PatientId { get; private set; }
        public string PatientName { get; private set; }
        public ValidationInfo Validation { get; private set; }
        public IEnumerable<LabResultItem> Items => _items;

        public ResultLabModel Create(string orderId, string patientId, string patientName)
        {
            var id = Guid.NewGuid().ToString();
            var _items = new List<LabResultItem>();
            var defDateTime = DateTime.Parse("3000-01-01");
            return new ResultLabModel(
                id, orderId, defDateTime, defDateTime,ResultStatus,
                patientId, patientName, ValidationInfo.Default,
                _items);
        }
    }
    public class LabResultItem
    {
        //---grouping
        public string Department { get; set; } /// <example>Hematologi</example>
        public string Group { get; set; } /// <example>DL 5 DIFF (Darah Lengkap)</example>
        public string Category { get; set; } /// <example>WBC (Jumlah Leukosit)</example>
        //---identification
        public string LabTestId { get; set; } // Map dari test_cd (WBC)
        public string LabTestName { get; set; } // Map dari test_nm (Jumlah Lekosit)
        public string OrderTestId { get; set; } // TarifId map dari order_testid
        public string LoincCode { get; set; } // Map dari loinc_cd (6690-2)
        //---result
        public string ResultValue { get; set; } // Map dari result_value
        public string Unit { get; set; } // Map dari unit (10^3/ul)
        public string Flag { get; set; } // Misal: "H" (High), "L" (Low)
        public string ReferenceRange { get; set; } // Map dari ref_range (4.0 - 10.0)
        public string Status { get; set; } // Map dari status (F = Final)
        public ResultDataTypeEnum  DataType { get; set; } // Map dari data_type (NM = Numeric)
        //---additional info
        public string Method { get; set; }
        public string ResultNote { get; set; }
        public string DisplaySequence { get; set; } // Map dari disp_seq
        public SpecimenInfo Specimen { get; private set; }
        public PegType Operator { get; private set; }
    }
    public record ValidationInfo(PegType ValidatedBy, DateTime ValidatedDateTime)
    {
        public static ValidationInfo Default => new(PegType.Default, DateTime.Parse("3000-01-01"));
    }
    public record SpecimenInfo (string SpecimenId, string LabNumber, DateTime CollectedDateTime, string CollectedBy);

}
