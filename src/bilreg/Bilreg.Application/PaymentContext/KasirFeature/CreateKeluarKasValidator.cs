using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class CreateKeluarKasValidator : AbstractValidator<CreateKeluarKasCommand>
{
    public CreateKeluarKasValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("ShiftId wajib diisi.");

        RuleFor(x => x.KasirId)
            .NotEmpty().WithMessage("KasirId wajib diisi.");

        RuleFor(x => x.TotalNilai)
            .GreaterThan(0).WithMessage("Total nilai pengeluaran kas harus lebih besar dari nol.");

        RuleFor(x => x.Payments)
            .NotNull().WithMessage("Rincian metode pembayaran wajib diisi minimal satu.")
            .Must(p => p != null && p.Any()).WithMessage("Rincian metode pembayaran wajib diisi minimal satu.");

        RuleForEach(x => x.Payments).ChildRules(item =>
        {
            item.RuleFor(p => p.MetodeBayar)
                .NotEmpty().WithMessage("Metode bayar wajib diisi.");

            item.RuleFor(p => p.NilaiBayar)
                .GreaterThan(0).WithMessage("Nilai bayar harus lebih besar dari nol.");
        });
    }
}
