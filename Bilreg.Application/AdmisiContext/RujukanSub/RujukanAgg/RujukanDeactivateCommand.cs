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
public record RujukanDeactivateCommand(string RujukanId) : IRequest, IRujukanKey;
public class RujukanDeactivateHandler : IRequestHandler<RujukanDeactivateCommand>
{
    private readonly IRujukanDal _rujukanDal;
    private readonly IRujukanWriter _writer;

    public RujukanDeactivateHandler(IRujukanDal rujukanDal, IRujukanWriter writer)
    {
        _rujukanDal = rujukanDal;
        _writer = writer;
    }

    public Task Handle(RujukanDeactivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RujukanId);

        var existingRujukan = _rujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rujukan id {request.RujukanId} not found");

        // BUILD
        existingRujukan.UnSetAktif();

        // WRITE
        _writer.Save(existingRujukan);
        return Task.CompletedTask;
    }
}
public class RujukanDeactivateHandlerTest
{
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly Mock<IRujukanWriter> _writer;
    private readonly RujukanDeactivateHandler _sut;

    public RujukanDeactivateHandlerTest()
    {
        _rujukanDal = new Mock<IRujukanDal>();
        _writer = new Mock<IRujukanWriter>();
        _sut = new RujukanDeactivateHandler(_rujukanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        RujukanDeactivateCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanDeactivateCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanDeactivateCommand("A");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
    {
        var request = new RujukanDeactivateCommand("A");
        var expected = RujukanModel.Create("A", "B");
        expected.UnSetAktif();
        RujukanModel actual = null;
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(expected);

        _writer.Setup(x => x.Save(It.IsAny<RujukanModel>()))
            .Callback<RujukanModel>(k => actual = k);

        await _sut.Handle(request, CancellationToken.None);
        actual?.IsAktif.Should().BeFalse();
    }
}

