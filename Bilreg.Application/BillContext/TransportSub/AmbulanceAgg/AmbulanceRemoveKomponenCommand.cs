using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceRemoveKomponenCommand(string AmbulanceId, string KomponenId) : IRequest, IAmbulanceKey, IKomponenTarifKey;
public class AmbulanceRemoveKomponenHandler: IRequestHandler<AmbulanceRemoveKomponenCommand>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceRemoveKomponenHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(AmbulanceRemoveKomponenCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        Guard.IsNotWhiteSpace(request.KomponenId);
        
        // BUILD
        var ambulance = _factory.Load(request);
        ambulance.Remove(x => x.KomponenId == request.KomponenId);
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}

public class AmbulanceRemoveKomponenHandlerTest
{
    private readonly Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>> _factory;
    private readonly Mock<IAmbulanceWriter> _writer;
    private readonly AmbulanceRemoveKomponenHandler _sut;
    
    public AmbulanceRemoveKomponenHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>>();
        _writer = new Mock<IAmbulanceWriter>();
        _sut = new AmbulanceRemoveKomponenHandler(_factory.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        AmbulanceRemoveKomponenCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyAmbulanceId_ThenThrowArgumentException()
    {
        var request = new AmbulanceRemoveKomponenCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyKomponenId_ThenThrowArgumentException()
    {
        var request = new AmbulanceRemoveKomponenCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidAmbulanceId_ThenThrowKeyNotFoundException()
    {
        var request = new AmbulanceRemoveKomponenCommand("A", "B");
        _factory.Setup(x => x.Load(It.IsAny<IAmbulanceKey>()))
            .Throws<KeyNotFoundException>();
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}
