using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.PenunjangContext.LabFeature
{
    public record TestSpecimenType(
        string SpecimenId,
        string SpecimenName,
        string SnomedId
     )
    {
        public static TestSpecimenType Default => new(
            SpecimenId: "",
            SpecimenName: "",
            SnomedId: ""
        );  
    }
}
