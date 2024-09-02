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
        // Arrange
        RujukanSetAlamatCommand request = null;

        // Act
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // Assert
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
    {
        // Arrange
        var request = new RujukanSetAlamatCommand("", "Alamat", "Alamat2", "Kota");

        // Act
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // Assert
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Value cannot be null or empty. (Parameter 'RujukanId')");
    }

    [Fact]
    public async Task GivenEmptyAlamat_ThenThrowArgumentException_Test()
    {
        // Arrange
        var request = new RujukanSetAlamatCommand("RujukanId", "", "Alamat2", "Kota");

        // Act
        var actual = async () => await _sut.Handle(request, CancellationToken.None);

        // Assert
        await actual.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Value cannot be null or whitespace. (Parameter 'Alamat')");
    }
}
