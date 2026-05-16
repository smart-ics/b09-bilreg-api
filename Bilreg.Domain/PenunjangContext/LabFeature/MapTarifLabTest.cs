using Bilreg.Domain.ChargeContext.TarifFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.PenunjangContext.LabFeature
{
    public class MapTarifLabTest: ITarifKey, ILabTestKey
    {
        public string TarifId { get; set; }
        public string LabTestId { get; set; }
    }
}
