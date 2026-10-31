using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OpCaseRepoTest
{
    private readonly Mock<IOpCaseDal> _opCaseDalMock = new();
    private readonly Mock<IOpCaseStateHistDal> _opCaseStateHistDalMock = new();
    private readonly Mock<IOpCaseAktifDal> _opCaseAktifDalMock = new();
    private readonly Mock<IOpCasePpaDal> _opCasePpaDalMock = new();
    private readonly OpCaseRepo _sut;

    public OpCaseRepoTest()
    {
        _sut = new OpCaseRepo(
            _opCaseDalMock.Object,
            _opCaseStateHistDalMock.Object,
            _opCaseAktifDalMock.Object,
            _opCasePpaDalMock.Object);
    }

    [Fact]
    public void ListActiveOpCase_WithoutFilter_DelegatesToDalDefaultFilterAndMapsToView()
    {
        // Arrange
        var dtoList = new List<OpCaseAktifDto>
        {
            new OpCaseAktifDto(
                OrderOpId: "ORD-001",
                OrderOpDate: new DateTime(2026, 10, 1),
                PasienId: "PAS-001",
                PasienName: "Budi",
                DokterPengorderId: "DR-01",
                DokterPengorderName: "dr. Pengorder",
                DokterOperatorId: "DR-02",
                DokterOperatorName: "dr. Operator",
                NamaOperasi: "Apendektomi",
                ProsedurTambahan: "Eksplorasi",
                KlasifikasiOperasi: 2,
                PreferedDate: new DateTime(2026, 10, 5),
                EstimasiDurasi: 90,
                UrgencyLevel: 1,
                OpCaseState: 1,
                TglLahir: "1990-01-01",
                Gender: "L")
        };

        _opCaseAktifDalMock
            .Setup(x => x.ListData(It.IsAny<OpCaseAktifFilter>()))
            .Returns(dtoList);

        // Act
        var result = _sut.ListActiveOpCase().ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result[0];
        item.OrderOpId.Should().Be("ORD-001");
        item.Pasien.PasienId.Should().Be("PAS-001");
        item.Pasien.PasienName.Should().Be("Budi");
        item.DokterPengorderId.Should().Be("DR-01");
        item.DokterPengorderName.Should().Be("dr. Pengorder");
        item.DokterOperatorId.Should().Be("DR-02");
        item.DokterOperatorName.Should().Be("dr. Operator");
        item.Dokter.PpaId.Should().Be("DR-02");
        item.Dokter.PpaName.Should().Be("dr. Operator");
        item.NamaOperasi.Should().Be("Apendektomi");
        item.ProsedurTambahan.Should().Be("Eksplorasi");
        item.KlasifikasiOperasi.Should().Be(2);
        item.PreferedDate.Should().Be(new DateTime(2026, 10, 5));
        item.EstimasiDurasi.Should().Be(90);
        item.UrgencyLevel.Should().Be(UrgencyLevelEnum.Elective);
        item.OpCaseState.Should().Be(OpCaseStateEnum.Scheduled);

        _opCaseAktifDalMock.Verify(x => x.ListData(It.IsAny<OpCaseAktifFilter>()), Times.Once);
    }

    [Fact]
    public void ListActiveOpCase_WithFilter_PassesFilterToDalAndMapsCorrectly()
    {
        // Arrange
        var filter = new OpCaseAktifFilter(OpCaseState: 1, Limit: 10);
        var dtoList = new List<OpCaseAktifDto>
        {
            new OpCaseAktifDto(
                OrderOpId: "ORD-002",
                OrderOpDate: new DateTime(2026, 10, 2),
                PasienId: "PAS-002",
                PasienName: "Siti",
                DokterPengorderId: "DR-03",
                DokterPengorderName: "dr. Pengorder 2",
                DokterOperatorId: "",
                DokterOperatorName: "",
                NamaOperasi: "Hernia Repair",
                ProsedurTambahan: "",
                KlasifikasiOperasi: 1,
                PreferedDate: new DateTime(2026, 10, 6),
                EstimasiDurasi: 45,
                UrgencyLevel: 2,
                OpCaseState: 0,
                TglLahir: "1985-05-15",
                Gender: "P")
        };

        _opCaseAktifDalMock
            .Setup(x => x.ListData(filter))
            .Returns(dtoList);

        // Act
        var result = _sut.ListActiveOpCase(filter).ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result[0];
        item.OrderOpId.Should().Be("ORD-002");
        item.Pasien.PasienId.Should().Be("PAS-002");
        item.Dokter.PpaId.Should().Be("DR-03"); // fallback to pengorder when operator empty
        item.Dokter.PpaName.Should().Be("dr. Pengorder 2");
        item.UrgencyLevel.Should().Be(UrgencyLevelEnum.Cito);
        item.OpCaseState.Should().Be(OpCaseStateEnum.Requested);

        _opCaseAktifDalMock.Verify(x => x.ListData(filter), Times.Once);
    }
}
