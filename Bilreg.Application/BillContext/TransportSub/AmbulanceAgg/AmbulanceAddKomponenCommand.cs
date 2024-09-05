using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public record AmbulanceAddKomponenCommand(string AmbulanceId, string KomponenId, decimal NilaiTarif):
    IRequest, IAmbulanceKey, IKomponenTarifKey;
    
public class AmbulanceAddKomponenHandler: IRequestHandler<AmbulanceAddKomponenCommand>
{
    private readonly IFactoryLoad<AmbulanceModel, IAmbulanceKey> _factory;
    private readonly IKomponenTarifDal _komponenTarifDal;
    private readonly IAmbulanceWriter _writer;

    public AmbulanceAddKomponenHandler(IFactoryLoad<AmbulanceModel, IAmbulanceKey> factory, IKomponenTarifDal komponenTarifDal, IAmbulanceWriter writer)
    {
        _factory = factory;
        _komponenTarifDal = komponenTarifDal;
        _writer = writer;
    }

    public Task Handle(AmbulanceAddKomponenCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.AmbulanceId);
        Guard.IsNotWhiteSpace(request.KomponenId);
        _ = _komponenTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Komponen with id {request.KomponenId} not found");
        
        // BUILD
        var ambulance = _factory.Load(request);
        var ambulanceKomponen = new AmbulanceKomponenModel(ambulance.AmbulanceId, request.KomponenId, request.NilaiTarif);
        ambulance.Add(ambulanceKomponen);
        
        // WRITE
        _ = _writer.Save(ambulance);
        return Task.CompletedTask;
    }
}

public class AmbulanceAddKomponenHandlerTest
{
    private readonly Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>> _factory;
    private readonly Mock<IKomponenTarifDal> _komponenTarifDal;
    private readonly Mock<IAmbulanceWriter> _writer;
    private readonly AmbulanceAddKomponenHandler _sut;

    public AmbulanceAddKomponenHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<AmbulanceModel, IAmbulanceKey>>();
        _komponenTarifDal = new Mock<IKomponenTarifDal>();
        _writer = new Mock<IAmbulanceWriter>();
        _sut = new AmbulanceAddKomponenHandler(_factory.Object, _komponenTarifDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        AmbulanceAddKomponenCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyAmbulanceId_ThenThrowArgumentException()
    {
        var request = new AmbulanceAddKomponenCommand("", "B", 1);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyKomponenId_ThenThrowArgumentException()
    {
        var request = new AmbulanceAddKomponenCommand("A", "", 1);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidAmbulanceId_ThenThrowKeyNotFoundException()
    {
        var request = new AmbulanceAddKomponenCommand("A", "B", 1);
        _factory.Setup(x => x.Load(It.IsAny<IAmbulanceKey>()))
            .Throws<KeyNotFoundException>();
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenInvalidKomponenId_ThenThrowKeyNotFoundException()
    {
        var request = new AmbulanceAddKomponenCommand("A", "B", 1);
        _komponenTarifDal.Setup(x => x.GetData(It.IsAny<IKomponenTarifKey>()))
            .Returns(null as KomponenTarifModel);
        
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}