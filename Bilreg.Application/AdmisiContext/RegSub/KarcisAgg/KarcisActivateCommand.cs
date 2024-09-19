using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public record KarcisActivateCommand(string KarcisId): IRequest, IKarcisKey;

public class KarcisActivateHandler: IRequestHandler<KarcisActivateCommand>
{
    private readonly IFactoryLoad<KarcisModel, IKarcisKey> _factory;
    private readonly IKarcisWriter _writer;

    public KarcisActivateHandler(IFactoryLoad<KarcisModel, IKarcisKey> factory, IKarcisWriter writer)
    {
        _factory = factory;
        _writer = writer;
    }

    public Task Handle(KarcisActivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.KarcisId);
        
        // BUILD
        var karcis = _factory.Load(request);
        karcis.Activate();
        
        // WRITE
        _ = _writer.Save(karcis);
        return Task.CompletedTask;
    }
}

public class KarcisActivateHandlerTest
{
    private readonly Mock<IFactoryLoad<KarcisModel, IKarcisKey>> _factory;
    private readonly Mock<IKarcisWriter> _writer;
    private readonly KarcisActivateHandler _sut;

    public KarcisActivateHandlerTest()
    {
        _factory = new Mock<IFactoryLoad<KarcisModel, IKarcisKey>>();
        _writer = new Mock<IKarcisWriter>();
        _sut = new KarcisActivateHandler(_factory.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException()
    {
        KarcisActivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKarcisId_ThenThrowArgumentException()
    {
        var request = new KarcisActivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidKarcisId_ThenThrowKeyNotFoundException()
    {
        var request = new KarcisActivateCommand("A");
        _factory.Setup(x => x.Load(It.IsAny<IKarcisKey>()))
            .Throws<KeyNotFoundException>();
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}