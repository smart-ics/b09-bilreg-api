using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabExternalFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.LabContext.LabExternalFeature;

public class LabExternalRepoTest
{
    private readonly Mock<ILabExternalDal> _dal = new();
    private readonly LabExternalRepo _sut;

    public LabExternalRepoTest()
    {
        _sut = new LabExternalRepo(_dal.Object);
    }

    private static LabExternalModel CreateTestModel(
        string regExternalId = "LEX26A14B8C1D",
        string externalMrNo = "EXT-202610-00001",
        string patientName = "Jane Doe")
    {
        var demographics = new LabExternalDemographicsType(
            patientName,
            "3171012345670001",
            new DateTime(1995, 5, 20),
            "F",
            "08123456789",
            "Jl. Melati No. 10");

        var referralInfo = new LabExternalReferralInfoType(
            LabReferralTypeEnum.Aps,
            "Dr. Budi",
            "Klinik Medika",
            "General check-up");

        var audit = new AuditInfoType("U001", new DateTime(2026, 10, 14, 8, 30, 0));

        return LabExternalModel.Create(
            regExternalId,
            externalMrNo,
            demographics,
            referralInfo,
            regExternalId,
            audit);
    }

    [Fact]
    public void SaveChanges_WhenNewEntity_CallsInsert()
    {
        // Arrange
        var model = CreateTestModel();
        _dal.Setup(x => x.GetData(model)).Returns((LabExternalDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _dal.Verify(x => x.Insert(It.Is<LabExternalDto>(d => d.RegExternalId == model.RegExternalId)), Times.Once);
        _dal.Verify(x => x.Update(It.IsAny<LabExternalDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_WhenExistingEntity_CallsUpdate()
    {
        // Arrange
        var model = CreateTestModel();
        var existingDto = LabExternalDto.FromModel(model);
        _dal.Setup(x => x.GetData(model)).Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _dal.Verify(x => x.Update(It.Is<LabExternalDto>(d => d.RegExternalId == model.RegExternalId)), Times.Once);
        _dal.Verify(x => x.Insert(It.IsAny<LabExternalDto>()), Times.Never);
    }

    [Fact]
    public void LoadEntity_WhenFound_ReturnsModel()
    {
        // Arrange
        var model = CreateTestModel();
        var dto = LabExternalDto.FromModel(model);
        _dal.Setup(x => x.GetData(It.IsAny<ILabExternalKey>())).Returns(dto);

        // Act
        var result = _sut.LoadEntity(model);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.RegExternalId.Should().Be(model.RegExternalId);
                m.ExternalMrNo.Should().Be(model.ExternalMrNo);
                m.Demographics.PatientName.Should().Be("Jane Doe");
                m.Status.Should().Be(LabExternalStatusEnum.Active);
            },
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void LoadEntity_WhenNotFound_ReturnsNone()
    {
        // Arrange
        var key = LabExternalModel.Key("NON_EXISTING");
        _dal.Setup(x => x.GetData(key)).Returns((LabExternalDto)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadByExternalMrNo_WhenFound_ReturnsModel()
    {
        // Arrange
        var model = CreateTestModel();
        var dto = LabExternalDto.FromModel(model);
        _dal.Setup(x => x.GetByExternalMrNo("EXT-202610-00001")).Returns(dto);

        // Act
        var result = _sut.LoadByExternalMrNo("EXT-202610-00001");

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.RegExternalId.Should().Be(model.RegExternalId);
                m.ExternalMrNo.Should().Be("EXT-202610-00001");
                m.Demographics.PatientName.Should().Be("Jane Doe");
            },
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void LoadByExternalMrNo_WhenNotFound_ReturnsNone()
    {
        // Arrange
        _dal.Setup(x => x.GetByExternalMrNo("NON_EXISTING")).Returns((LabExternalDto)null!);

        // Act
        var result = _sut.LoadByExternalMrNo("NON_EXISTING");

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void ListData_WhenFound_ReturnsMappedList()
    {
        // Arrange
        var m1 = CreateTestModel("LEX001", "EXT-202610-00001", "Patient A");
        var m2 = CreateTestModel("LEX002", "EXT-202610-00002", "Patient B");
        var dtos = new List<LabExternalDto> { LabExternalDto.FromModel(m1), LabExternalDto.FromModel(m2) };
        _dal.Setup(x => x.ListData("Patient", null, null, null)).Returns(dtos);

        // Act
        var result = _sut.ListData("Patient", null, null, null).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].RegExternalId.Should().Be("LEX001");
        result[1].RegExternalId.Should().Be("LEX002");
    }

    [Fact]
    public void ListData_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _dal.Setup(x => x.ListData(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
            .Returns(new List<LabExternalDto>());

        // Act
        var result = _sut.ListData("None", null, null, null);

        // Assert
        result.Should().BeEmpty();
    }
}
