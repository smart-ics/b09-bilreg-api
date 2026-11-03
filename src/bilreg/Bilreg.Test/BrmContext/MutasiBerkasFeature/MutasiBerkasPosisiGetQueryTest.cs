using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasPosisiGetQueryTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();

    private MutasiBerkasPosisiGetHandler CreateSut(bool includePasienRepo = true) =>
        includePasienRepo
            ? new MutasiBerkasPosisiGetHandler(_mutasiRepoMock.Object, _pasienRepoMock.Object)
            : new MutasiBerkasPosisiGetHandler(_mutasiRepoMock.Object);

    private static PasienModel CreateSamplePasien(
        string pasienId = "00-12-34-56",
        string name = "Budi Santoso",
        string gender = "L",
        DateOnly? dob = null)
    {
        return new PasienModel(
            pasienId: pasienId,
            person: new PersonInfoType(
                name,
                dob ?? new DateOnly(1990, 5, 20),
                gender,
                AlamatType.Default,
                new ContactType(JenisContactEnum.Mobile, "081234567890"),
                IdentitasType.Default
            ),
            nickName: name,
            tempatLahir: "Surabaya",
            golDarah: GolDarahType.Default,
            namaIbuKandung: "Siti Aminah",
            ktp: KtpType.Default,
            kelurahan: KelurahanType.Default,
            kartuKeluarga: IdentitasType.Default,
            listContact: new List<ContactType>(),
            keluarga: PasienKeluargaType.Default,
            agama: AgamaType.Default,
            suku: SukuType.Default,
            statusKawinDk: StatusKawinDkType.Default,
            pendidikanDk: PendidikanDkType.Default,
            pekerjaanDk: PekerjaanDkType.Default,
            tglMedRec: DateTime.Now,
            isAktif: true
        );
    }

    private static MutasiBerkasModel CreateSampleMutasi(string mrId = "00-12-34-56")
    {
        var jadwal = new JadwalPelayananType("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");
        var volume = VolumeBerkasType.CreatePinjam(2, 3);
        return MutasiBerkasModel.CreateKirim(
            mutasiBerkasId: "MK26100001",
            trsBuatId: "RG2610170001",
            pasienId: mrId,
            tglMutasi: new DateOnly(2026, 10, 17),
            jamMutasi: new TimeOnly(8, 0, 0),
            petugasId: "USR_FILING",
            jadwalPelayanan: jadwal,
            volume: volume
        );
    }

    [Fact]
    public async Task Posisi_Found_WithPatientDemographics_ReturnsEnrichedResponse()
    {
        // Arrange
        const string mrId = "00-12-34-56";
        var model = CreateSampleMutasi(mrId);
        var pasien = CreateSamplePasien(mrId, "Siti Rahmah", "P", new DateOnly(1985, 8, 15));

        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe.From(model));

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == mrId)))
            .Returns(MayBe.From(pasien));

        var sut = CreateSut(includePasienRepo: true);

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

        // Enriched demographics assertions (DEF-004, DEC-012)
        response.PasienName.Should().Be("Siti Rahmah");
        response.JenisKelamin.Should().Be("P");
        response.TglLahir.Should().Be("1985-08-15");
    }

    [Fact]
    public async Task Posisi_Found_WithoutPasienRepo_ReturnsResponseWithEmptyDemographics()
    {
        // Arrange
        const string mrId = "00-12-34-56";
        var model = CreateSampleMutasi(mrId);

        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe.From(model));

        var sut = CreateSut(includePasienRepo: false);

        // Act
        var response = await sut.Handle(new MutasiBerkasPosisiGetQuery(mrId), CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.MutasiId.Should().Be("MK26100001");
        response.PasienName.Should().BeEmpty();
        response.JenisKelamin.Should().BeEmpty();
        response.TglLahir.Should().BeEmpty();
    }

    [Fact]
    public async Task Posisi_Found_WhenPasienNotFoundInPasienRepo_ReturnsResponseWithEmptyDemographics()
    {
        // Arrange
        const string mrId = "00-12-34-56";
        var model = CreateSampleMutasi(mrId);

        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe.From(model));

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == mrId)))
            .Returns(MayBe<PasienModel>.None);

        var sut = CreateSut(includePasienRepo: true);

        // Act
        var response = await sut.Handle(new MutasiBerkasPosisiGetQuery(mrId), CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.MutasiId.Should().Be("MK26100001");
        response.PasienName.Should().BeEmpty();
        response.JenisKelamin.Should().BeEmpty();
        response.TglLahir.Should().BeEmpty();
    }

    [Fact]
    public async Task Posisi_WhenNoActiveMutasi_AndPasienRegistered_ReturnsDefaultFilingPositionWithDemographics()
    {
        // Arrange
        const string mrId = "00-12-34-56";
        var pasien = CreateSamplePasien(mrId, "Budi Santoso", "L", new DateOnly(1990, 5, 20));

        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe<MutasiBerkasModel>.None);

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == mrId)))
            .Returns(MayBe.From(pasien));

        var sut = CreateSut(includePasienRepo: true);

        // Act
        var response = await sut.Handle(new MutasiBerkasPosisiGetQuery(mrId), CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.MutasiId.Should().BeEmpty();
        response.TrsBuatId.Should().BeEmpty();
        response.MrId.Should().Be(mrId);
        response.LayananId.Should().Be("FILING");
        response.DokterId.Should().BeEmpty();
        response.PetugasId.Should().BeEmpty();
        response.Status.Should().Be(3);
        response.StatusDescription.Should().Be("Tersimpan di Ruang Filing RM");
        response.QtyPinjamRi.Should().Be(0);
        response.QtyKembaliRi.Should().Be(0);
        response.QtyPinjamRj.Should().Be(0);
        response.QtyKembaliRj.Should().Be(0);
        response.TotalPinjam.Should().Be(0);
        response.TotalKembali.Should().Be(0);
        response.SisaPinjam.Should().Be(0);
        response.TglMutasi.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        response.IsFound.Should().BeTrue();
        response.PasienName.Should().Be("Budi Santoso");
        response.JenisKelamin.Should().Be("L");
        response.TglLahir.Should().Be("1990-05-20");
    }

    [Fact]
    public async Task Posisi_WhenNoActiveMutasi_AndPasienNotRegistered_ThrowsKeyNotFoundException()
    {
        // Arrange
        const string mrId = "00-99-99-99";
        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe<MutasiBerkasModel>.None);

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == mrId)))
            .Returns(MayBe<PasienModel>.None);

        var sut = CreateSut(includePasienRepo: true);

        // Act
        var act = () => sut.Handle(new MutasiBerkasPosisiGetQuery(mrId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Data pasien dengan No RM '{mrId}' tidak ditemukan.");
    }

    [Fact]
    public async Task Posisi_WhenNoActiveMutasi_AndPasienRepoNull_ThrowsKeyNotFoundException()
    {
        // Arrange
        const string mrId = "00-99-99-99";
        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe<MutasiBerkasModel>.None);

        var sut = CreateSut(includePasienRepo: false);

        // Act
        var act = () => sut.Handle(new MutasiBerkasPosisiGetQuery(mrId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Data pasien dengan No RM '{mrId}' tidak ditemukan.");
    }

    [Fact]
    public async Task Posisi_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        const string mrId = "00-99-99-99";
        _mutasiRepoMock
            .Setup(x => x.GetLatestActiveByMr(mrId))
            .Returns(MayBe<MutasiBerkasModel>.None);

        var sut = CreateSut();

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
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(new MutasiBerkasPosisiGetQuery(invalidMrId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void PosisiResponse_EmptyFactory_ProducesDefaultValues_IncludingEmptyDemographics()
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
        empty.PasienName.Should().BeEmpty();
        empty.JenisKelamin.Should().BeEmpty();
        empty.TglLahir.Should().BeEmpty();
    }

    [Fact]
    public void MutasiBerkasPosisiGetResponse_Subclass_InstantiatesCorrectly()
    {
        // Act
        var res = new MutasiBerkasPosisiGetResponse(
            MutasiId: "MK26100001",
            TrsBuatId: "RG2610170001",
            MrId: "00-12-34-56",
            LayananId: "POL01",
            DokterId: "DR001",
            PetugasId: "USR_FILING",
            Status: 1,
            StatusDescription: "Dispatched",
            QtyPinjamRi: 0,
            QtyKembaliRi: 0,
            QtyPinjamRj: 1,
            QtyKembaliRj: 0,
            TotalPinjam: 1,
            TotalKembali: 0,
            SisaPinjam: 1,
            TglMutasi: new DateOnly(2026, 10, 17),
            JamMutasi: new TimeOnly(8, 0, 0),
            IsFound: true,
            PasienName: "Budi Santoso",
            JenisKelamin: "L",
            TglLahir: "1990-05-20"
        );

        // Assert
        res.PasienName.Should().Be("Budi Santoso");
        res.JenisKelamin.Should().Be("L");
        res.TglLahir.Should().Be("1990-05-20");
        res.Should().BeAssignableTo<MutasiBerkasPosisiResponse>();
    }
}
