using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class GetBillingUnpaidByRegValidator : AbstractValidator<GetBillingUnpaidByRegQuery>
{
    public GetBillingUnpaidByRegValidator()
    {
        RuleFor(x => x.RegId)
            .NotEmpty().WithMessage("RegId wajib diisi.")
            .MaximumLength(10).WithMessage("RegId maksimal 10 karakter.");
    }
}
