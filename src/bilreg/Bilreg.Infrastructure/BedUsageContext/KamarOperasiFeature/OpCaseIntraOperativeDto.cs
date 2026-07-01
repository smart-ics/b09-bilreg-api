using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCaseIntraOperativeDto(
    string OrderOpId, DateTime OrderOpDate, string PasienId, int OpCaseState,
    string PasienName, string TglLahir, string Gender, string NamaOperasi,
    int UrgencyLevel, DateTime PreferedDate, DateTime ScheduledDate, DateTime OperationDate,
    string DokterId, string DokterName
    )
{
}
