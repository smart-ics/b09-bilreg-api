using Bilreg.Domain.BillContext.RoomChargeSub.KelasDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.RoomChargeSub.KelasDkAgg
{
    public record KelasDkGetQuery(string KelasDkId):IRequest<KelasDkGetResponse>,IKelasDkKey;
    public record KelasDkGetResponse
    (
        string KelasDkId,
        string KelasDkName
    );
    public class KelasDkGetHandler : IRequestHandler<KelasDkGetQuery, KelasDkGetResponse>
    {
        private readonly IKelasDkDal _kelasDkDal;
        public KelasDkGetHandler(IKelasDkDal kelasDal)
        {
            _kelasDkDal = kelasDal;
        }
        public Task<KelasDkGetResponse> Handle(KelasDkGetQuery request, CancellationToken cancellationToken)
        {
            var result = _kelasDkDal.GetData(request)
                ?? throw new KeyNotFoundException($"KelasDK id :{request.KelasDkId} not found");

            var response = new KelasDkGetResponse(
                result.KelasDkId, result.KelasDkName
                );
            
            return Task.FromResult(response);
        }
    }

    public class KelasDkGetHandlerTest
    {
        private readonly Mock<IKelasDkDal> _kelasdkDal;
        private readonly KelasDkGetHandler _sut;

        public KelasDkGetHandlerTest() {
            _kelasdkDal = new Mock<IKelasDkDal>();
            _sut = new KelasDkGetHandler(_kelasdkDal.Object);
        }

        [Fact]
        public async Task GivenInvalidKelasDkId_ThenThrowKeyNotException_Test()
        {
            //  ARRANGE
            var request = new KelasDkGetQuery("123");
            _kelasdkDal.Setup(x => x.GetData(It.IsAny<IKelasDkKey>()))
                .Returns(null as KelasDkModel);

            //  ACT
            Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidKelasDkId_ThenThrowKeyNotException_Test()
        {
            //  ARRANGE
            var expected = KelasDkModel.Create("A", "B");
            var request = new KelasDkGetQuery("A");
            _kelasdkDal.Setup(x => x.GetData(It.IsAny<IKelasDkKey>()))
                .Returns(expected);

            //  ACT
            var act = await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().BeEquivalentTo(new KelasDkGetResponse(expected.KelasDkId, expected.KelasDkName));
        }
    }
}
