using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.PenunjangContext.LabFeature
{
    public record TestCategoryType(
        string TestCategoryId,
        string TestCategoryName
    )
    {
        public static TestCategoryType Default => new(
            TestCategoryId: "",
            TestCategoryName: ""
        );
    }
}
