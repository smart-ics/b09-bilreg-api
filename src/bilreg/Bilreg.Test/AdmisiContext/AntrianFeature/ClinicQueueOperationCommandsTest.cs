using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class ClinicQueueOperationCommandsTest
{
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IClinicQueueOperationDal> _clinicQueueDal = new();
    private readonly Mock<IClinicQueueRefreshPublisher> _publisher = new();

    #region CLINIC CALL TESTS

    [Theory]
    [InlineData("", 1, "POLI-1", "DOK-1", "usr")]
    [InlineData("Q1", 0, "POLI-1", "DOK-1", "usr")]
    [InlineData("Q1", -1, "POLI-1", "DOK-1", "usr")]
    [InlineData("Q1", 1, "", "DOK-1", "usr")]
    [InlineData("Q1", 1, "POLI-1", "", "usr")]
    [InlineData("Q1", 1, "POLI-1", "DOK-1", "")]
    public async Task Call_InvalidParameters_ThrowsException(
        string queueCode, int noUrut, string poliId, string dokterId, string userId)
    {
        var sut = new ClinicQueueCallHandler(_antrianRepo.Object, _clinicQueueDal.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var cmd = new ClinicQueueCallCmd(queueCode, noUrut, poliId, dokterId, "R1", userId);

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Call_QueueNotFound_ThrowsKeyNotFoundException()
    {
        _antrianRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>()))
            .Returns(MayBe<AntrianModel>.None);

        var sut = new ClinicQueueCallHandler(_antrianRepo.Object, _clinicQueueDal.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var cmd = new ClinicQueueCallCmd("NON_EXISTENT", 1, "POLI-1", "DOK-1", "R1", "u1");

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Antrian 'NON_EXISTENT' not found*");
    }

    [Fact]
    public async Task Call_EntryNotFound_ThrowsKeyNotFoundException()
    {
        SetupQueue(WaitingEntry(1));

        var sut = new ClinicQueueCallHandler(_antrianRepo.Object, _clinicQueueDal.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var cmd = new ClinicQueueCallCmd("Q", 99, "POLI-1", "DOK-1", "R1", "u1");

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Antrian entry 'Q' / 99 not found*");
    }

    [Fact]
    public async Task Call_EntryNotInWaitingStatus_ThrowsInvalidOperationException()
    {
        var entry = WaitingEntry(1);
        entry.Serve(TestTglJamProvider.Instance.Now);
        SetupQueue(entry);

        var sut = new ClinicQueueCallHandler(_antrianRepo.Object, _clinicQueueDal.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var cmd = new ClinicQueueCallCmd("Q", 1, "POLI-1", "DOK-1", "R1", "u1");

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only a Waiting queue entry may call*");
    }

    [Fact]
    public async Task Call_ValidWaitingEntry_IncrementsCallCountAndPersistsActiveCall()
    {
        var entry = WaitingEntry(1);
        entry.CallCount.Should().Be(0);
        SetupQueue(entry);

        ClinicQueueCallDto? savedDto = null;
        _clinicQueueDal.Setup(x => x.UpsertCall(It.IsAny<ClinicQueueCallDto>()))
            .Callback<ClinicQueueCallDto>(dto => savedDto = dto);

        var sut = new ClinicQueueCallHandler(_antrianRepo.Object, _clinicQueueDal.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var cmd = new ClinicQueueCallCmd("Q", 1, "POLI-1", "DOK-1", "R-101", "dr_smith");

        var result = await sut.Handle(cmd, default);

        result.QueueCode.Should().Be("Q");
        result.AntrianId.Should().Be("Q");
        result.NoUrut.Should().Be(1);
        result.Status.Should().Be("Waiting");
        result.CallCount.Should().Be(1);

        entry.CallCount.Should().Be(1);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);

        _antrianRepo.Verify(x => x.SaveChanges(It.Is<AntrianModel>(m => m.AntrianId == "Q")), Times.Once);

        savedDto.Should().NotBeNull();
        savedDto!.PoliId.Should().Be("POLI-1");
        savedDto.DokterId.Should().Be("DOK-1");
        savedDto.RuangId.Should().Be("R-101");
        savedDto.AntrianId.Should().Be("Q");
        savedDto.NoUrut.Should().Be(1);
        savedDto.CallCount.Should().Be(1);
        savedDto.CallStatus.Should().Be(ClinicQueueCallStatus.Active);
        savedDto.UserId.Should().Be("dr_smith");
        savedDto.CalledAt.Should().Be(TestTglJamProvider.Instance.Now);

        _publisher.Verify(x => x.PublishRefreshHint("POLI-1", "DOK-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CLINIC RECALL TESTS

    [Theory]
    [InlineData("", 1, "POLI-1", "DOK-1", "usr")]
    [InlineData("Q1", 0, "POLI-1", "DOK-1", "usr")]
    [InlineData("Q1", 1, "", "DOK-1", "usr")]
    [InlineData("Q1", 1, "POLI-1", "", "usr")]
    [InlineData("Q1", 1, "POLI-1", "DOK-1", "")]
    public async Task Recall_InvalidParameters_ThrowsException(
        string queueCode, int noUrut, string poliId, string dokterId, string userId)
    {
        var sut = new ClinicQueueRecallHandler(_antrianRepo.Object, _clinicQueueDal.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var cmd = new ClinicQueueRecallCmd(queueCode, noUrut, poliId, dokterId, "R1", userId);

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Recall_ValidWaitingEntry_IncrementsCallCountAndUpdatesActiveCall()
    {
        var entry = WaitingEntry(1);
        entry.RecordCall(); // CallCount was 1
        entry.CallCount.Should().Be(1);
        SetupQueue(entry);

        ClinicQueueCallDto? savedDto = null;
        _clinicQueueDal.Setup(x => x.UpsertCall(It.IsAny<ClinicQueueCallDto>()))
            .Callback<ClinicQueueCallDto>(dto => savedDto = dto);

        var sut = new ClinicQueueRecallHandler(_antrianRepo.Object, _clinicQueueDal.Object,
            TestTglJamProvider.Instance, _publisher.Object);

        var cmd = new ClinicQueueRecallCmd("Q", 1, "POLI-1", "DOK-1", "R-101", "dr_smith");

        var result = await sut.Handle(cmd, default);

        result.CallCount.Should().Be(2);
        entry.CallCount.Should().Be(2);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting);

        _antrianRepo.Verify(x => x.SaveChanges(It.Is<AntrianModel>(m => m.AntrianId == "Q")), Times.Once);

        savedDto.Should().NotBeNull();
        savedDto!.CallCount.Should().Be(2);
        savedDto.CallStatus.Should().Be(ClinicQueueCallStatus.Active);

        _publisher.Verify(x => x.PublishRefreshHint("POLI-1", "DOK-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CLINIC SKIP TESTS

    [Theory]
    [InlineData("", 1, "POLI-1", "DOK-1", "usr")]
    [InlineData("Q1", 0, "POLI-1", "DOK-1", "usr")]
    [InlineData("Q1", 1, "", "DOK-1", "usr")]
    [InlineData("Q1", 1, "POLI-1", "", "usr")]
    [InlineData("Q1", 1, "POLI-1", "DOK-1", "")]
    public async Task Skip_InvalidParameters_ThrowsException(
        string queueCode, int noUrut, string poliId, string dokterId, string userId)
    {
        var sut = new ClinicQueueSkipHandler(_antrianRepo.Object, _clinicQueueDal.Object, _publisher.Object);

        var cmd = new ClinicQueueSkipCmd(queueCode, noUrut, poliId, dokterId, userId);

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Skip_EntryNotInWaitingStatus_ThrowsInvalidOperationException()
    {
        var entry = WaitingEntry(1);
        entry.Serve(TestTglJamProvider.Instance.Now);
        SetupQueue(entry);

        var sut = new ClinicQueueSkipHandler(_antrianRepo.Object, _clinicQueueDal.Object, _publisher.Object);

        var cmd = new ClinicQueueSkipCmd("Q", 1, "POLI-1", "DOK-1", "usr");

        var act = () => sut.Handle(cmd, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only a Waiting queue entry may be skipped*");
    }

    [Fact]
    public async Task Skip_ValidWaitingEntry_MarksSkippedAndPreservesWaitingStatus()
    {
        var entry = WaitingEntry(1);
        entry.RecordCall(); // was called once before skip
        SetupQueue(entry);

        _clinicQueueDal.Setup(x => x.MarkSkipped("POLI-1", "DOK-1", "dr_smith"))
            .Returns(true);

        var sut = new ClinicQueueSkipHandler(_antrianRepo.Object, _clinicQueueDal.Object, _publisher.Object);

        var cmd = new ClinicQueueSkipCmd("Q", 1, "POLI-1", "DOK-1", "dr_smith");

        var result = await sut.Handle(cmd, default);

        result.Status.Should().Be("Waiting");
        result.CallCount.Should().Be(1);
        entry.AntrianStatus.Should().Be(AntrianStatusEnum.Waiting); // Status ticket remains Waiting (TD-02)

        _clinicQueueDal.Verify(x => x.MarkSkipped("POLI-1", "DOK-1", "dr_smith"), Times.Once);
        _publisher.Verify(x => x.PublishRefreshHint("POLI-1", "DOK-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region HELPERS

    private void SetupQueue(AntrianEntryModel entry)
    {
        var queue = new AntrianModel("Q", new DateOnly(2026, 10, 28), TimeOnly.MinValue,
            TimeOnly.MaxValue, "tag", "Poli Penyakit Dalam", new ServicePointType("POLI", "Poliklinik"),
            [entry], Mock.Of<ISequencer>(), "A");
        _antrianRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(queue));
    }

    private static AntrianEntryModel WaitingEntry(int noUrut) => AntrianEntryModel.Create(noUrut,
        PersonType.Default, PasienTrackerModel.Key("-"), "", "", TestTglJamProvider.Instance.Now);

    #endregion
}
