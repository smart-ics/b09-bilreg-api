using Bilreg.Domain.AdmPasienContext.KabupatenAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.KabupatenAgg;

public record KabupatenDeleteCommand(string KabupatenId) : IRequest, IKabupatenKey;

public class KabupatenDeleteHandler : IRequestHandler<KabupatenDeleteCommand>
{
    private readonly IKabupatenWriter _writer;
    public KabupatenDeleteHandler(IKabupatenWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(KabupatenDeleteCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.KabupatenId);

        //  WRITE
        _writer.Delete(request);
        return Task.CompletedTask;    }
}

public class KabupatenDeleteHandlerTest
{
    private readonly KabupatenDeleteHandler _sut;
    private readonly Mock<IKabupatenWriter> _writer;
    
    public KabupatenDeleteHandlerTest()
    {
        _writer = new Mock<IKabupatenWriter>();
        _sut = new KabupatenDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThrowExeption()
    {
        KabupatenDeleteCommand cmd = null;
        var actual = async () => await _sut.Handle(cmd, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task GivenEmptyKabupatenId_ThrowExeption()
    {
        var cmd = new KabupatenDeleteCommand("");
        var actual = async () => await _sut.Handle(cmd, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidData_ThenDelete()
    {
        var cmd = new KabupatenDeleteCommand("A");
        IKabupatenKey actual = null;
        _writer.Setup(x => x.Delete(It.IsAny<IKabupatenKey>()))
            .Callback<IKabupatenKey>(y => actual = y);
        await _sut.Handle(cmd, CancellationToken.None);
        actual?.KabupatenId.Should().Be("A");
    }
}

