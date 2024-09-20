using Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisSaveCommand(string KarcisId, string KarcisName, string InstalasiDkId, string RekapCetakId):
    IRequest, IKarcisKey, IInstalasiDkKey, IRekapCetakKey;
    
public class KarcisSaveHandler: IRequestHandler<KarcisSaveCommand>
{
    private readonly IInstalasiDkDal _instalasiDkDal;
    private readonly IRekapCetakDal _rekapCetakDal;
    private readonly IFactoryLoadOrNull<KarcisModel, IKarcisKey> _factory;
    private readonly IKarcisWriter _writer;
    
    public KarcisSaveHandler(IInstalasiDkDal instalasiDkDal, IRekapCetakDal rekapCetakDal, IFactoryLoadOrNull<KarcisModel, IKarcisKey> factory, IKarcisWriter writer)
    {
        _instalasiDkDal = instalasiDkDal;
        _rekapCetakDal = rekapCetakDal;
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(KarcisSaveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KarcisId);
        Guard.IsNotWhiteSpace(request.KarcisName);
        Guard.IsNotWhiteSpace(request.InstalasiDkId);
        Guard.IsNotWhiteSpace(request.RekapCetakId);
        var instalasiDk = _instalasiDkDal.GetData(request)
            ?? throw new KeyNotFoundException($"Instalasi Dk with id: {request.InstalasiDkId} not found");
        var rekapCetak = _rekapCetakDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rekap cetak with id: {request.InstalasiDkId} not found");
        
        // BUILD
        var karcis = _factory.LoadOrNull(request)
            ?? new KarcisModel(request.KarcisId, request.KarcisName);
        karcis.SetInstalasiDk(instalasiDk);
        karcis.SetRekapCetak(rekapCetak);
        
        // WRITE
        _ = _writer.Save(karcis);
        return Task.CompletedTask;
    }
}

public class KarcisSaveHandlerTest
{
    private readonly Mock<IInstalasiDkDal> _instalasiDkDal;
    private readonly Mock<IRekapCetakDal> _rekapCetakDal;
    private readonly Mock<IFactoryLoadOrNull<KarcisModel, IKarcisKey>> _factory;
    private readonly Mock<IKarcisWriter> _writer;
    private readonly KarcisSaveHandler _sut;

    public KarcisSaveHandlerTest()
    {
        _instalasiDkDal = new Mock<IInstalasiDkDal>();
        _rekapCetakDal = new Mock<IRekapCetakDal>();
        _factory = new Mock<IFactoryLoadOrNull<KarcisModel, IKarcisKey>>();
        _writer = new Mock<IKarcisWriter>();
        _sut = new KarcisSaveHandler(_instalasiDkDal.Object, _rekapCetakDal.Object, _factory.Object, _writer.Object);
    }
    
    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        KarcisSaveCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyKarcisId_ThenThrowArgumentException()
    {
        var request = new KarcisSaveCommand("", "B", "C", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyKarcisName_ThenThrowArgumentException()
    {
        var request = new KarcisSaveCommand("A", "", "C", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyInstalasiDkId_ThenThrowArgumentException()
    {
        var request = new KarcisSaveCommand("A", "B", "", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyRekapCetakId_ThenThrowArgumentException()
    {
        var request = new KarcisSaveCommand("A", "B", "C", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidInstalasiDkId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisSaveCommand("A", "B", "C", "D");
        _instalasiDkDal.Setup(x => x.GetData(It.IsAny<IInstalasiDkKey>()))
            .Returns(null as InstalasiDkModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
    
    [Fact]
    public async Task GivenInvalidRekapCetakId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisSaveCommand("A", "B", "C", "D");
        _rekapCetakDal.Setup(x => x.GetData(It.IsAny<IRekapCetakKey>()))
            .Returns(null as RekapCetakModel);
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}