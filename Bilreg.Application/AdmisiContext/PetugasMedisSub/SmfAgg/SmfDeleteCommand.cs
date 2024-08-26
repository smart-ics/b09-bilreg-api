using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SmfAgg;
public record SmfDeleteCommand(string SmfId) : IRequest, ISmfKey;

public class SmfDeleteHandler : IRequestHandler<SmfDeleteCommand>
{
    private readonly ISmfWriter _writer;

    public SmfDeleteHandler(ISmfWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(SmfDeleteCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SmfId);

        //  WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class SmfDeleteHandlerTest
{
    private readonly SmfDeleteHandler _sut;
    private readonly Mock<ISmfWriter> _writer;

    public SmfDeleteHandlerTest()
    {
        _writer = new Mock<ISmfWriter>();
        _sut = new SmfDeleteHandler(_writer.Object);
    }

    [Fact]
    public void GivenNullRequest_ThenThrowEx()
    {
        //  ACT
        var act = async () => await _sut.Handle(null!, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GivenEmptySmfId_ThenThrowEx()
    {
        //  ARRANGE
        var request = new SmfDeleteCommand("");

        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GivenValidRequest_ThenDeleteData()
    {
        //  ARRANGE
        var request = new SmfDeleteCommand("SMF0001");

        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().NotThrowAsync();
    }
}
