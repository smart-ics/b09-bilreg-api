using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public record TrsKasirDto(
    string TrsKasirId,
    string TglTrs,
    string JamTrs,
    string TglJamTrs,
    string JenisTrs,
    string ShiftId,
    string KasirId,
    string RegId,
    string PasienId,
    string PasienName,
    string OrderRef,
    string Keterangan,
    decimal TotalNilai,
    string StatusTrs,
    string TglVoid,
    string JamVoid,
    string PetugasVoid,
    string AlasanVoid,
    string crttgl,
    string crtjam,
    string crtusr,
    string updtgl,
    string updjam,
    string updusr)
{
    public static TrsKasirDto FromModel(TrsKasirModel model)
    {
        return new TrsKasirDto(
            TrsKasirId: model.TrsKasirId,
            TglTrs: model.TglTrs,
            JamTrs: model.JamTrs,
            TglJamTrs: model.TglJamTrsStr,
            JenisTrs: model.JenisTrs,
            ShiftId: model.ShiftId,
            KasirId: model.KasirId,
            RegId: model.RegId,
            PasienId: model.PasienId,
            PasienName: model.PasienName,
            OrderRef: model.OrderRef,
            Keterangan: model.Keterangan,
            TotalNilai: model.TotalNilai,
            StatusTrs: model.StatusTrs,
            TglVoid: string.IsNullOrWhiteSpace(model.TglVoid) ? "3000-01-01" : model.TglVoid,
            JamVoid: string.IsNullOrWhiteSpace(model.JamVoid) ? "00:00:00" : model.JamVoid,
            PetugasVoid: model.PetugasVoid,
            AlasanVoid: model.AlasanVoid,
            crttgl: model.Audit.Created.Timestamp.ToString(DateFormatEnum.YMD),
            crtjam: model.Audit.Created.Timestamp.ToString(DateFormatEnum.HMS),
            crtusr: model.Audit.Created.UserId,
            updtgl: model.Audit.Modified.Timestamp.ToString(DateFormatEnum.YMD),
            updjam: model.Audit.Modified.Timestamp.ToString(DateFormatEnum.HMS),
            updusr: model.Audit.Modified.UserId);
    }

    public TrsKasirModel ToModel(IEnumerable<TrsKasirPaymentType> payments)
    {
        var crtTgl = string.IsNullOrWhiteSpace(crttgl) ? "3000-01-01" : crttgl;
        var crtJam = string.IsNullOrWhiteSpace(crtjam) ? "00:00:00" : crtjam;
        var updTgl = string.IsNullOrWhiteSpace(updtgl) ? "3000-01-01" : updtgl;
        var updJam = string.IsNullOrWhiteSpace(updjam) ? "00:00:00" : updjam;
        var tglVoid = string.IsNullOrWhiteSpace(TglVoid) ? "3000-01-01" : TglVoid;
        var jamVoid = string.IsNullOrWhiteSpace(JamVoid) ? "00:00:00" : JamVoid;
        var petugasVoid = PetugasVoid ?? string.Empty;

        var auditTrail = new AuditTrailType(
            new AuditInfoType(crtusr ?? string.Empty, crtTgl, crtJam),
            new AuditInfoType(updusr ?? string.Empty, updTgl, updJam),
            new AuditInfoType(petugasVoid, tglVoid, jamVoid));

        if (StatusTrs == KasirConst.StatusTrsVoid && tglVoid != "3000-01-01")
        {
            var voidTimestamp = DateTime.TryParse($"{tglVoid} {jamVoid}", out var vt) ? vt : DateTime.Now;
            auditTrail.Batal(petugasVoid, voidTimestamp);
        }

        return new TrsKasirModel(
            trsKasirId: TrsKasirId,
            tglTrs: TglTrs,
            jamTrs: JamTrs,
            tglJamTrs: TglJamTrs,
            jenisTrs: JenisTrs,
            shiftId: ShiftId,
            kasirId: KasirId,
            regId: RegId ?? string.Empty,
            pasienId: PasienId ?? string.Empty,
            pasienName: PasienName ?? string.Empty,
            orderRef: OrderRef ?? string.Empty,
            keterangan: Keterangan ?? string.Empty,
            totalNilai: TotalNilai,
            statusTrs: StatusTrs,
            payments: payments ?? Enumerable.Empty<TrsKasirPaymentType>(),
            audit: auditTrail,
            tglVoid: tglVoid,
            jamVoid: jamVoid,
            petugasVoid: petugasVoid,
            alasanVoid: AlasanVoid ?? string.Empty);
    }
}
