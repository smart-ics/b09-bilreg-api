using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace Bilreg.Application.PasienContext.StatusSosialSub.StatusKawinDkAgg
{
    public record StatusKawinDkListQuery() : IRequest<IEnumerable<StatusKawinDkListResponse>>;

    public record StatusKawinDkListResponse(string StatusKawinDkId, string StatusKawinDkName);

    public class StatusKawinDkListHandler : IRequestHandler<StatusKawinDkListQuery, IEnumerable<StatusKawinDkListResponse>>
    {
        private readonly IStatusKawinDkDal _statuskawinDkDal;

        public StatusKawinDkListHandler(IStatusKawinDkDal statuskawinDkDal)
        {
            _statuskawinDkDal = statuskawinDkDal;
        }

        public Task<IEnumerable<StatusKawinDkListResponse>> Handle(StatusKawinDkListQuery request, CancellationToken cancellationToken)
        {
            //  QUERY
            var result = _statuskawinDkDal.ListData()
                ?? throw new KeyNotFoundException($"StatusKawinDk not found");

            //  RESPONSE
            var response = result.Select(x => new StatusKawinDkListResponse(x.StatusKawinDkId, x.StatusKawinDkName));
            return Task.FromResult(response);
        }

    }

    public class StatusKawinDkListHandlerTest
    {
        private readonly StatusKawinDkListHandler _skut;
        private readonly Mock<IStatusKawinDkDal> _statuskawinDkDal;

        public StatusKawinDkListHandlerTest()
        {
            _statuskawinDkDal = new Mock<IStatusKawinDkDal>();
            _skut = new StatusKawinDkListHandler(_statuskawinDkDal.Object);
        }

        [Fact]
        public void GivenNoData_ThenThrowKeyNotFoundException()
        {
            //  ARRANGE
            var request = new StatusKawinDkListQuery();
            _statuskawinDkDal.Setup(x => x.ListData())
                .Returns(null as IEnumerable<StatusKawinDkModel>);

            //  ACT
            Func<Task> act = () => _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenReturnExpected()
        {
            //  ARRANGE
            var expected = new List<StatusKawinDkModel> { StatusKawinDkModel.Create("A", "B") };
            var request = new StatusKawinDkListQuery();
            _statuskawinDkDal.Setup(x => x.ListData())
                .Returns(expected);

            //  ACT
            var act = await _skut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().BeEquivalentTo(expected);
        }
    }
}
