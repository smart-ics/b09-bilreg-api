using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienSetKtpHandlerTest
{
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IKelurahanRepo> _kelurahanRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    public PasienSetKtpHandlerTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 16, 10, 0, 0));
    }

    private PasienSetKtpHandler CreateHandler(ITglJamProvider? clock = null) =>
        new(_pasienRepoMock.Object, _kelurahanRepoMock.Object, _auditRepoMock.Object, clock ?? _tglJamProviderMock.Object);

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

    private static PasienSetKtpCommand CreateValidCommand(string pasienId = "00100123456", string userId = "user-test")
    {
        return new PasienSetKtpCommand(
            PasienId: pasienId,
            PasienName: "Budi Santoso",
            Nik: "3578010101900001",
            AlamatKtp: "Jl. Mawar No. 10",
            GolDarah: "O",
            Gender: "L",
            TempatLahir: "Surabaya",
            TglLahir: "1990-05-20",
            Agama: "Islam",
            StatusKawin: "Menikah",
            Rt: "01",
            Rw: "02",
            KelurahanKtpId: "KEL-01",
            IsForceUpdate: false
        )
        {
            UserId = userId
        };
    }

    [Fact]
    public async Task UT01_GivenValidRequestWithUserId_WhenHandle_ThenUpdatesKtpAndCallsSaveChanges()
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

        var handler = CreateHandler();
        var cmd = CreateValidCommand(pasienId, "user-test");

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.PasienId.Should().Be(pasienId);
        _pasienRepoMock.Verify(x => x.SaveChanges(pasien), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.UserId == "user-test" &&
            a.ActionType == "Pasien.SetKtp" &&
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
    public async Task UT04_GivenInvalidNik_WhenHandle_ThenThrowsArgumentException()
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var cmd = new PasienSetKtpCommand(
            pasienId, "Budi", "123", "Jl. Mawar", "O", "L", "Sby", "1990-05-20",
            "Islam", "Kawin", "01", "02", "KEL-01", false)
        {
            UserId = "user-test"
        };

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("NoKtp harus 16 digit angka.");
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UT05_GivenChildUnder17WithoutNik_WhenHandle_ThenUpdatesKtpSuccessfully(string? emptyNik)
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

        var handler = CreateHandler();
        var cmd = new PasienSetKtpCommand(
            pasienId, "Budi Santoso", emptyNik!, "Jl. Mawar No. 10", "O", "L", "Surabaya", "2024-01-01",
            "Islam", "Belum Kawin", "01", "02", "KEL-01", false)
        {
            UserId = "user-test"
        };

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.PasienId.Should().Be(pasienId);
        _pasienRepoMock.Verify(x => x.SaveChanges(It.Is<PasienModel>(p => p.Ktp.Nik == string.Empty)), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UT06_GivenAdult17OrOlderWithoutNik_WhenHandle_ThenThrowsArgumentException(string? emptyNik)
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var cmd = new PasienSetKtpCommand(
            pasienId, "Budi Santoso", emptyNik!, "Jl. Mawar No. 10", "O", "L", "Surabaya", "1990-05-20",
            "Islam", "Menikah", "01", "02", "KEL-01", false)
        {
            UserId = "user-test"
        };

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("NoKtp harus 16 digit angka.");
        _pasienRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task UT07_GivenOriginalBloodTypeDashAndRequestEmpty_WhenHandle_ThenIsDifferentIsFalse()
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        pasien.SyncFromKtp(pasien.Person.PersonName, pasien.Person.TglLahir, pasien.TempatLahir, pasien.Person.Gender, "-");

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        _kelurahanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKelurahanKey>()))
            .Returns(MayBe.From(KelurahanType.Default with { KelurahanId = "KEL-01" }));

        var handler = CreateHandler();
        var cmd = new PasienSetKtpCommand(
            pasienId, pasien.Person.PersonName, "3578010101900001", "Jl. Mawar No. 10", "", pasien.Person.Gender,
            pasien.TempatLahir, pasien.Person.TglLahir.ToString("yyyy-MM-dd"), "Islam", "Menikah", "01", "02", "KEL-01", false)
        {
            UserId = "user-test"
        };

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDifferent.Should().BeFalse();
        response.Original.GolDarah.Should().Be("-");
        response.Ktp.GolDarah.Should().Be("-");
    }

    [Fact]
    public async Task UT08_GivenBloodTypeChangedReal_WhenHandle_ThenIsDifferentIsTrue()
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        pasien.SyncFromKtp(pasien.Person.PersonName, pasien.Person.TglLahir, pasien.TempatLahir, pasien.Person.Gender, "-");

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        _kelurahanRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKelurahanKey>()))
            .Returns(MayBe.From(KelurahanType.Default with { KelurahanId = "KEL-01" }));

        var handler = CreateHandler();
        var cmd = new PasienSetKtpCommand(
            pasienId, pasien.Person.PersonName, "3578010101900001", "Jl. Mawar No. 10", "A", pasien.Person.Gender,
            pasien.TempatLahir, pasien.Person.TglLahir.ToString("yyyy-MM-dd"), "Islam", "Menikah", "01", "02", "KEL-01", false)
        {
            UserId = "user-test"
        };

        // Act
        var response = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDifferent.Should().BeTrue();
        response.Original.GolDarah.Should().Be("-");
        response.Ktp.GolDarah.Should().Be("A");
    }
}
