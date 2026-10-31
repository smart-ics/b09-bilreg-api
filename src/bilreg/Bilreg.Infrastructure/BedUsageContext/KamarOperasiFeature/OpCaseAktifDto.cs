using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCaseAktifDto(
    string OrderOpId,
    DateTime OrderOpDate,
    string PasienId,
    string PasienName,
    string DokterPengorderId,
    string DokterPengorderName,
    string DokterOperatorId,
    string DokterOperatorName,
    string NamaOperasi,
    string ProsedurTambahan,
    int KlasifikasiOperasi,
    DateTime PreferedDate,
    int EstimasiDurasi,
    int UrgencyLevel,
    int OpCaseState,
    string TglLahir = "3000-01-01",
    string Gender = "")
{
    public static OpCaseAktifDto FromModel(OpCaseReff model)
    {
        var tglLahir = model.Pasien.TglLahir.ToString("yyyy-MM-dd");
        return new OpCaseAktifDto(
            OrderOpId: model.OrderOpId,
            OrderOpDate: model.OrderOpDate,
            PasienId: model.Pasien.PasienId,
            PasienName: model.Pasien.PasienName,
            DokterPengorderId: "-",
            DokterPengorderName: "-",
            DokterOperatorId: "-",
            DokterOperatorName: "-",
            NamaOperasi: "-",
            ProsedurTambahan: string.Empty,
            KlasifikasiOperasi: 1,
            PreferedDate: new DateTime(3000, 1, 1),
            EstimasiDurasi: 0,
            UrgencyLevel: 0,
            OpCaseState: (int)model.OpCaseState,
            TglLahir: tglLahir,
            Gender: model.Pasien.Gender);
    }

    public OpCaseReff ToModel()
    {
        var parsedTglLahir = DateOnly.TryParse(TglLahir, out var d) ? d : new DateOnly(3000, 1, 1);
        var pasien = new PasienReff(PasienId, PasienName, parsedTglLahir, Gender);
        return new OpCaseReff(OrderOpId, OrderOpDate, pasien, (OpCaseStateEnum)OpCaseState);
    }

    public OpCaseOrderView ToView()
    {
        var parsedTglLahir = DateOnly.TryParse(TglLahir, out var d) ? d : new DateOnly(3000, 1, 1);
        var dokterId = !string.IsNullOrWhiteSpace(DokterOperatorId) && DokterOperatorId != "-"
            ? DokterOperatorId
            : DokterPengorderId;
        var dokterName = !string.IsNullOrWhiteSpace(DokterOperatorName) && DokterOperatorName != "-"
            ? DokterOperatorName
            : DokterPengorderName;

        return new OpCaseOrderView(
            OrderOpId: OrderOpId,
            Pasien: new PasienReff(PasienId, PasienName, parsedTglLahir, Gender),
            NamaOperasi: NamaOperasi,
            Dokter: new PpaReff(dokterId, dokterName),
            EstimasiDurasi: EstimasiDurasi,
            UrgencyLevel: (UrgencyLevelEnum)UrgencyLevel,
            PreferedDate: PreferedDate,
            OpCaseState: (OpCaseStateEnum)OpCaseState,
            DokterPengorderId: DokterPengorderId,
            DokterPengorderName: DokterPengorderName,
            DokterOperatorId: DokterOperatorId,
            DokterOperatorName: DokterOperatorName,
            ProsedurTambahan: ProsedurTambahan,
            KlasifikasiOperasi: KlasifikasiOperasi);
    }
}
