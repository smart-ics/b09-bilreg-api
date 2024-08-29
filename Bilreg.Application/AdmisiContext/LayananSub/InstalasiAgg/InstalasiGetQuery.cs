using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiAgg
{
    public record InstalasiGetQuery(string InstalasiId) : IRequest<InstalasiGetResponse>, IInstalasiKey;

    public record InstalasiGetResponse
    (
        string InstalasiId,
        string InstalasiName,
        string InstalasiDkId,
        string InstalasiDkName);

    public class InstalasiGetHandler : IRequestHandler<InstalasiGetQuery, InstalasiGetResponse>
    {
        private readonly IInstalasiDal _instalasiDal;
        public InstalasiGetHandler(IInstalasiDal instalasiDal)
        {
            _instalasiDal = instalasiDal;
        }

        public Task<InstalasiGetResponse> Handle(InstalasiGetQuery request, CancellationToken cancellationToken)
        {
            var result = _instalasiDal.GetData(request)
                ?? throw new KeyNotFoundException($"Instalasi id: {request.InstalasiId} not found");

            var response = new InstalasiGetResponse(
                result.InstalasiId, result.InstalasiName,
                result.InstalasiDkId, result.InstalasiDkName);

            return Task.FromResult(response);
        }
    }
    public class InstalasiGetHandlerTest
    {
        private readonly Mock<IInstalasiDal> _instalasiDal;
        private readonly InstalasiGetHandler _sut;

        public InstalasiGetHandlerTest()
        {
            _instalasiDal = new Mock<IInstalasiDal>();
            _sut = new InstalasiGetHandler(_instalasiDal.Object);
        }

        [Fact]
        public async Task GivenInvalidInstalasiId_ThenThrowKeyNotFoundException_Test()
        {
            var request = new InstalasiGetQuery("A");
            _instalasiDal.Setup(x => x.GetData(It.IsAny<IInstalasiKey>()))
                .Returns(null as InstalasiModel);

            var actual = async () => await _sut.Handle(request, CancellationToken.None);
            await actual.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidInstalasiId_ThenReturnExpected_Test()
        {
            var request = new InstalasiGetQuery("A");
            var expected = InstalasiModel.Create("A", "B");
            expected.Set(InstalasiDkModel.Create("C", "D"));
            _instalasiDal.Setup(x => x.GetData(It.IsAny<IInstalasiKey>()))
                .Returns(expected);

            var actual = await _sut.Handle(request, CancellationToken.None);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}