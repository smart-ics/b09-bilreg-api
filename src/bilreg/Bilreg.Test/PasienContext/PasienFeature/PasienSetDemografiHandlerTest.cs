using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.PasienContext.StatusSosialFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienSetDemografiHandlerTest
{
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IStatusKawinDkDal> _statusKawinDalMock = new();
    private readonly Mock<IAgamaDal> _agamaDalMock = new();
    private readonly Mock<ISukuDal> _sukuDalMock = new();
    private readonly Mock<IPekerjaanDkDal> _pekerjaanDalMock = new();
    private readonly Mock<IPendidikanDkDal> _pendidikanDkDalMock = new();
    private readonly Mock<IKelurahanRepo> _kelurahanRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    private PasienSetDemografiHandler CreateHandler() =>
        new(
            _pasienRepoMock.Object,
            _statusKawinDalMock.Object,
            _agamaDalMock.Object,
            _sukuDalMock.Object,
            _pekerjaanDalMock.Object,
            _pendidikanDkDalMock.Object,
            _kelurahanRepoMock.Object,
            _auditRepoMock.Object
        );

    private static PasienModel CreateSamplePasien(string pasienId = "00100123456")
    {
        return new PasienModel(
            pasienId: pasienId,
            person: new PersonInfoType(
                "Budi Santoso",
                new DateOnly(1990, 5, 20),
                "L",
                AlamatType.Default,
                new ContactType(JenisContactEnum.Mobile, "081234567890"),
                IdentitasType.Default
            ),
            nickName: "Budi",
            tempatLahir: "Surabaya",
            golDarah: GolDarahType.Default,
            namaIbuKandung: "Siti Aminah",
            ktp: KtpType.Default,
            kelurahan: KelurahanType.Default,
            kartuKeluarga: IdentitasType.Default,
            listContact: new List<ContactType>
            {
                new(JenisContactEnum.Email, "budi@example.com"),
                new(JenisContactEnum.Mobile, "081234567890")
            },
            keluarga: PasienKeluargaType.Default,
            agama: AgamaType.Default,
            suku: SukuType.Default,
            statusKawinDk: StatusKawinDkType.Default,
            pendidikanDk: PendidikanDkType.Default,
            pekerjaanDk: PekerjaanDkType.Default,
            tglMedRec: new DateTime(2020, 1, 1),
            isAktif: true
        );
    }

    private static PasienSetDemografiCommand CreateValidCommand(string pasienId = "00100123456", string userId = "user-test")
    {
        return new PasienSetDemografiCommand(
            PasienId: pasienId,
            StatusKawinId: "SK-01",
            AgamaId: "AG-01",
            SukuId: "SKU-01",
            PekerjaanId: "PK-01",
            PendidikanId: "PD-01",
            AlamatDomisili1: "Jl. Melati 1",
            AlamatDomisili2: "RT 01 RW 02",
            AlamatDomisili3: "",
            KelurahanId: "KEL-01",
            KotaDomisili: "Surabaya",
            kodePosDomisili: "60111",
            NoKartuKeluarga: "3578010101900002",
            Email: "budi@example.com",
            NoHp: "081234567890",
            NamaKeluarga: "Dewi",
            Relasi: "Istri",
            NoTelpKeluarga: "081234567899",
            Alamat1Keluarga: "Jl. Melati 1",
            Alamat2Keluarga: "",
            kotaKeluarga: "Surabaya",
            kodePosKeluarga: "60111",
            IbuKandung: "Siti Aminah"
        )
        {
            UserId = userId
        };
    }

    [Fact]
    public async Task UT01_GivenValidRequestWithUserId_WhenHandle_ThenUpdatesDemografiAndCallsSaveChanges()
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        _kelurahanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKelurahanKey>()))
            .Returns(MayBe.From(KelurahanType.Default with { KelurahanId = "KEL-01" }));

        _statusKawinDalMock
            .Setup(x => x.GetData(It.IsAny<IStatusKawinDkKey>()))
            .Returns(MayBe.From(StatusKawinDkType.Default));

        _agamaDalMock
            .Setup(x => x.GetData(It.IsAny<IAgamaKey>()))
            .Returns(MayBe.From(AgamaType.Default));

        _sukuDalMock
            .Setup(x => x.GetData(It.IsAny<ISukuKey>()))
            .Returns(MayBe.From(SukuType.Default));

        _pekerjaanDalMock
            .Setup(x => x.GetData(It.IsAny<IPekerjaanDkKey>()))
            .Returns(MayBe.From(PekerjaanDkType.Default));

        _pendidikanDkDalMock
            .Setup(x => x.GetData(It.IsAny<IPendidikanDkKey>()))
            .Returns(MayBe.From(PendidikanDkType.Default));

        var handler = CreateHandler();
        var cmd = CreateValidCommand(pasienId, "user-test");

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        _pasienRepoMock.Verify(x => x.SaveChanges(pasien), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.UserId == "user-test" &&
            a.ActionType == "Pasien.SetDemografi" &&
            a.EntityName == "Pasien" &&
            a.EntityId == pasienId &&
            !string.IsNullOrEmpty(a.OriginalDataJson))), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UT02_GivenEmptyOrWhitespaceUserId_WhenHandle_ThenThrowsArgumentException(string? invalidUserId)
    {
        // Arrange
        var handler = CreateHandler();
        var cmd = CreateValidCommand("00100123456", invalidUserId!);

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _pasienRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task UT03_GivenNonExistingPatient_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        const string pasienId = "99999999999";
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe<PasienModel>.None);

        var handler = CreateHandler();
        var cmd = CreateValidCommand(pasienId, "user-test");

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Pasien id {pasienId} not found");
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task UT04_GivenNonExistingStatusKawin_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        _kelurahanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKelurahanKey>()))
            .Returns(MayBe.From(KelurahanType.Default));

        _statusKawinDalMock
            .Setup(x => x.GetData(It.IsAny<IStatusKawinDkKey>()))
            .Returns(MayBe<StatusKawinDkType>.None);

        var handler = CreateHandler();
        var cmd = CreateValidCommand(pasienId, "user-test");

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Status Kawin id SK-01 not found");
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }
}
