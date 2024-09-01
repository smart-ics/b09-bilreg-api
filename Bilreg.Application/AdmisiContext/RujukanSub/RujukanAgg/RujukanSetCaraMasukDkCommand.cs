using Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
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
public record RujukanSetCaraMasukDkCommand(string RujukanId, string CaraMasukDkId) : IRequest, IRujukanKey, ICaraMasukDkKey;
public class RujukanSetCaraMasukDkHandler : IRequestHandler<RujukanSetCaraMasukDkCommand>
{
    private readonly ICaraMasukDkDal _caraMasukDkDal;
    private readonly IRujukanDal _rujukanDal;
    private readonly IRujukanWriter _writer;

    public RujukanSetCaraMasukDkHandler(ICaraMasukDkDal caraMasukDkDal, IRujukanDal rujukanDal, IRujukanWriter writer)
    {
        _caraMasukDkDal = caraMasukDkDal;
        _rujukanDal = rujukanDal;
        _writer = writer;
    }

    public Task Handle(RujukanSetCaraMasukDkCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.RujukanId);
        ArgumentException.ThrowIfNullOrEmpty(request.CaraMasukDkId);

        var existingRujukan = _rujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rujukan id {request.RujukanId} not found");

        var caraMasukDk = _caraMasukDkDal.GetData(request)
            ?? throw new KeyNotFoundException($"Cara masuk DK id {request.CaraMasukDkId} not found");

        // BUILD
        existingRujukan.SetCaraMasukDk(caraMasukDk);

        // WRITE
        _writer.Save(existingRujukan);

        return Task.CompletedTask;
    }
}
public class RujukanSetCaraMasukDkHandlerTest
{
    private readonly Mock<ICaraMasukDkDal> _caraMasukDkDal;
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly Mock<IRujukanWriter> _writer;
    private readonly RujukanSetCaraMasukDkHandler _sut;

    public RujukanSetCaraMasukDkHandlerTest()
    {
        _caraMasukDkDal = new Mock<ICaraMasukDkDal>();
        _rujukanDal = new Mock<IRujukanDal>();
        _writer = new Mock<IRujukanWriter>();
        _sut = new RujukanSetCaraMasukDkHandler(_caraMasukDkDal.Object, _rujukanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        RujukanSetCaraMasukDkCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetCaraMasukDkCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyCaraMasukDkId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetCaraMasukDkCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanSetCaraMasukDkCommand("A", "B");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenInvalidCaraMasukDkId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanSetCaraMasukDkCommand("A", "B");
        _caraMasukDkDal.Setup(x => x.GetData(It.IsAny<ICaraMasukDkKey>()))
            .Returns(null as CaraMasukDkModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}

