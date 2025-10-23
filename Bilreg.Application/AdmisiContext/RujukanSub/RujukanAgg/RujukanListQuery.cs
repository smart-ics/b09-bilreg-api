using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public record RujukanListQuery() : IRequest<IEnumerable<RujukanListResponse>>;
public record RujukanListResponse(
    string RujukanId,
    string RujukanName,
    bool IsAktif,
    AlamatType Alamat,
    string Telepon,
    string RujukanTipeId,
    string RujukanTipeName,
    string KelasId,
    string KelasName,
    string CaraMasukDkId,
    string CaraMasukDkName
);

public class RujukanListHandler : IRequestHandler<RujukanListQuery, IEnumerable<RujukanListResponse>>
{
    private readonly IRujukanDal _rujukanDal;

    public RujukanListHandler(IRujukanDal rujukanDal)
    {
        _rujukanDal = rujukanDal;
    }

    public Task<IEnumerable<RujukanListResponse>> Handle(RujukanListQuery request, CancellationToken cancellationToken)
        => _rujukanDal.ListData()
        .Match(
            onSome: x => Task.FromResult(x.Select(y
                => new RujukanListResponse(y.RujukanId, y.RujukanName, y.IsAktif,
                y.Alamat, y.Alamat.Kota, y.TipeRujukan.TipeRujukanId,
                y.TipeRujukan.TipeRujukanName, y.KelasRujukan.KelasRujukanId, y.KelasRujukan.KelasRujukanName,
                y.CaraMasukDk.CaraMasukDkId, y.CaraMasukDk.CaraMasukDkName))),
            onNone: () => throw new KeyNotFoundException("data not found"));
    
}

// public class RujukanListHandlerTest
// {
//     private readonly Mock<IRujukanDal> _rujukanDal;
//     private readonly RujukanListHandler _sut;
//
//     public RujukanListHandlerTest()
//     {
//         _rujukanDal = new Mock<IRujukanDal>();
//         _sut = new RujukanListHandler(_rujukanDal.Object);
//     }
//
//     [Fact]
//     public async Task GivenNoData_ThenThrowKeyNotFoundException_Test()
//     {
//         var request = new RujukanListQuery();
//         _rujukanDal.Setup(x => x.ListData())
//             .Returns(null as IEnumerable<RujukanModel>);
//         var actual = async () => await _sut.Handle(request, CancellationToken.None);
//         await actual.Should().ThrowAsync<KeyNotFoundException>();
//     }
// }
