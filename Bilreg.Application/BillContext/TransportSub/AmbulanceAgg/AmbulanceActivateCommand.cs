using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceActivateCommand(string AmbulanceId): IRequest, IAmbulanceKey;

public class AmbulanceActivateHandler: IRequestHandler<AmbulanceActivateCommand>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceActivateHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(AmbulanceActivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        
        // BUILD
        var ambulance = _factory.Load(request);
        ambulance.SetAktif();
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}

public class AmbulanceActivateHandlerTest
{
    private readonly Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>> _factory;
    private readonly Mock<IAmbulanceWriter> _writer;
    private readonly AmbulanceActivateHandler _sut;

    public AmbulanceActivateHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>>();
        _writer = new Mock<IAmbulanceWriter>();
        _sut = new AmbulanceActivateHandler(_factory.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        AmbulanceActivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyAmbulanceId_ThenThrowArgumentException()
    {
        var request = new AmbulanceActivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidAmbulanceId_ThenThrowKeyNotFoundException()
    {
        var request = new AmbulanceActivateCommand("A");
        _factory.Setup(x => x.Load(It.IsAny<IAmbulanceKey>()))
            .Throws<KeyNotFoundException>();
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}