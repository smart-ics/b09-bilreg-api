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

public record KarcisRemoveKomponenCommand(string KarcisId, string KomponenId): IRequest, IKarcisKey, IKomponenKey;

public class KarcisRemoveKomponenHandler: IRequestHandler<KarcisRemoveKomponenCommand>
{
    private readonly IFactoryLoad<KarcisModel, IKarcisKey> _factory;
    private readonly IKarcisWriter _writer;

    public KarcisRemoveKomponenHandler(IFactoryLoad<KarcisModel, IKarcisKey> factory, IKarcisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(KarcisRemoveKomponenCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KarcisId);
        Guard.IsNotWhiteSpace(request.KomponenId);
        
        // BUILD
        var karcis = _factory.Load(request);
        karcis.RemoveKomponen(x => x.KomponenId == request.KomponenId);
        karcis.SetNilai();
        
        // WRITE
        _ = _writer.Save(karcis);
        return Task.CompletedTask;
    }
}

public class KarcisRemoveKomponenHandlerTest
{
    private readonly Mock<IFactoryLoad<KarcisModel, IKarcisKey>> _factory;
    private readonly Mock<IKarcisWriter> _writer;
    private readonly KarcisRemoveKomponenHandler _sut;

    public KarcisRemoveKomponenHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<KarcisModel, IKarcisKey>>();
        _writer = new Mock<IKarcisWriter>();
        _sut = new KarcisRemoveKomponenHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        KarcisRemoveKomponenCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyKarcisId_ThenThrowArgumentException()
    {
        var request = new KarcisRemoveKomponenCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyKomponenId_ThenThrowArgumentException()
    {
        var request = new KarcisRemoveKomponenCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidKarcisId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisRemoveKomponenCommand("A", "B");
        _factory.Setup(x => x.Load(It.IsAny<IKarcisKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}