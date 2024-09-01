using Bilreg.Application.AdmisiContext.RujukanSub.TipeRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
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

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public record RujukanSetTipeRujukanCommand(string RujukanId, string TipeRujukanId) : IRequest, IRujukanKey, ITipeRujukanKey;

public class RujukanSetTipeRujukanHandler : IRequestHandler<RujukanSetTipeRujukanCommand>
{
    private readonly ITipeRujukanDal _tipeRujukanDal;
    private readonly IRujukanDal _rujukanDal;
    private readonly IRujukanWriter _writer;

    public RujukanSetTipeRujukanHandler(ITipeRujukanDal tipeRujukanDal, IRujukanDal rujukanDal, IRujukanWriter writer)
    {
        _tipeRujukanDal = tipeRujukanDal;
        _rujukanDal = rujukanDal;
        _writer = writer;
    }

    public Task Handle(RujukanSetTipeRujukanCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.RujukanId);
        ArgumentException.ThrowIfNullOrEmpty(request.TipeRujukanId);

        var existingRujukan = _rujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rujukan id {request.RujukanId} not found");

        var tipeRujukan = _tipeRujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tipe rujukan id {request.TipeRujukanId} not found");

        // BUILD
        existingRujukan.SetTipeRujukan(tipeRujukan);

        // WRITE
        _writer.Save(existingRujukan);

        return Task.CompletedTask;
    }
}

public class RujukanSetTipeRujukanHandlerTest
{
    private readonly Mock<ITipeRujukanDal> _tipeRujukanDal;
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly Mock<IRujukanWriter> _writer;
    private readonly RujukanSetTipeRujukanHandler _sut;

    public RujukanSetTipeRujukanHandlerTest()
    {
        _tipeRujukanDal = new Mock<ITipeRujukanDal>();
        _rujukanDal = new Mock<IRujukanDal>();
        _writer = new Mock<IRujukanWriter>();
        _sut = new RujukanSetTipeRujukanHandler(_tipeRujukanDal.Object, _rujukanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        RujukanSetTipeRujukanCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetTipeRujukanCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyTipeRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetTipeRujukanCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanSetTipeRujukanCommand("A", "B");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenInvalidTipeRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanSetTipeRujukanCommand("A", "B");
        _tipeRujukanDal.Setup(x => x.GetData(It.IsAny<ITipeRujukanKey>()))
            .Returns(null as TipeRujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}


