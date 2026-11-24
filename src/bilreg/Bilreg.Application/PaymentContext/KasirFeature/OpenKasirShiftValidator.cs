using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class OpenKasirShiftValidator : AbstractValidator<OpenKasirShiftCommand>
{
    public OpenKasirShiftValidator()
    {
        RuleFor(x => x.KodeShift)
            .NotEmpty().WithMessage("Kode shift wajib diisi.")
            .Must(k => k == "1" || k == "2" || k == "3").WithMessage("Kode shift harus bernilai '1', '2', atau '3'.");

        RuleFor(x => x.KasirId)
            .NotEmpty().WithMessage("KasirId wajib diisi.");

        RuleFor(x => x.SaldoAwal)
            .GreaterThanOrEqualTo(0).WithMessage("Saldo awal kasir tidak boleh negatif.");
    }
}
