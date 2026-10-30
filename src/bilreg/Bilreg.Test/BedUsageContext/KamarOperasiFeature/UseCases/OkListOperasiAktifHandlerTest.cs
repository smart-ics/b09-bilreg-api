using System.Globalization;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkListOperasiAktifHandlerTest
{
    private readonly Mock<IOpCaseRepo> _opCaseRepo = new();

    private OkListOperasiAktifHandler CreateSut()
    {
        return new OkListOperasiAktifHandler(_opCaseRepo.Object);
    }

    [Fact]
    public async Task Handle_WhenNoActiveCases_ShouldReturnEmpty()
    {
        // Arrange
        _opCaseRepo.Setup(x => x.ListActiveOpCase(It.IsAny<OpCaseAktifFilter>())).Returns([]);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new OkListOperasiAktifQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenActiveCasesExist_ShouldMapAllFieldsDirectlyFromActiveCase()
    {
        // Arrange
        var pasien = new PasienReff("PAS-001", "Tn. Budi", new DateOnly(1985, 5, 20), "L");
        var activeCase = new OpCaseOrderView(
            OrderOpId: "ORD-001",
            Pasien: pasien,
            NamaOperasi: "Apendektomi Laparoskopi",
            Dokter: new PpaReff("DOC-OPR", "dr. Operator Utama, Sp.B"),
            EstimasiDurasi: 90,
            UrgencyLevel: UrgencyLevelEnum.Cito,
            PreferedDate: new DateTime(2026, 10, 20),
            OpCaseState: OpCaseStateEnum.Scheduled,
            DokterPengorderId: "DOC-ORD",
            DokterPengorderName: "dr. Pengorder, Sp.B",
            DokterOperatorId: "DOC-OPR",
            DokterOperatorName: "dr. Operator Utama, Sp.B",
            ProsedurTambahan: "Eksplorasi Peritoneal",
            KlasifikasiOperasi: (int)KlasifikasiOperasiEnum.Besar);

        _opCaseRepo.Setup(x => x.ListActiveOpCase(It.IsAny<OpCaseAktifFilter>()))
            .Returns([activeCase]);

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
        item.EstimasiDurasi.Should().Be(90);
        item.UrgencyLevel.Should().Be(UrgencyLevelEnum.Cito.ToString());
        item.StatusOrderOp.Should().Be(OpCaseStateEnum.Scheduled.ToString());
        item.DokterId.Should().Be("DOC-OPR");
        item.DokterName.Should().Be("dr. Operator Utama, Sp.B");
    }

    [Fact]
    public async Task Handle_WhenFilterProvided_ShouldPassParsedFilterToRepo()
    {
        // Arrange
        OpCaseAktifFilter? capturedFilter = null;
        _opCaseRepo.Setup(x => x.ListActiveOpCase(It.IsAny<OpCaseAktifFilter>()))
            .Callback<OpCaseAktifFilter>(f => capturedFilter = f)
            .Returns([]);

        var sut = CreateSut();
        var query = new OkListOperasiAktifQuery(
            Status: "Requested",
            TglAwal: "2026-10-01",
            TglAkhir: "2026-10-31",
            Limit: 50);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        capturedFilter.Should().NotBeNull();
        capturedFilter!.OpCaseState.Should().Be((int)OpCaseStateEnum.Requested);
        capturedFilter.TglAwal.Should().Be(new DateTime(2026, 10, 1));
        capturedFilter.TglAkhir.Should().Be(new DateTime(2026, 10, 31));
        capturedFilter.Limit.Should().Be(50);
    }

    [Theory]
    [InlineData("Requested", OpCaseStateEnum.Requested)]
    [InlineData("1", OpCaseStateEnum.Requested)]
    [InlineData("Scheduled", OpCaseStateEnum.Scheduled)]
    [InlineData("2", OpCaseStateEnum.Scheduled)]
    public async Task Handle_WhenStatusVariantProvided_ResolvesCorrectOpCaseState(string statusInput, OpCaseStateEnum expectedState)
    {
        // Arrange
        OpCaseAktifFilter? capturedFilter = null;
        _opCaseRepo.Setup(x => x.ListActiveOpCase(It.IsAny<OpCaseAktifFilter>()))
            .Callback<OpCaseAktifFilter>(f => capturedFilter = f)
            .Returns([]);

        var sut = CreateSut();
        var query = new OkListOperasiAktifQuery(Status: statusInput);

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert
        capturedFilter.Should().NotBeNull();
        capturedFilter!.OpCaseState.Should().Be((int)expectedState);
    }

    [Fact]
    public async Task Handle_WhenStatusEmptyOrNull_ShouldPassNullOpCaseState()
    {
        // Arrange
        OpCaseAktifFilter? capturedFilter = null;
        _opCaseRepo.Setup(x => x.ListActiveOpCase(It.IsAny<OpCaseAktifFilter>()))
            .Callback<OpCaseAktifFilter>(f => capturedFilter = f)
            .Returns([]);

        var sut = CreateSut();
        var query = new OkListOperasiAktifQuery(Status: "");

        // Act
        await sut.Handle(query, CancellationToken.None);

        // Assert
        capturedFilter.Should().NotBeNull();
        capturedFilter!.OpCaseState.Should().BeNull();
    }
}
