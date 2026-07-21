using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdVisitDaftarHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly IgdVisitDaftarHandler _sut;

    public IgdVisitDaftarHandlerTest()
    {
        _sut = new IgdVisitDaftarHandler(_visitRepo.Object, TestTglJamProvider.Instance);
    }

    private static IgdVisitDaftarCmd ValidCmd() =>
        new(
            UserId: "U-daftar",
            VisitorName: "Pasien Tes",
            VisitorGender: "P",
            TglLahirYmd: "1990-05-15",
            VisitorKontak: "081234567890");

    [Fact]
    public async Task Handle_ValidRequest_CallsSaveChangesOnceAndReturnsSameVisitId()
    {
        IgdVisitModel? persisted = null;
        _visitRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitModel>()))
            .Callback<IgdVisitModel>(v => persisted = v);

        var cmd = ValidCmd();

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IgdVisitId.Should().NotBeNullOrWhiteSpace();
        persisted.Should().NotBeNull();
        result.IgdVisitId.Should().Be(persisted!.IgdVisitId);
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
        _visitRepo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistedAggregateMatchesCommandAndDomainRules()
    {
        IgdVisitModel? persisted = null;
        _visitRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitModel>()))
            .Callback<IgdVisitModel>(v => persisted = v);

        var cmd = ValidCmd();

        await _sut.Handle(cmd, CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.IgdVisitId.Should().StartWith("IGV");
        persisted.AdministrativeState.Should().Be(AdministrativeStateEnum.Daftar);
        persisted.Visitor.VisitorName.Should().Be(cmd.VisitorName);
        persisted.Visitor.Gender.Should().Be(cmd.VisitorGender);
        persisted.Visitor.TglLahir.Should().Be(new DateOnly(1990, 5, 15));
        persisted.Visitor.Kontak.Should().Be(cmd.VisitorKontak);
        persisted.DaftarDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));
        persisted.AuditTrail.Created.UserId.Should().Be(cmd.UserId);
        persisted.ListEvent.Should().ContainSingle(e => e.EventKind == IgdEventEnum.Daftar);
    }

    [Fact]
    public async Task Handle_WhitespaceGenderAndKontak_NormalizesToDashBeforePersisting()
    {
        IgdVisitModel? persisted = null;
        _visitRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitModel>()))
            .Callback<IgdVisitModel>(v => persisted = v);

        var cmd = ValidCmd() with { VisitorGender = "   ", VisitorKontak = "  " };

        await _sut.Handle(cmd, CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.Visitor.Gender.Should().Be("-");
        persisted.Visitor.Kontak.Should().Be("-");
    }

    [Fact]
    public async Task Handle_NullUserId_ThrowsArgumentException()
    {
        var cmd = ValidCmd() with { UserId = null! };

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyUserId_ThrowsArgumentException()
    {
        var cmd = ValidCmd() with { UserId = "" };

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhitespaceOnlyUserId_ThrowsArgumentException()
    {
        var cmd = ValidCmd() with { UserId = "   " };

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullVisitorName_ThrowsArgumentException()
    {
        var cmd = ValidCmd() with { VisitorName = null! };

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyVisitorName_ThrowsArgumentException()
    {
        var cmd = ValidCmd() with { VisitorName = "" };

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhitespaceOnlyVisitorName_ThrowsArgumentException()
    {
        var cmd = ValidCmd() with { VisitorName = "\t " };

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidTglLahirYmdFormat_ThrowsFormatException()
    {
        var cmd = ValidCmd() with { TglLahirYmd = "15-05-1990" };

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<FormatException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RepositorySaveChangesThrows_PropagatesException()
    {
        _visitRepo
            .Setup(r => r.SaveChanges(It.IsAny<IgdVisitModel>()))
            .Throws(new InvalidOperationException("persist failed"));

        var act = async () => await _sut.Handle(ValidCmd(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("persist failed");
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
    }
}
