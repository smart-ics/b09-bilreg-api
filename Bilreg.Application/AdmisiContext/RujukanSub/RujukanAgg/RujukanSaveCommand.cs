using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.TipeRujukanAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.ValidationHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public record RujukanSaveCommand(string RujukanId, string RujukanName) : IRequest, IRujukanKey;
public class RujukanSaveHandler : IRequestHandler<RujukanSaveCommand>
{
    private readonly IRujukanDal _rujukanDal;
    private readonly IRujukanWriter _writer;

    public RujukanSaveHandler(IRujukanDal rujukanDal, IRujukanWriter writer)
    {
        _rujukanDal = rujukanDal;
        _writer = writer;
    }
    public Task Handle(RujukanSaveCommand request, CancellationToken cancellationToken)
    {
        
        // GUARD 
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RujukanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RujukanName);

        // BUILD
        var rujukan = _rujukanDal.GetData(request)
            ?? RujukanModel.Create(request.RujukanId, request.RujukanName);
        rujukan.SetName(request.RujukanName);

        // WRITE
        _writer.Save(rujukan);
        return Task.CompletedTask;
    }
}
public class RujukanSaveHandlerTest
    {
        private readonly Mock<IRujukanDal> _rujukanDal;
        private readonly Mock<IRujukanWriter> _writer;
        private readonly RujukanSaveHandler _sut;

        public RujukanSaveHandlerTest()
        {
            _rujukanDal = new Mock<IRujukanDal>();
            _writer = new Mock<IRujukanWriter>();
            _sut = new RujukanSaveHandler(_rujukanDal.Object, _writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
        {
            RujukanSaveCommand request = null;

            var act = async () => await _sut.Handle(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyRujukanId_ThenThrowArgumentException_Test()
        {
            var request = new RujukanSaveCommand("", "ValidName");

            var act = async () => await _sut.Handle(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptyRujukanName_ThenThrowArgumentException_Test()
        {
            var request = new RujukanSaveCommand("ValidId", "");

            var act = async () => await _sut.Handle(request, CancellationToken.None);

            await act.Should().ThrowAsync<ArgumentException>();
        }
    }