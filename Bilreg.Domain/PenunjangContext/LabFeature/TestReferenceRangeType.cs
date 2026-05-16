using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Domain.PenunjangContext.LabFeature
{
    public class ReferenceRangeType
    {
        public Guid Id { get; set; }

        // Filter Demografis
        public GenderType Gender { get; set; } 
        public int? MinAgeDays { get; set; }
        public int? MaxAgeDays { get; set; }

        // Ambang Batas (Numeric)
        public double? NormalLow { get; set; }
        public double? NormalHigh { get; set; }

        // Ambang Batas Kritis (Emergency)
        public double? CriticalLow { get; set; }
        public double? CriticalHigh { get; set; }

        // Rujukan Tekstual (Untuk hasil non-angka)
        public string ExpectedValue { get; set; } // Misal: "Negative" atau "Non-Reactive"

        public string Note { get; set; } // Keterangan tambahan (misal: "Harus puasa 12 jam")
    }

}
