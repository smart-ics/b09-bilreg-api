// using Bilreg.Domain.PasienContext.StatusSosialSub.StatusKawinDkAgg;
// using FluentAssertions;
// using MediatR;
// using Moq;
// using Xunit;
//
// namespace Bilreg.Application.PasienContext.StatusSosialSub.StatusKawinDkAgg;
//
// public record StatusKawinDkListQuery() : IRequest<IEnumerable<StatusKawinDkListResponse>>;
//
// public record StatusKawinDkListResponse(string StatusKawinDkId, string StatusKawinDkName);
//
// public class StatusKawinDkListHandler : IRequestHandler<StatusKawinDkListQuery, IEnumerable<StatusKawinDkListResponse>>
// {
//     private readonly IStatusKawinDkDal _statuskawinDkDal;
//
//     public StatusKawinDkListHandler(IStatusKawinDkDal statuskawinDkDal)
//     {
//         _statuskawinDkDal = statuskawinDkDal;
//     }
//
//     public Task<IEnumerable<StatusKawinDkListResponse>> Handle(StatusKawinDkListQuery request, CancellationToken cancellationToken)
//     {
//         //  QUERY
//         var result = _statuskawinDkDal
//             .ListData2()
//             .Value;
//
//         //  RESPONSE
//         var response = result.Select(x => new StatusKawinDkListResponse(x.StatusKawinDkId, x.StatusKawinDkName));
//         return Task.FromResult(response);
//     }
// }