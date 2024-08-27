using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg
{
    public record SatuanTugasSaveCommand(string SatuanTugasId, string SatuanTugasName, bool IsMedis) : IRequest, ISatuanTugasKey;

    public class SatuanTugasSaveHandler : IRequestHandler<SatuanTugasSaveCommand>
    {
        private readonly ISatuanTugasWriter _writer;

        public SatuanTugasSaveHandler(ISatuanTugasWriter writer)
        {
            _writer = writer;
        }

        public Task Handle(SatuanTugasSaveCommand request, CancellationToken cancellationToken)
        {
            // GUARD
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SatuanTugasId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SatuanTugasName);

            // BUILD
            var satuanTugas = SatuanTugasModel.Create(request.SatuanTugasId, request.SatuanTugasName, request.IsMedis);

            // WRITE
            _writer.Save(satuanTugas);
            return Task.CompletedTask;
        }
    }

    public class SatuanTugasSaveHandlerTest
    {
        private SatuanTugasSaveHandler _sut;
        private Mock<ISatuanTugasWriter> _writer;

        public SatuanTugasSaveHandlerTest()
        {
            _writer = new Mock<ISatuanTugasWriter>();
            _sut = new SatuanTugasSaveHandler(_writer.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowEx()
        {
            // ARRANGE
            SatuanTugasSaveCommand request = null;
            // ACT
            var act = async () => await _sut.Handle(null!, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptySatuanTugasId_ThenThrowEx()
        {
            // ARRANGE
            var request = new SatuanTugasSaveCommand("", "Satuan Tugas", true);

            // ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptySatuanTugasName_ThenThrowEx()
        {
            // ARRANGE
            var request = new SatuanTugasSaveCommand("Id", "", true);

            // ACT
            var act = async () => await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenValidData_ThenSaveSatuanTugas()
        {
            // ARRANGE
            var request = new SatuanTugasSaveCommand("Id", "Satuan Tugas", true);

            // ACT
            await _sut.Handle(request, CancellationToken.None);

            // ASSERT
            _writer.Verify(w => w.Save(It.Is<SatuanTugasModel>(m => m.SatuanTugasId == "Id" && m.SatuanTugasName == "Satuan Tugas" && m.IsMedis == true)), Times.Once);
        }
    }
}

