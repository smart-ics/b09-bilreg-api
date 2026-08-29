using Bilreg.Domain.SalesContext.ReturJualFeature;
using MediatR;

namespace Bilreg.Application.SalesContext.ReturJualFeature.UseCases;

public record ReturJualGetQuery(string ReturJualId)
    : IRequest<ReturJualGetResponse>, IReturJualKey;

public record ReturJualGetResponse();