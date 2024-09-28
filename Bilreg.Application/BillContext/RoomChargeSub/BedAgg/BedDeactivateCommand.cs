using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.BedAgg;

public record BedDeactivateCommand(string BedId) : IRequest, IBedKey;

public class BedDeactivateHandler : IRequestHandler<BedDeactivateCommand>
{
    private readonly IBedDal _bedDal;
    private readonly IBedWriter _writer;

    public BedDeactivateHandler(IBedDal bedDal, IBedWriter writer)
    {
        _bedDal = bedDal;
        _writer = writer;
    }
    
    public Task Handle(BedDeactivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.BedId);
        
        // BUILD
        var statusDeactive = _bedDal.GetData(request)
            ?? throw new KeyNotFoundException($"The bed {request.BedId} Active");
        
        statusDeactive.UnSetAktif();
        _writer.Save(statusDeactive);
        return Task.CompletedTask;
    }
}

public class BedDeactivateHandlerTest
{
    private readonly Mock<IBedDal> _bedDal;
    private readonly Mock<IBedWriter> _writer;
    private readonly BedDeactivateHandler _sut;

    public BedDeactivateHandlerTest()
    {
        _bedDal = new Mock<IBedDal>();
        _writer = new Mock<IBedWriter>();
        _sut = new BedDeactivateHandler(_bedDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        BedDeactivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyBedId_ThenThrowArgumentException_Test()
    {
        var request = new BedDeactivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}