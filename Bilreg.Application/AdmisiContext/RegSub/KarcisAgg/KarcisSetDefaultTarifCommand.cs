using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Application.BillContext.TindakanSub.TarifAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.BillContext.TindakanSub.TarifAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisSetDefaultTarifCommand(string KarcisId, string TarifId): IRequest, IKarcisKey, ITarifKey;

public class KarcisSetDefaultTarifHandler: IRequestHandler<KarcisSetDefaultTarifCommand>
{
    private readonly IFactoryLoad<KarcisModel, IKarcisKey> _factory;
    private readonly ITarifDal _tarifDal;
    private readonly IKarcisWriter _writer;

    public KarcisSetDefaultTarifHandler(IFactoryLoad<KarcisModel, IKarcisKey> factory, ITarifDal tarifDal, IKarcisWriter writer)
    {
        _factory = factory;
        _tarifDal = tarifDal;
        _writer = writer;
    }

    public Task Handle(KarcisSetDefaultTarifCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KarcisId);
        Guard.IsNotWhiteSpace(request.TarifId);
        var tarif = _tarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tarif with id: {request.TarifId} not found");
        
        // BUILD
        var karcis = _factory.Load(request);
        karcis.SetTarif(tarif);
        
        // WRITE
        _ = _writer.Save(karcis);
        return Task.CompletedTask;
    }
}

public class KarcisSetDefaultTarifHandlerTest
{
    private readonly Mock<IFactoryLoad<KarcisModel, IKarcisKey>> _factory;
    private readonly Mock<ITarifDal> _tarifDal;
    private readonly Mock<IKarcisWriter> _writer;
    private readonly KarcisSetDefaultTarifHandler _sut;

    public KarcisSetDefaultTarifHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<KarcisModel, IKarcisKey>>();
        _tarifDal = new Mock<ITarifDal>();
        _writer = new Mock<IKarcisWriter>();
        _sut = new KarcisSetDefaultTarifHandler(_factory.Object, _tarifDal.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        KarcisSetDefaultTarifCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyKarcisId_ThenThrowArgumentException()
    {
        var request = new KarcisSetDefaultTarifCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyTarifId_ThenThrowArgumentException()
    {
        var request = new KarcisSetDefaultTarifCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidKarcisId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisSetDefaultTarifCommand("A", "B");
        _factory.Setup(x => x.Load(It.IsAny<IKarcisKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenInvalidTarifId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisSetDefaultTarifCommand("A", "B");
        _tarifDal.Setup(x => x.GetData(It.IsAny<ITarifKey>()))
            .Returns(null as TarifModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}