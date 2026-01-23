using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record DischargeOpDto(
    string DischargeOpId,
    DateTime DischargeOpDate,
    string OrderOpId,
    string PasienId,
    string RegId,
    string KamarId,
    string PpaId,
    int PatientCondition,

    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate,

    string PasienName,
    string TglLahir,
    string Gender,
    string KamarName,
    string PpaName)
{
    public static DischargeOpDto FromModel(DischargeOpModel model)
    {
        var result = new DischargeOpDto(
            model.DischargeOpId,
            model.DischargeDate,
            model.OrderOp.OrderOpId,
            model.Pasien.PasienId,
            model.Reg.RegId,
            model.KamarTujuan,
            model.TeamLead.PpaId,
            (int)model.KondisiPasien,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp,
            model.Pasien.PasienName,
            model.Pasien.TglLahir.ToString("yyyy-MM-dd"));
        return result;
    }
}
