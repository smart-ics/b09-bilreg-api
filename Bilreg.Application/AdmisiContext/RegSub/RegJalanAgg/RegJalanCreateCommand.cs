using Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegSub.RegJalanAgg;

public record RegJalanCreateCommand(string PasienId,
    string LayananId, DateTime RegDate) : IRequest<RegJalanModel>;
