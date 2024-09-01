using Bilreg.Application.BillContext.RoomChargeSub.KelasDkAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.KelasAgg
{
    public record KelasSetKelasDkCommand(string KelasId, string KelasDkId) : IRequest, IKelasKey, IKelasDkKey;
    public class KelasSetKelasDkHandler : IRequestHandler<KelasSetKelasDkCommand>
    {
        private readonly IKelasDal _kelasDal;
        private readonly IKelasDkDal _kelasDkDal;
        private readonly IKelasWriter _writer;

        public KelasSetKelasDkHandler(IKelasDal kelasDal, IKelasDkDal kelasDkDal, IKelasWriter writer)
        {
            _kelasDal = kelasDal;
            _kelasDkDal = kelasDkDal;
            _writer = writer;
        }

        

        public Task Handle(KelasSetKelasDkCommand request, CancellationToken cancellationToken)
        {
            // Guard
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.KelasId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.KelasDkId);
            
            // BUILD
            var kelas = _kelasDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kelas id {request.KelasId} not found");
            var kelasDk = _kelasDkDal.GetData(request)
            ?? throw new KeyNotFoundException($"Kelas dk id {request.KelasDkId} not found"); 

            kelas.Set(kelasDk);
            // WRITE
            _writer.Save(kelas);
            return Task.CompletedTask;
        }
    }

    public class KelasSetKelasDkHandlerTest
    {
        private readonly Mock<IKelasWriter> _writer;
        private readonly Mock<IKelasDal> _kelasDal;
        private readonly Mock<IKelasDkDal> _kelasDkDal;
        private readonly KelasSetKelasDkHandler _sut;

        public KelasSetKelasDkHandlerTest()
        {
            _writer = new Mock<IKelasWriter>();
            _kelasDal = new Mock<IKelasDal>();
            _kelasDkDal = new Mock<IKelasDkDal>();
            _sut = new KelasSetKelasDkHandler(_kelasDal.Object, _kelasDkDal.Object, _writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
        {
            KelasSetKelasDkCommand request = null;
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyKelasId_ThenThrowArgumentException_Test()
        {
            var request = new KelasSetKelasDkCommand("", "A");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }
    }
}
