using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
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
    public record KelasActivateCommand(string KelasId) : IRequest, IKelasKey;
    public class KelasActivateHandler : IRequestHandler<KelasActivateCommand>
    {
        private readonly IKelasDal _kelasDal;
        private readonly IKelasWriter _writer;

        public KelasActivateHandler(IKelasDal kelasDal, IKelasWriter writer)
        {
            _kelasDal = kelasDal;
            _writer = writer;
        }

        public Task Handle(KelasActivateCommand request, CancellationToken cancellationToken)
        {
            //  GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.KelasId);
            var statusKelas = _kelasDal.GetData(request)
                ?? throw new KeyNotFoundException($"Kelas id {request.KelasId} not found");

            //  BUILD
            statusKelas.SetAktif();

            //  WRITE
            _writer.Save(statusKelas);
            return Task.CompletedTask;
        }
    }
    public class KelasActivateHandlerTest
    {
        private readonly Mock<IKelasDal> _kelasDal;
        private readonly Mock<IKelasWriter> _writer;
        private readonly KelasActivateHandler _sut;

        public KelasActivateHandlerTest()
        {
            _kelasDal = new Mock<IKelasDal>();
            _writer = new Mock<IKelasWriter>();
            _sut = new KelasActivateHandler(_kelasDal.Object, _writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
        {
            KelasActivateCommand request = null;
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyKelasId_ThenThrowArgumentException_Test()
        {
            var request = new KelasActivateCommand("");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenInvalidKelas_ThenThrowKeyNotFoundException_Test()
        {
            var request = new KelasActivateCommand("A");
            _kelasDal.Setup(x => x.GetData(It.IsAny<IKelasKey>()))
                .Returns(null as KelasModel);

            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
        {
            var request = new KelasActivateCommand("A");
            var expected = KelasModel.Create("A", "B");
            expected.SetAktif();
            KelasModel actual = null;
            _kelasDal.Setup(x => x.GetData(It.IsAny<IKelasKey>()))
                .Returns(expected);

            _writer.Setup(x => x.Save(It.IsAny<KelasModel>()))
                .Callback<KelasModel>(k => actual = k);

            await _sut.Handle(request, CancellationToken.None);
            actual?.IsAktif.Should().BeTrue();
        }
    }
}
