using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public record RujukanSetNoTelpCommand(string RujukanId, string NoTelp) : IRequest, IRujukanKey;

public class RujukanSetNoTelpHandler : IRequestHandler<RujukanSetNoTelpCommand>
{
    private readonly IRujukanDal _rujukanDal;
    private readonly IRujukanWriter _writer;

    public RujukanSetNoTelpHandler(IRujukanDal rujukanDal, IRujukanWriter writer)
    {
        _rujukanDal = rujukanDal;
        _writer = writer;
    }

    public Task Handle(RujukanSetNoTelpCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.RujukanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NoTelp);

        var existingRujukan = _rujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rujukan id {request.RujukanId} not found");

        // BUILD
        existingRujukan.SetNoTelp(request.NoTelp);

        // WRITE
        _writer.Save(existingRujukan);
        return Task.CompletedTask;
    }
}

public class RujukanSetNoTelpHandlerTest
{
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly Mock<IRujukanWriter> _writer;
    private readonly RujukanSetNoTelpHandler _sut;

    public RujukanSetNoTelpHandlerTest()
    {
        _rujukanDal = new Mock<IRujukanDal>();
        _writer = new Mock<IRujukanWriter>();
        _sut = new RujukanSetNoTelpHandler(_rujukanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        RujukanSetNoTelpCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetNoTelpCommand("", "08123456789");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyNoTelp_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetNoTelpCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanSetNoTelpCommand("A", "08123456789");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenUpdateRujukanNoTelp_Test()
    {
        var request = new RujukanSetNoTelpCommand("A", "08123456789");
        var existingRujukan = RujukanModel.Create("A", "Existing Name");
        RujukanModel actual = null;

        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(existingRujukan);

        _writer.Setup(x => x.Save(It.IsAny<RujukanModel>()))
            .Callback<RujukanModel>(r => actual = r);

        await _sut.Handle(request, CancellationToken.None);
        actual.Should().NotBeNull();
        actual.NoTelp.Should().Be(request.NoTelp);
    }
}
