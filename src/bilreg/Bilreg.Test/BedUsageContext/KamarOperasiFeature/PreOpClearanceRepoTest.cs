using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class PreOpClearanceRepoTest
{
    private readonly Mock<IPreOpClearanceDal> _clearanceDalMock;
    private readonly Mock<IPreOpClearanceChecklistDal> _checklistDalMock;
    private readonly PreOpClearanceRepo _sut;

    public PreOpClearanceRepoTest()
    {
        _clearanceDalMock = new Mock<IPreOpClearanceDal>();
        _checklistDalMock = new Mock<IPreOpClearanceChecklistDal>();
        _sut = new PreOpClearanceRepo(_clearanceDalMock.Object, _checklistDalMock.Object);
    }

    [Fact]
    public void GivenNewClearance_WhenSaveChanges_ThenInsertHeaderAndRewriteChecklist()
    {
        // Arrange
        var model = CreateSampleModel(withChecklist: true);
        _clearanceDalMock.Setup(x => x.GetData(model.ClearanceId))
            .Returns((PreOpClearanceDto?)null);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _clearanceDalMock.Verify(x => x.Insert(It.Is<PreOpClearanceDto>(d =>
            d.ClearanceId == model.ClearanceId &&
            d.ScheduleOpId == model.ScheduleOpId &&
            d.OrderOpId == model.OrderOpId &&
            d.NoUrut == model.NoUrut &&
            d.ClearanceStatus == (int)model.ClearanceStatus)), Times.Once);
        _clearanceDalMock.Verify(x => x.Update(It.IsAny<PreOpClearanceDto>()), Times.Never);

        _checklistDalMock.Verify(x => x.Delete(model.ClearanceId), Times.Once);
        _checklistDalMock.Verify(x => x.Insert(It.Is<IEnumerable<PreOpClearanceChecklistDto>>(items =>
            items.Count() == 2 &&
            items.ElementAt(0).ClearanceId == model.ClearanceId &&
            items.ElementAt(0).NoUrut == 1 &&
            items.ElementAt(0).ChecklistKey == PreOpChecklistItemType.PatientIdentity &&
            items.ElementAt(0).Status == (int)ChecklistItemStatusEnum.Verified &&
            items.ElementAt(1).NoUrut == 2 &&
            items.ElementAt(1).ChecklistKey == PreOpChecklistItemType.InformedConsent &&
            items.ElementAt(1).Status == (int)ChecklistItemStatusEnum.Cleared)), Times.Once);
    }

    [Fact]
    public void GivenNewClearanceWithoutChecklist_WhenSaveChanges_ThenDeleteChecklistAndSkipChecklistInsert()
    {
        // Arrange
        var model = CreateSampleModel(withChecklist: false);
        _clearanceDalMock.Setup(x => x.GetData(model.ClearanceId))
            .Returns((PreOpClearanceDto?)null);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _clearanceDalMock.Verify(x => x.Insert(It.IsAny<PreOpClearanceDto>()), Times.Once);
        _checklistDalMock.Verify(x => x.Delete(model.ClearanceId), Times.Once);
        _checklistDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<PreOpClearanceChecklistDto>>()), Times.Never);
    }

    [Fact]
    public void GivenExistingClearance_WhenSaveChanges_ThenUpdateHeaderAndRewriteChecklist()
    {
        // Arrange
        var model = CreateSampleModel(withChecklist: true);
        var existingDto = PreOpClearanceDto.FromModel(model);
        _clearanceDalMock.Setup(x => x.GetData(model.ClearanceId))
            .Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _clearanceDalMock.Verify(x => x.Update(It.Is<PreOpClearanceDto>(d =>
            d.ClearanceId == model.ClearanceId &&
            d.ClearanceStatus == (int)model.ClearanceStatus)), Times.Once);
        _clearanceDalMock.Verify(x => x.Insert(It.IsAny<PreOpClearanceDto>()), Times.Never);

        _checklistDalMock.Verify(x => x.Delete(model.ClearanceId), Times.Once);
        _checklistDalMock.Verify(x => x.Insert(It.Is<IEnumerable<PreOpClearanceChecklistDto>>(items =>
            items.Count() == 2)), Times.Once);
    }

    [Fact]
    public void GivenExistingClearance_WhenGetLatestByScheduleOpId_ThenReturnHydratedModel()
    {
        // Arrange
        var model = CreateSampleModel(withChecklist: true);
        var dto = PreOpClearanceDto.FromModel(model);
        var checklistDtos = new List<PreOpClearanceChecklistDto>
        {
            new(model.ClearanceId, 1, PreOpChecklistItemType.PatientIdentity, (int)ChecklistItemStatusEnum.Verified, "OK"),
            new(model.ClearanceId, 2, PreOpChecklistItemType.InformedConsent, (int)ChecklistItemStatusEnum.Cleared, "Signed")
        };

        _clearanceDalMock.Setup(x => x.GetLatestByScheduleOpId(model.ScheduleOpId))
            .Returns(dto);
        _checklistDalMock.Setup(x => x.ListData(model.ClearanceId))
            .Returns(checklistDtos);

        // Act
        var actual = _sut.GetLatestByScheduleOpId(model.ScheduleOpId);

        // Assert
        actual.HasValue.Should().BeTrue();
        actual.Value.ClearanceId.Should().Be(model.ClearanceId);
        actual.Value.ScheduleOpId.Should().Be(model.ScheduleOpId);
        actual.Value.NoUrut.Should().Be(1);
        actual.Value.ClearanceStatus.Should().Be(PreOpClearanceStatusEnum.Cleared);
        actual.Value.ChecklistItems.Should().HaveCount(2);
        actual.Value.ChecklistItems.First().ChecklistKey.Should().Be(PreOpChecklistItemType.PatientIdentity);
    }

    [Fact]
    public void GivenNoClearance_WhenGetLatestByScheduleOpId_ThenReturnNone()
    {
        // Arrange
        const string scheduleOpId = "SCH-NONE";
        _clearanceDalMock.Setup(x => x.GetLatestByScheduleOpId(scheduleOpId))
            .Returns((PreOpClearanceDto?)null);

        // Act
        var actual = _sut.GetLatestByScheduleOpId(scheduleOpId);

        // Assert
        actual.HasValue.Should().BeFalse();
        _checklistDalMock.Verify(x => x.ListData(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GivenScheduleOpId_WhenCountByScheduleOpId_ThenReturnCountFromDal()
    {
        // Arrange
        const string scheduleOpId = "SCH-001";
        _clearanceDalMock.Setup(x => x.CountByScheduleOpId(scheduleOpId))
            .Returns(3);

        // Act
        var actual = _sut.CountByScheduleOpId(scheduleOpId);

        // Assert
        actual.Should().Be(3);
        _clearanceDalMock.Verify(x => x.CountByScheduleOpId(scheduleOpId), Times.Once);
    }

    private static PreOpClearanceModel CreateSampleModel(bool withChecklist)
    {
        var items = withChecklist
            ? new List<PreOpChecklistItemType>
            {
                new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "OK"),
                new(PreOpChecklistItemType.InformedConsent, ChecklistItemStatusEnum.Cleared, "Signed")
            }
            : new List<PreOpChecklistItemType>();

        return new PreOpClearanceModel(
            clearanceId: "CLR-001",
            scheduleOpId: "SCH-001",
            orderOpId: "ORD-001",
            regId: "REG-001",
            noUrut: 1,
            clearanceStatus: PreOpClearanceStatusEnum.Cleared,
            checklistItems: items,
            verifikatorPpaId: "PPA-VER-01",
            dokterPjId: "PPA-DOK-01",
            completedAt: new DateTime(2026, 11, 24, 9, 0, 0),
            dokterOverrideId: "",
            alasanOverride: "",
            alasanNotCleared: "",
            auditTrail: AuditTrailType.Create("USR-01", new DateTime(2026, 11, 24, 8, 30, 0))
        );
    }
}
