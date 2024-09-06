using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceSaveCommand(string AmbulanceId, string AmbulanceName, decimal Abonement): IRequest, IAmbulanceKey;

public class AmbulanceSaveHandler: IRequestHandler<AmbulanceSaveCommand>
{
    private readonly IFactoryLoadOrNull<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceSaveHandler(IFactoryLoadOrNull<AmbulanceModel, IAmbulanceKey> factory, IAmbulanceWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(AmbulanceSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        Guard.IsNotWhiteSpace(request.AmbulanceName);
        
        // BUILD
        var ambulance = _factory.LoadOrNull(request)
            ?? new AmbulanceModel(request.AmbulanceId, request.AmbulanceName);
        ambulance.SetName(request.AmbulanceName);
        ambulance.SetAbonement(request.Abonement);
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}

public class AmbulanceSaveHandlerTest
{
    private readonly Mock<IFactoryLoadOrNull<AmbulanceModel, IAmbulanceKey>> _factory;
    private readonly Mock<IAmbulanceWriter> _writer;
    private readonly AmbulanceSaveHandler _sut;
    public AmbulanceSaveHandlerTest()
    {
        _factory = new Mock<IFactoryLoadOrNull<AmbulanceModel, IAmbulanceKey>>();
        _writer = new Mock<IAmbulanceWriter>();
        _sut = new AmbulanceSaveHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        AmbulanceSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyAmbulanceId_ThenThrowArgumentException()
    {
        var request = new AmbulanceSaveCommand("", "B", 1);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyAmbulanceName_ThenThrowArgumentException()
    {
        var request = new AmbulanceSaveCommand("A", "", 1);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}