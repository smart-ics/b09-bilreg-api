using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class GetKasirShiftActiveValidator : AbstractValidator<GetKasirShiftActiveQuery>
{
    public GetKasirShiftActiveValidator()
    {
        RuleFor(x => x.KasirId)
            .NotEmpty().WithMessage("KasirId wajib diisi.");
    }
}
