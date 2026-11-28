using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkPreOpClearedEvent(
    PreOpClearanceModel Clearance,
    DateTime OccurredAt,
    PasienReff? Pasien = null,
    DateTime? TglOp = null) : INotification;
