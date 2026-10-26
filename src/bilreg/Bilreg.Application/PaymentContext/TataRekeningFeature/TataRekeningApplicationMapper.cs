using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

internal static class TataRekeningApplicationMapper
{
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

    public static TataRekeningPaymentType ToPaymentType(PaymentAllocationInputDto input) =>
        new(
            new PaymentType(input.PaymentId, input.PaymentName, input.IsTipeJaminan),
            input.NilaiJasa,
            input.NilaiObat,
            new CoaType(input.CoaId, input.CoaName));

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
        var jasaBills = bills.Where(x => x.ModulGroup == BillModulGroup.Jasa);
        var obatBills = bills.Where(x => x.ModulGroup == BillModulGroup.Obat);

        var jasaSubTotal = jasaBills.Sum(x => x.Nilai.SubTotal);
        var obatSubTotal = obatBills.Sum(x => x.Nilai.SubTotal);
        var diskon = bills.Sum(x => x.Nilai.Diskon);
        var pajak = bills.Sum(x => x.Nilai.Tax);
        var tagihanSaatIniTotal = jasaSubTotal + obatSubTotal - diskon + pajak;

        var tagihanSaatIni = new TagihanSaatIniComponent(
            jasaSubTotal,
            obatSubTotal,
            diskon,
            pajak,
            tagihanSaatIniTotal);

        var administrasi = bills.Sum(x => x.Nilai.Biaya);
        var biayaAdministrasi = new BiayaAdministrasiComponent(
            administrasi,
            0m,
            0m,
            administrasi);

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
}
