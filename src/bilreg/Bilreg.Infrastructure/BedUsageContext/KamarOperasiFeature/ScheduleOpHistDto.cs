using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record ScheduleOpHistDto(
    string ScheduleOpHistId,
    string ScheduleOpId,
    int NoUrut,
    DateTime OldTglOp,
    DateTime NewTglOp,
    int OldDurasi,
    int NewDurasi,
    string OldKamarId,
    string NewKamarId,
    string OldPpaId,
    string NewPpaId,
    string Alasan,
    bool IsOverride,
    string OverrideReason,
    string UserId,
    DateTime CrtDate)
{
    public static ScheduleOpHistDto FromModel(string scheduleOpId, ScheduleOpHistType model)
    {
        return new ScheduleOpHistDto(
            Ulid.NewUlid().ToString(),
            scheduleOpId,
            model.NoUrut,
            model.OldTglOp,
            model.NewTglOp,
            model.OldDurasi,
            model.NewDurasi,
            model.OldKamar?.KamarId ?? string.Empty,
            model.NewKamar?.KamarId ?? string.Empty,
            model.OldTeamLead?.PpaId ?? string.Empty,
            model.NewTeamLead?.PpaId ?? string.Empty,
            model.Alasan,
            model.IsOverride,
            model.OverrideReason,
            model.UserId,
            model.CrtDate
        );
    }

    public ScheduleOpHistType ToModel()
    {
        var oldKamar = new KamarReff(OldKamarId, string.Empty);
        var newKamar = new KamarReff(NewKamarId, string.Empty);
        var oldTeamLead = new PpaReff(OldPpaId, string.Empty);
        var newTeamLead = new PpaReff(NewPpaId, string.Empty);
        return new ScheduleOpHistType(
            NoUrut,
            OldTglOp,
            NewTglOp,
            OldDurasi,
            NewDurasi,
            oldKamar,
            newKamar,
            oldTeamLead,
            newTeamLead,
            Alasan,
            IsOverride,
            OverrideReason,
            UserId,
            CrtDate);
    }
}
