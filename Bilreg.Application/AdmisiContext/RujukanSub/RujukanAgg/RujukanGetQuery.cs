using Bilreg.Domain.AdmisiContext.RujukanSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public record RujukanGetQuery(string RujukanId) : IRequest<RujukanGetResponse>, IRujukanKey;
public record RujukanGetResponse(
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
public class RujukanGetHandler : IRequestHandler<RujukanGetQuery, RujukanGetResponse>
{
    private readonly IRujukanDal _rujukanDal;

    public RujukanGetHandler(IRujukanDal rujukanDal)
    {
        _rujukanDal = rujukanDal;
    }
    public Task<RujukanGetResponse> Handle(RujukanGetQuery request, CancellationToken cancellationToken)
        => _rujukanDal.GetData(RujukanType.Key(request.RujukanId))
        .Match(
            onSome: x => Task.FromResult(new RujukanGetResponse(x.RujukanId, x.RujukanName, x.IsAktif,
                x.Alamat, x.Alamat.Kota, x.TipeRujukan.TipeRujukanId,
                x.TipeRujukan.TipeRujukanName, x.KelasRujukan.KelasRujukanId, x.KelasRujukan.KelasRujukanName,
                x.CaraMasukDk.CaraMasukDkId, x.CaraMasukDk.CaraMasukDkName)),
            onNone: () => throw new KeyNotFoundException($"Rujukan {request.RujukanId} not found"));
    
}

// public class RujukanGetHandlerTest
// {
//     private readonly Mock<IRujukanDal> _rujukanDal;
//     private readonly RujukanGetHandler _sut;
//
//     public RujukanGetHandlerTest()
//     {
//         _rujukanDal = new Mock<IRujukanDal>();
//         _sut = new RujukanGetHandler(_rujukanDal.Object);
//     }
//
//     [Fact]
//     public async Task GivenInvalidRujukanId_ThenThrowKeyNotFoundException_Test()
//     {
//         var request = new RujukanGetQuery("A");
//         _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
//             .Returns(null as RujukanModel);
//
//         var actual = async () => await _sut.Handle(request, CancellationToken.None);
//
//         await actual.Should().ThrowAsync<KeyNotFoundException>();
//     }
// }
//
