using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.PenunjangContext.LabFeature
{
    public class LabTestModel: ILabTestKey
    {
        public LabTestModel(string labTestId, string loincCode, string labTestName, 
            string description, TestDepartmenType department, TestGroupType group, 
            TestCategoryType category, TestSpecimenType specimen, string method, string unit)
        {
            LabTestId = labTestId;
            LoincCode = loincCode;
            LabTestName = labTestName;
            Description = description;
            Department = department;
            Group = group;
            Category = category;
            Specimen = specimen;
            Method = method;
            Unit = unit;
        }

        public string LabTestId { get; private set; } 
        public string LoincCode { get; private set; } // Logical Observation Identifiers Names and Codes
        public string LabTestName { get; private set; }
        public string Description { get; private set; }

        public TestDepartmenType Department { get; private set; } // (Hematologi, Kimia Klinik, Imunologi)
        public TestGroupType Group { get; private set; } // (Panel, Single Test)
        public TestCategoryType Category { get; private set; } // (DL-5Diff)

        public TestSpecimenType Specimen { get; private set; }
        public string Method { get; private set; } // Spectrophotometry, ELISA, PCR
        public string Unit { get; private set; } // Satuan: mg/dL, g/L, mmol/L

        public ResultDataTypeEnum DataType { get; private set; } // Numeric, String, Boolean (Pos/Neg), Scale

        public IEnumerable<ReferenceRangeType> ReferenceRanges { get; private set; }

        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public string LastUpdatedBy { get; private set; }

        //behavior
        private static LabTestModel Key(string id) 
            => new(
                id, "", "", "", 
                TestDepartmenType.Default, TestGroupType.Default, 
                TestCategoryType.Default, TestSpecimenType.Default, 
                "", "");

        public void SetActive(bool isActive, string userId)
        {
            IsActive = isActive;
            LastUpdatedBy = userId;
       
        }
    }

    public enum ResultDataTypeEnum
    {
        Numeric,
        Text,
        Qualitative, // Positif/Negatif
        Ordinal      // Skala (1+, 2+, 3+)
    }
}
