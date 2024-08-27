using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using FluentAssertions;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public record InstalasiDkListQuery() : IRequest<IEnumerable<InstalasiDkListResponse>>;
    public record InstalasiDkListResponse(string InstalasiDkId, string InstalasiDkName);
    public class InstalasiDkListHandler : IRequestHandler<InstalasiDkListQuery, IEnumerable<InstalasiDkListResponse>>
    {
        private readonly IInstalasiDkDal _instalasiDkDal;

        public InstalasiDkListHandler(IInstalasiDkDal instalasiDkDal)
        {
            _instalasiDkDal = instalasiDkDal;
        }

        public Task<IEnumerable<InstalasiDkListResponse>> Handle(InstalasiDkListQuery request, CancellationToken cancellationToken)
        {
            //  QUERY
            var result = _instalasiDkDal.ListData()
                ?? throw new KeyNotFoundException("InstalasiDk not found");

            //  RESPONSE
            var response = result.Select(x => new InstalasiDkListResponse(x.InstalasiDkId, x.InstalasiDkName));
            return Task.FromResult(response);
        }
    }

    public class InstalasiDkListHandlerTest
    {
        private readonly InstalasiDkListHandler _sut;
        private readonly Mock<IInstalasiDkDal> _instalasiDkDal;

        public InstalasiDkListHandlerTest()
        {
            _instalasiDkDal = new Mock<IInstalasiDkDal>();
            _sut = new InstalasiDkListHandler(_instalasiDkDal.Object);
        }

        [Fact]
        public void GivenNoData_ThenThrowKeyNotFoundException()
        {
            //  ARRANGE
            var request = new InstalasiDkListQuery();
            _instalasiDkDal.Setup(x => x.ListData())
                .Returns(null as IEnumerable<InstalasiDkModel>);

            //  ACT
            Func<Task> act = () => _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GivenValidRequest_ThenReturnExpected()
        {
            //  ARRANGE
            var expected = new List<InstalasiDkModel> { InstalasiDkModel.Create("A", "B") };
            var request = new InstalasiDkListQuery();
            _instalasiDkDal.Setup(x => x.ListData())
                .Returns(expected);

            //  ACT
            var act = await _sut.Handle(request, CancellationToken.None);

            //  ASSERT
            act.Should().BeEquivalentTo(expected.Select(x => new InstalasiDkListResponse(x.InstalasiDkId, x.InstalasiDkName)));
        }
    }
}
