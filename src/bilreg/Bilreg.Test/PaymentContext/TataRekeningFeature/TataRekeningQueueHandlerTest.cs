using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

public class TataRekeningQueueHandlerTest
{
    private readonly Mock<IBilrgTataRekeningDal> _dalMock = new();
    private readonly TataRekeningQueueHandler _sut;

    public TataRekeningQueueHandlerTest()
    {
        _sut = new TataRekeningQueueHandler(_dalMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDalReturnsList_MapsAllFieldsAndJenisRegStringAccurately()
    {
        // Arrange
        var sampleData = new List<TataRekeningQueueDto>
        {
            new(
                RegId: "REG-01",
                RegDate: new DateTime(2026, 11, 18, 9, 30, 0),
                PasienId: "MR-01",
                JenisReg: "0",
                LayananId: "LYN-01",
                DokterId: "DR-01",
                TipeJaminanId: "JAM-01",
                PasienName: "Pasien Satu",
                TglLahir: "1990-01-15",
                Gender: "L",
                LayananName: "Poli Umum",
                DokterName: "dr. Dokter 1",
                TipeJaminanName: "Umum",
                StatusTataRekening: 0),
            new(
                RegId: "REG-02",
                RegDate: new DateTime(2026, 11, 18, 10, 0, 0),
                PasienId: "MR-02",
                JenisReg: "1",
                LayananId: "LYN-02",
                DokterId: "DR-02",
                TipeJaminanId: "JAM-02",
                PasienName: "Pasien Dua",
                TglLahir: "1985-05-20",
                Gender: "P",
                LayananName: "Bangsal Melati",
                DokterName: "dr. Dokter 2",
                TipeJaminanName: "BPJS Kesehatan",
                StatusTataRekening: 1),
            new(
                RegId: "REG-03",
                RegDate: new DateTime(2026, 11, 18, 11, 15, 0),
                PasienId: "MR-03",
                JenisReg: "3",
                LayananId: "LYN-03",
                DokterId: "DR-03",
                TipeJaminanId: "JAM-03",
                PasienName: "Pasien Tiga",
                TglLahir: "2000-12-01",
                Gender: "L",
                LayananName: "IGD Emergency",
                DokterName: "dr. Dokter 3",
                TipeJaminanName: "Asuransi Swasta",
                StatusTataRekening: 2),
            new(
                RegId: "REG-04",
                RegDate: new DateTime(2026, 11, 18, 12, 0, 0),
                PasienId: "MR-04",
                JenisReg: "99",
                LayananId: "LYN-04",
                DokterId: "DR-04",
                TipeJaminanId: "JAM-04",
                PasienName: "Pasien Empat",
                TglLahir: "",
                Gender: "-",
                LayananName: "Layanan Lain",
                DokterName: "dr. Dokter 4",
                TipeJaminanName: "Lainnya",
                StatusTataRekening: 3)
        };

        _dalMock.Setup(x => x.ListDataQueue(null))
            .Returns(sampleData);

        var query = new TataRekeningQueueQuery();

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(4);

        // Rajal ("0")
        var item1 = result[0];
        item1.RegId.Should().Be("REG-01");
        item1.RegDate.Should().Be("2026-11-18");
        item1.PasienId.Should().Be("MR-01");
        item1.PasienName.Should().Be("Pasien Satu");
        item1.TglLahir.Should().Be("1990-01-15");
        item1.Gender.Should().Be("L");
        item1.JenisReg.Should().Be("0");
        item1.JenisRegString.Should().Be("Rawat Jalan");
        item1.LayananId.Should().Be("LYN-01");
        item1.LayananName.Should().Be("Poli Umum");
        item1.DokterId.Should().Be("DR-01");
        item1.DokterName.Should().Be("dr. Dokter 1");
        item1.TipeJaminanId.Should().Be("JAM-01");
        item1.TipeJaminanName.Should().Be("Umum");
        item1.StatusTataRekening.Should().Be(0);

        // Ranap ("1")
        var item2 = result[1];
        item2.RegId.Should().Be("REG-02");
        item2.JenisReg.Should().Be("1");
        item2.JenisRegString.Should().Be("Rawat Inap");
        item2.StatusTataRekening.Should().Be(1);

        // Darurat ("3")
        var item3 = result[2];
        item3.RegId.Should().Be("REG-03");
        item3.JenisReg.Should().Be("3");
        item3.JenisRegString.Should().Be("Rawat Darurat");
        item3.StatusTataRekening.Should().Be(2);

        // Unknown ("99")
        var item4 = result[3];
        item4.RegId.Should().Be("REG-04");
        item4.JenisReg.Should().Be("99");
        item4.JenisRegString.Should().Be("-");
        item4.StatusTataRekening.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenDalReturnsEmptyList_ReturnsEmptyCollection()
    {
        // Arrange
        _dalMock.Setup(x => x.ListDataQueue(null))
            .Returns(Enumerable.Empty<TataRekeningQueueDto>());

        var query = new TataRekeningQueueQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenDalReturnsNull_ReturnsEmptyCollection()
    {
        // Arrange
        _dalMock.Setup(x => x.ListDataQueue(It.IsAny<int?>()))
            .Returns((IEnumerable<TataRekeningQueueDto>)null!);

        var query = new TataRekeningQueueQuery(1);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenJenisRegSpecified_PassesParameterCorrectlyToDal()
    {
        // Arrange
        _dalMock.Setup(x => x.ListDataQueue(0))
            .Returns(new List<TataRekeningQueueDto>
            {
                new(
                    RegId: "REG-RJL",
                    RegDate: new DateTime(2026, 11, 18),
                    PasienId: "MR-01",
                    JenisReg: "0",
                    LayananId: "LYN-01",
                    DokterId: "DR-01",
                    TipeJaminanId: "JAM-01",
                    PasienName: "Pasien Rajal",
                    TglLahir: "1990-01-01",
                    Gender: "L",
                    LayananName: "Poli",
                    DokterName: "dr. Dokter",
                    TipeJaminanName: "Umum",
                    StatusTataRekening: 0)
            });

        var query = new TataRekeningQueueQuery(0);

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        _dalMock.Verify(x => x.ListDataQueue(0), Times.Once);
        _dalMock.Verify(x => x.ListDataQueue(It.IsNotIn(0)), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTglLahirContainsTimestamp_FormatsToIsoDateString()
    {
        // Arrange
        var sample = new List<TataRekeningQueueDto>
        {
            new(
                RegId: "REG-TIME-01",
                RegDate: new DateTime(2026, 11, 18, 14, 0, 0),
                PasienId: "MR-TIME",
                JenisReg: "0",
                LayananId: "LYN-01",
                DokterId: "DR-01",
                TipeJaminanId: "JAM-01",
                PasienName: "Pasien Waktu",
                TglLahir: "1988-07-23 00:00:00.000",
                Gender: "P",
                LayananName: "Poli",
                DokterName: "dr. Dokter",
                TipeJaminanName: "Umum",
                StatusTataRekening: 0)
        };

        _dalMock.Setup(x => x.ListDataQueue(null))
            .Returns(sample);

        // Act
        var result = (await _sut.Handle(new TataRekeningQueueQuery(), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TglLahir.Should().Be("1988-07-23");
    }
}
