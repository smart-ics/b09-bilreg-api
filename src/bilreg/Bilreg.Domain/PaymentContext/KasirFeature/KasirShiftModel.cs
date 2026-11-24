using Bilreg.Domain.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PaymentContext.KasirFeature;

public interface IKasirShiftKey
{
    string ShiftId { get; }
}

public record KasirShiftKey(string ShiftId) : IKasirShiftKey;

public class KasirShiftModel : IKasirShiftKey
{
    public KasirShiftModel(
        string shiftId,
        string tglShift,
        string kodeShift,
        string kasirId,
        string loket,
        string tglJamBuka,
        decimal saldoAwal,
        string tglJamTutup,
        decimal saldoAkhirFisik,
        decimal totalTerimaSistem,
        decimal totalKeluarSistem,
        string statusShift,
        AuditTrailType audit)
    {
        ShiftId = shiftId?.Trim() ?? string.Empty;
        TglShift = tglShift?.Trim() ?? string.Empty;
        KodeShift = kodeShift?.Trim() ?? string.Empty;
        KasirId = kasirId?.Trim() ?? string.Empty;
        Loket = loket?.Trim() ?? string.Empty;
        TglJamBuka = tglJamBuka?.Trim() ?? string.Empty;
        SaldoAwal = saldoAwal;
        TglJamTutup = string.IsNullOrWhiteSpace(tglJamTutup) ? "3000-01-01 00:00:00" : tglJamTutup.Trim();
        SaldoAkhirFisik = saldoAkhirFisik;
        TotalTerimaSistem = totalTerimaSistem;
        TotalKeluarSistem = totalKeluarSistem;
        StatusShift = (statusShift ?? string.Empty).Trim().ToUpperInvariant();
        Audit = audit ?? AuditTrailType.Default;

        ValidateInvariants();
    }

    public string ShiftId { get; private set; }
    public string TglShift { get; private set; }
    public string KodeShift { get; private set; }
    public string KasirId { get; private set; }
    public string Loket { get; private set; }
    public string TglJamBuka { get; private set; }
    public decimal SaldoAwal { get; private set; }
    public string TglJamTutup { get; private set; }
    public decimal SaldoAkhirFisik { get; private set; }
    public decimal TotalTerimaSistem { get; private set; }
    public decimal TotalKeluarSistem { get; private set; }
    public string StatusShift { get; private set; }
    public AuditTrailType Audit { get; private set; }

    public bool IsOpen => StatusShift == KasirConst.StatusShiftBuka;
    public bool IsClosed => StatusShift == KasirConst.StatusShiftTutup;
    public decimal SaldoAkhirSistem => SaldoAwal + TotalTerimaSistem - TotalKeluarSistem;
    public decimal SelisihKas => SaldoAkhirFisik - SaldoAkhirSistem;

    public static KasirShiftModel OpenShift(
        string kodeShift,
        string kasirId,
        string loket,
        decimal saldoAwal,
        DateTime openTime)
    {
        if (string.IsNullOrWhiteSpace(kodeShift))
            throw new BusinessValidationException("Kode shift wajib diisi");

        if (string.IsNullOrWhiteSpace(kasirId))
            throw new BusinessValidationException("KasirId wajib diisi");

        if (saldoAwal < 0)
            throw new BusinessValidationException("Saldo awal kasir tidak boleh negatif");

        var shiftId = NunaId.NewLegacy("SH", 'A');
        var tglShift = openTime.ToString("yyyy-MM-dd");
        var tglJamBuka = openTime.ToString("yyyy-MM-dd HH:mm:ss");
        var audit = AuditTrailType.Create(kasirId, openTime);

        return new KasirShiftModel(
            shiftId: shiftId,
            tglShift: tglShift,
            kodeShift: kodeShift,
            kasirId: kasirId,
            loket: loket,
            tglJamBuka: tglJamBuka,
            saldoAwal: saldoAwal,
            tglJamTutup: "3000-01-01 00:00:00",
            saldoAkhirFisik: 0,
            totalTerimaSistem: 0,
            totalKeluarSistem: 0,
            statusShift: KasirConst.StatusShiftBuka,
            audit: audit);
    }

    public void CloseShift(decimal saldoAkhirFisik, decimal totalTerima, decimal totalKeluar, DateTime closeTime)
    {
        if (StatusShift != KasirConst.StatusShiftBuka)
            throw new BusinessValidationException("Shift kasir sudah berstatus TUTUP dan tidak dapat ditutup kembali");

        if (saldoAkhirFisik < 0)
            throw new BusinessValidationException("Saldo akhir fisik tidak boleh negatif");

        if (totalTerima < 0)
            throw new BusinessValidationException("Total terima sistem tidak boleh negatif");

        if (totalKeluar < 0)
            throw new BusinessValidationException("Total keluar sistem tidak boleh negatif");

        SaldoAkhirFisik = saldoAkhirFisik;
        TotalTerimaSistem = totalTerima;
        TotalKeluarSistem = totalKeluar;
        TglJamTutup = closeTime.ToString("yyyy-MM-dd HH:mm:ss");
        StatusShift = KasirConst.StatusShiftTutup;
        Audit.Modif(KasirId, closeTime);
    }

    public void UpdateRekapSistem(decimal totalTerima, decimal totalKeluar)
    {
        if (StatusShift != KasirConst.StatusShiftBuka)
            throw new BusinessValidationException("Shift kasir sudah TUTUP, rekap sistem tidak dapat diperbarui");

        if (totalTerima < 0)
            throw new BusinessValidationException("Total terima sistem tidak boleh negatif");

        if (totalKeluar < 0)
            throw new BusinessValidationException("Total keluar sistem tidak boleh negatif");

        TotalTerimaSistem = totalTerima;
        TotalKeluarSistem = totalKeluar;
    }

    private void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(ShiftId))
            throw new BusinessValidationException("ShiftId wajib diisi");

        if (string.IsNullOrWhiteSpace(KodeShift))
            throw new BusinessValidationException("Kode shift wajib diisi");

        if (string.IsNullOrWhiteSpace(KasirId))
            throw new BusinessValidationException("KasirId wajib diisi");

        if (StatusShift != KasirConst.StatusShiftBuka && StatusShift != KasirConst.StatusShiftTutup)
            throw new BusinessValidationException($"Status shift '{StatusShift}' tidak valid. Harus BUKA atau TUTUP");

        if (SaldoAwal < 0)
            throw new BusinessValidationException("Saldo awal kasir tidak boleh negatif");
    }
}
