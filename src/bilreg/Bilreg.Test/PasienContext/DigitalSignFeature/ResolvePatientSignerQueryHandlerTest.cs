using Bilreg.Application.PasienContext.DigitalSignFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.Param;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.DigitalSignFeature;

public class ResolvePatientSignerQueryHandlerTest
{
    private const string HospitalId = "RSHSD";
    private const string Mr = "317304001590223";

    private readonly Mock<IGetProjectIdService> _projectIdMock = new();
    private readonly Mock<IHiDokPatientSignerResolveClient> _clientMock = new();
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();

    private ResolvePatientSignerQueryHandler CreateHandler() => new(
        _projectIdMock.Object, _clientMock.Object, _pasienRepoMock.Object);

    [Fact]
    public async Task UT01_GivenHiDokNotFound_WhenHandle_ThenIncludesPatientSocialData()
    {
        _projectIdMock.Setup(x => x.Execute()).Returns(HospitalId);
        _clientMock.Setup(x => x.Execute(It.IsAny<HiDokPatientSignerResolveRequest>()))
            .Returns(new HiDokPatientSignerResolveResponse(
                HiDokPatientSignerResolveStatus.NotFound, "", "", "Pasien tidak ditemukan"));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == Mr)))
            .Returns(MayBe.From(BuildPasien(
                new ContactType(JenisContactEnum.Mobile, "085312345678"),
                new ContactType(JenisContactEnum.Email, "mail@mail.com"))));

        var result = await CreateHandler().Handle(new ResolvePatientSignerQuery(Mr), CancellationToken.None);

        result.Status.Should().Be(HiDokPatientSignerResolveStatus.NotFound);
        result.Patient.Should().NotBeNull();
        result.Patient!.Should().BeEquivalentTo(new ResolvePatientSignerPatientInfo(
            "", Mr, "GABRIELLA SIFA", "KAPAL BTN SOSIAL BLOK C/31",
            "18-05-2002", "085312345678", "3171045105020003", HospitalId));
    }

    [Fact]
    public async Task UT02_GivenHiDokNotVerified_WhenHandle_ThenIncludesPatientSocialData()
    {
        _projectIdMock.Setup(x => x.Execute()).Returns(HospitalId);
        _clientMock.Setup(x => x.Execute(It.IsAny<HiDokPatientSignerResolveRequest>()))
            .Returns(new HiDokPatientSignerResolveResponse(
                HiDokPatientSignerResolveStatus.NotVerified, "U-123", "", "Signer belum terverifikasi"));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == Mr)))
            .Returns(MayBe.From(BuildPasien(
                new ContactType(JenisContactEnum.Mobile, "085312345678"))));

        var result = await CreateHandler().Handle(new ResolvePatientSignerQuery(Mr), CancellationToken.None);

        result.Status.Should().Be(HiDokPatientSignerResolveStatus.NotVerified);
        result.Patient.Should().NotBeNull();
        result.Patient!.UserrId.Should().Be("U-123");
        result.Patient!.NoTelp.Should().Be("085312345678");
    }

    [Fact]
    public async Task UT03_GivenHiDokProvisionFailed_WhenHandle_ThenIncludesPatientSocialData()
    {
        _projectIdMock.Setup(x => x.Execute()).Returns(HospitalId);
        _clientMock.Setup(x => x.Execute(It.IsAny<HiDokPatientSignerResolveRequest>()))
            .Returns(new HiDokPatientSignerResolveResponse(
                HiDokPatientSignerResolveStatus.ProvisionFailed, "", "", "Gagal provisi signer"));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == Mr)))
            .Returns(MayBe.From(BuildPasien()));

        var result = await CreateHandler().Handle(new ResolvePatientSignerQuery(Mr), CancellationToken.None);

        result.Status.Should().Be(HiDokPatientSignerResolveStatus.ProvisionFailed);
        result.Patient.Should().NotBeNull();
    }

    [Fact]
    public async Task UT04_GivenPasienNotFoundLocally_WhenHandle_ThenResponseHasNoPatient()
    {
        _projectIdMock.Setup(x => x.Execute()).Returns(HospitalId);
        _clientMock.Setup(x => x.Execute(It.IsAny<HiDokPatientSignerResolveRequest>()))
            .Returns(new HiDokPatientSignerResolveResponse(
                HiDokPatientSignerResolveStatus.NotFound, "", "", "Pasien tidak ditemukan"));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == Mr)))
            .Returns(MayBe<PasienModel>.None);

        var result = await CreateHandler().Handle(new ResolvePatientSignerQuery(Mr), CancellationToken.None);

        result.Status.Should().Be(HiDokPatientSignerResolveStatus.NotFound);
        result.Patient.Should().BeNull();
    }

    [Fact]
    public async Task UT05_GivenHiDokSuccess_WhenHandle_ThenNoLocalPasienLookup()
    {
        _projectIdMock.Setup(x => x.Execute()).Returns(HospitalId);
        _clientMock.Setup(x => x.Execute(It.IsAny<HiDokPatientSignerResolveRequest>()))
            .Returns(new HiDokPatientSignerResolveResponse(
                HiDokPatientSignerResolveStatus.Success, "U-123", "SIG-456", ""));

        var result = await CreateHandler().Handle(new ResolvePatientSignerQuery(Mr), CancellationToken.None);

        result.Status.Should().Be(HiDokPatientSignerResolveStatus.Success);
        result.Patient.Should().BeNull();
        _pasienRepoMock.Verify(x => x.LoadEntity(It.IsAny<IPasienKey>()), Times.Never);
    }

    [Fact]
    public async Task UT06_GivenNoMobileContact_WhenHandle_ThenNoTelpUsesPhoneFallback()
    {
        _projectIdMock.Setup(x => x.Execute()).Returns(HospitalId);
        _clientMock.Setup(x => x.Execute(It.IsAny<HiDokPatientSignerResolveRequest>()))
            .Returns(new HiDokPatientSignerResolveResponse(
                HiDokPatientSignerResolveStatus.NotVerified, "", "", "Signer belum terverifikasi"));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == Mr)))
            .Returns(MayBe.From(BuildPasien("022-123456",
                new ContactType(JenisContactEnum.Phone, "022-123456"),
                new ContactType(JenisContactEnum.Email, "mail@mail.com"))));

        var result = await CreateHandler().Handle(new ResolvePatientSignerQuery(Mr), CancellationToken.None);

        result.Patient.Should().NotBeNull();
        result.Patient!.NoTelp.Should().Be("022-123456");
    }

    private static PasienModel BuildPasien(params ContactType[] contacts)
        => BuildPasien("-", contacts);

    private static PasienModel BuildPasien(string personPhone, params ContactType[] contacts)
    {
        var person = new PersonInfoType(
            "GABRIELLA SIFA",
            new DateOnly(2002, 5, 18),
            "P",
            new AlamatType(["KAPAL BTN SOSIAL BLOK C/31", "-", "-"], "BANDUNG", "40135"),
            new ContactType(JenisContactEnum.Phone, personPhone),
            IdentitasType.Default);

        var ktp = new KtpType("3171045105020003", AlamatType.Default, "02", "09", KelurahanType.Default);

        return new PasienModel(
            "0000031290", person, "-", "BANDUNG", GolDarahType.Default, "-",
            ktp, KelurahanType.Default, IdentitasType.Default,
            contacts,
            PasienKeluargaType.Default, AgamaType.Default, SukuType.Default,
            StatusKawinDkType.Default, PendidikanDkType.Default, PekerjaanDkType.Default,
            DateTime.Now, true);
    }
}