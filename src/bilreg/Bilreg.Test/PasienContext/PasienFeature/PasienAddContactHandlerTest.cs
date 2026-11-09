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
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienAddContactHandlerTest
{
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    private PasienAddContactHandler CreateHandler() =>
        new(_pasienRepoMock.Object, _auditRepoMock.Object);

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
            listContact: new List<ContactType>(),
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

    [Fact]
    public async Task UT01_GivenValidRequestWithUserId_WhenHandle_ThenAddsContactAndCallsSaveChanges()
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var cmd = new PasienAddContactCommand(pasienId, (int)JenisContactEnum.Mobile, "08987654321")
        {
            UserId = "user-test"
        };

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        pasien.ListContact.Should().Contain(c => c.ContactDetail == "08987654321");
        _pasienRepoMock.Verify(x => x.SaveChanges(pasien), Times.Once);
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.UserId == "user-test" &&
            a.ActionType == "Pasien.AddContact" &&
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
        var cmd = new PasienAddContactCommand("00100123456", (int)JenisContactEnum.Mobile, "08987654321")
        {
            UserId = invalidUserId!
        };

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
        var cmd = new PasienAddContactCommand(pasienId, (int)JenisContactEnum.Mobile, "08987654321")
        {
            UserId = "user-test"
        };

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Pasien id {pasienId} not found");
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }
}
