using Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisAddLayananCommand(string KarcisId, string LayananId): IRequest, IKarcisKey, ILayananKey;

public class KarcisAddLayananHandler: IRequestHandler<KarcisAddLayananCommand>
{
    private readonly IFactoryLoad<KarcisModel, IKarcisKey> _factory;
    private readonly ILayananDal _layananDal;
    private readonly IKarcisWriter _writer;

    public KarcisAddLayananHandler(IFactoryLoad<KarcisModel, IKarcisKey> factory, ILayananDal layananDal, IKarcisWriter writer)
    {
        _factory = factory;
        _layananDal = layananDal;
        _writer = writer;
    }

    public Task Handle(KarcisAddLayananCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KarcisId);
        Guard.IsNotWhiteSpace(request.LayananId);
        var layanan = _layananDal.GetData(request)
            ?? throw new KeyNotFoundException($"Layanan with id: {request.LayananId} not found");
        
        // BUILD
        var karcis = _factory.Load(request);
        karcis.AddLayanan(layanan);
        
        // WRITE
        _ = _writer.Save(karcis);
        return Task.CompletedTask;
    }
}

public class KarcisAddLayananHandlerTest
{
    private readonly Mock<IFactoryLoad<KarcisModel, IKarcisKey>> _factory;
    private readonly Mock<ILayananDal> _layananDal;
    private readonly Mock<IKarcisWriter> _writer;
    private readonly KarcisAddLayananHandler _sut;

    public KarcisAddLayananHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<KarcisModel, IKarcisKey>>();
        _layananDal = new Mock<ILayananDal>();
        _writer = new Mock<IKarcisWriter>();
        _sut = new KarcisAddLayananHandler(_factory.Object, _layananDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        KarcisAddLayananCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyKarcisId_ThenThrowArgumentException()
    {
        var request = new KarcisAddLayananCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyLayananId_ThenThrowArgumentException()
    {
        var request = new KarcisAddLayananCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidKarcisId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisAddLayananCommand("A", "B");
        _factory.Setup(x => x.Load(It.IsAny<IKarcisKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenInvalidLayananId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisAddLayananCommand("A", "B");
        _layananDal.Setup(x => x.GetData(It.IsAny<ILayananKey>()))
            .Returns(null as LayananModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}