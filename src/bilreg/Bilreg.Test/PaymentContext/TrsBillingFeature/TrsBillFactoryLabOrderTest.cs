using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillFactoryLabOrderTest
{
    [Fact]
    public void GivenLabOrder_WhenCreateFromLabOrderItem_ThenPopulatesLayananAndOrderIdAsTrsMainId()
    {
        // Arrange
        const string labLayananId = "4LB01";
        const string labLayananName = "LABORATORIUM";

        var labOrder = LabOrderTestSupport.CreateEmrOrder();
        var orderItem = labOrder.Items[0];

        var reg = new RegModel(
            labOrder.Patient.RegId,
            new DateOnly(2026, 1, 1),
            new AuditInfoType("USER1", DateTime.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff("00000", "Umum"),
            PolisModel.Default.ToReff(),
            new KelasReff("III", "Kelas III"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        var jaminan = JaminanType.Umum;
        var tarif = TarifType.Default with { TarifId = orderItem.TarifId, TarifName = orderItem.TarifName };
        var nilaiTarif = NilaiTarifType.Default with
        {
            Nilai = 50000m,
            Kelas = new KelasReff("III", "Kelas III")
        };
        var labLayanan = new LayananReff(labLayananId, labLayananName);

        // Act
        var bill = TrsBillFactory.CreateFromLabOrderItem(
            labOrder, orderItem, reg, jaminan, tarif, nilaiTarif, [], labLayanan, DateTime.Now);

        // Assert Domain Model
        bill.Layanan.LayananId.Should().Be(labLayananId);
        bill.Layanan.LayananName.Should().Be(labLayananName);
        bill.Keterangan.TrsMainId.Should().Be(labOrder.OrderId);

        // Assert DTO Mapping to ta_trs_billing columns
        var dto = TrsBillingDto.FromModel(bill);
        dto.fs_kd_layanan.Should().Be(labLayananId);
        dto.fs_kd_trs_main.Should().Be(labOrder.OrderId);
    }
}
