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

public class PasienCreateHandlerTest
{
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IPasienFactory> _pasienFactoryMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private PasienCreateHandler CreateHandler() =>
        new(
            _pasienRepoMock.Object,
            _pasienFactoryMock.Object,
            _tglJamProviderMock.Object
        );

    private static PasienCreateCommand CreateSampleCommand() =>
        new(
            PasienName: "Siti Nurhaliza",
            TempatLahir: "Jakarta",
            TglLahir: "1995-08-17",
            Gender: "P",
            NickName: "Siti",
            IbuKandung: "Fatimah",
            GolDarah: "A",
            Alamat1: "Jl. Thamrin No. 1",
            Alamat2: "Kav. 5",
            Alamat3: "Blok B",
            Kota: "Jakarta Pusat",
            KodePos: "10350",
            NoTelp: "08123456789",
            NoKtp: "3171015708950001"
        );

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreatePasienWithCompletePersonInfoAndKtp()
    {
        // Arrange
        var command = CreateSampleCommand();

        _pasienRepoMock
            .Setup(x => x.GetDataByNik(command.NoKtp))
            .Returns(MayBe<PasienPersonView>.None);

        _pasienRepoMock
            .Setup(x => x.SearchPasien(It.IsAny<string>()))
            .Returns(Enumerable.Empty<PasienPersonView>());

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
                    "00100000002",
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
            .Returns(Result<IPasienKey>.Success(PasienModel.Key("00100000002")));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienId.Should().Be("00100000002");

        savedPasien.Should().NotBeNull();

        // Verifikasi PersonInfo tidak bernilai default
        savedPasien!.Person.Should().NotBe(PersonInfoType.Default);
        savedPasien.Person.PersonName.Should().Be("Siti Nurhaliza");
        savedPasien.Person.TglLahir.Should().Be(new DateOnly(1995, 8, 17));
        savedPasien.Person.Gender.Should().Be("P");
        savedPasien.Person.Contact.ContactDetail.Should().Be("08123456789");
        savedPasien.Person.Identity.JenisId.Should().Be("KTP");
        savedPasien.Person.Identity.NomorId.Should().Be("3171015708950001");
        savedPasien.Person.Identity.NomorId.Should().NotBe("-");

        // Verifikasi KTP tersimpan utuh dan tidak default (GAP-003, TD-003)
        savedPasien.Ktp.Should().NotBe(KtpType.Default);
        savedPasien.Ktp.Nik.Should().Be("3171015708950001");
        savedPasien.Ktp.Nik.Should().NotBe("-");
        savedPasien.Ktp.Alamat.Alamat.Should().Equal("Jl. Thamrin No. 1", "Kav. 5", "Blok B");
        savedPasien.Ktp.Alamat.Kota.Should().Be("Jakarta Pusat");
        savedPasien.Ktp.Alamat.KodePos.Should().Be("10350");

        // Verifikasi atribut demografi lainnya
        savedPasien.TempatLahir.Should().Be("Jakarta");
        savedPasien.NickName.Should().Be("Siti");
        savedPasien.NamaIbuKandung.Should().Be("Fatimah");
        savedPasien.GolDarah.ToString().Should().Be("A");
        savedPasien.IsAktif.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateNik_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = CreateSampleCommand();
        var existingPerson = new PersonInfoType("Existing Patient", new DateOnly(1995, 8, 17), "P",
            AlamatType.Default, ContactType.Default, IdentitasType.Ktp(command.NoKtp));
        var existingView = new PasienPersonView("00100000001", true, existingPerson);

        _pasienRepoMock
            .Setup(x => x.GetDataByNik(command.NoKtp))
            .Returns(MayBe.From(existingView));

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Patient with the supplied NIK already exists*");
    }

    [Fact]
    public async Task Handle_DuplicateDemographicIdentity_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = CreateSampleCommand();
        var matchingPerson = new PersonInfoType(command.PasienName, DateOnly.Parse(command.TglLahir), command.Gender,
            AlamatType.Default, new ContactType(JenisContactEnum.Phone, command.NoTelp), IdentitasType.Ktp(command.NoKtp));
        var matchingView = new PasienPersonView("00100000001", true, matchingPerson);

        _pasienRepoMock
            .Setup(x => x.GetDataByNik(command.NoKtp))
            .Returns(MayBe<PasienPersonView>.None);

        _pasienRepoMock
            .Setup(x => x.SearchPasien($"{command.PasienName} {command.TglLahir}"))
            .Returns(new[] { matchingView });

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Patient with matching identity already exists*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("317101570895000A")]
    public async Task Handle_InvalidNoKtp_ShouldThrowArgumentException(string invalidNoKtp)
    {
        // Arrange
        var command = CreateSampleCommand() with { NoKtp = invalidNoKtp };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*NoKtp harus 16 digit angka*");
    }

    [Fact]
    public async Task Handle_InvalidTglLahirFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var command = CreateSampleCommand() with { TglLahir = "17/08/1995" };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_ChildUnder17WithoutNik_ShouldSucceed(string emptyNik)
    {
        // Arrange
        var fixedNow = new DateTime(2026, 10, 14, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(fixedNow);

        // Pasien anak usia 11 tahun (lahir 2015-05-20)
        var command = CreateSampleCommand() with
        {
            PasienName = "Ananda Pratama",
            TglLahir = "2015-05-20",
            NoKtp = emptyNik
        };

        _pasienRepoMock
            .Setup(x => x.SearchPasien(It.IsAny<string>()))
            .Returns(Enumerable.Empty<PasienPersonView>());

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
                    "00100000003",
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
            .Returns(Result<IPasienKey>.Success(PasienModel.Key("00100000003")));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienId.Should().Be("00100000003");

        savedPasien.Should().NotBeNull();
        savedPasien!.Person.Identity.Should().Be(IdentitasType.Default);
        savedPasien.Ktp.Nik.Should().Be("-");
        savedPasien.Ktp.Should().BeEquivalentTo(KtpType.Default);

        // Pastikan bypass pengecekan GetDataByNik saat NoKtp kosong
        _pasienRepoMock.Verify(x => x.GetDataByNik(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ChildUnder17WithValidNik_ShouldSucceed()
    {
        // Arrange
        var fixedNow = new DateTime(2026, 10, 14, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(fixedNow);

        // Pasien anak usia 11 tahun memiliki NIK (misal dari KIA/KK)
        var command = CreateSampleCommand() with
        {
            PasienName = "Ananda Pratama",
            TglLahir = "2015-05-20",
            NoKtp = "3171015708150001"
        };

        _pasienRepoMock
            .Setup(x => x.GetDataByNik(command.NoKtp))
            .Returns(MayBe<PasienPersonView>.None);

        _pasienRepoMock
            .Setup(x => x.SearchPasien(It.IsAny<string>()))
            .Returns(Enumerable.Empty<PasienPersonView>());

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
                    "00100000004",
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
            .Returns(Result<IPasienKey>.Success(PasienModel.Key("00100000004")));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienId.Should().Be("00100000004");

        savedPasien.Should().NotBeNull();
        savedPasien!.Person.Identity.NomorId.Should().Be("3171015708150001");
        savedPasien.Ktp.Nik.Should().Be("3171015708150001");

        _pasienRepoMock.Verify(x => x.GetDataByNik(command.NoKtp), Times.Once);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("317101570815000A")]
    public async Task Handle_ChildUnder17WithInvalidNik_ShouldThrowArgumentException(string invalidNik)
    {
        // Arrange
        var fixedNow = new DateTime(2026, 10, 14, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(fixedNow);

        var command = CreateSampleCommand() with
        {
            TglLahir = "2015-05-20",
            NoKtp = invalidNik
        };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*NoKtp harus 16 digit angka*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_Adult17OrOlderWithoutNik_ShouldThrowArgumentException(string emptyNik)
    {
        // Arrange
        var fixedNow = new DateTime(2026, 10, 14, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(fixedNow);

        // Pasien dewasa usia 31 tahun (lahir 1995-08-17)
        var command = CreateSampleCommand() with
        {
            TglLahir = "1995-08-17",
            NoKtp = emptyNik
        };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Pasien berusia 17 tahun ke atas wajib memiliki NoKtp*");
    }

    [Fact]
    public async Task Handle_BoundaryAge_Exact17WithoutNik_ShouldThrowArgumentException()
    {
        // Arrange: reference date = 2026-10-14, lahir 2009-10-14 (tepat 17 tahun)
        var fixedNow = new DateTime(2026, 10, 14, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(fixedNow);

        var command = CreateSampleCommand() with
        {
            TglLahir = "2009-10-14",
            NoKtp = ""
        };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Pasien berusia 17 tahun ke atas wajib memiliki NoKtp*");
    }

    [Fact]
    public async Task Handle_BoundaryAge_OneDayUnder17WithoutNik_ShouldSucceed()
    {
        // Arrange: reference date = 2026-10-14, lahir 2009-10-15 (16 tahun 364 hari, belum 17)
        var fixedNow = new DateTime(2026, 10, 14, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(fixedNow);

        var command = CreateSampleCommand() with
        {
            TglLahir = "2009-10-15",
            NoKtp = ""
        };

        _pasienRepoMock
            .Setup(x => x.SearchPasien(It.IsAny<string>()))
            .Returns(Enumerable.Empty<PasienPersonView>());

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
                    "00100000005",
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

        _pasienRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<PasienModel>()))
            .Returns(Result<IPasienKey>.Success(PasienModel.Key("00100000005")));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienId.Should().Be("00100000005");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_EmptyPasienName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var command = CreateSampleCommand() with { PasienName = invalidName! };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_EmptyAlamat1_ShouldThrowArgumentException(string? invalidAlamat)
    {
        // Arrange
        var command = CreateSampleCommand() with { Alamat1 = invalidAlamat! };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_EmptyNoTelp_ShouldThrowArgumentException(string? invalidTelp)
    {
        // Arrange
        var command = CreateSampleCommand() with { NoTelp = invalidTelp! };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_EmptyIbuKandung_ShouldThrowArgumentException(string? invalidIbu)
    {
        // Arrange
        var command = CreateSampleCommand() with { IbuKandung = invalidIbu! };
        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
