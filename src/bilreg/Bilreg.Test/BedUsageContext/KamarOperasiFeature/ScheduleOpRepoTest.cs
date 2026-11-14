using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpRepoTests
{
    private readonly Mock<IScheduleOpDal> _scheduleOpDalMock;
    private readonly Mock<IScheduleOpPpaDal> _scheduleOpPpaDalMock;
    private readonly Mock<IScheduleOpHistDal> _scheduleOpHistDalMock;
    private readonly ScheduleOpRepo _repository;

    public ScheduleOpRepoTests()
    {
        _scheduleOpDalMock = new Mock<IScheduleOpDal>();
        _scheduleOpPpaDalMock = new Mock<IScheduleOpPpaDal>();
        _scheduleOpHistDalMock = new Mock<IScheduleOpHistDal>();
        _repository = new ScheduleOpRepo(
            _scheduleOpDalMock.Object, 
            _scheduleOpPpaDalMock.Object,
            _scheduleOpHistDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _scheduleOpDalMock
            .Setup(x => x.GetData(It.IsAny<IScheduleOpKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _scheduleOpDalMock.Verify(x => x.Update(It.IsAny<ScheduleOpDto>()), Times.Once);
        _scheduleOpDalMock.Verify(x => x.Insert(It.IsAny<ScheduleOpDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns((ScheduleOpDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _scheduleOpDalMock.Verify(x => x.Insert(It.IsAny<ScheduleOpDto>()), Times.Once);
        _scheduleOpDalMock.Verify(x => x.Update(It.IsAny<ScheduleOpDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var expectedPpaDtos = new List<ScheduleOpPpaDto> { CreateTestPpaDto() };
        var key = CreateTestKey();
        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        _scheduleOpPpaDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedPpaDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.Should().NotBeNull(),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT4_GivenNonExistingEntity_WhenLoadEntity_ThenNoneIsReturned()
    {
        // Arrange
        var key = CreateTestKey();
        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns((ScheduleOpDto)null!);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: model => Assert.Fail("Expected None but got Some"),
            onNone: () => { });
    }

    [Fact]
    public void UT5_GivenKey_WhenDeleteEntity_ThenDeleteIsCalled()
    {
        // Arrange
        var key = CreateTestKey();

        // Act
        _repository.DeleteEntity(key);

        // Assert
        _scheduleOpDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenKey_WhenLoadEntity_ThenPpaListIsLoaded()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var expectedPpaDtos = new List<ScheduleOpPpaDto> { CreateTestPpaDto() };
        var key = CreateTestKey();
        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        _scheduleOpPpaDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedPpaDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        _scheduleOpPpaDalMock.Verify(x => x.ListData(key), Times.Once);
    }

    [Fact]
    public void UT7_GivenDateFilter_WhenListDataIsCalled_ThenCorrectListIsReturned()
    {
        // Arrange
        var filterDate = new DateTime(2025, 12, 1);
        var expectedDtos = new List<ScheduleOpDto> { CreateTestDto(), CreateTestDto() };

        _scheduleOpDalMock
            .Setup(x => x.ListData(filterDate))
            .Returns(expectedDtos);

        // Act
        var result = _repository.ListData(filterDate).ToList();

        // Assert
        _scheduleOpDalMock.Verify(x => x.ListData(filterDate), Times.Once);
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(expectedDtos.Count);
    }

    [Fact]
    public void UT8_GivenPasienKeyFilter_WhenListDataIsCalled_ThenCorrectListIsReturned()
    {
        // Arrange
        var filterKey = CreatePasienKey();
        var expectedDtos = new List<ScheduleOpDto> { CreateTestDto() };

        _scheduleOpDalMock
            .Setup(x => x.ListData(filterKey))
            .Returns(expectedDtos);

        // Act
        var result = _repository.ListData(filterKey).ToList();

        // Assert
        _scheduleOpDalMock.Verify(x => x.ListData(filterKey), Times.Once);
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(expectedDtos.Count);
    }

    [Fact]
    public void UT9_GivenEntityWithNewHistory_WhenSaveChanges_ThenHistoryIsInserted()
    {
        // Arrange
        var model = CreateTestModelWithHistory();
        _scheduleOpDalMock
            .Setup(x => x.GetData(It.IsAny<IScheduleOpKey>()))
            .Returns(CreateTestDto());
        _scheduleOpHistDalMock
            .Setup(x => x.ListData(It.IsAny<IScheduleOpKey>()))
            .Returns([]);

        // Act
        _repository.SaveChanges(model);

        // Assert
        _scheduleOpHistDalMock.Verify(x => x.Insert(It.Is<IEnumerable<ScheduleOpHistDto>>(h => h.Count() == 1)), Times.Once);
    }

    [Fact]
    public void UT10_GivenEntityWithExistingHistory_WhenSaveChanges_ThenOnlyNewHistoryIsInserted()
    {
        // Arrange
        var hist1 = CreateTestHistType(1);
        var hist2 = CreateTestHistType(2);
        var model = CreateTestModelWithHistory([hist1, hist2]);

        _scheduleOpDalMock
            .Setup(x => x.GetData(It.IsAny<IScheduleOpKey>()))
            .Returns(CreateTestDto());

        // Existing history has NoUrut 1
        var existingDto1 = CreateTestHistDto(1);
        _scheduleOpHistDalMock
            .Setup(x => x.ListData(It.IsAny<IScheduleOpKey>()))
            .Returns([existingDto1]);

        // Act
        _repository.SaveChanges(model);

        // Assert: only NoUrut 2 is inserted
        _scheduleOpHistDalMock.Verify(x => x.Insert(It.Is<IEnumerable<ScheduleOpHistDto>>(h => 
            h.Count() == 1 && h.First().NoUrut == 2)), Times.Once);
    }

    [Fact]
    public void UT11_GivenExistingEntityWithHistory_WhenLoadEntity_ThenHistoryListIsLoaded()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var expectedPpaDtos = new List<ScheduleOpPpaDto> { CreateTestPpaDto() };
        var expectedHistDtos = new List<ScheduleOpHistDto> { CreateTestHistDto(1), CreateTestHistDto(2) };
        var key = CreateTestKey();

        _scheduleOpDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        _scheduleOpPpaDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedPpaDtos);
        _scheduleOpHistDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedHistDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        _scheduleOpHistDalMock.Verify(x => x.ListData(key), Times.Once);
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.ListHistory.Should().HaveCount(2);
                model.ListHistory.First().NoUrut.Should().Be(1);
                model.ListHistory.Last().NoUrut.Should().Be(2);
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT12_GivenRoomAndDate_WhenListActiveSchedulesInRoom_ThenCorrectListIsReturned()
    {
        // Arrange
        var date = new DateTime(2026, 10, 20);
        var kamarId = "K01";
        var expectedDtos = new List<ScheduleOpDto> { CreateTestDto() };

        _scheduleOpDalMock
            .Setup(x => x.ListActiveSchedulesInRoom(kamarId, date))
            .Returns(expectedDtos);

        // Act
        var result = _repository.ListActiveSchedulesInRoom(kamarId, date).ToList();

        // Assert
        _scheduleOpDalMock.Verify(x => x.ListActiveSchedulesInRoom(kamarId, date), Times.Once);
        result.Should().HaveCount(1);
        result[0].ScheduleOpId.Should().Be(expectedDtos[0].ScheduleOpId);
        result[0].TurnoverBuffer.Should().Be(expectedDtos[0].TurnoverBuffer);
    }

    [Fact]
    public void UT13_GivenDoctorAndDate_WhenListActiveSchedulesForDoctor_ThenCorrectListIsReturned()
    {
        // Arrange
        var date = new DateTime(2026, 10, 20);
        var ppaId = "DOC-01";
        var expectedDtos = new List<ScheduleOpDto> { CreateTestDto() };

        _scheduleOpDalMock
            .Setup(x => x.ListActiveSchedulesForDoctor(ppaId, date))
            .Returns(expectedDtos);

        // Act
        var result = _repository.ListActiveSchedulesForDoctor(ppaId, date).ToList();

        // Assert
        _scheduleOpDalMock.Verify(x => x.ListActiveSchedulesForDoctor(ppaId, date), Times.Once);
        result.Should().HaveCount(1);
        result[0].ScheduleOpId.Should().Be(expectedDtos[0].ScheduleOpId);
    }

    [Fact]
    public void UT14_GivenModelWithAlasanBatalAndTurnoverBuffer_WhenSaveChanges_ThenDtoContainsValues()
    {
        // Arrange
        var model = new ScheduleOpModel(
            "A",
            new DateTime(3000, 1, 1),
            AuditTrailType.Default,
            OrderOpModel.Default.ToReff(),
            PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Elective,
            120,
            DateTime.Today,
            KamarType.Default.ToReff(),
            RegModel.Default.ToReff(),
            PpaType.Default.ToReff(),
            new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled,
            [],
            "Alasan pembatalan resmi",
            45,
            []
        );

        ScheduleOpDto? capturedDto = null;
        _scheduleOpDalMock
            .Setup(x => x.GetData(It.IsAny<IScheduleOpKey>()))
            .Returns(CreateTestDto());
        _scheduleOpDalMock
            .Setup(x => x.Update(It.IsAny<ScheduleOpDto>()))
            .Callback<ScheduleOpDto>(dto => capturedDto = dto);

        // Act
        _repository.SaveChanges(model);

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.AlasanBatal.Should().Be("Alasan pembatalan resmi");
        capturedDto.TurnoverBuffer.Should().Be(45);
    }

    [Fact]
    public void UT15_GivenPpaIdAndDate_WhenListActiveSchedulesForPpaInTeam_ThenDelegatesToDalAndReturnsMappedViews()
    {
        // Arrange
        var date = new DateTime(2026, 10, 20);
        var ppaId = "PPA-01";
        var dto = CreateTestDto() with
        {
            ProfesiId = "PROF-02",
            ProfesiName = "Perawat Anestesi"
        };
        var expectedDtos = new List<ScheduleOpDto> { dto };

        _scheduleOpDalMock
            .Setup(x => x.ListActiveSchedulesForPpaInTeam(ppaId, date))
            .Returns(expectedDtos);

        // Act
        var result = _repository.ListActiveSchedulesForPpaInTeam(ppaId, date).ToList();

        // Assert
        _scheduleOpDalMock.Verify(x => x.ListActiveSchedulesForPpaInTeam(ppaId, date), Times.Once);
        result.Should().HaveCount(1);
        result[0].ScheduleOpId.Should().Be(dto.ScheduleOpId);
        result[0].ProfesiId.Should().Be("PROF-02");
        result[0].ProfesiName.Should().Be("Perawat Anestesi");
        result[0].TurnoverBuffer.Should().Be(dto.TurnoverBuffer);
    }

    private static ScheduleOpModel CreateTestModel()
        => ScheduleOpModel.Default;

    private static ScheduleOpModel CreateTestModelWithHistory(IEnumerable<ScheduleOpHistType>? history = null)
    {
        var histList = history?.ToList() ?? [CreateTestHistType(1)];
        return new ScheduleOpModel(
            "A",
            new DateTime(3000, 1, 1),
            AuditTrailType.Default,
            OrderOpModel.Default.ToReff(),
            PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Elective,
            120,
            DateTime.Today,
            KamarType.Default.ToReff(),
            RegModel.Default.ToReff(),
            PpaType.Default.ToReff(),
            new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled,
            [],
            "",
            30,
            histList
        );
    }

    private static ScheduleOpHistType CreateTestHistType(int noUrut)
        => new(
            noUrut,
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 10, 0, 0),
            120,
            150,
            KamarReff.Default,
            KamarReff.Default,
            PpaReff.Default,
            PpaReff.Default,
            "Reschedule",
            false,
            "",
            "USER_1",
            new DateTime(2026, 10, 19, 10, 0, 0));

    private static ScheduleOpHistDto CreateTestHistDto(int noUrut)
        => new(
            $"HIST_{noUrut}",
            "A",
            noUrut,
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 10, 0, 0),
            120,
            150,
            "K01",
            "K02",
            "P01",
            "P02",
            "Reschedule",
            false,
            "",
            "USER_1",
            new DateTime(2026, 10, 19, 10, 0, 0));

    private static ScheduleOpDto CreateTestDto()
        => ScheduleOpDto.FromModel(ScheduleOpModel.Default); 

    private static ScheduleOpPpaDto CreateTestPpaDto()
        => new ScheduleOpPpaDto("A", 1, "B", "C", "D", "E", "F", "G");

    private static IScheduleOpKey CreateTestKey()
        => ScheduleOpModel.Key("A");

    private static IPasienKey CreatePasienKey()
        => PasienModel.Key("P001");
}
