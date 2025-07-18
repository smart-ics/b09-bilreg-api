// using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
// using FluentAssertions;
// using MediatR;
// using Moq;
// using Xunit;
//
// namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;
//
// public record KotaListQuery(): IRequest<IEnumerable<KotaListResponse>>;
//
// public record KotaListResponse(string KotaId, string KotaName);
//
// public class KotaListHandler: IRequestHandler<KotaListQuery, IEnumerable<KotaListResponse>>
// {
//     private readonly IKotaDal _kotaDal;
//
//     public KotaListHandler(IKotaDal kotaDal)
//     {
//         _kotaDal = kotaDal;
//     }
//
//     public Task<IEnumerable<KotaListResponse>> Handle(KotaListQuery request, CancellationToken cancellationToken)
//     {
//         // QUERY
//         var result = _kotaDal.ListData()
//             ?? throw new KeyNotFoundException("Kota not found");
//
//         // RESPONSE
//         var response = result.Select(x => new KotaListResponse(x.KotaId, x.KotaName));
//         return Task.FromResult(response);
//     }
// }