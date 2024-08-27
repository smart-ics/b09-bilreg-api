using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg;
public record SatuanTugasDeleteCommand(string SatuanTugasId) : IRequest, ISatuanTugasKey;

public class SatuanTugasDeleteHandler : IRequestHandler<SatuanTugasDeleteCommand>
{
    private readonly ISatuanTugasWriter _writer;

    public SatuanTugasDeleteHandler(ISatuanTugasWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(SatuanTugasDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SatuanTugasId);

        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}
public class SatuanTugasDeleteHandlerTest
{
    private readonly SatuanTugasDeleteHandler _sut;
    private readonly Mock<ISatuanTugasWriter> _writer;

    public SatuanTugasDeleteHandlerTest()
    {
        _writer = new Mock<ISatuanTugasWriter>();
        _sut = new SatuanTugasDeleteHandler(_writer.Object);
    }

    [Fact]
    public void GivenNullRequest_ThenThrowEx()
    {
        // ACT
        var act = async () => await _sut.Handle(null!, CancellationToken.None);

        // ASSERT
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void GivenEmptySatuanTugasId_ThenThrowEx()
    {
        // ARRANGE
        var request = new SatuanTugasDeleteCommand("");

        // ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GivenValidRequest_ThenDeleteData()
    {
        // ARRANGE
        var request = new SatuanTugasDeleteCommand("ST0001");

        // ACT
        var act = async () => await _sut.Handle(request, CancellationToken.None);

        // ASSERT
        act.Should().NotThrowAsync();
    }
}
