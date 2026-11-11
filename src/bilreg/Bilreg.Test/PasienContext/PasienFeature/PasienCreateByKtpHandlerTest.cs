using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienCreateByKtpHandlerTest
{
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IKelurahanRepo> _kelurahanRepoMock = new();
    private readonly Mock<IPasienFactory> _pasienFactoryMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private PasienCreateByKtpHandler CreateHandler() =>
        new(
            _pasienRepoMock.Object,
            _pasienFactoryMock.Object,
            _kelurahanRepoMock.Object,
            _tglJamProviderMock.Object
        );

    private static KelurahanType CreateSampleKelurahan(string id = "KEL01", string name = "Kelurahan Melati") =>
        KelurahanType.Default with { KelurahanId = id, KelurahanName = name };

    private static PasienCreateByKtpCommand CreateSampleCommand() =>
        new(
            PasienName: "Ahmad Dahlan",
            Nik: "3201012304900001",
            AlamatKtp: "Jl. Merdeka No. 10",
            GolDarah: "O",
            Gender: "L",
            TempatLahir: "Bandung",
            TglLahir: "1990-04-23",
            Agama: "ISLAM",
            StatusKawin: "KAWIN",
            KotaKtp: "Bandung",
            KodePosKtp: "40111",
            Rt: "01",
            Rw: "02",
            KelurahanKtpId: "KEL01"
        );

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreatePasienWithCompletePersonInfoAndKtp()
    {
        // Arrange
        var command = CreateSampleCommand();
        var kelurahan = CreateSampleKelurahan("KEL01");
        _kelurahanRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelurahanKey>(k => k.KelurahanId == command.KelurahanKtpId)))
            .Returns(MayBe.From(kelurahan));

        var fixedNow = new DateTime(2026, 10, 14, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(fixedNow);

        _pasienFactoryMock
            .Setup(x => x.CreateFromPerson(
                It.IsAny<PersonInfoType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<GolDarahType>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .Returns((PersonInfoType person, string nick, string tempat, GolDarahType gol, string ibu, DateTime createdAt) =>
                new PasienModel(
                    "00100000001",
                    person,
                    nick,
                    tempat,
                    gol,
                    ibu,
                    KtpType.Default,
                    KelurahanType.Default,
                    IdentitasType.Default,
                    new List<ContactType>(),
                    PasienKeluargaType.Default,
                    AgamaType.Default,
                    SukuType.Default,
                    StatusKawinDkType.Default,
                    PendidikanDkType.Default,
                    PekerjaanDkType.Default,
                    createdAt,
                    true
                ));

        PasienModel? savedPasien = null;
        _pasienRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<PasienModel>()))
            .Callback<PasienModel>(p => savedPasien = p)
            .Returns(Result<IPasienKey>.Success(PasienModel.Key("00100000001")));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienId.Should().Be("00100000001");

        savedPasien.Should().NotBeNull();
        // Verifikasi PersonInfo tidak bernilai default (GAP-001, TD-001)
        savedPasien!.Person.Should().NotBe(PersonInfoType.Default);
        savedPasien.Person.PersonName.Should().Be("Ahmad Dahlan");
        savedPasien.Person.TglLahir.Should().Be(new DateOnly(1990, 4, 23));
        savedPasien.Person.Gender.Should().Be("L");
        savedPasien.Person.Identity.JenisId.Should().Be("KTP");
        savedPasien.Person.Identity.NomorId.Should().Be("3201012304900001");
        savedPasien.Person.Alamat.Kota.Should().Be("Bandung");
        savedPasien.Person.Alamat.KodePos.Should().Be("40111");

        // Verifikasi demografi & KTP
        savedPasien.TempatLahir.Should().Be("Bandung");
        savedPasien.GolDarah.ToString().Should().Be("O");
        savedPasien.Ktp.Nik.Should().Be("3201012304900001");
        savedPasien.Ktp.Rt.Should().Be("01");
        savedPasien.Ktp.Rw.Should().Be("02");
        savedPasien.Ktp.Kelurahan.KelurahanId.Should().Be("KEL01");
        savedPasien.Kelurahan.KelurahanId.Should().Be("KEL01");
        savedPasien.IsAktif.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidKelurahan_ShouldThrowArgumentException()
    {
        // Arrange
        var command = CreateSampleCommand();
        _kelurahanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKelurahanKey>()))
            .Returns(MayBe<KelurahanType>.None);

        _pasienFactoryMock
            .Setup(x => x.CreateFromPerson(
                It.IsAny<PersonInfoType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<GolDarahType>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()))
            .Returns((PersonInfoType person, string nick, string tempat, GolDarahType gol, string ibu, DateTime createdAt) =>
                new PasienModel("00100000001", person, nick, tempat, gol, ibu,
                    KtpType.Default, KelurahanType.Default, IdentitasType.Default,
                    new List<ContactType>(), PasienKeluargaType.Default,
                    AgamaType.Default, SukuType.Default, StatusKawinDkType.Default,
                    PendidikanDkType.Default, PekerjaanDkType.Default,
                    createdAt, true));

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid Kelurahan KTP*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("320101230490000A")]
    public async Task Handle_InvalidNik_ShouldThrowArgumentException(string invalidNik)
    {
        // Arrange
        var command = CreateSampleCommand() with { Nik = invalidNik };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*16 digit angka*");
    }

    [Fact]
    public async Task Handle_EmptyTglLahir_ShouldThrowArgumentException()
    {
        // Arrange
        var command = CreateSampleCommand() with { TglLahir = "" };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
