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
    public record KelasSaveCommand(string KelasId, string KelasName):IRequest,IKelasKey;

    public class KelasSaveHandler : IRequestHandler<KelasSaveCommand>
    {
        private readonly IKelasDal _kelasDal;
        private readonly IKelasWriter _writter;

        public KelasSaveHandler(IKelasDal kelasDal,IKelasWriter writer)
        {
            _kelasDal = kelasDal;
            _writter = writer;
            
        }

        public Task Handle(KelasSaveCommand request, CancellationToken cancellationToken)
        {
            //  GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.KelasId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.KelasName);

            //  BUILD
            var kelas = _kelasDal.GetData(request)
                ?? KelasModel.Create(request.KelasId, request.KelasName);
            kelas.SetName(request.KelasName);
            
            //  WRITE
            _writter.Save(kelas);
            return Task.CompletedTask;
        }
    }

    public class KelasSaveHandlerTest {
        private readonly Mock<IKelasDal> _kelasDal;
        private readonly Mock<IKelasWriter> _writer;
        private readonly KelasSaveHandler _sut;

        public KelasSaveHandlerTest()
        {
            _kelasDal = new Mock<IKelasDal>();
            _writer = new Mock<IKelasWriter>();
            _sut = new KelasSaveHandler(_kelasDal.Object, _writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowArgumentNullException_Test()
        {
            KelasSaveCommand request = null;
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyKelasId_ThenThrowArgumentException_Test()
        {
            var request = new KelasSaveCommand(" ", "B");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptyKelasName_ThenThrowArgumentException_Test()
        {
            var request = new KelasSaveCommand("A", "");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
        {
            var request = new KelasSaveCommand("A", "B");
            var expected = KelasModel.Create("A", "B");
            KelasModel actual = null;
            _writer.Setup(x => x.Save(It.IsAny<KelasModel>()))
                .Callback<KelasModel>(k => actual = k);
            await _sut.Handle(request, CancellationToken.None);
            actual.Should().BeEquivalentTo(expected);
        }
    }

}
