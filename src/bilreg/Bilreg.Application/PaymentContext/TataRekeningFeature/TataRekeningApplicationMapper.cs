using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

internal static class TataRekeningApplicationMapper
{
    public const string DefaultKasirCoaId = "1499901";
    public const string DefaultKasirCoaName = "Rekening Piutang Kasir Perantara";

    public static TataRekeningSummaryDto ToSummaryDto(TataRekeningModel model) =>
        new(
            model.RegId,
            model.Status,
            model.FinancialVerificationStatus,
            model.IsFinancialResponsibilityAllocated,
            model.SettlementInitiated);

    public static TrsBillSummaryDto ToBillSummaryDto(TrsBillType bill) =>
        new(
            bill.TrsBillingId,
            bill.Reg.RegId,
            bill.ModulGroup,
            bill.Nilai.Total,
            bill.FinancialTotal,
            bill.TglTrs,
            bill.Layanan.LayananName,
            bill.Keterangan.Keterangan,
            bill.Keterangan.RefBiaya,
            bill.RekapCetak.RekapCetakName);

    public static PaymentProjectionDto ToProjectionDto(TataRekeningPaymentType payment) =>
        new(
            payment.Payment.PaymentId,
            payment.Payment.PaymentName,
            payment.Payment.IsTipeJaminan,
            payment.NilaiJasa,
            payment.NilaiObat);

    public static MergeRequestSummaryDto ToMergeRequestDto(MergeRequestModel request) =>
        new(
            request.MergeRequestId,
            request.SourceRegId,
            request.TargetRegId,
            request.Status);

    public static TataRekeningPaymentType ToPaymentType(PaymentAllocationInputDto input)
    {
        var coaId = input.CoaId;
        var coaName = input.CoaName;

        if (string.Equals(input.PaymentId, PaymentType.ByKas.PaymentId, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(coaId) ||
                string.Equals(coaId.Trim(), "BYKAS", StringComparison.OrdinalIgnoreCase) ||
                coaId.Trim() == "-")
            {
                coaId = DefaultKasirCoaId;
                coaName = DefaultKasirCoaName;
            }
            else if (string.IsNullOrWhiteSpace(coaName) ||
                     string.Equals(coaName.Trim(), "KAS", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(coaName.Trim(), "BYKAS", StringComparison.OrdinalIgnoreCase) ||
                     coaName.Trim() == "-")
            {
                coaName = DefaultKasirCoaName;
            }
        }

        return new(
            new PaymentType(input.PaymentId, input.PaymentName, input.IsTipeJaminan),
            input.NilaiJasa,
            input.NilaiObat,
            new CoaType(coaId ?? string.Empty, coaName ?? string.Empty));
    }

    public static PaymentType? ToSubsidyPayer(FinancialAdjustmentInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.SubsidyPaymentId))
            return null;

        return new PaymentType(
            input.SubsidyPaymentId,
            input.SubsidyPaymentName ?? input.SubsidyPaymentId,
            false);
    }

    public static TataRekeningPatientContextDto ToPatientContextDto(
        RegModel reg,
        DepositSaldoModel? saldo,
        decimal advancePayment = 0m)
    {
        return new TataRekeningPatientContextDto(
            PasienId: reg.Pasien?.PasienId ?? string.Empty,
            PasienName: reg.Pasien?.PasienName ?? string.Empty,
            Gender: reg.Pasien?.Gender ?? string.Empty,
            TglLahir: reg.Pasien?.TglLahir.ToString("yyyy-MM-dd") ?? string.Empty,
            LayananId: reg.Layanan?.LayananId ?? string.Empty,
            LayananName: reg.Layanan?.LayananName ?? string.Empty,
            JenisReg: reg.JenisReg.ToString(),
            TipeJaminanId: reg.TipeJaminan?.TipeJaminanId ?? string.Empty,
            TipeJaminanName: reg.TipeJaminan?.TipeJaminanName ?? string.Empty,
            KelasName: reg.Kelas?.KelasName,
            DpjpName: reg.Dokter?.PpaName,
            AdvancePayment: advancePayment,
            DepositPayment: saldo?.SaldoAktif ?? 0m);
    }

    public static TataRekeningBillingBreakdownDto ToBillingBreakdownDto(
        TataRekeningModel model,
        decimal depositPayment = 0m,
        decimal advancePayment = 0m)
    {
        var tagihanLalu = new TagihanLaluComponent(0m, 0m, 0m);

        var bills = model.ListTrsBill.ToList();
        var pureJasaBills = bills.Where(x => x.ModulGroup == BillModulGroup.Jasa && ClassifyAdministrativeBill(x) == AdministrativeCategory.None);
        var pureObatBills = bills.Where(x => x.ModulGroup == BillModulGroup.Obat && ClassifyAdministrativeBill(x) == AdministrativeCategory.None);

        var jasaSubTotal = pureJasaBills.Sum(x => x.Nilai.SubTotal);
        var obatSubTotal = pureObatBills.Sum(x => x.Nilai.SubTotal);
        var diskon = bills.Sum(x => x.Nilai.Diskon);
        var pajak = bills.Sum(x => x.Nilai.Tax);
        var tagihanSaatIniTotal = jasaSubTotal + obatSubTotal - diskon + pajak;

        var tagihanSaatIni = new TagihanSaatIniComponent(
            jasaSubTotal,
            obatSubTotal,
            diskon,
            pajak,
            tagihanSaatIniTotal);

        var meteraiBills = bills.Where(x => ClassifyAdministrativeBill(x) == AdministrativeCategory.Meterai);
        var pembulatanBills = bills.Where(x => ClassifyAdministrativeBill(x) == AdministrativeCategory.Pembulatan);
        var adminBills = bills.Where(x => ClassifyAdministrativeBill(x) == AdministrativeCategory.Administrasi);

        var meterai = meteraiBills.Sum(x => x.Nilai.SubTotal != 0 ? x.Nilai.SubTotal : x.FinancialTotal);
        var pembulatan = pembulatanBills.Sum(x => x.Nilai.SubTotal != 0 ? x.Nilai.SubTotal : x.FinancialTotal);

        var adminTransTotal = adminBills.Sum(x => (x.Nilai.SubTotal != 0 || x.Nilai.Biaya != 0)
            ? x.Nilai.SubTotal + x.Nilai.Biaya
            : x.FinancialTotal);
        var otherBillsBiaya = bills.Where(x => ClassifyAdministrativeBill(x) != AdministrativeCategory.Administrasi).Sum(x => x.Nilai.Biaya);
        var administrasi = adminTransTotal + otherBillsBiaya;

        var biayaAdministrasiTotal = administrasi + meterai + pembulatan;
        var biayaAdministrasi = new BiayaAdministrasiComponent(
            administrasi,
            meterai,
            pembulatan,
            biayaAdministrasiTotal);

        var grandTotalKewajiban = tagihanLalu.Total + tagihanSaatIni.Total + biayaAdministrasi.Total;

        decimal deduction;
        if (model.ListPayment.Any())
        {
            deduction = model.ListPayment
                .Where(p => !string.Equals(p.Payment.PaymentId, "BYKAS", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.NilaiJasa + p.NilaiObat);
        }
        else
        {
            deduction = depositPayment + advancePayment;
        }

        var sisaTagihanKasir = grandTotalKewajiban - deduction;

        return new TataRekeningBillingBreakdownDto(
            TagihanLalu: tagihanLalu,
            TagihanSaatIni: tagihanSaatIni,
            BiayaAdministrasi: biayaAdministrasi,
            GrandTotalKewajiban: grandTotalKewajiban,
            SisaTagihanKasir: sisaTagihanKasir);
    }

    private enum AdministrativeCategory
    {
        None,
        Meterai,
        Pembulatan,
        Administrasi
    }

    private static readonly string[] MeteraiKeywords = ["METERAI", "MATERAI"];
    private static readonly string[] PembulatanKeywords = ["PEMBULATAN", "BULAT"];
    private static readonly string[] AdministrasiKeywords = ["ADMINISTRASI", "ADM", "KARCIS"];

    private static bool ContainsAnyKeyword(string? text, string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool MatchesKeywords(TrsBillType bill, string[] keywords)
    {
        return ContainsAnyKeyword(bill.RekapCetak?.RekapCetakName, keywords) ||
               ContainsAnyKeyword(bill.Keterangan?.Keterangan, keywords);
    }

    private static bool IsMeterai(TrsBillType bill) => MatchesKeywords(bill, MeteraiKeywords);

    private static bool IsPembulatan(TrsBillType bill) => MatchesKeywords(bill, PembulatanKeywords);

    private static bool IsBiayaAdministrasi(TrsBillType bill) =>
        MatchesKeywords(bill, AdministrasiKeywords) ||
        (bill.Nilai.Biaya > 0 && bill.Nilai.SubTotal == 0);

    private static AdministrativeCategory ClassifyAdministrativeBill(TrsBillType bill)
    {
        if (IsMeterai(bill))
            return AdministrativeCategory.Meterai;

        if (IsPembulatan(bill))
            return AdministrativeCategory.Pembulatan;

        if (IsBiayaAdministrasi(bill))
            return AdministrativeCategory.Administrasi;

        return AdministrativeCategory.None;
    }
}
