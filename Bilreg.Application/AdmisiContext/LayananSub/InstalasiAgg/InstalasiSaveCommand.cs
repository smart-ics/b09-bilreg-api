using Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg
{
    public record InstalasiSaveCommand(
        string InstalasiId,
        string InstalasiName,
        string InstalasiDkId) : IRequest, IInstalasiDkKey;

    public class InstalasiSaveHandler : IRequestHandler<InstalasiSaveCommand>
    {
        private readonly IInstalasiWriter _writer;
        private readonly IInstalasiDkDal _instalasiDkDal;


        public InstalasiSaveHandler(IInstalasiWriter writer, IInstalasiDkDal instalasiDkDal)
        {
            _writer = writer;
            _instalasiDkDal = instalasiDkDal;
        }

        public Task Handle(InstalasiSaveCommand request, CancellationToken cancellationToken)
        {
            // Guard 
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.InstalasiId);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.InstalasiName);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.InstalasiDkId);
            var instalasiDk = _instalasiDkDal.GetData(request)
                ?? throw new KeyNotFoundException($"InstalasiDk id: {request.InstalasiDkId} not found");


            //  Build
            var instalasi = InstalasiModel.Create(request.InstalasiId, request.InstalasiName);
            instalasi.Set(instalasiDk);

            //  Write
            _writer.Save(instalasi);
            return Task.CompletedTask;
        }
    }

    public class InstalasiSaveHandlerTest
    {
        private readonly Mock<IInstalasiDkDal> _instalasiDkDal;

        private readonly Mock<IInstalasiWriter> _writer;
        private readonly InstalasiSaveHandler _sut;

        public InstalasiSaveHandlerTest()
        {
            _instalasiDkDal = new Mock<IInstalasiDkDal>();
            _writer = new Mock<IInstalasiWriter>();
            _sut = new InstalasiSaveHandler(_writer.Object, _instalasiDkDal.Object);
        }

        [Fact]
        public async Task GivenNullRequest_ThenThrowNullArgumentException_Test()
        {
            InstalasiSaveCommand request = null;
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GivenEmptyInstalasiId_ThenThrowArgumentException_Test()
        {
            var request = new InstalasiSaveCommand(" ", "B", "C");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptyInstalasiName_ThenThrowArgumentException_Test()
        {
            var request = new InstalasiSaveCommand("A", " ", "C");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenEmptyInstalasiIDkId_ThenThrowArgumentException_Test()
        {
            var request = new InstalasiSaveCommand("A", "B", " ");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GivenInvalidInstalasiId_ThenThrowKeyNotFoundException_Test()
        {
            var request = new InstalasiSaveCommand("A1", "B", "1");
            _instalasiDkDal.Setup(x => x.GetData(It.IsAny<IInstalasiDkKey>()))
                .Returns(null as InstalasiDkModel);

            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenCreateExpectedObject_Test()
        {
            var expected = InstalasiModel.Create("01", "Rawat Jalan");
            expected.Set(InstalasiDkModel.Create("2", "Instalasi1"));
            var request = new InstalasiSaveCommand("01", "Rawat Jalan", "2");
            InstalasiModel actual = null;

            _instalasiDkDal.Setup(x => x.GetData(It.IsAny<IInstalasiDkKey>()))
                .Returns(InstalasiDkModel.Create("2", "Instalasi1"));
            _writer.Setup(x => x.Save(It.IsAny<InstalasiModel>()))
                .Callback((InstalasiModel k) => actual = k);

            await _sut.Handle(request, CancellationToken.None);

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GivenInvalidCharInstalasiId_ThenThrowArgumentException_Test()
        {
            var request = new InstalasiSaveCommand("17", "BAGIAN PENERBANGAN", "4");
            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
