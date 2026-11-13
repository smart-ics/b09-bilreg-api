using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class LoketServicePointsHandlerTest
{
    private readonly Mock<IPharmacyLoketServicePointRepo> _loketServicePoints = new();
    private readonly Mock<IPharmacyQueueOperationRepo> _operations = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly AllowAllAuth _auth = new();

    private readonly DateTime _now = new(2026, 10, 26, 14, 0, 0);
    private readonly byte[] _tokenV1 = [1, 0, 0, 0, 0, 0, 0, 1];
    private readonly byte[] _tokenV2 = [2, 0, 0, 0, 0, 0, 0, 2];

    public LoketServicePointsHandlerTest()
    {
        _clock.Setup(x => x.Now).Returns(_now);
    }

    private UpdateLoketServicePointsCmdHandler CreateUpdateHandler()
        => new(_loketServicePoints.Object, _operations.Object, _publisher.Object, _auditRepo.Object, _clock.Object, _auth);

    private GetLoketServicePointsQryHandler CreateGetHandler()
        => new(_loketServicePoints.Object, _operations.Object);

    private static PharmacyLoketSession CreateLoketSession(string loketKey = "APT-RCV-01", string depoId = "DEPO-FAR-01")
        => new(
            LoketKey: loketKey,
            LoketType: LoketTypeEnum.PenerimaanResep,
            DisplayName: "Loket Penerimaan 1",
            DepoFarmasiId: depoId,
            AntrianId: "",
            NoUrut: 0,
            ClaimState: LoketClaimStateEnum.Idle,
            IsActive: false,
            AnnouncementVersion: 0,
            CalledAt: ApotekDate.Empty,
            ServiceStartedAt: ApotekDate.Empty,
            ReleasedAt: ApotekDate.Empty,
            RowVersion: [1, 0, 0, 0, 0, 0, 0, 1]);

    #region UpdateLoketServicePoints Tests

    [Fact]
    public async Task UpdateLoketServicePoints_Throws_When_LoketKey_Empty()
    {
        var sut = CreateUpdateHandler();
        var act = () => sut.Handle(new UpdateLoketServicePointsCmd("", ["SP-FAR-BPJS"], _tokenV1), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateLoketServicePoints_Throws_When_RowVersion_Empty()
    {
        var sut = CreateUpdateHandler();
        var act = () => sut.Handle(new UpdateLoketServicePointsCmd("APT-RCV-01", ["SP-FAR-BPJS"], []), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateLoketServicePoints_Throws_When_Loket_NotFound()
    {
        _operations.Setup(x => x.GetLoket("APT-RCV-99")).Returns(MayBe<PharmacyLoketSession>.None);

        var sut = CreateUpdateHandler();
        var act = () => sut.Handle(new UpdateLoketServicePointsCmd("APT-RCV-99", ["SP-FAR-BPJS"], _tokenV1), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*APT-RCV-99*");
    }

    [Fact]
    public async Task UpdateLoketServicePoints_Throws_ApotekConcurrencyException_When_RowVersion_Mismatch()
    {
        var loket = CreateLoketSession("APT-RCV-01") with { RowVersion = _tokenV2 };
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(loket));

        var sut = CreateUpdateHandler();
        // Request passes _tokenV1, but loket has _tokenV2
        var act = () => sut.Handle(new UpdateLoketServicePointsCmd("APT-RCV-01", ["SP-FAR-BPJS"], _tokenV1), CancellationToken.None);
        await act.Should().ThrowAsync<ApotekConcurrencyException>()
            .WithMessage("*Failed to update service points*");
    }

    [Fact]
    public async Task UpdateLoketServicePoints_ReplacesServicePoints_SavesAudit_PublishesRefresh_ReturnsUpdated()
    {
        // Arrange
        var loket = CreateLoketSession("APT-RCV-01", "DEPO-FAR-01");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(loket));

        var updatedSpIds = new List<string> { "SP-FAR-BPJS", "SP-FAR-UMUM" };
        _loketServicePoints.Setup(x => x.ListServicePointsByLoket("APT-RCV-01"))
            .Returns(updatedSpIds);

        var sut = CreateUpdateHandler();

        // Act
        var result = await sut.Handle(
            new UpdateLoketServicePointsCmd("APT-RCV-01", updatedSpIds, _tokenV1, "admin-1"),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.LoketKey.Should().Be("APT-RCV-01");
        result.ServicePointIds.Should().BeEquivalentTo(updatedSpIds);
        result.RowVersion.Should().BeEquivalentTo(_tokenV1);

        _loketServicePoints.Verify(x => x.ReplaceServicePoints("APT-RCV-01", updatedSpIds, "admin-1", _now), Times.Once);

        _auditRepo.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "UPDATE_LOKET_SERVICE_POINTS" &&
            a.EntityName == "PharmacyLoketServicePoint" &&
            a.EntityId == "APT-RCV-01" &&
            a.UserId == "admin-1")), Times.Once);

        _publisher.Verify(x => x.PublishWorklistRefresh("DEPO-FAR-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetLoketServicePoints Tests

    [Fact]
    public async Task GetLoketServicePoints_Throws_When_LoketKey_Empty()
    {
        var sut = CreateGetHandler();
        var act = () => sut.Handle(new GetLoketServicePointsQry(""), CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetLoketServicePoints_Throws_When_Loket_NotFound()
    {
        _operations.Setup(x => x.GetLoket("APT-RCV-99")).Returns(MayBe<PharmacyLoketSession>.None);

        var sut = CreateGetHandler();
        var act = () => sut.Handle(new GetLoketServicePointsQry("APT-RCV-99"), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*APT-RCV-99*");
    }

    [Fact]
    public async Task GetLoketServicePoints_Returns_ServicePoints_And_RowVersion()
    {
        var loket = CreateLoketSession("APT-RCV-01");
        _operations.Setup(x => x.GetLoket("APT-RCV-01")).Returns(MayBe.From(loket));
        _loketServicePoints.Setup(x => x.ListServicePointsByLoket("APT-RCV-01")).Returns(["SP-FAR-BPJS"]);

        var sut = CreateGetHandler();

        var result = await sut.Handle(new GetLoketServicePointsQry("APT-RCV-01"), CancellationToken.None);

        result.Should().NotBeNull();
        result.LoketKey.Should().Be("APT-RCV-01");
        result.ServicePointIds.Should().ContainSingle().Which.Should().Be("SP-FAR-BPJS");
        result.RowVersion.Should().BeEquivalentTo(_tokenV1);
    }

    #endregion
}
