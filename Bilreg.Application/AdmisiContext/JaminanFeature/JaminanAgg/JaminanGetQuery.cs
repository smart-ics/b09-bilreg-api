// using Ardalis.GuardClauses;
// using Bilreg.Domain.AdmisiContext.JaminanFeature;
// using Bilreg.Domain.PasienContext.PasienFeature;
// using MediatR;
//
// namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
//
// public record JaminanGetQuery(string JaminanId) : IRequest<JaminanGetResponse>, IJaminanKey;
//
// public record JaminanGetResponse(
//     string JaminanId,
//     string JaminanName,
//     AlamatType Address,
//     bool IsAktif,
//     CaraBayarDkType CaraBayarDk,
//     GroupJaminanReff GrupJaminan,
//     IEnumerable<JaminanTipeTarifType> ListTipeTarif);
//
// public class JaminanGetHandler : IRequestHandler<JaminanGetQuery, JaminanGetResponse>
// {
//     private readonly IJaminanRepo _repo;
//
//     public JaminanGetHandler(IJaminanRepo repo)
//     {
//         _repo = repo;
//     }
//
//     public Task<JaminanGetResponse> Handle(JaminanGetQuery request, CancellationToken cancellationToken)
//     { 
//         Guard.Against.NullOrWhiteSpace(request.JaminanId, nameof(request.JaminanId));
//
//         var jaminan = _repo.LoadEntity(request)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"Jaminan {request.JaminanId} not found")
//                 );
//         var result = new JaminanGetResponse(
//             jaminan.JaminanId, jaminan.JaminanName, jaminan.Alamat, jaminan.IsAktif, 
//             jaminan.CaraBayarDk, jaminan.GroupJaminan, jaminan.ListTipeTarif);
//         return Task.FromResult( result );
//     }
// }