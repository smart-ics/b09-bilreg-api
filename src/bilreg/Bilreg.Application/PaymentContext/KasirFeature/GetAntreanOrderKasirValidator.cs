using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class GetAntreanOrderKasirValidator : AbstractValidator<GetAntreanOrderKasirQuery>
{
    public GetAntreanOrderKasirValidator()
    {
        RuleFor(x => x.Keyword)
            .MaximumLength(100).WithMessage("Kata kunci pencarian maksimal 100 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Keyword));
    }
}
