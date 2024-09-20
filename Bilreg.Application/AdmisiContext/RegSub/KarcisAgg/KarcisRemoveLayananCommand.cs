using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisRemoveLayananCommand(string KarcisId, string LayananId): IRequest, IKarcisKey, ILayananKey;

public class KarcisRemoveLayananHandler: IRequestHandler<KarcisRemoveLayananCommand>
{
    private readonly IFactoryLoad<KarcisModel, IKarcisKey> _factory;
    private readonly IKarcisWriter _writer;

    public KarcisRemoveLayananHandler(IFactoryLoad<KarcisModel, IKarcisKey> factory, IKarcisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(KarcisRemoveLayananCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KarcisId);
        Guard.IsNotWhiteSpace(request.LayananId);
        
        // BUILD
        var karcis = _factory.Load(request);
        karcis.RemoveLayanan(x => x.LayananId == request.LayananId);
        
        // WRITE
        _ = _writer.Save(karcis);
        return Task.CompletedTask;
    }
}

public class KarcisRemoveLayananHandlerTest
{
    private readonly Mock<IFactoryLoad<KarcisModel, IKarcisKey>> _factory;
    private readonly Mock<IKarcisWriter> _writer;
    private readonly KarcisRemoveLayananHandler _sut;

    public KarcisRemoveLayananHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<KarcisModel, IKarcisKey>>();
        _writer = new Mock<IKarcisWriter>();
        _sut = new KarcisRemoveLayananHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        KarcisRemoveLayananCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyKarcisId_ThenThrowArgumentException()
    {
        var request = new KarcisRemoveLayananCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenEmptyLayananId_ThenThrowArgumentException()
    {
        var request = new KarcisRemoveLayananCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
    
    [Fact]
    public async Task GivenInvalidKarcisId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisRemoveLayananCommand("A", "B");
        _factory.Setup(x => x.Load(It.IsAny<IKarcisKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}