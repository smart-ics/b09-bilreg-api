using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.BedAgg;

public record BedDeleteCommand(string BedId) : IRequest, IBedKey;

public class BedDeleteHandler : IRequestHandler<BedDeleteCommand>
{
    private readonly IBedWriter _writer;

    public BedDeleteHandler(IBedWriter writer)
    {
        _writer = writer;
    }
    public Task Handle(BedDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.BedId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class BedDeleteHandlerTest
{
    private readonly Mock<IBedWriter> _writer;
    private readonly BedDeleteHandler _sut;

    public BedDeleteHandlerTest()
    {
        _writer = new Mock<IBedWriter>();
        _sut = new BedDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        BedDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenNullBedId_ThenThrowArgumentNullException()
    {
        var request = new BedDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
}
