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

namespace Bilreg.Application.BillContext.RoomChargeSub.KelasDkAgg
{
    public record KelasDkListQuery():IRequest<IEnumerable<KelasDkListResponse>>;
    public record KelasDkListResponse(string KelasDkId, string KelasDkName);
    public class KelasDkListHandler : IRequestHandler<KelasDkListQuery, IEnumerable<KelasDkListResponse>>
    {
        private readonly IKelasDkDal _kelasDkDal;
        public KelasDkListHandler(IKelasDkDal kelasDkDal)
        {
            _kelasDkDal = kelasDkDal;
        }
        public Task<IEnumerable<KelasDkListResponse>> Handle(KelasDkListQuery request, CancellationToken cancellationToken)
        {
            // Query
            var result = _kelasDkDal.ListData()
                ?? throw new KeyNotFoundException("KelasDk not Found");
            // Response
            var response = result.Select(x => new KelasDkListResponse(x.KelasDkId, x.KelasDkName));
            return Task.FromResult(response);
        }
    }

    public class KelasDkListHandlerTest
    {
        private readonly KelasDkListHandler _sut;
        private readonly Mock<IKelasDkDal> _kelasDkDal;

        public KelasDkListHandlerTest()
        {
            _kelasDkDal = new Mock<IKelasDkDal>();
            _sut = new KelasDkListHandler(_kelasDkDal.Object);
        }

        [Fact]
        public void GivenNoData_ThenThrowKeyNotFoundException()
        {
            //  ARRANGE
            var request = new KelasDkListQuery();
            _kelasDkDal.Setup(x => x.ListData())
                .Returns(null as IEnumerable<KelasDkModel>);

            //  ACT
            Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenReturnExpected()
        {
            //  ARRANGE
            var expected = new List<KelasDkModel> { KelasDkModel.Create("A", "B") };
            var request = new KelasDkListQuery();
            _kelasDkDal.Setup(x => x.ListData())
                .Returns(expected);

            //  ACT
            var act = await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().BeEquivalentTo(expected.Select(x => new KelasDkListResponse(x.KelasDkId, x.KelasDkName)));
        }
    }
}
