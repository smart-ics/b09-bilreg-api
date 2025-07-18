// using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
// using FluentAssertions;
// using MediatR;
// using Moq;
// using Xunit;
//
// namespace Bilreg.Application.PasienContext.StatusSosialSub.PendidikanDkAgg;
//
// public record PendidikanDkListQuery(): IRequest<IEnumerable<PendidikanDkListResponse>>;
//
// public record PendidikanDkListResponse(string PendidikanDkId, string PendidikanDkName);
//
// public class PendidikanDkListHandler : IRequestHandler<PendidikanDkListQuery, IEnumerable<PendidikanDkListResponse>>
// {
//     private readonly IPendidikanDkDal _pendidikanDkDal;
//
//     public PendidikanDkListHandler(IPendidikanDkDal pendidikanDkDal)
//     {
//         _pendidikanDkDal = pendidikanDkDal;
//     }
//
//     public Task<IEnumerable<PendidikanDkListResponse>> Handle(PendidikanDkListQuery request, CancellationToken cancellationToken)
//     {
//         // QUERY
//         var result = _pendidikanDkDal
//             .ListData2()
//             .Value;
//
//         // RESPONSE
//         var response = result.Select(x => new PendidikanDkListResponse(x.PendidikanDkId, x.PendidikanDkName));
//         return Task.FromResult(response);
//
//     }
// }