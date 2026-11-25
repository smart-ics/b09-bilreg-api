using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class CloseKasirShiftValidator : AbstractValidator<CloseKasirShiftCommand>
{
    public CloseKasirShiftValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("ShiftId wajib diisi.");

        RuleFor(x => x.KasirId)
            .NotEmpty().WithMessage("KasirId wajib diisi.");

        RuleFor(x => x.SaldoAkhirFisik)
            .GreaterThanOrEqualTo(0).WithMessage("Saldo akhir fisik tidak boleh negatif.");
    }
}
