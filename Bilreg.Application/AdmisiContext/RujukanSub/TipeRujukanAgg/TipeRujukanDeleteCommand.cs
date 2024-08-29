using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg;
public record TipeRujukanDeleteCommand(string TipeRujukanId) : IRequest, ITipeRujukanKey;
public class TipeRujukanDeleteHandler : IRequestHandler<TipeRujukanDeleteCommand>
{
    private readonly ITipeRujukanWriter _writer;

    public TipeRujukanDeleteHandler(ITipeRujukanWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(TipeRujukanDeleteCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TipeRujukanId);

        //  WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class TipeRujukanDeleteHandlerTest
{
    private readonly TipeRujukanDeleteHandler _sut;
    private readonly Mock<ITipeRujukanWriter> _writer;

    public TipeRujukanDeleteHandlerTest()
    {
        _writer = new Mock<ITipeRujukanWriter>();
        _sut = new TipeRujukanDeleteHandler(_writer.Object);
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
    public void GivenEmptyTipeRujukanId_ThenThrowEx()
    {
        //  ARRANGE
        var request = new TipeRujukanDeleteCommand("");

        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GivenValidRequest_ThenDeleteData()
    {
        //  ARRANGE
        var request = new TipeRujukanDeleteCommand("TR001");

        //  ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);

        //  ASSERT
        act.Should().NotThrowAsync();
    }
}

