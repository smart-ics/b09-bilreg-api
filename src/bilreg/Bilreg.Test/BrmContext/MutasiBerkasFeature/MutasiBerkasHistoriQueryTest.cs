using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasHistoriQueryTest
{
    private readonly Mock<IMutasiBerkasWorklistDal> _worklistDalMock = new();
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();

    private MutasiBerkasHistoriHandler CreateHistoriSut() =>
        new(_worklistDalMock.Object);

    private MutasiBerkasPosisiGetHandler CreatePosisiSut() =>
        new(_mutasiRepoMock.Object);

    [Fact]
    public async Task Histori_ValidMrId_ReturnsItemsFromDal()
    {
        // Arrange
        var mrId = "00-12-34-56";
        var expectedHistori = new List<MutasiBerkasHistoriItemDto>
        {
            new()
            {
                MutasiId = "MM26100001",
                TrsBuatId = "RG2610170001",
                MrId = mrId,
                TglMutasi = "2026-10-17",
                JamMutasi = "11:00:00",
                DokterId = "DR001",
                DokterName = "dr. Andi",
                LayananId = "POL01",
                LayananName = "Poli Penyakit Dalam",
                TglPraktek = "2026-10-17",
                JamPraktek = "08:00",
                SesiPoliId = "1",
                Status = 3,
                PetugasId = "USR_RM",
                FrekuensiCetak = 1,
                PetugasCetak = "USR_FILING",
                TglCetak = "2026-10-17",
                JamCetak = "08:05:00",
                IsVoid = false
            },
            new()
            {
                MutasiId = "MK26100001",
                TrsBuatId = "RG2610170001",
                MrId = mrId,
                TglMutasi = "2026-10-17",
                JamMutasi = "08:00:00",
                DokterId = "DR001",
                DokterName = "dr. Andi",
                LayananId = "POL01",
                LayananName = "Poli Penyakit Dalam",
                TglPraktek = "2026-10-17",
                JamPraktek = "08:00",
                SesiPoliId = "1",
                Status = 1,
                PetugasId = "USR_FILING",
                FrekuensiCetak = 1,
                PetugasCetak = "USR_FILING",
                TglCetak = "2026-10-17",
                JamCetak = "08:05:00",
                IsVoid = false
            }
        };

        _worklistDalMock
            .Setup(x => x.QueryHistori(mrId))
            .Returns(expectedHistori);

        var sut = CreateHistoriSut();

        // Act
        var result = (await sut.Handle(new MutasiBerkasHistoriQuery(mrId), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].MutasiId.Should().Be("MM26100001");
        result[0].Status.Should().Be(3);
        result[1].MutasiId.Should().Be("MK26100001");
        result[1].Status.Should().Be(1);
        _worklistDalMock.Verify(x => x.QueryHistori(mrId), Times.Once);
    }

    [Fact]
    public async Task Histori_EmptyResults_ReturnsEmptyCollection()
    {
        // Arrange
        var mrId = "00-99-99-99";
        _worklistDalMock
            .Setup(x => x.QueryHistori(mrId))
            .Returns(Enumerable.Empty<MutasiBerkasHistoriItemDto>());

        var sut = CreateHistoriSut();

        // Act
        var result = await sut.Handle(new MutasiBerkasHistoriQuery(mrId), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Histori_InvalidMrId_ThrowsArgumentException(string invalidMrId)
    {
        // Arrange
        var sut = CreateHistoriSut();

        // Act
        var act = () => sut.Handle(new MutasiBerkasHistoriQuery(invalidMrId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Posisi_Found_ReturnsMutasiBerkasPosisiResponse()
    {
        // Arrange
        var mrId = "00-12-34-56";
        var jadwal = new JadwalPelayananType("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");
        var volume = VolumeBerkasType.CreatePinjam(2, 3);
        var model = MutasiBerkasModel.CreateKirim(
            mutasiBerkasId: "MK26100001",
            trsBuatId: "RG2610170001",
            pasienId: mrId,
            tglMutasi: new DateOnly(2026, 10, 17),
            jamMutasi: new TimeOnly(8, 0, 0),
            petugasId: "USR_FILING",
            jadwalPelayanan: jadwal,
            volume: volume
        );

        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe.From(model));

        var sut = CreatePosisiSut();

        // Act
        var response = await sut.Handle(new MutasiBerkasPosisiGetQuery(mrId), CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.MutasiId.Should().Be("MK26100001");
        response.TrsBuatId.Should().Be("RG2610170001");
        response.MrId.Should().Be(mrId);
        response.LayananId.Should().Be("POL01");
        response.DokterId.Should().Be("DR001");
        response.PetugasId.Should().Be("USR_FILING");
        response.Status.Should().Be(1);
        response.StatusDescription.Should().Be("Dispatched");
        response.QtyPinjamRi.Should().Be(2);
        response.QtyPinjamRj.Should().Be(3);
        response.TotalPinjam.Should().Be(5);
        response.TotalKembali.Should().Be(0);
        response.SisaPinjam.Should().Be(5);
        response.TglMutasi.Should().Be(new DateOnly(2026, 10, 17));
        response.JamMutasi.Should().Be(new TimeOnly(8, 0, 0));
        response.IsFound.Should().BeTrue();
    }

    [Fact]
    public async Task Posisi_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var mrId = "00-99-99-99";
        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe<MutasiBerkasModel>.None);

        var sut = CreatePosisiSut();

        // Act
        var act = () => sut.Handle(new MutasiBerkasPosisiGetQuery(mrId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*'{mrId}'*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Posisi_InvalidMrId_ThrowsArgumentException(string invalidMrId)
    {
        // Arrange
        var sut = CreatePosisiSut();

        // Act
        var act = () => sut.Handle(new MutasiBerkasPosisiGetQuery(invalidMrId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PosisiResponse_EmptyFactory_ProducesDefaultValues()
    {
        // Act
        var empty = MutasiBerkasPosisiResponse.Empty("00-11-22-33");

        // Assert
        empty.MrId.Should().Be("00-11-22-33");
        empty.IsFound.Should().BeFalse();
        empty.MutasiId.Should().BeEmpty();
        empty.LayananId.Should().BeEmpty();
        empty.Status.Should().Be(0);
        empty.StatusDescription.Should().Be("Tidak Ditemukan");
        empty.TotalPinjam.Should().Be(0);
        empty.TotalKembali.Should().Be(0);
        empty.SisaPinjam.Should().Be(0);
    }
}
