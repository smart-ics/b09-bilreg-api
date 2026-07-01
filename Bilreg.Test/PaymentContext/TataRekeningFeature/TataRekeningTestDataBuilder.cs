using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

internal static class TataRekeningTestDataBuilder
{
    public static readonly DateTime TestDate = new(2026, 6, 16, 10, 0, 0);

    public static TataRekeningModel Hydrate(
        string regId,
        TataRekeningStatusEnum status,
        params TrsBillType[] listTrsBill) =>
        new(regId, status, TataRekeningFinalizationType.Default, [], listTrsBill);

    public static TataRekeningModel HydrateClosed(string regId, params TrsBillType[] bills)
    {
        var model = Hydrate(regId, TataRekeningStatusEnum.Opened, bills);
        model.Close();
        return model;
    }

    public static TataRekeningPaymentType BuildPayment(PaymentType payment, decimal nilaiJasa, decimal nilaiObat) =>
        new(payment, nilaiJasa, nilaiObat, CoaType.Default);

    public static TrsBillType CreateBill(string billId, string regId, decimal amount)
    {
        var komponen = new TrsBill2KomponenType("KOMP-01", "Komponen Test");
        var trans = TrsBill2TransEventType.Create(
            0, komponen, TrsBillJenisBayarType.Pdp, amount,
            PpaType.Default.ToReff(), ValidPdpCoa);

        return new TrsBillType(
            billId, BillModulGroup.Jasa, TestDate,
            new RegReff(regId, "-", "-"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(amount, 0, 0, 0),
            new TrsBillKetType("Test", "", "REF", 1, ""),
            [trans], [], []);
    }

    private static TrsBill2CoaType ValidPdpCoa => new(
        new CoaType("PPDP-01", ""),
        new CoaType("PDPT-01", ""),
        CoaType.Default,
        CoaType.Default,
        CoaType.Default,
        CoaType.Default);
}
