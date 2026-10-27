using Bilreg.Api.Controllers.ApotekContext;
using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Application.Shared;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.Api;

public class ApotekControllerTest
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ICurrentUserContext> _user = new();
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly ApotekController _sut;

    public ApotekControllerTest()
    {
        _clock.Setup(x => x.Now).Returns(new DateTime(2026, 2, 11, 14, 30, 0));
        _sut = new ApotekController(_mediator.Object, _user.Object, _clock.Object);
    }

    [Fact]
    public async Task TelaahWorklist_WhenBusinessDateNull_UsesClockBusinessDate()
    {
        TelaahWorklistQuery? capturedQuery = null;
        _mediator
            .Setup(x => x.Send(It.IsAny<TelaahWorklistQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IReadOnlyList<TelaahWorklistItem>>, CancellationToken>((req, _) =>
                capturedQuery = (TelaahWorklistQuery)req)
            .ReturnsAsync(new List<TelaahWorklistItem>());

        var result = await _sut.TelaahWorklist(null);

        result.Should().BeOfType<OkObjectResult>();
        capturedQuery.Should().NotBeNull();
        capturedQuery!.BusinessDate.Should().Be(new DateOnly(2026, 2, 11));
    }

    [Fact]
    public async Task TelaahWorklist_WhenBusinessDateProvided_UsesProvidedDate()
    {
        var customDate = new DateOnly(2026, 3, 15);
        TelaahWorklistQuery? capturedQuery = null;
        _mediator
            .Setup(x => x.Send(It.IsAny<TelaahWorklistQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IReadOnlyList<TelaahWorklistItem>>, CancellationToken>((req, _) =>
                capturedQuery = (TelaahWorklistQuery)req)
            .ReturnsAsync(new List<TelaahWorklistItem>());

        var result = await _sut.TelaahWorklist(customDate);

        result.Should().BeOfType<OkObjectResult>();
        capturedQuery.Should().NotBeNull();
        capturedQuery!.BusinessDate.Should().Be(customDate);
    }

    [Fact]
    public async Task Pelayanan_WhenBusinessDateNull_UsesClockBusinessDate()
    {
        PelayananWorklistQuery? capturedQuery = null;
        _mediator
            .Setup(x => x.Send(It.IsAny<PelayananWorklistQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IReadOnlyList<PelayananWorklistItem>>, CancellationToken>((req, _) =>
                capturedQuery = (PelayananWorklistQuery)req)
            .ReturnsAsync(new List<PelayananWorklistItem>());

        var result = await _sut.Pelayanan("ANT1", 1, null);

        result.Should().BeOfType<OkObjectResult>();
        capturedQuery.Should().NotBeNull();
        capturedQuery!.BusinessDate.Should().Be(new DateOnly(2026, 2, 11));
    }

    [Fact]
    public async Task Pelayanan_WhenBusinessDateProvided_UsesProvidedDate()
    {
        var customDate = new DateOnly(2026, 3, 15);
        PelayananWorklistQuery? capturedQuery = null;
        _mediator
            .Setup(x => x.Send(It.IsAny<PelayananWorklistQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IReadOnlyList<PelayananWorklistItem>>, CancellationToken>((req, _) =>
                capturedQuery = (PelayananWorklistQuery)req)
            .ReturnsAsync(new List<PelayananWorklistItem>());

        var result = await _sut.Pelayanan("ANT1", 1, customDate);

        result.Should().BeOfType<OkObjectResult>();
        capturedQuery.Should().NotBeNull();
        capturedQuery!.BusinessDate.Should().Be(customDate);
    }

    [Fact]
    public async Task Serah_WhenAsOfNull_UsesClockNow()
    {
        SerahWorklistQuery? capturedQuery = null;
        _mediator
            .Setup(x => x.Send(It.IsAny<SerahWorklistQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IReadOnlyList<SerahWorklistItem>>, CancellationToken>((req, _) =>
                capturedQuery = (SerahWorklistQuery)req)
            .ReturnsAsync(new List<SerahWorklistItem>());

        var result = await _sut.Serah(null);

        result.Should().BeOfType<OkObjectResult>();
        capturedQuery.Should().NotBeNull();
        capturedQuery!.AsOf.Should().Be(new DateTime(2026, 2, 11, 14, 30, 0));
    }
}
