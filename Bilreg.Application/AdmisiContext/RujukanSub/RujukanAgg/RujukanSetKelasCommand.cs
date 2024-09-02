using Bilreg.Application.AdmisiContext.RujukanSub.KelasRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
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
public record RujukanSetKelasRujukanCommand(string RujukanId, string KelasRujukanId) : IRequest, IRujukanKey, IKelasRujukanKey;
public class RujukanSetKelasRujukanHandler : IRequestHandler<RujukanSetKelasRujukanCommand>
{
    private readonly IKelasRujukanDal _kelasRujukanDal;
    private readonly IRujukanDal _rujukanDal;
    private readonly IRujukanWriter _writer;

    public RujukanSetKelasRujukanHandler(IKelasRujukanDal kelasRujukanDal, IRujukanDal rujukanDal, IRujukanWriter writer)
    {
        _kelasRujukanDal = kelasRujukanDal;
        _rujukanDal = rujukanDal;
        _writer = writer;
    }

    public Task Handle(RujukanSetKelasRujukanCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.RujukanId);
        ArgumentException.ThrowIfNullOrEmpty(request.KelasRujukanId);

        var existingRujukan = _rujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rujukan id {request.RujukanId} not found");

        var kelasRujukan = _kelasRujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kelas rujukan id {request.KelasRujukanId} not found");

        // BUILD
        existingRujukan.SetKelasRujukan(kelasRujukan);

        // WRITE
        _writer.Save(existingRujukan);

        return Task.CompletedTask;
    }
}
public class RujukanSetKelasRujukanHandlerTest
{
    private readonly Mock<IKelasRujukanDal> _kelasRujukanDal;
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly Mock<IRujukanWriter> _writer;
    private readonly RujukanSetKelasRujukanHandler _sut;

    public RujukanSetKelasRujukanHandlerTest()
    {
        _kelasRujukanDal = new Mock<IKelasRujukanDal>();
        _rujukanDal = new Mock<IRujukanDal>();
        _writer = new Mock<IRujukanWriter>();
        _sut = new RujukanSetKelasRujukanHandler(_kelasRujukanDal.Object, _rujukanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        RujukanSetKelasRujukanCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetKelasRujukanCommand("", "B");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyKelasRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetKelasRujukanCommand("A", "");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanSetKelasRujukanCommand("A", "B");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenInvalidKelasRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanSetKelasRujukanCommand("A", "B");
        _kelasRujukanDal.Setup(x => x.GetData(It.IsAny<IKelasRujukanKey>()))
            .Returns(null as KelasRujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }
}

