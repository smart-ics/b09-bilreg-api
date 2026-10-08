using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.Param;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienRepoTest
{
    private const string PasienId = "00000001";
    private readonly Mock<IPasienDal> _pasienDalMock = new();
    private readonly Mock<IPasienKtpDal> _pasienKtpDalMock = new();
    private readonly Mock<IGetKodeRsService> _getKodeRsSvcMock = new();
    private readonly Mock<IPasienIdDal> _pasienIdDalMock = new();
    private readonly Mock<IPasienTelpDal> _pasienTelpDalMock = new();
    private readonly Mock<IPasienSasetDal> _pasienSasetDalMock = new();

    private readonly PasienRepo _sut;

    public PasienRepoTest()
    {
        _sut = new PasienRepo(
            _pasienDalMock.Object,
            _pasienKtpDalMock.Object,
            _getKodeRsSvcMock.Object,
            _pasienIdDalMock.Object,
            _pasienTelpDalMock.Object,
            _pasienSasetDalMock.Object
        );
    }

    private static PasienModel CreateSamplePasien(string pasienId = PasienId)
    {
        return new PasienModel(
            pasienId: pasienId,
            person: new PersonInfoType("John Doe", new DateOnly(1990, 5, 20), "L",
                AlamatType.Default, ContactType.Default, IdentitasType.Default),
            nickName: "John",
            tempatLahir: "Jakarta",
            golDarah: GolDarahType.Default,
            namaIbuKandung: "Ibu Kandung",
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

    private static PasienDto CreateSamplePasienDto(string pasienId = PasienId) =>
        PasienDto.FromModel(CreateSamplePasien(pasienId));

    #region LoadEntity Tests

    [Fact]
    public void GivenPasienNotFound_WhenLoadEntity_ThenReturnsNone()
    {
        // Arrange
        var key = PasienModel.Key(PasienId);
        _pasienDalMock.Setup(x => x.GetData(key)).Returns((PasienDto)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        _pasienSasetDalMock.Verify(x => x.GetData(It.IsAny<IPasienKey>()), Times.Never);
    }

    [Fact]
    public void GivenPasienExistsWithoutSaset_WhenLoadEntity_ThenInitializesDefaultSaset()
    {
        // Arrange
        var key = PasienModel.Key(PasienId);
        _pasienDalMock.Setup(x => x.GetData(key)).Returns(CreateSamplePasienDto(PasienId));
        _pasienSasetDalMock.Setup(x => x.GetData(key)).Returns((PasienSasetDto)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        var model = result.Value;
        model.PasienSaset.Should().NotBeNull();
        model.PasienSaset.PasienId.Should().Be(PasienId);
        model.PasienSaset.SasetId.Should().Be("-");
        model.PasienSaset.IsApprovedUpload.Should().BeFalse();
        model.PasienSaset.TglJamApprovedUpload.Should().Be(new DateTime(3000, 1, 1));
        _pasienSasetDalMock.Verify(x => x.GetData(key), Times.Once);
    }

    [Fact]
    public void GivenPasienExistsWithSaset_WhenLoadEntity_ThenHydratesSasetData()
    {
        // Arrange
        var key = PasienModel.Key(PasienId);
        var approvedAt = new DateTime(2026, 10, 7, 8, 30, 0);
        var sasetDto = new PasienSasetDto(
            KodeMr: PasienId,
            KodeSaset: "SAT-12345",
            IsApprovedUpload: true,
            TglJamApprovedUpload: approvedAt,
            FileGeneralConcentUpload: "consent_upload.pdf",
            IsApprovedView: false,
            TglJamApprovedView: new DateTime(3000, 1, 1),
            FileGeneralConcentView: "-"
        );

        _pasienDalMock.Setup(x => x.GetData(key)).Returns(CreateSamplePasienDto(PasienId));
        _pasienSasetDalMock.Setup(x => x.GetData(key)).Returns(sasetDto);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        var model = result.Value;
        model.PasienSaset.Should().NotBeNull();
        model.PasienSaset.PasienId.Should().Be(PasienId);
        model.PasienSaset.SasetId.Should().Be("SAT-12345");
        model.PasienSaset.IsApprovedUpload.Should().BeTrue();
        model.PasienSaset.TglJamApprovedUpload.Should().Be(approvedAt);
        model.PasienSaset.FileGeneralConcentUpload.Should().Be("consent_upload.pdf");
        _pasienSasetDalMock.Verify(x => x.GetData(key), Times.Once);
    }

    #endregion

    #region SaveChanges Tests

    [Fact]
    public void GivenSasetNotExistsInDb_WhenSaveChanges_ThenInsertsSaset()
    {
        // Arrange
        var model = CreateSamplePasien(PasienId);
        _pasienDalMock.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns((PasienDto)null!);
        _pasienSasetDalMock.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns((PasienSasetDto)null!);

        // Act
        var result = _sut.SaveChanges(model);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.PasienId.Should().Be(PasienId);
        _pasienSasetDalMock.Verify(x => x.Insert(It.Is<PasienSasetDto>(d =>
            d.KodeMr == PasienId &&
            d.IsApprovedUpload == false)), Times.Once);
        _pasienSasetDalMock.Verify(x => x.Update(It.IsAny<PasienSasetDto>()), Times.Never);
    }

    [Fact]
    public void GivenSasetAlreadyExistsInDb_WhenSaveChanges_ThenUpdatesSaset()
    {
        // Arrange
        var model = CreateSamplePasien(PasienId);
        var approvedAt = new DateTime(2026, 10, 7, 9, 0, 0);
        model.ApproveUploadSaset(approvedAt);

        var existingSasetDto = new PasienSasetDto(
            KodeMr: PasienId,
            KodeSaset: "-",
            IsApprovedUpload: false,
            TglJamApprovedUpload: new DateTime(3000, 1, 1),
            FileGeneralConcentUpload: "-",
            IsApprovedView: false,
            TglJamApprovedView: new DateTime(3000, 1, 1),
            FileGeneralConcentView: "-"
        );

        _pasienDalMock.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns((PasienDto)null!);
        _pasienSasetDalMock.Setup(x => x.GetData(It.IsAny<IPasienKey>())).Returns(existingSasetDto);

        // Act
        var result = _sut.SaveChanges(model);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.PasienId.Should().Be(PasienId);
        _pasienSasetDalMock.Verify(x => x.Update(It.Is<PasienSasetDto>(d =>
            d.KodeMr == PasienId &&
            d.IsApprovedUpload == true &&
            d.TglJamApprovedUpload == approvedAt)), Times.Once);
        _pasienSasetDalMock.Verify(x => x.Insert(It.IsAny<PasienSasetDto>()), Times.Never);
    }

    #endregion

    #region DeleteEntity Tests

    [Fact]
    public void GivenPasienKey_WhenDeleteEntity_ThenDeletesSasetRecord()
    {
        // Arrange
        var key = PasienModel.Key(PasienId);

        // Act
        _sut.DeleteEntity(key);

        // Assert
        _pasienDalMock.Verify(x => x.Delete(It.Is<IPasienKey>(k => k.PasienId == PasienId)), Times.Once);
        _pasienKtpDalMock.Verify(x => x.Delete(It.Is<IPasienKey>(k => k.PasienId == PasienId)), Times.Once);
        _pasienSasetDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    #endregion
}
