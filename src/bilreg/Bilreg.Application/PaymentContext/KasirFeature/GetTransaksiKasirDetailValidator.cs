using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class GetTransaksiKasirDetailValidator : AbstractValidator<GetTransaksiKasirDetailQuery>
{
    public GetTransaksiKasirDetailValidator()
    {
        RuleFor(x => x.TrsKasirId)
            .NotEmpty().WithMessage("TrsKasirId wajib diisi.");
    }
}
