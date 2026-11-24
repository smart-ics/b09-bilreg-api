using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class VoidTransaksiKasirValidator : AbstractValidator<VoidTransaksiKasirCommand>
{
    public VoidTransaksiKasirValidator()
    {
        RuleFor(x => x.TrsKasirId)
            .NotEmpty().WithMessage("TrsKasirId wajib diisi.");

        RuleFor(x => x.SupervisorUserId)
            .NotEmpty().WithMessage("Supervisor UserId wajib diisi.");

        RuleFor(x => x.SupervisorPassword)
            .NotEmpty().WithMessage("Supervisor Password wajib diisi.");

        RuleFor(x => x.AlasanVoid)
            .NotEmpty().WithMessage("Alasan void wajib diisi.")
            .MinimumLength(10).WithMessage("Alasan void minimal 10 karakter.");
    }
}
