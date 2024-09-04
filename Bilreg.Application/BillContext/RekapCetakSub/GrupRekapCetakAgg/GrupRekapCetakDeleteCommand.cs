using Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.BillContext.RekapCetakSub.GrupRekapCetakAgg;
public record GrupRekapCetakDeleteCommand(string GrupRekapCetakId): IRequest, IGrupRekapCetakKey;
public class GrupRekapCetakDeleteHandler : IRequestHandler<GrupRekapCetakDeleteCommand>
{
    private readonly IGrupRekapCetakWriter _writer;

    public GrupRekapCetakDeleteHandler(IGrupRekapCetakWriter writer)
    {
        _writer = writer;
    }

    public Task Handle(GrupRekapCetakDeleteCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.GrupRekapCetakId);
        
        // WRITE
        _writer.Delete(request);
        return Task.CompletedTask;
    }
}

public class GrupRekapCetakDeleteHandlerTest
{
    private readonly Mock<IGrupRekapCetakWriter> _writer;
    private readonly GrupRekapCetakDeleteHandler _sut;

    public GrupRekapCetakDeleteHandlerTest()
    {
        _writer = new Mock<IGrupRekapCetakWriter>();
        _sut = new GrupRekapCetakDeleteHandler(_writer.Object);
    }

    [Fact]
    public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
    {
        GrupRekapCetakDeleteCommand request = null;
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenEmptyGrupJaminanId_ThenThrowArgumentException_Test()
    {
        var request = new GrupRekapCetakDeleteCommand("");
        var actual = async () => await _sut.Handle(request, CancellationToken.None);
        await actual.Should().ThrowAsync<ArgumentException>();
    }
}