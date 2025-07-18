// using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
// using FluentAssertions;
// using MediatR;
// using Moq;
// using Xunit;
//
// namespace Bilreg.Application.PasienContext.StatusSosialSub.PendidikanDkAgg;
//
// public record PendidikanDkGetQuery(string PendidikanDkId) : IRequest<PendidikanDkGetResponse>, IPendidikanDkKey;
//
// public record PendidikanDkGetResponse(string PendidikanDkId, string PendidikanDkName);
//
// public class PendidikanDkGetHandler: IRequestHandler<PendidikanDkGetQuery, PendidikanDkGetResponse>
// {
//     private readonly IPendidikanDkDal _pendidikanDkDal;
//
//     public PendidikanDkGetHandler(IPendidikanDkDal pendidikanDkDal)
//     {
//         _pendidikanDkDal = pendidikanDkDal;
//     }
//
//     public Task<PendidikanDkGetResponse> Handle(PendidikanDkGetQuery request, CancellationToken cancellationToken)
//     {
//         // QUERY
//         var result = _pendidikanDkDal
//             .GetData2(request)
//             .OrThrowNotFoundException()
//             .Value;
//         
//         // RESPONSE
//         var response = new PendidikanDkGetResponse(result.PendidikanDkId, result.PendidikanDkName);
//         return Task.FromResult(response);
//     }
// }