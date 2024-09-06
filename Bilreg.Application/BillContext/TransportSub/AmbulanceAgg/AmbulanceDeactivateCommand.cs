using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceDeactivateCommand(string AmbulanceId): IRequest, IAmbulanceKey;

public class AmbulanceDeactivateHandler: IRequestHandler<AmbulanceDeactivateCommand>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceDeactivateHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(AmbulanceDeactivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        
        // BUILD
        var ambulance = _factory.Load(request);
        ambulance.UnSetAktif();
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}

public class AmbulanceDeactivateHandlerTest
{
    private readonly Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>> _factory;
    private readonly Mock<IAmbulanceWriter> _writer;
    private readonly AmbulanceDeactivateHandler _sut;

    public AmbulanceDeactivateHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>>();
        _writer = new Mock<IAmbulanceWriter>();
        _sut = new AmbulanceDeactivateHandler(_factory.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        AmbulanceDeactivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyAmbulanceId_ThenThrowArgumentException()
    {
        var request = new AmbulanceDeactivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidAmbulanceId_ThenThrowKeyNotFoundException()
    {
        var request = new AmbulanceDeactivateCommand("A");
        _factory.Setup(x => x.Load(It.IsAny<IAmbulanceKey>()))
            .Throws<KeyNotFoundException>();
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}