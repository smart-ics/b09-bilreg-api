using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.Shared.Param.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.Param;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienGetHandlerTest
{
    private const string KodeRs = "001";
    private readonly Mock<IParamSistemDal> _paramSistemDalMock = new();
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IGetKodeRsService> _getKdRsSvcMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    public PasienGetHandlerTest()
    {
        _getKdRsSvcMock.Setup(x => x.Execute()).Returns(KodeRs);
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 7, 12, 0, 0));
    }

    private PasienGetHandler CreateHandler() =>
        new(
            _paramSistemDalMock.Object,
            _pasienRepoMock.Object,
            _getKdRsSvcMock.Object,
            _tglJamProviderMock.Object
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

    [Fact]
    public async Task GivenPasienWithDefaultSaset_WhenHandle_ThenMapsDefaultSasetDataCorrectly()
    {
        // Arrange
        var pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var query = new PasienGetQuery(pasienId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienId.Should().Be(pasienId);
        result.PasienSaset.Should().NotBeNull();
        result.PasienSaset.SasetId.Should().Be("-");
        result.PasienSaset.IsApprovedUpload.Should().BeFalse();
        result.PasienSaset.TglJamApprovedUpload.Should().BeEmpty();
        result.PasienSaset.FileGeneralConcentUpload.Should().Be("-");
        result.PasienSaset.IsApprovedView.Should().BeFalse();
        result.PasienSaset.TglJamApprovedView.Should().BeEmpty();
        result.PasienSaset.FileGeneralConcentView.Should().Be("-");
    }

    [Fact]
    public async Task GivenPasienWithApprovedSaset_WhenHandle_ThenMapsApprovedSasetDataCorrectly()
    {
        // Arrange
        var pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        var uploadTime = new DateTime(2026, 10, 7, 10, 15, 30);
        var viewTime = new DateTime(2026, 10, 7, 11, 20, 0);
        var sasetModel = new PasienSasetModel(
            pasienId,
            "SAT-98765432",
            true,
            uploadTime,
            "general_consent_upload.pdf",
            true,
            viewTime,
            "general_consent_view.pdf"
        );
        pasien.SetSaset(sasetModel);

        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var query = new PasienGetQuery(pasienId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienSaset.Should().NotBeNull();
        result.PasienSaset.SasetId.Should().Be("SAT-98765432");
        result.PasienSaset.IsApprovedUpload.Should().BeTrue();
        result.PasienSaset.TglJamApprovedUpload.Should().Be("2026-10-07 10:15:30");
        result.PasienSaset.FileGeneralConcentUpload.Should().Be("general_consent_upload.pdf");
        result.PasienSaset.IsApprovedView.Should().BeTrue();
        result.PasienSaset.TglJamApprovedView.Should().Be("2026-10-07 11:20:00");
        result.PasienSaset.FileGeneralConcentView.Should().Be("general_consent_view.pdf");
    }

    [Fact]
    public async Task GivenPasienWithApproveUploadSasetMutation_WhenHandle_ThenMapsApprovedUploadTimestamp()
    {
        // Arrange
        var pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        var approvedAt = new DateTime(2026, 10, 7, 14, 0, 0);
        pasien.ApproveUploadSaset(approvedAt);

        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var query = new PasienGetQuery(pasienId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienSaset.Should().NotBeNull();
        result.PasienSaset.IsApprovedUpload.Should().BeTrue();
        result.PasienSaset.TglJamApprovedUpload.Should().Be("2026-10-07 14:00:00");
        result.PasienSaset.IsApprovedView.Should().BeFalse();
        result.PasienSaset.TglJamApprovedView.Should().BeEmpty();
    }

    [Theory]
    [InlineData("123456", "00100123456")]
    [InlineData("00123456", "00100123456")]
    public async Task GivenShortPasienId_WhenHandle_ThenResolvesPasienIdWithHospitalPrefix(string inputId, string expectedResolvedId)
    {
        // Arrange
        var pasien = CreateSamplePasien(expectedResolvedId);
        _pasienRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == expectedResolvedId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var query = new PasienGetQuery(inputId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PasienId.Should().Be(expectedResolvedId);
        _pasienRepoMock.Verify(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == expectedResolvedId)), Times.Once);
    }

    [Fact]
    public async Task GivenPasienNotFound_WhenHandle_ThenThrowsException()
    {
        // Arrange
        _pasienRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe<PasienModel>.None);

        var handler = CreateHandler();
        var query = new PasienGetQuery("00100999999");

        // Act
        var act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*Pasien id 00100999999 not found*");
    }
}
