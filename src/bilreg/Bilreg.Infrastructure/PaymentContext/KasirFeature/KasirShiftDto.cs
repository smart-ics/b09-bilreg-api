using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public record KasirShiftDto(
    string ShiftId,
    string TglShift,
    string KodeShift,
    string KasirId,
    string Loket,
    string TglJamBuka,
    decimal SaldoAwal,
    string TglJamTutup,
    decimal SaldoAkhirFisik,
    decimal TotalTerimaSistem,
    decimal TotalKeluarSistem,
    string StatusShift,
    string crttgl,
    string crtjam,
    string crtusr,
    string updtgl,
    string updjam,
    string updusr)
{
    public static KasirShiftDto FromModel(KasirShiftModel model)
    {
        return new KasirShiftDto(
            ShiftId: model.ShiftId,
            TglShift: model.TglShift,
            KodeShift: model.KodeShift,
            KasirId: model.KasirId,
            Loket: model.Loket,
            TglJamBuka: model.TglJamBuka,
            SaldoAwal: model.SaldoAwal,
            TglJamTutup: string.IsNullOrWhiteSpace(model.TglJamTutup) ? "3000-01-01 00:00:00" : model.TglJamTutup,
            SaldoAkhirFisik: model.SaldoAkhirFisik,
            TotalTerimaSistem: model.TotalTerimaSistem,
            TotalKeluarSistem: model.TotalKeluarSistem,
            StatusShift: model.StatusShift,
            crttgl: model.Audit.Created.Timestamp.ToString(DateFormatEnum.YMD),
            crtjam: model.Audit.Created.Timestamp.ToString(DateFormatEnum.HMS),
            crtusr: model.Audit.Created.UserId,
            updtgl: model.Audit.Modified.Timestamp.ToString(DateFormatEnum.YMD),
            updjam: model.Audit.Modified.Timestamp.ToString(DateFormatEnum.HMS),
            updusr: model.Audit.Modified.UserId);
    }

    public KasirShiftModel ToModel()
    {
        var crtTgl = string.IsNullOrWhiteSpace(crttgl) ? "3000-01-01" : crttgl;
        var crtJam = string.IsNullOrWhiteSpace(crtjam) ? "00:00:00" : crtjam;
        var updTgl = string.IsNullOrWhiteSpace(updtgl) ? "3000-01-01" : updtgl;
        var updJam = string.IsNullOrWhiteSpace(updjam) ? "00:00:00" : updjam;

        var auditTrail = new AuditTrailType(
            new AuditInfoType(crtusr ?? string.Empty, crtTgl, crtJam),
            new AuditInfoType(updusr ?? string.Empty, updTgl, updJam),
            new AuditInfoType(string.Empty, "3000-01-01", "00:00:00"));

        return new KasirShiftModel(
            shiftId: ShiftId,
            tglShift: TglShift,
            kodeShift: KodeShift,
            kasirId: KasirId,
            loket: Loket ?? string.Empty,
            tglJamBuka: TglJamBuka,
            saldoAwal: SaldoAwal,
            tglJamTutup: string.IsNullOrWhiteSpace(TglJamTutup) ? "3000-01-01 00:00:00" : TglJamTutup,
            saldoAkhirFisik: SaldoAkhirFisik,
            totalTerimaSistem: TotalTerimaSistem,
            totalKeluarSistem: TotalKeluarSistem,
            statusShift: StatusShift,
            audit: auditTrail);
    }
}
