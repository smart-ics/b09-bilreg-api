using Bilreg.Domain.AdmPasienContext.StatusKawinAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.StatusKawinAgg
{
    public record StatusKawinListQuery() : IRequest<IEnumerable<StatusKawinListResponse>>;

    public record StatusKawinListResponse(string StatusKawinId, string StatusKawinName);

    public class StatusKawinListHandler : IRequestHandler<StatusKawinListQuery, IEnumerable<StatusKawinListResponse>>
    {
        private readonly IStatusKawinDal _statuskawinDal;

        public StatusKawinListHandler(IStatusKawinDal statuskawinDal)
        {
            _statuskawinDal = statuskawinDal;
        }

        public Task<IEnumerable<StatusKawinListResponse>> Handle(StatusKawinListQuery request, CancellationToken cancellationToken)
        {
            //  QUERY
            var result = _statuskawinDal.ListData()
                ?? throw new KeyNotFoundException($"StatusKawin not found");

            //  RESPONSE
            var response = result.Select(x => new StatusKawinListResponse(x.StatusKawinId, x.StatusKawinName));
            return Task.FromResult(response);
        }

    }

    public class StatusKawinListHandlerTest
    {
        private readonly StatusKawinListHandler _skut;
        private readonly Mock<IStatusKawinDal> _statuskawinDal;

        public StatusKawinListHandlerTest()
        {
            _statuskawinDal = new Mock<IStatusKawinDal>();
            _skut = new StatusKawinListHandler(_statuskawinDal.Object);
        }

        [Fact]
        public void GivenNoData_ThenThrowKeyNotFoundException()
        {
            //  ARRANGE
            var request = new StatusKawinListQuery();
            _statuskawinDal.Setup(x => x.ListData())
                .Returns(null as IEnumerable<StatusKawinModel>);

            //  ACT
            Func<Task> act = () => _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenReturnExpected()
        {
            //  ARRANGE
            var expected = new List<StatusKawinModel> { StatusKawinModel.Create("A", "B") };
            var request = new StatusKawinListQuery();
            _statuskawinDal.Setup(x => x.ListData())
                .Returns(expected);

            //  ACT
            var act = await _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().BeEquivalentTo(expected);
        }
    }
}
