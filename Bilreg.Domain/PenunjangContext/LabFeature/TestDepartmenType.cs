using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.PenunjangContext.LabFeature
{
    public record TestDepartmenType(
        string TestDepartmenId,
        string TestDepartmenName
    )
    {
        public static TestDepartmenType Default => new(
            TestDepartmenId: "",
            TestDepartmenName: ""
        );
    }
}
