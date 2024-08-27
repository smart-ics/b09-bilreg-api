using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public record InstalasiDkGetQuery(string InstalasiDkId) : IRequest<InstalasiDkGetResponse>, IInstalasiDkKey;
    public record InstalasiDkGetResponse(string InstalasiDkId, string InstalasiDkName);

    public class InstalasiDkGetHandler : IRequestHandler<InstalasiDkGetQuery, InstalasiDkGetResponse>
    {
        private readonly IInstalasiDkDal _instalasiDkDal;

        public InstalasiDkGetHandler(IInstalasiDkDal instalasiDkDal)
        {
            _instalasiDkDal = instalasiDkDal;
        }

        public Task<InstalasiDkGetResponse> Handle(InstalasiDkGetQuery request, CancellationToken cancellationToken)
        {
            //  QUERY
            var result = _instalasiDkDal.GetData(request)
                ?? throw new KeyNotFoundException($"InstalasiDk not found: {request.InstalasiDkId}");

            //  RESPONSE
            var response = new InstalasiDkGetResponse(result.InstalasiDkId, result.InstalasiDkName);
            return Task.FromResult(response);
        }
    }

    public class InstalasiDkGetHandlerTest
    {
        private readonly Mock<IInstalasiDkDal> _instalasiDkDal;
        private readonly InstalasiDkGetHandler _sut;


        public InstalasiDkGetHandlerTest()
        {
            _instalasiDkDal = new Mock<IInstalasiDkDal>();
            _sut = new InstalasiDkGetHandler(_instalasiDkDal.Object);
        }

        [Fact]
        public void GivenInvalidSmfId_ThenThrowKeyNotFoundException()
        {
            //  ARRANGE
            var request = new InstalasiDkGetQuery("123");
            _instalasiDkDal.Setup(x => x.GetData(It.IsAny<IInstalasiDkKey>()))
                .Returns(null as InstalasiDkModel);

            //  ACT
            Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidSmfId_ThenReturnExpected()
        {
            //  ARRANGE
            var expected = InstalasiDkModel.Create("A", "B");
            var request = new InstalasiDkGetQuery("A");
            _instalasiDkDal.Setup(x => x.GetData(It.IsAny<IInstalasiDkKey>()))
                .Returns(expected);

            //  ACT
            var act = await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().BeEquivalentTo(new InstalasiDkGetResponse(expected.InstalasiDkId, expected.InstalasiDkName));
        }
    }
}
