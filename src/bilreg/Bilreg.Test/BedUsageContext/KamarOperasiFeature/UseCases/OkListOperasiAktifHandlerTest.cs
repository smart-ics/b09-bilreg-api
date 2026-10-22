using System.Globalization;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkListOperasiAktifHandlerTest
{
    private readonly Mock<IOrderOpRepo> _orderOpRepo = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepo = new();

    private OkListOperasiAktifHandler CreateSut()
    {
        return new OkListOperasiAktifHandler(
            _orderOpRepo.Object,
            _opCaseRepo.Object);
    }

    [Fact]
    public async Task Handle_WhenNoActiveCases_ShouldReturnEmpty()
    {
        // Arrange
        _opCaseRepo.Setup(x => x.ListActiveOpCase()).Returns([]);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new OkListOperasiAktifQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenActiveCasesExist_WithOrderOpModel_ShouldReturnEnrichedProjections()
    {
        // Arrange
        var pasien = new PasienReff("PAS-001", "Tn. Budi", new DateOnly(1985, 5, 20), "L");
        var activeCase = new OpCaseOrderView(
            "ORD-001",
            pasien,
            "Apendektomi Laparoskopi",
            new PpaReff("DOC-ORD", "dr. Pengorder, Sp.B"),
            90,
            UrgencyLevelEnum.Cito,
            new DateTime(2026, 10, 20),
            OpCaseStateEnum.Scheduled);

        var reg = RegModel.Default;
        var layananPengorder = new LayananReff("LYN-001", "Poli Bedah");
        var dokterPengorder = new PpaReff("DOC-ORD", "dr. Pengorder, Sp.B");
        var orderOp = OrderOpModel.CreateByReg(reg, layananPengorder, dokterPengorder, "user-nurse", new DateTime(2026, 10, 16));
        orderOp.SetProsedur("Apendektomi Laparoskopi", "Eksplorasi Peritoneal", KlasifikasiOperasiEnum.Besar);
        orderOp.SetDokterOperator(new PpaReff("DOC-OPR", "dr. Operator Utama, Sp.B"));
        orderOp.SetUrgencyLevel(UrgencyLevelEnum.Cito);

        _opCaseRepo.Setup(x => x.ListActiveOpCase()).Returns([activeCase]);
        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));

        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new OkListOperasiAktifQuery(), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result[0];
        item.OrderOpId.Should().Be("ORD-001");
        item.PasienId.Should().Be("PAS-001");
        item.PasienName.Should().Be("Tn. Budi");
        item.DokterPengorderId.Should().Be("DOC-ORD");
        item.DokterPengorderName.Should().Be("dr. Pengorder, Sp.B");
        item.DokterOperatorId.Should().Be("DOC-OPR");
        item.DokterOperatorName.Should().Be("dr. Operator Utama, Sp.B");
        item.NamaOperasi.Should().Be("Apendektomi Laparoskopi");
        item.ProsedurTambahan.Should().Be("Eksplorasi Peritoneal");
        item.KlasifikasiOperasi.Should().Be(KlasifikasiOperasiEnum.Besar.ToString());
        item.PreferedDate.Should().Be(new DateTime(2026, 10, 20).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        item.UrgencyLevel.Should().Be(UrgencyLevelEnum.Cito.ToString());
        item.StatusOrderOp.Should().Be(OpCaseStateEnum.Scheduled.ToString());
        item.DokterId.Should().Be("DOC-OPR");
        item.DokterName.Should().Be("dr. Operator Utama, Sp.B");
    }

    [Fact]
    public async Task Handle_WhenActiveCasesExist_WithoutOrderOpModel_ShouldFallbackToActiveCaseData()
    {
        // Arrange
        var pasien = new PasienReff("PAS-002", "Ny. Siti", new DateOnly(1992, 8, 14), "P");
        var activeCase = new OpCaseOrderView(
            "ORD-002",
            pasien,
            "Sectio Caesarea",
            new PpaReff("DOC-OBGYN", "dr. Obgyn, Sp.OG"),
            60,
            UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 22),
            OpCaseStateEnum.Requested);

        _opCaseRepo.Setup(x => x.ListActiveOpCase()).Returns([activeCase]);
        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-002")))
            .Returns(MayBe<OrderOpModel>.None);

        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new OkListOperasiAktifQuery(), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result[0];
        item.OrderOpId.Should().Be("ORD-002");
        item.PasienId.Should().Be("PAS-002");
        item.PasienName.Should().Be("Ny. Siti");
        item.DokterPengorderId.Should().Be("DOC-OBGYN");
        item.DokterPengorderName.Should().Be("dr. Obgyn, Sp.OG");
        item.DokterOperatorId.Should().BeEmpty();
        item.DokterOperatorName.Should().BeEmpty();
        item.NamaOperasi.Should().Be("Sectio Caesarea");
        item.ProsedurTambahan.Should().BeEmpty();
        item.KlasifikasiOperasi.Should().Be(KlasifikasiOperasiEnum.Kecil.ToString());
        item.EstimasiDurasi.Should().Be(60);
        item.UrgencyLevel.Should().Be(UrgencyLevelEnum.Elective.ToString());
        item.StatusOrderOp.Should().Be(OpCaseStateEnum.Requested.ToString());
        item.DokterId.Should().Be("DOC-OBGYN");
        item.DokterName.Should().Be("dr. Obgyn, Sp.OG");
    }

    [Fact]
    public async Task Handle_WhenOrderOpDokterOperatorUpdated_ReflectsUpdatedDoctorInResponse()
    {
        // Arrange
        var pasien = new PasienReff("PAS-001", "Tn. Budi", new DateOnly(1985, 5, 20), "L");
        var activeCase = new OpCaseOrderView(
            "ORD-001",
            pasien,
            "Apendektomi Laparoskopi",
            new PpaReff("DR00083", "dr. Martahadinan, Sp.B"),
            90,
            UrgencyLevelEnum.Cito,
            new DateTime(2026, 10, 20),
            OpCaseStateEnum.Scheduled);

        var reg = RegModel.Default;
        var layananPengorder = new LayananReff("LYN-001", "Poli Bedah");
        var dokterPengorder = new PpaReff("DR00083", "dr. Martahadinan, Sp.B");
        var orderOp = OrderOpModel.CreateByReg(reg, layananPengorder, dokterPengorder, "user-nurse", new DateTime(2026, 10, 16));
        orderOp.SetProsedur("Apendektomi Laparoskopi", "Eksplorasi Peritoneal", KlasifikasiOperasiEnum.Besar);
        // Initial doctor DR00083
        orderOp.SetDokterOperator(new PpaReff("DR00083", "dr. Martahadinan, Sp.B"));

        // Mutation: Updated to DR00139 (dr. Arya Yunan) via AssignLeader sync
        orderOp.SetDokterOperator(new PpaReff("DR00139", "dr. Arya Yunan, Sp.B"));

        _opCaseRepo.Setup(x => x.ListActiveOpCase()).Returns([activeCase]);
        _orderOpRepo.Setup(x => x.LoadEntity(It.Is<IOrderOpKey>(k => k.OrderOpId == "ORD-001")))
            .Returns(MayBe.From(orderOp));

        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new OkListOperasiAktifQuery(), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result[0];
        item.DokterPengorderId.Should().Be("DR00083");
        item.DokterOperatorId.Should().Be("DR00139");
        item.DokterOperatorName.Should().Be("dr. Arya Yunan, Sp.B");
        item.DokterId.Should().Be("DR00139");
        item.DokterName.Should().Be("dr. Arya Yunan, Sp.B");
    }
}
