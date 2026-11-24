using FluentValidation;

namespace Bilreg.Application.PaymentContext.KasirFeature;

public class GetMasterJenisPaymentValidator : AbstractValidator<GetMasterJenisPaymentQuery>
{
    public GetMasterJenisPaymentValidator()
    {
        RuleFor(x => x.Kategori)
            .MaximumLength(50).WithMessage("Kategori maksimal 50 karakter.")
            .When(x => !string.IsNullOrEmpty(x.Kategori));
    }
}
