using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillFactoryRadOrderTest
{
    private static RadOrderModel CreateRadOrder(
        string orderId = "RDO0001",
        int itemNo = 1,
        string dokterId = "DOK01",
        string dokterName = "dr. Radiologi, Sp.Rad")
    {
        var patient = new PatientSnapshotType(
            "REG001",
            "MR0001",
            "Pasien Radiologi",
            new DateTime(1990, 1, 15),
            "L",
            36);

        var item = RadOrderItemModel.Create(
            orderId: orderId,
            itemNo: itemNo,
            tarifId: "RAD-THORAX",
            tarifName: "Thorax AP/PA",
            modality: RadModalityEnum.CR,
            urgency: RadUrgencyEnum.Rutin);

        return new RadOrderModel(
            orderId: orderId,
            emrOrderId: "EMR-001",
            orderNo: "RAD-2026-0001",
            orderSource: RadOrderSourceEnum.Internal,
            modality: RadModalityEnum.CR,
            radOrderStatus: RadOrderStatusEnum.Requested,
            urgency: RadUrgencyEnum.Rutin,
            patient: patient,
            executionRegId: "REG001",
            dokterPengorderId: dokterId,
            dokterPengorderName: dokterName,
            layananPengorderId: "LAY01",
            layananPengorderName: "Poli Paru",
            clinicalNote: "Batuk berkepanjangan",
            cancelledReason: string.Empty,
            cancelledDate: RadOrderModel.EmptyDate,
            cancelledUserId: string.Empty,
            auditTrail: AuditTrailType.Create("USER01", new DateTime(2026, 11, 24, 8, 0, 0)),
            items: [item]);
    }

    private static RegModel CreateReg(string regId = "REG001", JenisRegEnum jenisReg = JenisRegEnum.RegJalan)
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 1, 1),
            new AuditInfoType("USER1", DateTime.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            jenisReg,
            PasienModel.Default.ToReff(),
            new TipeJaminanReff("00000", "Umum"),
            PolisModel.Default.ToReff(),
            new KelasReff("III", "Kelas III"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff("4PL01", "Poli Paru"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    [Fact]
    public void GivenRadOrder_WhenCreateFromRadOrderItem_ThenPopulatesBillingIdAndModulGroupJasa()
    {
        // Arrange
        var radOrder = CreateRadOrder();
        var radOrderItem = radOrder.Items[0];
        var reg = CreateReg();
        var jaminan = JaminanType.Umum;
        var tarif = TarifType.Default with { TarifId = radOrderItem.TarifId, TarifName = radOrderItem.TarifName };
        var komponen = KomponenType.Default with
        {
            KomponenId = "KOMP01",
            KomponenName = "Jasa Radiologi",
            RekPdpt = new CoaType("41101", "Pendapatan"),
            RekDiskon = new CoaType("51101", "Diskon")
        };
        var tarifKomponen = new NilaiTarifKomponenType(1, komponen.ToReff(), 75000m);
        var nilaiTarif = new NilaiTarifType(
            "NIL01",
            radOrderItem.TarifId,
            radOrderItem.TarifName,
            new TipeTarifReff("TP01", "Tipe 1"),
            new KelasReff("III", "Kelas III"),
            75000m,
            [tarifKomponen]);
        var radLayanan = new LayananReff("4RD01", "RADIOLOGI");
        var createdAt = new DateTime(2026, 11, 24, 9, 30, 0);

        // Act
        var bill = TrsBillFactory.CreateFromRadOrderItem(
            radOrder, radOrderItem, reg, jaminan, tarif, nilaiTarif, [komponen], radLayanan, createdAt);

        // Assert
        bill.TrsBillingId.Should().Be($"{radOrder.OrderId}-{radOrderItem.ItemNo}");
        bill.ModulGroup.Should().Be(BillModulGroup.Jasa);
        bill.Keterangan.TrsMainId.Should().Be(radOrder.OrderId);
        bill.Keterangan.Keterangan.Should().Be(tarif.TarifName);
        bill.Keterangan.RefBiaya.Should().Be(tarif.TarifId);
        bill.Keterangan.Qty.Should().Be(1);
        bill.Kelas.Should().Be(reg.Kelas);
        bill.Layanan.LayananId.Should().Be(radLayanan.LayananId);
        bill.Layanan.LayananName.Should().Be(radLayanan.LayananName);
        bill.Nilai.Total.Should().Be(75000m);
        bill.AuditInfo.UserId.Should().Be("USER01");

        // Assert TransEvent & Ppa mapping
        bill.ListTransaction.Should().HaveCount(1);
        var transEvent = bill.ListTransaction.First();
        transEvent.PetugasMedis.PpaId.Should().Be(radOrder.DokterPengorderId);
        transEvent.PetugasMedis.PpaName.Should().Be(radOrder.DokterPengorderName);
        transEvent.Nilai.Should().Be(75000m);
        transEvent.Komponen.BillKompId.Should().Be("KOMP01");
        transEvent.Coa.Pdpt.CoaId.Should().Be("41101");
    }

    [Fact]
    public void GivenRadOrder_WhenNoLayananOverride_ThenUsesRegLayanan()
    {
        // Arrange
        var radOrder = CreateRadOrder();
        var radOrderItem = radOrder.Items[0];
        var reg = CreateReg();
        var jaminan = JaminanType.Umum;
        var tarif = TarifType.Default with { TarifId = radOrderItem.TarifId, TarifName = radOrderItem.TarifName };
        var nilaiTarif = NilaiTarifType.Default with { Nilai = 50000m };

        // Act
        var bill = TrsBillFactory.CreateFromRadOrderItem(
            radOrder, radOrderItem, reg, jaminan, tarif, nilaiTarif, []);

        // Assert
        bill.Layanan.LayananId.Should().Be(reg.Layanan.LayananId);
        bill.Layanan.LayananName.Should().Be(reg.Layanan.LayananName);
    }

    [Fact]
    public void GivenMismatchedJaminan_WhenCreateFromRadOrderItem_ThenThrowsArgumentException()
    {
        // Arrange
        var radOrder = CreateRadOrder();
        var radOrderItem = radOrder.Items[0];
        var reg = CreateReg();
        var mismatchedJaminan = JaminanType.Default with { JaminanId = "999" };
        var tarif = TarifType.Default;
        var nilaiTarif = NilaiTarifType.Default;

        // Act
        var act = () => TrsBillFactory.CreateFromRadOrderItem(
            radOrder, radOrderItem, reg, mismatchedJaminan, tarif, nilaiTarif, []);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Jaminan tidak sesuai registrasi");
    }

    [Fact]
    public void GivenAddBillAppService_WhenFromRadOrderItem_ThenPersistsTataRekeningAndBill()
    {
        // Arrange
        var tataRekeningRepoMock = new Mock<ITataRekeningRepo>();
        var trsBillingRepoMock = new Mock<ITrsBillingRepo>();
        var createBillDomService = new CreateBillDomService();
        var sut = new AddBillAppService(
            tataRekeningRepoMock.Object,
            trsBillingRepoMock.Object,
            createBillDomService);

        var radOrder = CreateRadOrder();
        var radOrderItem = radOrder.Items[0];
        var reg = CreateReg();
        var jaminan = JaminanType.Umum;
        var tarif = TarifType.Default with { TarifId = radOrderItem.TarifId, TarifName = radOrderItem.TarifName };
        var nilaiTarif = NilaiTarifType.Default with { Nilai = 100000m };

        tataRekeningRepoMock
            .Setup(x => x.LoadEntity(reg))
            .Returns(MayBe<TataRekeningModel>.None);

        // Act
        var bill = sut.FromRadOrderItem(
            radOrder, radOrderItem, reg, jaminan, tarif, nilaiTarif, []);

        // Assert
        bill.Should().NotBeNull();
        bill.TrsBillingId.Should().Be($"{radOrder.OrderId}-{radOrderItem.ItemNo}");
        tataRekeningRepoMock.Verify(x => x.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Once);
        trsBillingRepoMock.Verify(x => x.SaveChanges(bill), Times.Once);
    }
}
