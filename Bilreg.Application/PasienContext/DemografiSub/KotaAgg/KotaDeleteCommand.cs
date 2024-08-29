using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public record KotaDeleteCommand(string KotaId): IRequest, IKotaKey;

public class KotaDeleteHandler: IRequestHandler<KotaDeleteCommand>
{
    private readonly IKotaWriter _writer;

    public KotaDeleteHandler(IKotaWriter writer)
    {
        _writer = writer;
    }
    
    public Task Handle(KotaDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KotaId);
        
        // WRITER
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class KotaDeleteHandlerTest
{
    private readonly Mock<IKotaWriter> _writer;
    private readonly KotaDeleteHandler _sut;

    public KotaDeleteHandlerTest()
    {
        _writer = new Mock<IKotaWriter>();
        _sut = new KotaDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        KotaDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyKotaId_ThenThrowArgumentException_Test()
    {
        var request = new KotaDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenDeleteData_Test()
    {
        var request = new KotaDeleteCommand("A");
        IKotaKey actual = null;
        _writer.Setup(x => x.Delete(It.IsAny<IKotaKey>()))
            .Callback<IKotaKey>(k => actual = k);
        await _sut.Handle(request, CancellationToken.None);
        actual?.KotaId.Should().BeEquivalentTo("A");
    }
}