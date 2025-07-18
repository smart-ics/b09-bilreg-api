// using Bilreg.Domain.PasienContext.StatusSosialSub.AgamaAgg;
// using FluentAssertions;
// using MediatR;
// using Moq;
// using Nuna.Lib.ValidationHelper;
// using Xunit;
//
// namespace Bilreg.Application.PasienContext.StatusSosialSub.AgamaAgg;
//
// public record AgamaGetQuery(string AgamaId) : IRequest<AgamaGetResponse>, IAgamaKey;
//
// public record AgamaGetResponse(string AgamaId, string AgamaName);
//
// public class AgamaGetHandler : IRequestHandler<AgamaGetQuery, AgamaGetResponse>
// {
//     private readonly IAgamaDal _agamaDal;
//
//     public AgamaGetHandler(IAgamaDal agamaDal)
//     {
//         _agamaDal = agamaDal;
//     }
//
//     public Task<AgamaGetResponse> Handle(AgamaGetQuery request, CancellationToken cancellationToken)
//     {
//         //  QUERY
//         var result = _agamaDal
//             .GetData2(request)
//             .OrThrowNotFoundException()
//             .Value;
//
//         //  RESPONSE
//         var response = new AgamaGetResponse(result.AgamaId, result.AgamaName);
//         return Task.FromResult(response);
//     }
// }