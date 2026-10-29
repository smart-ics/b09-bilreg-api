using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabExternalFeature;

public class LabExternalQueryHandlersTest
{
    private readonly Mock<ILabExternalRepo> _repoMock = new();
    private readonly LabExternalGetHandler _getHandler;
    private readonly LabExternalListHandler _listHandler;

    public LabExternalQueryHandlersTest()
    {
        _getHandler = new LabExternalGetHandler(_repoMock.Object);
        _listHandler = new LabExternalListHandler(_repoMock.Object);
    }

    private static LabExternalModel CreateSampleModel(
        string regExternalId = "LX26A14B8C",
        string externalMrNo = "EXT-202610-00001",
        string patientName = "Jane Doe",
        string nik = "3171012345670001",
        DateTime? birthDate = null,
        string gender = "F",
        string phone = "08123456789",
        string address = "Jl. Melati No. 10",
        LabReferralTypeEnum referralType = LabReferralTypeEnum.FaskesLuar,
        string doctor = "Dr. Budi",
        string origin = "Klinik Sehat",
        string notes = "Pemeriksaan Darah Lengkap",
        string? billingAccountId = null,
        string createdUser = "U001",
        DateTime? createdDate = null)
    {
        var demographics = new LabExternalDemographicsType(
            patientName,
            nik,
            birthDate ?? new DateTime(1995, 5, 20),
            gender,
            phone,
            address);

        var referralInfo = new LabExternalReferralInfoType(
            referralType,
            doctor,
            origin,
            notes);

        var audit = new AuditInfoType(createdUser, createdDate ?? new DateTime(2026, 10, 14, 8, 30, 0));

        return LabExternalModel.Create(
            regExternalId,
            externalMrNo,
            demographics,
            referralInfo,
            billingAccountId ?? regExternalId,
            audit);
    }

    #region LabExternalGetHandler Tests

    [Fact]
    public async Task GetHandler_WhenEntityFound_ReturnsExpectedDetailResponse()
    {
        // Arrange
        var birthDate = new DateTime(1995, 5, 20);
        var createdDate = new DateTime(2026, 10, 14, 8, 30, 0);
        var model = CreateSampleModel(
            regExternalId: "LEX001",
            externalMrNo: "EXT-202610-00001",
            patientName: "Jane Doe",
            nik: "3171012345670001",
            birthDate: birthDate,
            gender: "F",
            phone: "08123456789",
            address: "Jl. Melati No. 10",
            referralType: LabReferralTypeEnum.FaskesLuar,
            doctor: "Dr. Budi",
            origin: "Klinik Sehat",
            notes: "Cek Hemoglobin",
            billingAccountId: "LEX001",
            createdUser: "U001",
            createdDate: createdDate);

        _repoMock.Setup(x => x.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX001")))
            .Returns(MayBe.From(model));

        var query = new LabExternalGetQuery("LEX001");

        // Act
        var result = await _getHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegExternalId.Should().Be("LEX001");
        result.ExternalMrNo.Should().Be("EXT-202610-00001");
        result.PatientName.Should().Be("Jane Doe");
        result.Nik.Should().Be("3171012345670001");
        result.BirthDate.Should().Be(birthDate);
        result.Age.Should().Be(model.Demographics.CalculateAge());
        result.Gender.Should().Be("F");
        result.Phone.Should().Be("08123456789");
        result.Address.Should().Be("Jl. Melati No. 10");
        result.ReferralType.Should().Be((int)LabReferralTypeEnum.FaskesLuar);
        result.ReferralDoctor.Should().Be("Dr. Budi");
        result.ReferralOrigin.Should().Be("Klinik Sehat");
        result.ClinicalNotes.Should().Be("Cek Hemoglobin");
        result.BillingAccountId.Should().Be("LEX001");
        result.Status.Should().Be((int)LabExternalStatusEnum.Active);
        result.CancelReason.Should().BeEmpty();
        result.CancelledUserId.Should().BeEmpty();
        result.CancelledDate.Should().Be(LabExternalModel.EmptyDate);
        result.CreatedDate.Should().Be(createdDate);
        result.CreatedUserId.Should().Be("U001");

        _repoMock.Verify(x => x.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX001")), Times.Once);
    }

    [Fact]
    public async Task GetHandler_WhenCancelledEntityFound_ReturnsExpectedCancelDetails()
    {
        // Arrange
        var model = CreateSampleModel(regExternalId: "LEX002");
        var cancelDate = new DateTime(2026, 10, 14, 11, 0, 0);
        model.Cancel("Pasien membatalkan sendiri", "ADMIN_USER", cancelDate, hasPaidCashier: false, hasCollectedSpecimen: false);

        _repoMock.Setup(x => x.LoadEntity(It.Is<ILabExternalKey>(k => k.RegExternalId == "LEX002")))
            .Returns(MayBe.From(model));

        var query = new LabExternalGetQuery("LEX002");

        // Act
        var result = await _getHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegExternalId.Should().Be("LEX002");
        result.Status.Should().Be((int)LabExternalStatusEnum.Cancelled);
        result.CancelReason.Should().Be("Pasien membatalkan sendiri");
        result.CancelledUserId.Should().Be("ADMIN_USER");
        result.CancelledDate.Should().Be(cancelDate);
    }

    [Fact]
    public async Task GetHandler_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(x => x.LoadEntity(It.IsAny<ILabExternalKey>()))
            .Returns(MayBe<LabExternalModel>.None);

        var query = new LabExternalGetQuery("NON_EXISTING");

        // Act
        var act = async () => await _getHandler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NON_EXISTING*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetHandler_WhenRegExternalIdIsNullOrWhiteSpace_ThrowsArgumentException(string id)
    {
        // Arrange
        var query = new LabExternalGetQuery(id);

        // Act
        var act = async () => await _getHandler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetHandler_WhenRegExternalIdIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var query = new LabExternalGetQuery(null!);

        // Act
        var act = async () => await _getHandler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetHandler_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _getHandler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region LabExternalListHandler Tests

    [Fact]
    public async Task ListHandler_WhenSearchTermProvided_ReturnsMappedList()
    {
        // Arrange
        var birthDate1 = new DateTime(1990, 1, 1);
        var createdDate1 = new DateTime(2026, 10, 14, 8, 0, 0);
        var item1 = CreateSampleModel(
            regExternalId: "LEX001",
            externalMrNo: "EXT-202610-00001",
            patientName: "John Budi",
            nik: "3171000000000001",
            birthDate: birthDate1,
            gender: "M",
            phone: "081234567890",
            referralType: LabReferralTypeEnum.Aps,
            origin: "APS",
            billingAccountId: "LEX001",
            createdDate: createdDate1);

        var item2 = CreateSampleModel(
            regExternalId: "LEX002",
            externalMrNo: "EXT-202610-00002",
            patientName: "Budi Santoso",
            nik: "3171000000000002",
            gender: "M",
            phone: "081298765432",
            referralType: LabReferralTypeEnum.FaskesLuar,
            origin: "RS Luar",
            billingAccountId: "LEX002");

        _repoMock.Setup(x => x.ListData("Budi", null, null, null))
            .Returns(new List<LabExternalModel> { item1, item2 });

        var query = new LabExternalListQuery("Budi");

        // Act
        var result = await _listHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var first = result[0];
        first.RegExternalId.Should().Be("LEX001");
        first.ExternalMrNo.Should().Be("EXT-202610-00001");
        first.PatientName.Should().Be("John Budi");
        first.Nik.Should().Be("3171000000000001");
        first.BirthDate.Should().Be(birthDate1);
        first.Age.Should().Be(item1.Demographics.CalculateAge());
        first.Gender.Should().Be("M");
        first.Phone.Should().Be("081234567890");
        first.ReferralType.Should().Be((int)LabReferralTypeEnum.Aps);
        first.ReferralOrigin.Should().Be("APS");
        first.BillingAccountId.Should().Be("LEX001");
        first.Status.Should().Be((int)LabExternalStatusEnum.Active);
        first.CreatedDate.Should().Be(createdDate1);

        var second = result[1];
        second.RegExternalId.Should().Be("LEX002");
        second.PatientName.Should().Be("Budi Santoso");
        second.ReferralType.Should().Be((int)LabReferralTypeEnum.FaskesLuar);
        second.ReferralOrigin.Should().Be("RS Luar");

        _repoMock.Verify(x => x.ListData("Budi", null, null, null), Times.Once);
    }

    [Fact]
    public async Task ListHandler_WhenDateRangeAndStatusProvided_PassesParametersToRepo()
    {
        // Arrange
        var d1 = new DateTime(2026, 10, 1);
        var d2 = new DateTime(2026, 10, 31);
        var item = CreateSampleModel(regExternalId: "LEX003");

        _repoMock.Setup(x => x.ListData(null, d1, d2, 1))
            .Returns(new List<LabExternalModel> { item });

        var query = new LabExternalListQuery(SearchTerm: null, Date1: d1, Date2: d2, Status: 1);

        // Act
        var result = await _listHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].RegExternalId.Should().Be("LEX003");
        _repoMock.Verify(x => x.ListData(null, d1, d2, 1), Times.Once);
    }

    [Fact]
    public async Task ListHandler_WhenRepoReturnsEmpty_ReturnsEmptyList()
    {
        // Arrange
        _repoMock.Setup(x => x.ListData(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
            .Returns(new List<LabExternalModel>());

        var query = new LabExternalListQuery("NonExistent");

        // Act
        var result = await _listHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListHandler_WhenRepoReturnsNull_ReturnsEmptyList()
    {
        // Arrange
        _repoMock.Setup(x => x.ListData(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
            .Returns((List<LabExternalModel>)null!);

        var query = new LabExternalListQuery();

        // Act
        var result = await _listHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListHandler_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _listHandler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}
