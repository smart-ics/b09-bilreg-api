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
    public record KelasListQuery() : IRequest<IEnumerable<KelasListResponse>>;
    public record KelasListResponse(string KelasId, string KelasName, string isAktif, string kelasDkId);
    public class KelasListHandler : IRequestHandler<KelasListQuery, IEnumerable<KelasListResponse>>
    {
        private readonly IKelasDal _kelasDal;
        public KelasListHandler(IKelasDal kelasDal)
        {
            _kelasDal = kelasDal;
        }
        public Task<IEnumerable<KelasListResponse>> Handle(KelasListQuery request, CancellationToken cancellationToken)
        {
            // Query
            var result = _kelasDal.ListData()
                ?? throw new KeyNotFoundException("Kelas not Found");
            // Response
            var response = result.Select(x => new KelasListResponse(x.KelasId, x.KelasName,x.IsAktif,x.KelasDkId));
            return Task.FromResult(response);
        }
    }

    public class KelasListHandlerTest
    {
        private readonly KelasListHandler _sut;
        private readonly Mock<IKelasDal> _kelasDal;

        public KelasListHandlerTest()
        {
            _kelasDal = new Mock<IKelasDal>();
            _sut = new KelasListHandler(_kelasDal.Object);
        }

        [Fact]
        public void GivenNoData_ThenThrowKeyNotFoundException()
        {
            //  ARRANGE
            var request = new KelasListQuery();
            _kelasDal.Setup(x => x.ListData())
                .Returns(null as IEnumerable<KelasModel>);

            //  ACT
            Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenReturnExpected()
        {
            //  ARRANGE
            var expected = new List<KelasModel> { KelasModel.Create("A", "B","C","D") };
            var request = new KelasListQuery();
            _kelasDal.Setup(x => x.ListData())
                .Returns(expected);

            //  ACT
            var act = await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().BeEquivalentTo(expected.Select(x => new KelasListResponse(x.KelasId, x.KelasName, x.IsAktif, x.KelasDkId)));
        }
    }
}
