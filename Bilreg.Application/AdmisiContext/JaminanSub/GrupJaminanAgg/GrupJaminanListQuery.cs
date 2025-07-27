// using MediatR;
//
// namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;
//
// public record GrupJaminanListQuery(): IRequest<IEnumerable<GrupJaminanListResponse>>;
//
// public record GrupJaminanListResponse(
//     string GrupJaminanId,
//     string GrupJaminanName,
//     bool IsKaryawan,
//     string Keterangan
//     );
//
// public class GrupJaminanListHandler: IRequestHandler<GrupJaminanListQuery, IEnumerable<GrupJaminanListResponse>>
// {
//     private readonly IGrupJaminanDal _grupJaminanDal;
//
//     public GrupJaminanListHandler(IGrupJaminanDal grupJaminanDal)
//     {
//         _grupJaminanDal = grupJaminanDal;
//     }
//
//     public Task<IEnumerable<GrupJaminanListResponse>> Handle(GrupJaminanListQuery request, CancellationToken cancellationToken)
//     {
//         var result = _grupJaminanDal
//             .ListData2()
//             .Value;
//         
//         var response = result.Select(x =>
//             new GrupJaminanListResponse(
//                 x.GrupJaminanId, x.GrupJaminanName, 
//                 x.IsKaryawan, x.Keterangan));
//         return Task.FromResult(response);
//     }
// }