using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public record KecamatanDeleteCommand(string KecamatanId): IRequest, IKecamatanKey;

public class KecamatanDeleteHandler: IRequestHandler<KecamatanDeleteCommand>
{
    private readonly IKecamatanWriter _writer;

    public KecamatanDeleteHandler(IKecamatanWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(KecamatanDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KecamatanId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class KecamatanDeleteHandlerTest
{
    private readonly Mock<IKecamatanWriter> _writer;
    private readonly KecamatanDeleteHandler _sut;
    
    public KecamatanDeleteHandlerTest()
    {
        _writer = new Mock<IKecamatanWriter>();
        _sut = new KecamatanDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        KecamatanDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKecamatanId_ThenThrowArgumentException_Test()
    {
        var request = new KecamatanDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenDeleteData_Test()
    {
        var request = new KecamatanDeleteCommand("A");
        IKecamatanKey actual = null;
        _writer.Setup(x => x.Delete(It.IsAny<IKecamatanKey>()))
            .Callback<IKecamatanKey>(y => actual = y);
        await _sut.Handle(request, CancellationToken.None);
        actual?.KecamatanId.Should().Be("A");
    }
    
}