using Bilreg.Domain.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PaymentContext.KasirFeature;

public interface ITrsKasirKey
{
    string TrsKasirId { get; }
}

public record TrsKasirKey(string TrsKasirId) : ITrsKasirKey;

public class TrsKasirModel : ITrsKasirKey
{
    public TrsKasirModel(
        string trsKasirId,
        DateTime tglJamTrs,
        string jenisTrs,
        string shiftId,
        string kasirId,
        string regId,
        string pasienId,
        string pasienName,
        string orderRef,
        string keterangan,
        decimal totalNilai,
        string statusTrs,
        IEnumerable<TrsKasirPaymentType> payments,
        AuditTrailType audit,
        string tglVoid = "3000-01-01",
        string jamVoid = "00:00:00",
        string petugasVoid = "",
        string alasanVoid = "")
    {
        TrsKasirId = trsKasirId?.Trim() ?? string.Empty;
        TglJamTrs = tglJamTrs;
        TglTrs = tglJamTrs.ToString("yyyy-MM-dd");
        JamTrs = tglJamTrs.ToString("HH:mm:ss");
        JenisTrs = (jenisTrs ?? string.Empty).Trim().ToUpperInvariant();
        ShiftId = shiftId?.Trim() ?? string.Empty;
        KasirId = kasirId?.Trim() ?? string.Empty;
        RegId = regId?.Trim() ?? string.Empty;
        PasienId = pasienId?.Trim() ?? string.Empty;
        PasienName = pasienName?.Trim() ?? string.Empty;
        OrderRef = orderRef?.Trim() ?? string.Empty;
        Keterangan = keterangan?.Trim() ?? string.Empty;
        TotalNilai = totalNilai;
        StatusTrs = (statusTrs ?? string.Empty).Trim().ToUpperInvariant();
        Audit = audit ?? AuditTrailType.Default;

        TglVoid = string.IsNullOrWhiteSpace(tglVoid) ? "3000-01-01" : tglVoid.Trim();
        JamVoid = string.IsNullOrWhiteSpace(jamVoid) ? "00:00:00" : jamVoid.Trim();
        PetugasVoid = petugasVoid?.Trim() ?? string.Empty;
        AlasanVoid = alasanVoid?.Trim() ?? string.Empty;

        Payments = payments?.ToList().AsReadOnly() ?? new List<TrsKasirPaymentType>().AsReadOnly();

        ValidateInvariants();
    }

    public TrsKasirModel(
        string trsKasirId,
        string tglTrs,
        string jamTrs,
        string tglJamTrs,
        string jenisTrs,
        string shiftId,
        string kasirId,
        string regId,
        string pasienId,
        string pasienName,
        string orderRef,
        string keterangan,
        decimal totalNilai,
        string statusTrs,
        IEnumerable<TrsKasirPaymentType> payments,
        AuditTrailType audit,
        string tglVoid = "3000-01-01",
        string jamVoid = "00:00:00",
        string petugasVoid = "",
        string alasanVoid = "")
        : this(
            trsKasirId,
            ParseDateTime(tglTrs, jamTrs, tglJamTrs),
            jenisTrs,
            shiftId,
            kasirId,
            regId,
            pasienId,
            pasienName,
            orderRef,
            keterangan,
            totalNilai,
            statusTrs,
            payments,
            audit,
            tglVoid,
            jamVoid,
            petugasVoid,
            alasanVoid)
    {
    }

    public string TrsKasirId { get; private set; }
    public DateTime TglJamTrs { get; private set; }
    public string TglTrs { get; private set; }
    public string JamTrs { get; private set; }
    public string TglJamTrsStr => TglJamTrs.ToString("yyyy-MM-dd HH:mm:ss");
    public string JenisTrs { get; private set; }
    public string ShiftId { get; private set; }
    public string KasirId { get; private set; }
    public string RegId { get; private set; }
    public string PasienId { get; private set; }
    public string PasienName { get; private set; }
    public string OrderRef { get; private set; }
    public string Keterangan { get; private set; }
    public decimal TotalNilai { get; private set; }
    public string StatusTrs { get; private set; }
    public string TglVoid { get; private set; }
    public string JamVoid { get; private set; }
    public string PetugasVoid { get; private set; }
    public string AlasanVoid { get; private set; }
    public AuditTrailType Audit { get; private set; }
    public IReadOnlyList<TrsKasirPaymentType> Payments { get; private set; }

    public bool IsVoid => StatusTrs == KasirConst.StatusTrsVoid;
    public bool IsBerhasil => StatusTrs == KasirConst.StatusTrsBerhasil;

    public static TrsKasirModel CreateTerimaKas(
        string shiftId,
        string kasirId,
        string regId,
        string pasienId,
        string pasienName,
        string orderRef,
        string keterangan,
        decimal totalNilai,
        IEnumerable<TrsKasirPaymentType> payments,
        DateTime? trsTime = null)
    {
        var time = trsTime ?? DateTime.Now;
        var trsKasirId = NunaId.NewLegacy("DX", 'A');
        var audit = AuditTrailType.Create(kasirId, time);

        var mappedPayments = (payments ?? Enumerable.Empty<TrsKasirPaymentType>())
            .Select((p, idx) => p.WithTransaction(trsKasirId, idx + 1))
            .ToList();

        return new TrsKasirModel(
            trsKasirId: trsKasirId,
            tglJamTrs: time,
            jenisTrs: KasirConst.JenisTrsTerima,
            shiftId: shiftId,
            kasirId: kasirId,
            regId: regId,
            pasienId: pasienId,
            pasienName: pasienName,
            orderRef: orderRef,
            keterangan: keterangan,
            totalNilai: totalNilai,
            statusTrs: KasirConst.StatusTrsBerhasil,
            payments: mappedPayments,
            audit: audit);
    }

    public static TrsKasirModel CreateKeluarKas(
        string shiftId,
        string kasirId,
        string regId,
        string pasienId,
        string pasienName,
        string orderRef,
        string keterangan,
        decimal totalNilai,
        IEnumerable<TrsKasirPaymentType> payments,
        DateTime? trsTime = null)
    {
        var time = trsTime ?? DateTime.Now;
        var trsKasirId = NunaId.NewLegacy("DX", 'A');
        var audit = AuditTrailType.Create(kasirId, time);

        var mappedPayments = (payments ?? Enumerable.Empty<TrsKasirPaymentType>())
            .Select((p, idx) => p.WithTransaction(trsKasirId, idx + 1))
            .ToList();

        return new TrsKasirModel(
            trsKasirId: trsKasirId,
            tglJamTrs: time,
            jenisTrs: KasirConst.JenisTrsKeluar,
            shiftId: shiftId,
            kasirId: kasirId,
            regId: regId,
            pasienId: pasienId,
            pasienName: pasienName,
            orderRef: orderRef,
            keterangan: keterangan,
            totalNilai: totalNilai,
            statusTrs: KasirConst.StatusTrsBerhasil,
            payments: mappedPayments,
            audit: audit);
    }

    public static TrsKasirModel CreateKeluarKas(
        string shiftId,
        string kasirId,
        string regId,
        string pasienId,
        string pasienName,
        string orderRef,
        string keterangan,
        decimal totalNilai,
        string metodeBayar = KasirConst.MetodeBayarTunai,
        DateTime? trsTime = null)
    {
        var payment = TrsKasirPaymentType.Create(
            metodeBayar: string.IsNullOrWhiteSpace(metodeBayar) ? KasirConst.MetodeBayarTunai : metodeBayar,
            nilaiBayar: totalNilai,
            nilaiKembalian: 0);

        return CreateKeluarKas(
            shiftId,
            kasirId,
            regId,
            pasienId,
            pasienName,
            orderRef,
            keterangan,
            totalNilai,
            new[] { payment },
            trsTime);
    }

    public void Void(string petugasVoid, string alasanVoid, DateTime voidTime)
    {
        if (StatusTrs != KasirConst.StatusTrsBerhasil)
            throw new BusinessValidationException("Transaksi kasir sudah berstatus VOID dan tidak dapat dibatalkan lagi");

        if (string.IsNullOrWhiteSpace(petugasVoid))
            throw new BusinessValidationException("Petugas void wajib diisi");

        if (string.IsNullOrWhiteSpace(alasanVoid))
            throw new BusinessValidationException("Alasan void wajib diisi");

        StatusTrs = KasirConst.StatusTrsVoid;
        PetugasVoid = petugasVoid.Trim();
        AlasanVoid = alasanVoid.Trim();
        TglVoid = voidTime.ToString("yyyy-MM-dd");
        JamVoid = voidTime.ToString("HH:mm:ss");

        Audit.Batal(PetugasVoid, voidTime);
    }

    private void ValidateInvariants()
    {
        if (string.IsNullOrWhiteSpace(TrsKasirId))
            throw new BusinessValidationException("TrsKasirId wajib diisi");

        if (string.IsNullOrWhiteSpace(ShiftId))
            throw new BusinessValidationException("ShiftId wajib diisi");

        if (string.IsNullOrWhiteSpace(KasirId))
            throw new BusinessValidationException("KasirId wajib diisi");

        if (JenisTrs != KasirConst.JenisTrsTerima && JenisTrs != KasirConst.JenisTrsKeluar)
            throw new BusinessValidationException($"Jenis transaksi '{JenisTrs}' tidak valid. Harus TERIMA atau KELUAR");

        if (StatusTrs != KasirConst.StatusTrsBerhasil && StatusTrs != KasirConst.StatusTrsVoid)
            throw new BusinessValidationException($"Status transaksi '{StatusTrs}' tidak valid. Harus BERHASIL atau VOID");

        if (TotalNilai <= 0)
            throw new BusinessValidationException("Total nilai transaksi kasir harus lebih besar dari nol");

        if (Payments is null || Payments.Count == 0)
            throw new BusinessValidationException("Rincian metode pembayaran wajib diisi minimal satu");

        var sumNetto = Payments.Sum(p => p.NilaiNetto);
        if (sumNetto != TotalNilai)
        {
            throw new BusinessValidationException(
                $"Total pembayaran netto ({sumNetto:#,##0.00}) tidak sesuai dengan TotalNilai transaksi ({TotalNilai:#,##0.00})");
        }

        foreach (var p in Payments)
        {
            if (p.MetodeBayar != KasirConst.MetodeBayarTunai && p.NilaiKembalian > 0)
            {
                throw new BusinessValidationException(
                    $"Nilai kembalian hanya diizinkan untuk metode bayar TUNAI, bukan {p.MetodeBayar}");
            }

            if (p.MetodeBayar == KasirConst.MetodeBayarTunai && p.NilaiBayar < p.NilaiKembalian)
            {
                throw new BusinessValidationException(
                    "Nilai bayar tunai tidak boleh lebih kecil dari nilai kembalian");
            }
        }

        var nonCashPayments = Payments
            .Where(p => p.MetodeBayar != KasirConst.MetodeBayarTunai)
            .Select(p => !string.IsNullOrWhiteSpace(p.KodeJenisPayment) ? p.KodeJenisPayment.Trim().ToUpperInvariant() : p.MetodeBayar.Trim().ToUpperInvariant())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();

        var duplicate = nonCashPayments
            .GroupBy(k => k)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            throw new BusinessValidationException($"Jenis pembayaran '{duplicate.Key}' tidak boleh digunakan lebih dari satu kali dalam satu transaksi");
        }
    }

    private static DateTime ParseDateTime(string tglTrs, string jamTrs, string tglJamTrs)
    {
        if (DateTime.TryParseExact(tglJamTrs, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParse($"{tglTrs} {jamTrs}", out dt))
            return dt;
        if (DateTime.TryParse(tglTrs, out dt))
            return dt;
        return DateTime.Now;
    }
}
