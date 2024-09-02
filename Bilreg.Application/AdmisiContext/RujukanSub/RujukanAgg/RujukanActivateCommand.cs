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
public record RujukanActivateCommand(string RujukanId) : IRequest, IRujukanKey;

public class RujukanActivateHandler : IRequestHandler<RujukanActivateCommand>
{
    private readonly IRujukanDal _rujukanDal;
    private readonly IRujukanWriter _writer;

    public RujukanActivateHandler(IRujukanDal rujukanDal, IRujukanWriter writer)
    {
        _rujukanDal = rujukanDal;
        _writer = writer;
    }

    public Task Handle(RujukanActivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RujukanId);

        var existingRujukan = _rujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rujukan id {request.RujukanId} not found");

        // BUILD
        existingRujukan.SetAktif();

        // WRITE
        _writer.Save(existingRujukan);
        return Task.CompletedTask;
    }
}
public class RujukanActivateHandlerTest
{
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly Mock<IRujukanWriter> _writer;
    private readonly RujukanActivateHandler _sut;

    public RujukanActivateHandlerTest()
    {
        _rujukanDal = new Mock<IRujukanDal>();
        _writer = new Mock<IRujukanWriter>();
        _sut = new RujukanActivateHandler(_rujukanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        RujukanActivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanActivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanActivateCommand("A");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var request = new RujukanActivateCommand("A");
        var expected = RujukanModel.Create("A", "B");
        expected.SetAktif();
        RujukanModel actual = null;
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(expected);

        _writer.Setup(x => x.Save(It.IsAny<RujukanModel>()))
            .Callback<RujukanModel>(k => actual = k);

        await _sut.Handle(request, CancellationToken.None);
        actual?.IsAktif.Should().BeTrue();
    }
}

