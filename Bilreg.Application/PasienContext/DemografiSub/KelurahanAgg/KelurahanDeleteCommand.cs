using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;

public record KelurahanDeleteCommand(string KelurahanId): IRequest, IKelurahanKey;

public class KelurahanDeleteHandler: IRequestHandler<KelurahanDeleteCommand>
{
    private readonly IKelurahanWriter _writer;

    public KelurahanDeleteHandler(IKelurahanWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(KelurahanDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KelurahanId);

        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class KelurahanDeleteHandlerTest
{
    private readonly Mock<IKelurahanWriter> _writer;
    private readonly KelurahanDeleteHandler _sut;
    
    public KelurahanDeleteHandlerTest()
    {
        _writer = new Mock<IKelurahanWriter>();
        _sut = new KelurahanDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        KelurahanDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyKecamatanId_ThenThrowArgumentException_Test()
    {
        var request = new KelurahanDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenDeleteData_Test()
    {
        var request = new KelurahanDeleteCommand("1234567ABC");
        IKelurahanKey actual = null;
        _writer.Setup(x => x.Delete(It.IsAny<IKelurahanKey>()))
            .Callback<IKelurahanKey>(k => actual = k);
        await _sut.Handle(request, CancellationToken.None);
        actual?.KelurahanId.Should().BeEquivalentTo("1234567ABC");
    }
}

