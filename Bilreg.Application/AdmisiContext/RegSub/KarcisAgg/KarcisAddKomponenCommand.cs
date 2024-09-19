using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisAddKomponenCommand(string KarcisId, string KomponenId, decimal Nilai): IRequest, IKarcisKey, IKomponenKey;

public class KarcisAddKomponenHandler: IRequestHandler<KarcisAddKomponenCommand>
{
    private readonly IFactoryLoad<KarcisModel, IKarcisKey> _factory;
    private readonly IKomponenTarifDal _komponenTarifDal;
    private readonly IKarcisWriter _writer;

    public KarcisAddKomponenHandler(IFactoryLoad<KarcisModel, IKarcisKey> factory, IKomponenTarifDal komponenTarifDal, IKarcisWriter writer)
    {
        _factory = factory;
        _komponenTarifDal = komponenTarifDal;
        _writer = writer;
    }

    public Task Handle(KarcisAddKomponenCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KarcisId);
        Guard.IsNotWhiteSpace(request.KomponenId);
        var komponen = _komponenTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Komponen with id: {request.KomponenId} not found");
        
        // BUILD
        var karcis = _factory.Load(request);
        karcis.AddKomponen(komponen, request.Nilai);
        
        // WRITER
        _ = _writer.Save(karcis);
        return Task.CompletedTask;
    }
}

public class KarcisAddKomponenHandlerTest
{
    private readonly Mock<IFactoryLoad<KarcisModel, IKarcisKey>> _factory;
    private readonly Mock<IKomponenTarifDal> _komponenTarifDal;
    private readonly Mock<IKarcisWriter> _writer;
    private readonly KarcisAddKomponenHandler _sut;

    public KarcisAddKomponenHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<KarcisModel, IKarcisKey>>();
        _komponenTarifDal = new Mock<IKomponenTarifDal>();
        _writer = new Mock<IKarcisWriter>();
        _sut = new KarcisAddKomponenHandler(_factory.Object, _komponenTarifDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        KarcisAddKomponenCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyKarcisId_ThenThrowArgumentException()
    {
        var request = new KarcisAddKomponenCommand("", "B", 10);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyKomponenId_ThenThrowArgumentException()
    {
        var request = new KarcisAddKomponenCommand("A", "", 10);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidKarcisId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisAddKomponenCommand("A", "B", 10);
        _factory.Setup(x => x.Load(It.IsAny<IKarcisKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenInvalidKomponenId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisAddKomponenCommand("A", "B", 10);
        _komponenTarifDal.Setup(x => x.GetData(It.IsAny<IKomponenKey>()))
            .Returns(null as KomponenModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}