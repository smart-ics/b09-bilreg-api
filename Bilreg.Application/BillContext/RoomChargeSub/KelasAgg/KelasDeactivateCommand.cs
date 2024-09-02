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
    public record KelasDeactivateCommand(string KelasId) : IRequest, IKelasKey;
    public class KelasDeadActivateHandler : IRequestHandler<KelasDeactivateCommand>
    {
        private readonly IKelasDal _kelasDal;
        private readonly IKelasWriter _writer;

        public KelasDeadActivateHandler(IKelasDal kelasDal, IKelasWriter writer)
        {
            _kelasDal = kelasDal;
            _writer = writer;
        }

        public Task Handle(KelasDeactivateCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.KelasId);

            var statusKelas = _kelasDal.GetData(request)
                ?? throw new KeyNotFoundException($"Kelas id {request.KelasId} not found");

            // BUILD
            statusKelas.UnSetAktif();

            // WRITE
            _writer.Save(statusKelas);
            return Task.CompletedTask;
        }
    }

    public class KelasDeadActivateHandlerTest
    {
        private readonly Mock<IKelasDal> _kelasDal;
        private readonly Mock<IKelasWriter> _writer;
        private readonly KelasDeadActivateHandler _sut;

        public KelasDeadActivateHandlerTest()
        {
            _kelasDal = new Mock<IKelasDal>();
            _writer = new Mock<IKelasWriter>();
            _sut = new KelasDeadActivateHandler(_kelasDal.Object, _writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
        {
            KelasDeactivateCommand request = null;
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyKelasId_ThenThrowArgumentException_Test()
        {
            var request = new KelasDeactivateCommand("");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenInvalidKelasId_ThenThrowKeyNotFoundException_Test()
        {
            var request = new KelasDeactivateCommand("A");
            _kelasDal.Setup(x => x.GetData(It.IsAny<IKelasKey>()))
                .Returns(null as KelasModel);

            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
        {
            var request = new KelasDeactivateCommand("A");
            var expected = KelasModel.Create("A", "B");
            expected.UnSetAktif();
            KelasModel actual = null;
            _kelasDal.Setup(x => x.GetData(It.IsAny<IKelasKey>()))
                .Returns(expected);

            _writer.Setup(x => x.Save(It.IsAny<KelasModel>()))
                .Callback<KelasModel>(k => actual = k);

            await _sut.Handle(request, CancellationToken.None);
            actual?.IsAktif.Should().BeFalse();
        }
    }
}
