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
public record RujukanSetAlamatCommand(string RujukanId, string Alamat, string Alamat2, string Kota) : IRequest, IRujukanKey;
public class RujukanSetAlamatHandler : IRequestHandler<RujukanSetAlamatCommand>
{
    private readonly IRujukanDal _rujukanDal;
    private readonly IRujukanWriter _writer;

    public RujukanSetAlamatHandler(IRujukanDal rujukanDal, IRujukanWriter writer)
    {
        _rujukanDal = rujukanDal;
        _writer = writer;
    }

    public Task Handle(RujukanSetAlamatCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.RujukanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Alamat);

        var existingRujukan = _rujukanDal.GetData(request)
            ?? throw new KeyNotFoundException($"Rujukan id {request.RujukanId} not found");

        // BUILD
        existingRujukan.SetAlamat(request.Alamat, request.Alamat2, request.Kota);

        // WRITE
        _writer.Save(existingRujukan);
        return Task.CompletedTask;
    }
}
public class RujukanSetAlamatHandlerTest
{
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly Mock<IRujukanWriter> _writer;
    private readonly RujukanSetAlamatHandler _sut;

    public RujukanSetAlamatHandlerTest()
    {
        _rujukanDal = new Mock<IRujukanDal>();
        _writer = new Mock<IRujukanWriter>();
        _sut = new RujukanSetAlamatHandler(_rujukanDal.Object, _writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        RujukanSetAlamatCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetAlamatCommand("", "B", "C", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenEmptyAlamat1_ThenThrowArgumentException_Test()
    {
        var request = new RujukanSetAlamatCommand("A", "", "C", "D");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
    {
        var request = new RujukanSetAlamatCommand("A", "B", "C", "D");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);

        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GivenValidRequest_ThenUpdateRujukanAlamat_Test()
    {
        var request = new RujukanSetAlamatCommand("A", "B", "C", "D");
        var existingRujukan = RujukanModel.Create("A", "OldName");
        RujukanModel actual = null;

        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(existingRujukan);

        _writer.Setup(x => x.Save(It.IsAny<RujukanModel>()))
            .Callback<RujukanModel>(k => actual = k);

        await _sut.Handle(request, CancellationToken.None);
        actual.Should().NotBeNull();
        actual.Alamat.Should().Be("B");
        actual.Alamat2.Should().Be("C");
        actual.Kota.Should().Be("D");
    }
}
