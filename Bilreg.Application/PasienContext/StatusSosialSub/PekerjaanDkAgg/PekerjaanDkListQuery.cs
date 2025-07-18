// using Bilreg.Domain.PasienContext.StatusSosialSub.PekerjaanDkAgg;
// using FluentAssertions;
// using MediatR;
// using Moq;
// using Xunit;
//
// namespace Bilreg.Application.PasienContext.StatusSosialSub.PekerjaanDkAgg;
//
// public record PekerjaanDkListQuery() : IRequest<IEnumerable<PekerjaanDkListResponse>>;
//
// public record PekerjaanDkListResponse(string PekerjaanDkId, string PekerjaanDkName);
//
// public class PekerjaanDkListHandler : IRequestHandler<PekerjaanDkListQuery, IEnumerable<PekerjaanDkListResponse>>
// {
//     private readonly IPekerjaanDkDal _pekerjaanDkDal;
//
//     public PekerjaanDkListHandler(IPekerjaanDkDal pekerjaanDal)
//     {
//         _pekerjaanDkDal = pekerjaanDal;
//     }
//
//     public Task<IEnumerable<PekerjaanDkListResponse>> Handle(PekerjaanDkListQuery request, CancellationToken cancellationToken)
//     {
//         //  QUERY
//         var result = _pekerjaanDkDal
//             .ListData2()
//             .Value;
//
//         //  RESPONSE
//         var response = result.Select(x => new PekerjaanDkListResponse(x.PekerjaanDkId, x.PekerjaanDkName));
//         return Task.FromResult(response);
//     }
//
// }
