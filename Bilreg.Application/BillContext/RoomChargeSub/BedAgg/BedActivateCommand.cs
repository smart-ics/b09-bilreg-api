using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.BedAgg;

public record BedActivateCommand(string BedId) : IRequest, IBedKey;

public class BedActivateHandler : IRequestHandler<BedActivateCommand>
{
    private readonly IBedDal _bedDal;
    private readonly IBedWriter _writer;

    public BedActivateHandler(IBedDal bedDal, IBedWriter writer)
    {
        _bedDal = bedDal;
        _writer = writer;
    }
    
    public Task Handle(BedActivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.BedId);
        
        // BUILD 
        var status = _bedDal.GetData(request)
            ?? throw new KeyNotFoundException($"BedId {request.BedId} Not Active");
        
        status.SetAktif();
        _writer.Save(status);
        return Task.CompletedTask;
    }
}

public class BedActivateHandlerTest
{
    private readonly Mock<IBedDal> _bedDal;
    private readonly Mock<IBedWriter> _writer;
    private readonly BedActivateHandler _sut;

    public BedActivateHandlerTest()
    {
        _bedDal = new Mock<IBedDal>();
        _writer = new Mock<IBedWriter>();
        _sut = new BedActivateHandler(_bedDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        BedActivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyBedId_ThenThrowArgumentException_Test()
    {
        var request = new BedActivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
}